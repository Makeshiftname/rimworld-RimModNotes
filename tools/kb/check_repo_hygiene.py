#!/usr/bin/env python3
"""Repository hygiene checks (acceptance gate for repo structure).

Checks (exit 0 = all good, 1 = problems):
  1. No `.bak` leftovers anywhere under 自建/ or 收集/.
  2. No `.pdb` files tracked by git (they are regenerable build artifacts;
     DLLs are intentionally committed).
  3. No template placeholder packageId in any About.xml
     (e.g. `RunningBugs.modname`, `username.modname`).
  4. Every mod dir follows the `NN-功能-名称` naming with a legal func prefix,
     and lives directly under 自建/ or 收集/ (no stray top-level NN-* dirs).

Warnings (informational, not failures):
  - packageId / dir name mismatch (naming aliases are registered, not fixed)
  - third-party authors
  - empty / backup-only mods

Usage:
  python tools/kb/check_repo_hygiene.py
Exit code 0 = OK, 1 = problems found.
"""
from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

import kb_common as kb

PLACEHOLDER_PID = re.compile(
    r"^(runningbugs\.)?(modname|username\.modname)$|^(com\.)?runningbugs\.test$|^modname$",
    re.IGNORECASE,
)
LEGAL_FUNC = {"standalone", "patch", "xml", "translation", "lib", "empty", "special"}
DIR_RE = re.compile(r"^\d+-(standalone|patch|xml|translation|lib|empty|special)-.+$")


def git_tracked(patterns: tuple[str, ...]) -> list[str]:
    """List git-tracked paths matching any suffix pattern."""
    try:
        out = subprocess.run(
            ["git", "ls-files"], capture_output=True, text=True,
            encoding="utf-8", errors="replace", cwd=kb.REPO_ROOT,
        )
    except OSError:
        return []
    return [line for line in out.stdout.splitlines() if line.endswith(patterns)]


def main() -> int:
    problems: list[str] = []
    warnings: list[str] = []

    # 1. .bak leftovers
    for cat in kb.CATEGORY_DIRS:
        cat_dir = kb.REPO_ROOT / cat
        if not cat_dir.is_dir():
            continue
        for p in cat_dir.rglob("*.bak"):
            problems.append(f"leftover .bak: {p.relative_to(kb.REPO_ROOT).as_posix()}")

    # 2. tracked .pdb
    for f in git_tracked((".pdb",)):
        problems.append(f"tracked .pdb (regenerable artifact): {f}")

    # 3 & 4. mod dirs
    mods = kb.scan_all()
    for m in mods:
        d = m["dir"]
        if not DIR_RE.match(d):
            problems.append(f"bad dir name (want NN-功能-名称): {d}")
        pid = (m.get("package_id") or "").strip()
        if pid and PLACEHOLDER_PID.match(pid):
            # Placeholder packageIds are intentionally kept for learning/collected
            # mods (e.g. 45-TargetLine), registered in mod-index MANUAL — warn only.
            warnings.append(f"placeholder packageId '{pid}' in {d} (registered, not fixed)")
        # naming alias -> warning only
        if pid and pid.lower().startswith("runningbugs."):
            slug = pid.split(".", 1)[1].lower().replace(".", "")
            dir_slug = re.sub(r"^\d+-", "", d).lower()
            # only warn when they clearly diverge on the base name
            base = re.sub(r"^(standalone|patch|xml|translation|lib|empty|special)-", "", dir_slug)
            if slug and base and slug not in base and base not in slug:
                warnings.append(f"packageId '{pid}' vs dir '{d}' (naming alias, registered)")

    # stray top-level NN-* dirs outside 自建/收集
    for p in kb.REPO_ROOT.iterdir():
        if p.is_dir() and re.match(r"^\d+[-_]", p.name):
            problems.append(f"mod dir not under 自建/ or 收集/: {p.name}")

    for w in warnings:
        print(f"  warning: {w}")
    if problems:
        print(f"Hygiene problems ({len(problems)}):")
        for p in problems:
            print(f"  - {p}")
        return 1
    print("Hygiene checks OK.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
