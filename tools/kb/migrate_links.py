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


def rewrite_text(text: str, rows: list[dict], md_path: Path | None = None) -> tuple[str, int]:
    """替换正文里的旧目录名（NN-名称）为新路径（大类/NN-功能-名称）。

    保护游戏安装路径（`/Mods/NN-名称` / `\\Mods\\NN-名称`）不被误改；
    对带 `../` 相对前缀的引用按新位置重算相对路径（md 位于新目录结构下）。
    """
    items = sorted(rows, key=lambda r: len(r["old"]), reverse=True)
    count = 0
    # 1) 保护游戏安装路径中的旧目录名（Mods/<NN-名称>）
    ph: dict[str, str] = {}

    def _protect(m: re.Match) -> str:
        k = f"@@MODS_PH{len(ph)}@@"
        ph[k] = m.group(2)          # 只存 old 部分（不含 Mods/ 前缀）
        return m.group(1) + k

    for r in items:
        old = r["old"]
        # 前缀是 Mods/（游戏安装路径）即保护，不要求 old 后有特定分隔符
        #（路径后可能是反引号/中文标点/行尾）
        text = re.sub(
            r"((?:^|[/\\\\])Mods[/\\\\])(" + re.escape(old) + r")",
            _protect, text,
        )
    # 2) 主替换（相对路径重算 / 普通替换）
    for r in items:
        old, new = r["old"], r["new_dir"]
        pat = re.compile(r"((?:\.\./)*)" + re.escape(old))

        def _repl(m: re.Match) -> str:
            nonlocal count
            count += 1
            prefix = m.group(1)
            if prefix and md_path is not None:
                rel = os.path.relpath((REPO_ROOT / new).resolve(), md_path.parent).replace("\\", "/")
                return rel
            return new

        text = pat.sub(_repl, text)
    # 3) 恢复被保护的游戏路径
    for k, v in ph.items():
        text = text.replace(k, v)
    return text, count


def main() -> None:
    dry = "--dry-run" in sys.argv
    text_mode = "--text" in sys.argv
    mapping = build_map()
    rows = build_map()
    total_files, total_links = 0, 0

    if text_mode:
        # 正文旧目录名替换（不处理 mod-index.md，其 MANUAL 段人工维护；
        # 覆盖 AGENTS、docs/knowledge 与所有 mod 内 md）
        targets = []
        agents = REPO_ROOT / "AGENTS.md"
        if agents.exists():
            targets.append(agents)
        targets.extend(sorted(p for p in (REPO_ROOT / "docs" / "knowledge").glob("*.md")
                              if p.name != "mod-index.md"))
        for cat in ("自建", "收集"):
            cat_dir = REPO_ROOT / cat
            if cat_dir.is_dir():
                targets.extend(p for p in cat_dir.rglob("*.md")
                               if not any(part.startswith(".") for part in p.relative_to(REPO_ROOT).parts))
        for path in targets:
            try:
                text = path.read_text(encoding="utf-8", errors="replace")
            except OSError:
                continue
            new_text, count = rewrite_text(text, rows, path)
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
