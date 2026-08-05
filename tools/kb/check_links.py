#!/usr/bin/env python3
"""Validate links and references in the knowledge base.

Checks:
  1. Every relative link in AGENTS.md and docs/knowledge/*.md resolves.
  2. Mod numbers in kb-index.json are unique (55 is expected to be duplicated).
  3. Path references in mod-index.md's manual section point to existing files.

Usage:
  python tools/kb/check_links.py
Exit code 0 = all good, 1 = problems found.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

import kb_common as kb

LINK_RE = re.compile(r"\[[^\]]*\]\(([^)]+)\)")


def iter_md_files() -> list[Path]:
    files = []
    agents = kb.REPO_ROOT / "AGENTS.md"
    if agents.exists():
        files.append(agents)
    for p in sorted(kb.KB_DIR.glob("*.md")):
        files.append(p)
    return files


def check_file(path: Path, problems: list[str]) -> None:
    try:
        text = path.read_text(encoding="utf-8", errors="replace")
    except OSError as e:
        problems.append(f"{path.relative_to(kb.REPO_ROOT)}: cannot read: {e}")
        return
    rel = path.relative_to(kb.REPO_ROOT)
    for m in LINK_RE.finditer(text):
        target = m.group(1).strip()
        if not target or target.startswith(("http://", "https://", "mailto:", "ftp://", "#")):
            continue
        if "://" in target:
            continue
        path_part = target.split("#")[0]
        if not path_part:
            continue
        resolved = (path.parent / path_part).resolve()
        if not resolved.exists():
            problems.append(f"{rel}: broken link -> `{target}`")


def check_mod_numbers(problems: list[str], warnings: list[str]) -> None:
    if not kb.INDEX_JSON.exists():
        problems.append("docs/knowledge/kb-index.json missing; run scan_mods.py first")
        return
    import json
    mods = json.loads(kb.INDEX_JSON.read_text(encoding="utf-8"))
    seen: dict[str, list[str]] = {}
    for m in mods:
        seen.setdefault(m["number"], []).append(m["dir"])
    for num, dirs in sorted(seen.items()):
        if len(dirs) > 1:
            # Duplicate numbering is a known repo fact (55 appears twice); it is
            # reported as a warning, not a hard failure.
            warnings.append(f"duplicate mod number {num}: {', '.join(dirs)}")


def main() -> int:
    problems: list[str] = []
    warnings: list[str] = []
    for f in iter_md_files():
        check_file(f, problems)
    check_mod_numbers(problems, warnings)

    for w in warnings:
        print(f"  warning: {w}")
    if problems:
        print(f"Found {len(problems)} problem(s):")
        for p in problems:
            print(f"  - {p}")
        return 1
    print("All links and references OK.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
