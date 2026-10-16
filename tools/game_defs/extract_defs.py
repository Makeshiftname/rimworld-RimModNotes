#!/usr/bin/env python3
"""从本地 RimWorld 游戏数据抽取 Def 底稿。

把三样东西 join 到一起，产出可供人工蒸馏的 Markdown 底稿：

1. ``Data/<DLC>/Defs/**/*.xml``          —— Def 的结构：defName、父类、字段
2. ``Data/<DLC>/Languages/ChineseSimplified*.tar`` 里的 ``DefInjected/``  —— 官方中���译名与描述
3. DefInjected XML 里的 ``<!-- EN: ... -->`` 注释 —— 英文原文（官方中英对照）

只用标准库（tarfile / xml.etree / pathlib），不依赖第三方包。

用法::

    # 看看有哪些 Def 类型、各有多少实例
    python tools/game_defs/extract_defs.py --list

    # 抽指定类型（游戏目录默认取本机安装位置）
    python tools/game_defs/extract_defs.py --def MemeDef PreceptDef

    # ThingDef 有 1800+ 个，用过滤条件切出关心的子集。
    # 护甲在 ThingDefs_Misc/Apparel_*.xml，武器在 ThingDefs_Misc/Weapons/：
    python tools/game_defs/extract_defs.py --def ThingDef --source apparel
    python tools/game_defs/extract_defs.py --def ThingDef --source misc/weapons

    # 也可以按字段过滤，或限定 DLC
    python tools/game_defs/extract_defs.py --def ThingDef --has apparel --dlc Core Ideology
    python tools/game_defs/extract_defs.py --def PreceptDef --contains cannibal --dlc Ideology

    # 指定游戏目录与输出位置
    python tools/game_defs/extract_defs.py --game "F:/Games/GBL/rimworld" \
        --def HediffDef --out .cache/hediff

底稿不进仓库（``tools/game_defs/.cache/`` 已被 .gitignore 忽略）。
"""

from __future__ import annotations

import argparse
import re
import sys
import tarfile
from collections import Counter, defaultdict
from pathlib import Path
from xml.etree import ElementTree as ET

# DLC 加载顺序。同一 defName 在多个 DLC 出现时以先加载的为准。
DLC_ORDER = ["Core", "Royalty", "Ideology", "Biotech", "Anomaly", "Odyssey"]

DEFAULT_GAME_DIRS = [
    Path(r"F:\Games\GBL\rimworld"),
    Path(r"C:\Program Files (x86)\Steam\steamapps\common\RimWorld"),
    Path(r"C:\Program Files\Steam\steamapps\common\RimWorld"),
]

LANG_RE = re.compile(r"ChineseSimplified.*\.tar$", re.IGNORECASE)
DEF_TAG_RE = re.compile(r"^[A-Z][A-Za-z0-9_]*Def$")


# --------------------------------------------------------------------------
# 载入
# --------------------------------------------------------------------------


def find_game_dir(explicit: str | None) -> Path:
    if explicit:
        p = Path(explicit)
        if not (p / "Data").is_dir():
            sys.exit(f"不是有效的 RimWorld 目录（找不到 Data/）：{p}")
        return p
    for p in DEFAULT_GAME_DIRS:
        if (p / "Data").is_dir():
            return p
    sys.exit(
        "找不到 RimWorld 安装目录，请用 --game 指定。\n"
        "已尝试：" + ", ".join(str(p) for p in DEFAULT_GAME_DIRS)
    )


def strip_ns(tag: str) -> str:
    """去掉 XML 命名空间前缀。"""
    return tag.split("}", 1)[-1] if "}" in tag else tag


def flatten(elem: ET.Element, prefix: str = "") -> dict[str, str]:
    """把 Def 的子节点拍平成 ``字段路径 -> 文本``。

    重复子节点用索引区分（``traits.li.0``、``traits.li.1``），
    这样 ``<li>`` 列表既保序又不丢内容。
    """
    out: dict[str, str] = {}
    seen: Counter[str] = Counter()
    for child in elem:
        if not isinstance(child.tag, str):  # 注释 / PI
            continue
        name = strip_ns(child.tag)
        if prefix:
            name = f"{prefix}.{name}"
        idx = seen[name]
        seen[name] += 1
        key = name if idx == 0 else f"{name}.{idx}"
        # 有子节点就继续下钻，否则收文本
        if len(child):
            out.update(flatten(child, key))
        else:
            text = (child.text or "").strip()
            if text:
                out[key] = text
            else:
                # 可能是 <x /> 空节点，用属性表示
                if child.attrib:
                    out[key] = " ".join(f"{k}={v}" for k, v in sorted(child.attrib.items()))
                else:
                    out[key] = ""
    return out


class Def:
    __slots__ = ("tag", "defname", "parent", "abstract", "fields", "dlc", "source")

    def __init__(self, tag, defname, parent, abstract, fields, dlc, source):
        self.tag = tag
        self.defname = defname
        self.parent = parent
        self.abstract = abstract
        self.fields = fields
        self.dlc = dlc
        self.source = source

    @property
    def label(self) -> str:
        return self.fields.get("label", "")


def load_defs(data_dir: Path) -> dict[str, dict[str, Def]]:
    """扫描全部 DLC 的 Defs，返回 ``{DefType: {defName: Def}}``。"""
    by_type: dict[str, dict[str, Def]] = defaultdict(dict)
    for dlc in DLC_ORDER:
        defs_root = data_dir / dlc / "Defs"
        if not defs_root.is_dir():
            continue
        for xml in sorted(defs_root.rglob("*.xml")):
            try:
                tree = ET.parse(xml)
            except ET.ParseError as exc:
                print(f"  ! 跳过解析失败 {xml}: {exc}", file=sys.stderr)
                continue
            root = tree.getroot()
            for elem in root:
                if not isinstance(elem.tag, str):
                    continue
                tag = strip_ns(elem.tag)
                if not DEF_TAG_RE.match(tag):
                    continue
                fields = flatten(elem)
                # defName / ParentName / abstract 也可能作为属性出现
                for key in ("defName", "ParentName", "abstract", "Name"):
                    if key in elem.attrib and key not in fields:
                        fields[key] = elem.attrib[key]
                defname = fields.get("defName", "").strip()
                if not defname:
                    continue
                rel = xml.relative_to(data_dir)
                rec = Def(
                    tag=tag,
                    defname=defname,
                    parent=fields.get("ParentName", "").strip(),
                    abstract=fields.get("abstract", "").strip().lower() == "true",
                    fields=fields,
                    dlc=dlc,
                    source=str(rel).replace("\\", "/"),
                )
                table = by_type[tag]
                # 先加载的 DLC 优先；同 DLC 内先出现的优先
                table.setdefault(defname, rec)
    return by_type


def load_translations(data_dir: Path) -> dict[tuple[str, str, str], tuple[str, str]]:
    """从各 DLC 的简中 tar 里读 DefInjected。

    返回 ``{(DefType, defName, 字段路径): (中文, 英文原文)}``。
    英文原文来自元素前的 ``<!-- EN: ... -->`` 注释。
    """
    out: dict[tuple[str, str, str], tuple[str, str]] = {}
    for dlc in DLC_ORDER:
        lang_dir = data_dir / dlc / "Languages"
        if not lang_dir.is_dir():
            continue
        for tar_path in sorted(lang_dir.iterdir()):
            if not tar_path.is_file() or not LANG_RE.search(tar_path.name):
                continue
            with tarfile.open(tar_path, "r:*") as tf:
                members = [
                    m
                    for m in tf.getmembers()
                    if m.isfile() and m.name.startswith("DefInjected/") and m.name.endswith(".xml")
                ]
                for m in members:
                    parts = m.name.split("/")
                    if len(parts) < 3:
                        continue
                    deftype = parts[1]
                    fh = tf.extractfile(m)
                    if fh is None:
                        continue
                    raw = fh.read()
                    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
                    try:
                        node = ET.fromstring(raw.decode("utf-8"), parser=parser)
                    except ET.ParseError as exc:
                        print(f"  ! 跳过翻译解析失败 {m.name}: {exc}", file=sys.stderr)
                        continue
                    prev_en = ""
                    for child in node:
                        if not isinstance(child.tag, str):
                            # 注释节点：``<!-- EN: xxx -->`` 紧挨在它标注的元素前面
                            text = (child.text or "").strip()
                            if text.startswith("EN:"):
                                prev_en = text[3:].strip()
                            continue
                        tag = strip_ns(child.tag)
                        if "." not in tag:
                            continue
                        defname, _, fieldpath = tag.partition(".")
                        out.setdefault((deftype, defname, fieldpath), ((child.text or "").strip(), prev_en))
                        prev_en = ""
    return out


# --------------------------------------------------------------------------
# 产出
# --------------------------------------------------------------------------


def inheritance_chain(rec: Def, table: dict[str, Def]) -> list[str]:
    """返回 ``[defName, Parent, GrandParent, ...]``，遇环即停。"""
    chain = [rec.defname]
    seen = {rec.defname}
    cur = rec
    while cur.parent:
        nxt = table.get(cur.parent)
        if nxt is None or nxt.defname in seen:
            chain.append(cur.parent + ("（未在本类型内找到）" if nxt is None else "（成环）"))
            break
        chain.append(nxt.defname)
        seen.add(nxt.defname)
        cur = nxt
    return chain


def md_escape(text: str) -> str:
    return text.replace("|", "\\|").replace("\n", " ").strip()


def sample_values(recs: list[Def], key: str, limit: int = 3) -> str:
    vals = []
    for r in recs:
        v = r.fields.get(key)
        if v and v not in vals:
            vals.append(v)
        if len(vals) >= limit:
            break
    return ", ".join(f"`{md_escape(v)}`" for v in vals) if vals else "—"


def write_overview(
    deftype: str,
    recs: list[Def],
    trans: dict,
    out: Path,
    table: dict[str, Def],
) -> None:
    total = len(recs)
    abstract = sum(1 for r in recs if r.abstract)
    with_parent = sum(1 for r in recs if r.parent)
    dlc_counter = Counter(r.dlc for r in recs)
    translated = sum(1 for r in recs if (deftype, r.defname, "label") in trans)

    # 字段词表。不同值数只精确统计到 40，超出记为下界，避免为少数超大字段吃掉内存。
    LIMIT = 40
    field_counts: Counter[str] = Counter()
    field_values: dict[str, set[str]] = defaultdict(set)
    field_overflow: Counter[str] = Counter()
    for r in recs:
        for k, v in r.fields.items():
            field_counts[k] += 1
            if len(field_values[k]) < LIMIT:
                field_values[k].add(v[:60])
            else:
                field_overflow[k] += 1

    lines = [
        f"# {deftype} — 框架概览",
        "",
        f"- 实例总数：**{total}**（其中 abstract {abstract}，有 ParentName {with_parent}）",
        f"- 有官方中文标签：**{translated} / {total}**"
        + ("" if translated == total else "  ← 差额通常是隐式派生 Def 或未翻译项"),
        "- DLC 分布：" + "，".join(f"{k} {v}" for k, v in sorted(dlc_counter.items())),
        "",
        "## 字段词表",
        "",
        "| 字段路径 | 出现数 | 占比 | 不同值数 | 样例值 |",
        "|---|---|---|---|---|",
    ]
    for key, cnt in field_counts.most_common():
        pct = cnt * 100.0 / total if total else 0
        distinct = len(field_values[key])
        shown = f"≥{distinct + 1}" if field_overflow[key] else str(distinct)
        lines.append(
            f"| `{key}` | {cnt} | {pct:.0f}% | {shown} | {sample_values(recs, key)} |"
        )
    out.write_text("\n".join(lines) + "\n", encoding="utf-8")


def write_instances(
    deftype: str,
    recs: list[Def],
    trans: dict,
    out: Path,
    table: dict[str, Def],
    show_fields: list[str] | None,
) -> None:
    recs_sorted = sorted(recs, key=lambda r: (r.dlc, r.defname))
    lines = [
        f"# {deftype} — 内容清单",
        "",
        f"共 {len(recs_sorted)} 个实例。中文标签与描述取自官方简中 DefInjected。",
        "",
        "| defName | 中文标签 | 继承链 | DLC | 源文件 |",
        "|---|---|---|---|---|",
    ]
    for r in recs_sorted:
        label = trans.get((deftype, r.defname, "label"), ("", ""))[0]
        chain = " → ".join(inheritance_chain(r, table))
        lines.append(
            f"| `{r.defname}` | {md_escape(label) or '—'} | {md_escape(chain)} | {r.dlc} | `{r.source}` |"
        )
    out.write_text("\n".join(lines) + "\n", encoding="utf-8")

    # 详情：中文描述 + 指定字段
    detail = [
        f"# {deftype} — 实例详情",
        "",
        "每条给出官方中文描述（含英文原文对照）与选定字段值。",
        "",
    ]
    for r in recs_sorted:
        label = trans.get((deftype, r.defname, "label"), ("", ""))[0]
        desc_zh = trans.get((deftype, r.defname, "description"), ("", ""))[0]
        desc_en = trans.get((deftype, r.defname, "description"), ("", ""))[1]
        detail.append(f"## `{r.defname}` — {md_escape(label) or '(无中文标签)'}")
        detail.append("")
        detail.append(f"- 继承：`{' → '.join(inheritance_chain(r, table))}`")
        detail.append(f"- DLC：{r.dlc}　源文件：`{r.source}`")
        if desc_zh:
            detail.append(f"- 中文描述：{desc_zh}")
        if desc_en:
            detail.append(f"- 英文原文：{desc_en}")
        for key in show_fields or []:
            v = r.fields.get(key)
            if v:
                detail.append(f"- `{key}` = `{md_escape(v)}`")
        detail.append("")
    out.with_name(out.stem + "-detail.md").write_text("\n".join(detail) + "\n", encoding="utf-8")


# --------------------------------------------------------------------------


def main() -> int:
    ap = argparse.ArgumentParser(description="从本地 RimWorld 游戏数据抽取 Def 底稿")
    ap.add_argument("--game", help="RimWorld 安装目录（含 Data/）")
    ap.add_argument("--def", nargs="+", dest="deftypes", metavar="DefType", help="要抽取的 Def 类型")
    ap.add_argument("--out", help="输出目录，默认为 tools/game_defs/.cache/<DefType>")
    ap.add_argument(
        "--fields",
        nargs="*",
        default=[],
        help="实例详情里额外展开的字段路径（留空则不展开）",
    )
    ap.add_argument("--has", nargs="+", default=[], metavar="FIELD", help="只保留含这些字段的实例")
    ap.add_argument("--contains", nargs="+", default=[], metavar="TEXT", help="只保留字段值含这些文本的实例")
    ap.add_argument("--source", nargs="+", default=[], metavar="TEXT", help="只保留源文件路径含这些文本的实例")
    ap.add_argument("--dlc", nargs="+", default=[], metavar="DLC", help="只保留这些 DLC 的实例")
    ap.add_argument("--list", action="store_true", help="只列出所有 Def 类型及实例数")
    args = ap.parse_args()

    game = find_game_dir(args.game)
    data = game / "Data"
    print(f"游戏目录：{game}")
    print("载入 Defs ...", flush=True)
    by_type = load_defs(data)
    print(f"  共 {sum(len(v) for v in by_type.values())} 个 Def 实例，{len(by_type)} 种类型")

    if args.list:
        print()
        for tag in sorted(by_type, key=lambda t: -len(by_type[t])):
            print(f"  {len(by_type[tag]):6d}  {tag}")
        return 0

    if not args.deftypes:
        ap.error("需要 --def 或 --list")

    print("载入官方简中 DefInjected ...", flush=True)
    trans = load_translations(data)
    print(f"  共 {len(trans)} 条译文")

    root_out = Path(args.out) if args.out else Path(__file__).parent / ".cache"
    for deftype in args.deftypes:
        full_table = by_type.get(deftype, {})
        if not full_table:
            print(f"! 未找到 {deftype}（已知类型见 --list）", file=sys.stderr)
            continue
        recs = list(full_table.values())
        before = len(recs)
        # 过滤：继承链始终用完整表解析，只有展示列表被过滤
        if args.dlc:
            recs = [r for r in recs if r.dlc in args.dlc]
        if args.has:
            recs = [r for r in recs if all(any(k == f or k.startswith(f + ".") for k in r.fields) for f in args.has)]
        if args.contains:
            recs = [
                r
                for r in recs
                if all(
                    any(t.lower() in v.lower() for v in r.fields.values()) for t in args.contains
                )
            ]
        if args.source:
            recs = [r for r in recs if all(t.lower() in r.source.lower() for t in args.source)]
        d = root_out / deftype
        if args.dlc or args.has or args.contains or args.source:
            d = root_out / (deftype + "__filtered")
        d.mkdir(parents=True, exist_ok=True)
        write_overview(deftype, recs, trans, d / "overview.md", full_table)
        write_instances(deftype, recs, trans, d / "instances.md", full_table, args.fields)
        note = f"（共 {before}，过滤后 {len(recs)}）" if len(recs) != before else ""
        print(f"  {deftype}: {len(recs)} 实例{note} -> {d}")

    print("\n底稿已生成。请据此手写文档，不要把底稿直接入库。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
