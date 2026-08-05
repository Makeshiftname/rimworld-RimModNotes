#!/usr/bin/env python3
"""Detect placeholder READMEs and track rewrite progress.

A README is considered a placeholder when it is a template copy ("template"),
nearly empty ("stub"), or missing entirely.

Usage:
  python tools/kb/validate_readme.py --list     # list all mods + README status
  python tools/kb/validate_readme.py --todo     # list mods that still need rewriting
  python tools/kb/validate_readme.py --verify 01-AlertUtility   # check one mod
  python tools/kb/validate_readme.py --progress # show batch rewrite progress
Exit code 0 = ok; 1 = requested verification failed / placeholders remaining for --verify.
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import kb_common as kb

PROGRESS_FILE = Path(__file__).resolve().parent / "kb-progress.json"

NEEDS_REWRITE = {"template", "stub", "missing"}


def load_progress() -> dict:
    if PROGRESS_FILE.exists():
        try:
            return json.loads(PROGRESS_FILE.read_text(encoding="utf-8"))
        except json.JSONDecodeError:
            pass
    return {"batches": [], "done": []}


def save_progress(p: dict) -> None:
    PROGRESS_FILE.write_text(json.dumps(p, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--list", action="store_true", help="list all mods + README status")
    ap.add_argument("--todo", action="store_true", help="list mods needing rewrite")
    ap.add_argument("--verify", metavar="MOD_DIR", help="verify one mod README is not placeholder")
    ap.add_argument("--progress", action="store_true", help="show batch rewrite progress")
    args = ap.parse_args()

    mods = kb.scan_all()
    by_dir = {m["dir"]: m for m in mods}

    if args.verify:
        mod = by_dir.get(args.verify)
        if mod is None:
            print(f"unknown mod dir: {args.verify}", file=sys.stderr)
            return 2
        if mod["readme_status"] in NEEDS_REWRITE:
            print(f"{args.verify}: FAIL ({mod['readme_status']})")
            return 1
        print(f"{args.verify}: OK ({mod['readme_status']})")
        return 0

    if args.todo:
        todo = [m["dir"] for m in mods if m["readme_status"] in NEEDS_REWRITE]
        if not todo:
            print("All READMEs are in good shape.")
            return 0
        print(f"{len(todo)} README(s) need attention:")
        for d in todo:
            print(f"  - {d} ({by_dir[d]['readme_status']})")
        return 0

    if args.progress:
        prog = load_progress()
        done = set(prog["done"])
        total = [m["dir"] for m in mods if m["readme_status"] in NEEDS_REWRITE]
        remaining = [d for d in total if d not in done]
        print(f"batches: {len(prog['batches'])}")
        print(f"done:    {len(done)}")
        print(f"still placeholder: {len(remaining)}")
        for d in remaining:
            print(f"  - {d}")
        return 0

    # default: --list
    for m in mods:
        print(f"{m['readme_status']:<8} {m['dir']}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
