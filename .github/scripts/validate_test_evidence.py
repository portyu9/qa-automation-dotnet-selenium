"""Validate MTP TRX execution and Cobertura coverage evidence."""

from __future__ import annotations

import argparse
from collections import Counter
from pathlib import Path
import xml.etree.ElementTree as ET


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def require_exactly_one(paths: list[Path], label: str) -> Path:
    if len(paths) != 1:
        raise SystemExit(f"expected exactly one {label}, found {len(paths)}: {paths}")
    return paths[0]


def validate_trx(results_dir: Path, minimum_tests: int) -> tuple[int, Counter[str]]:
    trx = require_exactly_one(sorted(results_dir.rglob("*.trx")), "TRX report")
    root = ET.parse(trx).getroot()
    results = [element for element in root.iter() if local_name(element.tag) == "UnitTestResult"]
    outcomes: Counter[str] = Counter(result.attrib.get("outcome", "<missing>") for result in results)
    executed = len(results)

    if executed < minimum_tests:
        raise SystemExit(
            f"TRX execution floor failed: executed={executed}, minimum={minimum_tests}, outcomes={dict(outcomes)}"
        )
    if outcomes != Counter({"Passed": executed}):
        raise SystemExit(f"TRX contains non-passing outcomes: {dict(outcomes)}")

    print(f"TRX contract: {trx} executed={executed} passed={outcomes['Passed']}")
    return executed, outcomes


def validate_coverage(results_dir: Path, minimum_line_rate: float, minimum_branch_rate: float) -> tuple[float, float]:
    report = require_exactly_one(
        sorted(results_dir.rglob("coverage.cobertura.*.xml")),
        "Cobertura report",
    )
    root = ET.parse(report).getroot()

    try:
        line_rate = float(root.attrib["line-rate"])
        branch_rate = float(root.attrib["branch-rate"])
        lines_covered = int(root.attrib["lines-covered"])
        lines_valid = int(root.attrib["lines-valid"])
        branches_covered = int(root.attrib["branches-covered"])
        branches_valid = int(root.attrib["branches-valid"])
    except (KeyError, ValueError) as exc:
        raise SystemExit(f"Cobertura report is missing required numeric metadata: {exc}") from exc

    if lines_valid <= 0 or branches_valid <= 0:
        raise SystemExit(
            f"Cobertura report is structurally empty: lines-valid={lines_valid}, branches-valid={branches_valid}"
        )
    if not 0 <= lines_covered <= lines_valid or not 0 <= branches_covered <= branches_valid:
        raise SystemExit(
            "Cobertura counters are inconsistent: "
            f"lines={lines_covered}/{lines_valid}, branches={branches_covered}/{branches_valid}"
        )

    calculated_line_rate = lines_covered / lines_valid
    calculated_branch_rate = branches_covered / branches_valid
    if abs(calculated_line_rate - line_rate) > 0.0001:
        raise SystemExit(
            f"Cobertura line-rate does not reconcile: declared={line_rate:.4f}, calculated={calculated_line_rate:.4f}"
        )
    if abs(calculated_branch_rate - branch_rate) > 0.0001:
        raise SystemExit(
            f"Cobertura branch-rate does not reconcile: declared={branch_rate:.4f}, calculated={calculated_branch_rate:.4f}"
        )
    if line_rate < minimum_line_rate:
        raise SystemExit(
            f"line coverage regression: {line_rate:.2%} < required {minimum_line_rate:.2%}"
        )
    if branch_rate < minimum_branch_rate:
        raise SystemExit(
            f"branch coverage regression: {branch_rate:.2%} < required {minimum_branch_rate:.2%}"
        )

    print(
        "Cobertura contract: "
        f"{report} line={line_rate:.2%} ({lines_covered}/{lines_valid}) "
        f"branch={branch_rate:.2%} ({branches_covered}/{branches_valid})"
    )
    return line_rate, branch_rate


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("results_dir", type=Path)
    parser.add_argument("--min-tests", type=int, default=28)
    parser.add_argument("--min-line-rate", type=float, default=0.80)
    parser.add_argument("--min-branch-rate", type=float, default=0.55)
    args = parser.parse_args()

    if not args.results_dir.is_dir():
        raise SystemExit(f"results directory is missing: {args.results_dir}")
    if args.min_tests < 1:
        raise SystemExit("--min-tests must be positive")
    for name, value in (
        ("--min-line-rate", args.min_line_rate),
        ("--min-branch-rate", args.min_branch_rate),
    ):
        if not 0 <= value <= 1:
            raise SystemExit(f"{name} must be between 0 and 1")

    validate_trx(args.results_dir, args.min_tests)
    validate_coverage(args.results_dir, args.min_line_rate, args.min_branch_rate)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
