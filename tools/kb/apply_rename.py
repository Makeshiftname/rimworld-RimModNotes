# -*- coding: utf-8 -*-
"""执行「自建/收集 × 功能」目录重构（git mv，保留历史）。

一次性重构工具：读取 gen_rename_map.build_map() 的映射并执行 git mv。
支持 --dry-run 预览（默认提示，需显式传 --apply 才真正执行）。

用法（仓库根）:
    python tools/kb/apply_rename.py --dry-run   # 预览
    python tools/kb/apply_rename.py --apply     # 实际执行
"""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from kb_common import REPO_ROOT  # noqa: E402
from gen_rename_map import build_map  # noqa: E402


def main() -> None:
    apply = "--apply" in sys.argv
    dry = not apply
    rows = build_map()

    # 先确保大类目录存在
    for cat in ("自建", "收集"):
        d = REPO_ROOT / cat
        if not d.exists():
            if dry:
                print(f"[DRY] 将创建大类目录: {cat}/")
            else:
                d.mkdir()
                print(f"[OK] 创建大类目录: {cat}/")

    for r in rows:
        src = REPO_ROOT / r["old"]
        dst = REPO_ROOT / r["new_dir"]
        if not src.exists():
            print(f"[SKIP] 源不存在: {r['old']}")
            continue
        if dst.exists():
            print(f"[SKIP] 目标已存在: {r['new_dir']}")
            continue
        if dry:
            print(f"[DRY] {r['old']}  ->  {r['new_dir']}")
            continue
        subprocess.run(["git", "mv", str(src), str(dst)], cwd=REPO_ROOT, check=True)
        print(f"[OK] {r['old']}  ->  {r['new_dir']}")

    if dry:
        print("\n以上为预览，未做任何改动。确认无误后运行：python tools/kb/apply_rename.py --apply")


if __name__ == "__main__":
    main()
