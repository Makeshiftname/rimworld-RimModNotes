# -*- coding: utf-8 -*-
"""迁移文档中的相对链接到「自建/收集 × 功能」新目录结构。

一次性迁移工具：扫描 AGENTS.md、docs/knowledge/*.md 与所有 mod 目录下的
*.md，把 markdown 链接目标里的旧目录名（NN-名称）替换为新相对路径
（大类/NN-功能-名称）。

原则：
  - 只改链接目标 `](...)`，不触碰正文纯文本编号引用（那些在 MANUAL/AGENTS 人工处理）
  - 按旧目录名长度降序匹配，避免 `55-Common` 命中 `55-CommonModCompatibilityPatches`
  - 每个链接目标只替换一次；绝对/锚点链接跳过

用法（仓库根）:
    python tools/kb/migrate_links.py            # 实际执行
    python tools/kb/migrate_links.py --dry-run  # 预览统计不落盘
"""
from __future__ import annotations

import os
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from kb_common import REPO_ROOT  # noqa: E402
from gen_rename_map import build_map  # noqa: E402

LINK_RE = re.compile(r"(\[[^\]]*\]\()([^)]+)(\))")
SKIP_PREFIX = ("http://", "https://", "mailto:", "ftp://", "#")


def build_mapping() -> list[tuple[str, Path]]:
    """old dirname -> new absolute dir, sorted by old-name length descending."""
    rows = build_map()
    mapping = [(r["old"], (REPO_ROOT / r["new_dir"]).resolve()) for r in rows]
    mapping.sort(key=lambda x: len(x[0]), reverse=True)
    return mapping


def rewrite_target(target: str, md_dir: Path, mapping: list[tuple[str, Path]]) -> str:
    if target.startswith(SKIP_PREFIX):
        return target
    for old, abs_dir in mapping:
        if old in target:
            suffix = target.split(old, 1)[1]
            new_rel = os.path.relpath(abs_dir, md_dir).replace("\\", "/")
            return new_rel + suffix
    return target


def iter_md_files() -> list[Path]:
    files = []
    agents = REPO_ROOT / "AGENTS.md"
    if agents.exists():
        files.append(agents)
    files.extend(sorted((REPO_ROOT / "docs" / "knowledge").glob("*.md")))
    for cat in ("自建", "收集"):
        cat_dir = REPO_ROOT / cat
        if cat_dir.is_dir():
            files.extend(p for p in cat_dir.rglob("*.md")
                         if not any(part.startswith(".") for part in p.relative_to(REPO_ROOT).parts))
    return files


def rewrite_text(text: str, rows: list[dict]) -> tuple[str, int]:
    """替换正文里的旧目录名（NN-名称）为新路径（大类/NN-功能-名称）。"""
    items = sorted(rows, key=lambda r: len(r["old"]), reverse=True)
    count = 0
    for r in items:
        old, new = r["old"], r["new_dir"]
        if old in text:
            count += text.count(old)
            text = text.replace(old, new)
    return text, count


def main() -> None:
    dry = "--dry-run" in sys.argv
    text_mode = "--text" in sys.argv
    mapping = build_map()
    rows = build_map()
    total_files, total_links = 0, 0

    if text_mode:
        # 正文旧目录名替换（不处理 mod-index.md，其 MANUAL 段人工维护）
        targets = []
        agents = REPO_ROOT / "AGENTS.md"
        if agents.exists():
            targets.append(agents)
        targets.extend(sorted(p for p in (REPO_ROOT / "docs" / "knowledge").glob("*.md")
                              if p.name != "mod-index.md"))
        for path in targets:
            try:
                text = path.read_text(encoding="utf-8", errors="replace")
            except OSError:
                continue
            new_text, count = rewrite_text(text, rows)
            if count:
                total_files += 1
                total_links += count
                rel = path.relative_to(REPO_ROOT).as_posix()
                if dry:
                    print(f"[DRY] {rel}: {count} 处旧目录名待替换")
                else:
                    path.write_text(new_text, encoding="utf-8")
                    print(f"[OK]  {rel}: {count} 处已替换")
        print(f"\n总览（正文）：{total_files} 个文件，{total_links} 处替换")
        if dry:
            print("以上为预览。确认后运行：python tools/kb/migrate_links.py --text")
        return

    for path in iter_md_files():
        try:
            text = path.read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue

        counter = {"n": 0}

        def repl_with_count(m: re.Match) -> str:
            new_target = rewrite_target(m.group(2), path.parent, mapping)
            if new_target != m.group(2):
                counter["n"] += 1
            return m.group(1) + new_target + m.group(3)

        new_text, count = LINK_RE.subn(repl_with_count, text)
        if counter["n"]:
            total_files += 1
            total_links += counter["n"]
            rel = path.relative_to(REPO_ROOT).as_posix()
            if dry:
                print(f"[DRY] {rel}: {counter['n']} link(s) to rewrite")
            else:
                path.write_text(new_text, encoding="utf-8")
                print(f"[OK]  {rel}: {counter['n']} link(s) rewritten")
    print(f"\n总览：{total_files} 个文件，{total_links} 处链接")
    if dry:
        print("以上为预览。确认后运行：python tools/kb/migrate_links.py")


if __name__ == "__main__":
    main()
