#!/usr/bin/env python3
"""Scan all RimWorld mods in the repo and generate the knowledge base index.

Outputs:
  docs/knowledge/kb-index.json  - machine-readable metadata for every mod
  docs/knowledge/mod-index.md   - markdown index (auto section regenerated,
                                  manual section preserved)

Usage:
  python tools/kb/scan_mods.py             # (re)generate index
  python tools/kb/scan_mods.py --check     # compare vs last index, report changes
  python tools/kb/scan_mods.py --json-only # only refresh kb-index.json
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import kb_common as kb


def tick(v: bool) -> str:
    return "✓" if v else "–"


def type_label(t: str, conf: str) -> str:
    return f"{t}?" if conf == "low" else t


def build_markdown(mods: list[dict]) -> str:
    stats = {}
    for m in mods:
        stats[m["type"]] = stats.get(m["type"], 0) + 1
    readme_stats = {}
    for m in mods:
        readme_stats[m["readme_status"]] = readme_stats.get(m["readme_status"], 0) + 1

    lines = []
    lines.append("# Mod Index — RimWorld 知识库")
    lines.append("")
    lines.append("> 元数据表由 `tools/kb/scan_mods.py` 自动生成，请勿手改本区间。")
    lines.append("> 「要点与知识点链接」在文件末尾 `<!-- MANUAL -->` 区间内人工维护。")
    lines.append("")
    lines.append("## 统计")
    lines.append("")
    cat_stats = {}
    for m in mods:
        c = m.get("category", "")
        cat_stats[c] = cat_stats.get(c, 0) + 1
    lines.append(
        f"- Mod 总数：**{len(mods)}**（"
        + "、".join(f"`{c}` × {n}" for c, n in sorted(cat_stats.items(), key=lambda x: x[1], reverse=True))
        + "）"
    )
    lines.append(
        "- 类型分布：" + "、".join(f"`{t}` × {c}" for t, c in sorted(stats.items()))
    )
    lines.append(
        "- README 状态：" + "、".join(f"`{k}` × {v}" for k, v in sorted(readme_stats.items()))
    )
    lines.append("")
    lines.append("## 索引表")
    lines.append("")
    lines.append(
        "| # | 类 | Mod | Type | Versions | C# | 翻 | Doc | Tst | Pub | README |"
    )
    lines.append(
        "|---|----|-----|------|----------|----|----|-----|-----|-----|--------|"
    )
    for m in mods:
        vers = ",".join(m["versions"]) or ("backup" if m["in_backup"] else "-")
        lines.append(
            f"| {m['number']} | {m.get('category', '')} | {m['title']} | "
            f"{type_label(m['type'], m['type_conf'])} "
            f"| {vers} | {tick(m['has_csharp'])} | {tick(m['has_languages'])} "
            f"| {tick(m['has_docs'])} | {tick(m['has_tests'])} "
            f"| {tick(m['has_publisher_plus'])} | {m['readme_status']} |"
        )
    lines.append("")
    lines.append("> 图例：C#=有 C# 源码；翻=Languages/ 翻译；Doc=docs/；Tst=Tests/；")
    lines.append("> Pub=`_PublisherPlus.xml`；README=ok/模板占位(template)/空壳(stub)/缺失(missing)。")
    lines.append("> Type 带 `?` 表示启发式推断置信度低（patch 与 standalone 需人工核实）。")
    lines.append("")
    return "\n".join(lines)


def build_json(mods: list[dict]) -> str:
    return json.dumps(mods, ensure_ascii=False, indent=2)


def write_index(mods: list[dict], json_only: bool = False) -> None:
    kb.KB_DIR.mkdir(parents=True, exist_ok=True)

    (kb.KB_DIR / "kb-index.json").write_text(build_json(mods) + "\n", encoding="utf-8")
    if json_only:
        print(f"wrote {kb.INDEX_JSON}")
        return

    auto = f"{kb.AUTO_START}\n{build_markdown(mods)}\n{kb.AUTO_END}"
    manual = ""
    if kb.INDEX_MD.exists():
        existing = kb.INDEX_MD.read_text(encoding="utf-8", errors="replace")
        body = kb.extract_manual(existing)
        if body is not None:
            manual = f"\n{kb.MANUAL_START}\n{body}\n{kb.MANUAL_END}\n"

    header = (
        "# RimWorld Mod 知识库 — Mod 索引\n\n"
        "> 本文件两段式：上方「索引表」由脚本自动维护；下方「要点」为人工维护的"
        "知识点与链接，脚本更新时保留不动。\n\n"
    )
    kb.INDEX_MD.write_text(header + auto + "\n" + manual, encoding="utf-8")
    print(f"wrote {kb.INDEX_MD}")
    print(f"wrote {kb.INDEX_JSON}")


def check_changes(mods: list[dict]) -> int:
    if not kb.INDEX_JSON.exists():
        print("no previous kb-index.json; run without --check first", file=sys.stderr)
        return 2
    prev = json.loads(kb.INDEX_JSON.read_text(encoding="utf-8"))
    prev_by_dir = {m["dir"]: m for m in prev}
    cur_by_dir = {m["dir"]: m for m in mods}

    changes = []
    for d in sorted(set(cur_by_dir) - set(prev_by_dir)):
        changes.append(f"  [added]   {d}")
    for d in sorted(set(prev_by_dir) - set(cur_by_dir)):
        changes.append(f"  [removed] {d}")
    for d in sorted(set(cur_by_dir) & set(prev_by_dir)):
        a, b = prev_by_dir[d], cur_by_dir[d]
        diffs = []
        for field in ("readme_status", "type", "versions", "has_csharp", "has_languages",
                      "has_docs", "has_tests", "has_agents", "has_publisher_plus"):
            if a.get(field) != b.get(field):
                diffs.append(f"{field}:{a.get(field)}->{b.get(field)}")
        if diffs:
            changes.append(f"  [changed] {d}: {'; '.join(diffs)}")

    if changes:
        print("Changes since last index:")
        print("\n".join(changes))
        return 1
    print("No changes since last index.")
    return 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--check", action="store_true",
                    help="compare vs last index and report changes (no rewrite)")
    ap.add_argument("--json-only", action="store_true",
                    help="only refresh kb-index.json, leave mod-index.md untouched")
    args = ap.parse_args()

    mods = kb.scan_all()
    if args.check:
        return check_changes(mods)

    write_index(mods, json_only=args.json_only)

    # warnings for known structural oddities
    warn = []
    seen = {}
    for m in mods:
        # number is unique within a category (自建/收集 each start at 01)
        key = (m.get("category", ""), m["number"])
        seen.setdefault(key, []).append(m["dir"])
        if m["readme_status"] == "missing" and not m["in_backup"]:
            warn.append(f"  no README: {m['dir']}")
        author = (m.get("author") or "").strip()
        if author and "runningbugs" not in author.lower():
            warn.append(f"  third-party author: {m['dir']} ({author})")
    for key, dirs in seen.items():
        if len(dirs) > 1:
            warn.append(f"  duplicate number {key}: {dirs}")
    empties = [m["dir"] for m in mods if m["type"] == "empty"]
    if empties:
        warn.append(f"  empty mod dirs: {empties}")
    backups = [m["dir"] for m in mods if m["in_backup"]]
    if backups:
        warn.append(f"  content only under backup/: {backups}")
    if warn:
        print("Warnings:")
        print("\n".join(warn))
    return 0


if __name__ == "__main__":
    sys.exit(main())
