"""Validate that repository .NET runtime declarations form one qualified contract."""
from __future__ import annotations

import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
GLOBAL_JSON = ROOT / "global.json"
PROJECT = ROOT / "UiTests.csproj"
WORKFLOWS = {
    "ci": ROOT / ".github" / "workflows" / "ci.yml",
    "extended": ROOT / ".github" / "workflows" / "extended.yml",
    "security": ROOT / ".github" / "workflows" / "security.yml",
}
DOCS = ROOT / ".github" / "workflows" / "docs.yml"
SDK_RE = re.compile(r"^\d+\.\d+\.\d+$")
ENV_SDK_RE = re.compile(r"(?m)^\s*DOTNET_SDK_VERSION:\s*([0-9]+\.[0-9]+\.[0-9]+)\s*$")
SETUP_SDK_RE = re.compile(r"(?m)^\s*dotnet-version:\s*([0-9]+\.[0-9]+\.[0-9]+)\s*$")


def fail(message: str, errors: list[str]) -> None:
    errors.append(message)


def load_contract(errors: list[str]) -> tuple[str, int] | None:
    try:
        payload = json.loads(GLOBAL_JSON.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        fail(f"global.json is unreadable: {exc}", errors)
        return None

    sdk = payload.get("sdk")
    test = payload.get("test")
    if not isinstance(sdk, dict):
        fail("global.json must define an sdk object", errors)
        return None

    version = sdk.get("version")
    if not isinstance(version, str) or not SDK_RE.fullmatch(version):
        fail("global.json sdk.version must be an exact numeric SDK version", errors)
        return None
    if sdk.get("rollForward") != "disable":
        fail("global.json must disable SDK roll-forward", errors)
    if sdk.get("allowPrerelease") is not False:
        fail("global.json must reject prerelease SDKs", errors)
    if not isinstance(test, dict) or test.get("runner") != "Microsoft.Testing.Platform":
        fail("global.json must select Microsoft.Testing.Platform", errors)

    major = int(version.split(".", 1)[0])
    return version, major


def validate_target_framework(sdk_major: int, errors: list[str]) -> None:
    try:
        root = ET.parse(PROJECT).getroot()
    except (OSError, ET.ParseError) as exc:
        fail(f"UiTests.csproj is unreadable: {exc}", errors)
        return

    values = [
        (node.text or "").strip()
        for node in root.findall(".//TargetFramework")
        if (node.text or "").strip()
    ]
    expected = f"net{sdk_major}.0"
    if values != [expected]:
        fail(
            f"UiTests.csproj must target exactly {expected!r} for SDK major {sdk_major}; got {values!r}",
            errors,
        )


def validate_workflow(name: str, path: Path, sdk_version: str, errors: list[str]) -> None:
    if not path.is_file():
        fail(f"required workflow is missing: {path.relative_to(ROOT)}", errors)
        return
    text = path.read_text(encoding="utf-8")

    env_versions = ENV_SDK_RE.findall(text)
    if not env_versions:
        fail(f"{name} workflow must declare DOTNET_SDK_VERSION", errors)
    elif set(env_versions) != {sdk_version}:
        fail(
            f"{name} DOTNET_SDK_VERSION declarations must equal global.json {sdk_version}; got {env_versions}",
            errors,
        )

    setup_versions = SETUP_SDK_RE.findall(text)
    if not setup_versions:
        fail(f"{name} workflow must configure actions/setup-dotnet with an exact SDK", errors)
    elif set(setup_versions) != {sdk_version}:
        fail(
            f"{name} setup-dotnet declarations must equal global.json {sdk_version}; got {setup_versions}",
            errors,
        )

    if '[[ "$(dotnet --version)" == "$DOTNET_SDK_VERSION" ]]' not in text:
        fail(f"{name} workflow must assert the installed SDK against DOTNET_SDK_VERSION", errors)


def validate_docs_surface(errors: list[str]) -> None:
    if not DOCS.is_file():
        fail("docs workflow is missing", errors)
        return
    text = DOCS.read_text(encoding="utf-8")
    if "python .github/scripts/validate_runtime_policy.py" not in text:
        fail("docs workflow must execute validate_runtime_policy.py", errors)


def main() -> int:
    errors: list[str] = []
    contract = load_contract(errors)
    if contract is not None:
        sdk_version, sdk_major = contract
        validate_target_framework(sdk_major, errors)
        for name, path in WORKFLOWS.items():
            validate_workflow(name, path, sdk_version, errors)
        validate_docs_surface(errors)

    if errors:
        print(".NET runtime policy contract failed:")
        for error in errors:
            print(f"- {error}")
        return 1

    assert contract is not None
    sdk_version, sdk_major = contract
    print(
        ".NET runtime policy contract passed: "
        f"global.json SDK={sdk_version}, target=net{sdk_major}.0, "
        "CI/Extended/Security setup and runtime assertions are synchronized, MTP selected"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
