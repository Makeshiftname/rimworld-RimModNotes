# -*- coding: utf-8 -*-
"""RimWorld 简体中文翻译错误来源分析（正式版，由 tools/tmp_scan_translations.py 迁移）。

扫描游戏本地数据 + 激活 mod，找出：
  1. 全局缺失 Keyed / DefInjected 翻译数（英文有、中文无）
  2. 各 mod 缺失翻译统计（英文 key 未被中文覆盖）
  3. 各 mod 的确定错误：坏 def 引用、占位符不匹配、命名占位符、XML 解析、重复 key

用法（默认指向 F:\\Games\\GBL 游戏路径）:
    python tools/kb/scan_translations.py [--mods 路径] [--data 路径] [--config 路径]

输出:
    终端统计 + tools/kb/translation_errors.txt（完整确定错误清单）

注意:
    - 该脚本是启发式静态扫描，非游戏内翻译报告（明细以游戏内
      Options→Language 报告为准）
    - 需指向实际游戏安装路径（含 Data/ 与 Mods/）
"""
import argparse
import io
import os
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

DEFAULT_MODS = r"F:\Games\GBL\rimworld\Mods"
DEFAULT_DATA = r"F:\Games\GBL\rimworld\Data"
DEFAULT_CONFIG = r"F:\Games\GBL\Config\ModsConfig.xml"
CN_DIR_NAMES = ("ChineseSimplified", "ChineseSimplified (简体中文)", "简体中文")


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--mods", default=DEFAULT_MODS, help="游戏 Mods 目录")
    ap.add_argument("--data", default=DEFAULT_DATA, help="游戏 Data 目录")
    ap.add_argument("--config", default=DEFAULT_CONFIG, help="ModsConfig.xml 路径")
    args = ap.parse_args()

    MODS_ROOT, DATA_ROOT, CONFIG_XML = args.mods, args.data, args.config

    # ---------- 激活 mod ----------
    active_ids = []
    with open(CONFIG_XML, "r", encoding="utf-8") as f:
        for line in f:
            m = re.search(r"<li>(.*?)</li>", line)
            if m:
                active_ids.append(m.group(1).strip())

    def read_about_attr(about_path, tag):
        try:
            t = ET.parse(about_path)
            n = t.getroot().find(tag)
            return n.text.strip() if n is not None and n.text else ""
        except Exception:
            return ""

    pkg_to_dir = {}
    for d in os.listdir(MODS_ROOT):
        about = os.path.join(MODS_ROOT, d, "About", "About.xml")
        if os.path.isfile(about):
            pid = read_about_attr(about, "packageId")
            if pid:
                pkg_to_dir.setdefault(pid, d)

    # ---------- defName 集 ----------
    def_names = set()

    def collect_defs(root_dir):
        for dirpath, dirnames, filenames in os.walk(root_dir):
            for fn in filenames:
                if not fn.endswith(".xml"):
                    continue
                try:
                    for event, elem in ET.iterparse(os.path.join(dirpath, fn), events=("end",)):
                        if elem.tag == "defName":
                            t = (elem.text or "").strip()
                            if t:
                                def_names.add(t)
                            elem.clear()
                except Exception:
                    pass

    for sub in ("Core", "Biotech", "Ideology", "Royalty", "Anomaly"):
        p = os.path.join(DATA_ROOT, sub, "Defs")
        if os.path.isdir(p):
            collect_defs(p)
    for pid in active_ids:
        d = pkg_to_dir.get(pid)
        if not d:
            continue
        for rel in ("Defs", "Common/Defs", "1.6/Defs", "1.5/Defs", "1.4/Defs"):
            p = os.path.join(MODS_ROOT, d, rel)
            if os.path.isdir(p):
                collect_defs(p)
    print(f"[defName] {len(def_names)}")

    # ---------- 工具 ----------
    def parse_keys(fullpath):
        """返回 (ok, err_msg_or_None, {key:text})"""
        try:
            raw = open(fullpath, "r", encoding="utf-8-sig").read()
        except Exception as e:
            return False, f"读取失败 {e}", {}
        try:
            root = ET.fromstring(raw)
        except ET.ParseError as e:
            return False, f"XML 解析失败: {e}", {}
        out = {}
        for elem in root.iter():
            if elem.tag in ("LanguageData", "Defs", "DefInjected", "Strings"):
                continue
            out[elem.tag] = elem.text or ""
        return True, None, out

    def scan_lang_tree(langpath, keyed_out, def_out, stat, mod_dir):
        """递归扫描 Keyed/DefInjected/Strings。stat 为 None 表示只收集全局。"""
        for sub in ("Keyed", "DefInjected", "Strings"):
            base = os.path.join(langpath, sub)
            if not os.path.isdir(base):
                continue
            for dirpath, dirnames, filenames in os.walk(base):
                for fn in filenames:
                    if not (fn.endswith(".xml") or fn.endswith(".keyed")):
                        continue
                    fp = os.path.join(dirpath, fn)
                    ok, err, t = parse_keys(fp)
                    rel = os.path.relpath(fp, langpath).replace("\\", "/")
                    if not ok:
                        if stat is not None:
                            stat["xml"] += 1
                            stat["samples"].append((rel, "XML", err))
                        continue
                    for k, v in t.items():
                        if sub == "Keyed":
                            keyed_out[k] = v
                            if stat is not None:
                                if " " in k or re.search(r'[#=?<>/\\&"]', k):
                                    stat["badkey"] += 1
                                    if len(stat["samples"]) < 12:
                                        stat["samples"].append((rel, "非法字符", k))
                                src = en_keyed.get(k)
                                if src is not None:
                                    if re.findall(r"\{(\d+)\}", v) != re.findall(r"\{(\d+)\}", src):
                                        stat["ph"] += 1
                                        if len(stat["samples"]) < 12:
                                            stat["samples"].append((rel, "占位符数不匹配", k))
                                    named = re.findall(r"\{([^0-9}][^}]*)\}", v)
                                    if named:
                                        stat["ph"] += 1
                                        if len(stat["samples"]) < 12:
                                            stat["samples"].append((rel, f"命名占位符 {sorted(set(named))[:4]}", k))
                        else:
                            def_out[k] = v
                            if stat is not None and sub == "DefInjected":
                                dname = k.split(".", 1)[0]
                                if dname not in def_names:
                                    stat["baddef"] += 1
                                    if len(stat["samples"]) < 12:
                                        stat["samples"].append((rel, f"引用不存在 def: {dname}", k))

    # ---------- 英文 Keyed（全局） ----------
    en_keyed = {}

    def scan_en(langpath):
        base = os.path.join(langpath, "Keyed")
        if not os.path.isdir(base):
            return
        for fn in os.listdir(base):
            if fn.endswith(".xml") or fn.endswith(".keyed"):
                ok, _, t = parse_keys(os.path.join(base, fn))
                if ok:
                    en_keyed.update(t)

    scan_en(os.path.join(DATA_ROOT, "Core", "Languages", "English"))
    for pid in active_ids:
        d = pkg_to_dir.get(pid)
        if not d:
            continue
        lp = os.path.join(MODS_ROOT, d, "Languages", "English")
        if not os.path.isdir(lp):
            lp = os.path.join(MODS_ROOT, d, "Common", "Languages", "English")
        if os.path.isdir(lp):
            scan_en(lp)
    print(f"[EN Keyed] {len(en_keyed)}")

    # ---------- 中文扫描 ----------
    cn_keyed, cn_def = {}, {}
    stats = []
    for pid in active_ids:
        d = pkg_to_dir.get(pid)
        if not d:
            continue
        for lang in CN_DIR_NAMES:
            lp = os.path.join(MODS_ROOT, d, "Languages", lang)
            if not os.path.isdir(lp):
                lp = os.path.join(MODS_ROOT, d, "Common", "Languages", lang)
            if not os.path.isdir(lp):
                continue
            stat = {"name": f"{d} ({pid})", "xml": 0, "badkey": 0, "ph": 0, "baddef": 0, "samples": []}
            scan_lang_tree(lp, cn_keyed, cn_def, stat, d)
            stats.append(stat)
    print(f"[CN Keyed] {len(cn_keyed)}  [CN DefInjected] {len(cn_def)}")

    # ---------- 缺失统计 ----------
    missing_keyed = [k for k in en_keyed if k not in cn_keyed]
    print(f"\n===== 缺失翻译（英文有、中文无）=====")
    print(f"缺失 Keyed: {len(missing_keyed)}")
    # DefInjected 缺失：按 defName 覆盖度（近似）
    cn_defnames = set(k.split(".", 1)[0] for k in cn_def if "." in k)
    missing_defnames = def_names - cn_defnames
    print(f"全局 def 数: {len(def_names)}; 中文 DefInjected 覆盖 def 数: {len(cn_defnames)}; 未覆盖 def 数: {len(missing_defnames)}")

    # ---------- 每 mod 统计 ----------
    def w(s): return s["baddef"] * 3 + s["ph"] * 3 + s["xml"] * 5 + s["badkey"] * 2
    stats.sort(key=w, reverse=True)
    print(f"\n===== 确定错误（坏 def 引用/占位符/格式）Top 25 =====")
    tot = {k: sum(s[k] for s in stats) for k in ("xml", "badkey", "ph", "baddef")}
    print(f"合计: XML={tot['xml']} 非法字符={tot['badkey']} 占位符={tot['ph']} 坏def引用={tot['baddef']}")
    for s in stats[:25]:
        if w(s) == 0:
            continue
        print(f"  {s['name']}: baddef={s['baddef']} ph={s['ph']} badkey={s['badkey']} xml={s['xml']}")
        for fn, kind, det in s["samples"][:6]:
            print(f"      [{kind}] {fn}: {det}")

    # ---------- 每 mod 缺失翻译 ----------
    print(f"\n===== 各 mod 英文 Keyed 未覆盖情况（缺失翻译来源）Top 40 =====")
    rows = []
    for pid in active_ids:
        d = pkg_to_dir.get(pid)
        if not d:
            continue
        lp = os.path.join(MODS_ROOT, d, "Languages", "English")
        if not os.path.isdir(lp):
            lp = os.path.join(MODS_ROOT, d, "Common", "Languages", "English")
        base = os.path.join(lp, "Keyed") if os.path.isdir(lp) else None
        if not base or not os.path.isdir(base):
            continue
        mod_en = {}
        for fn in os.listdir(base):
            if fn.endswith(".xml") or fn.endswith(".keyed"):
                ok, _, t = parse_keys(os.path.join(base, fn))
                if ok:
                    mod_en.update(t)
        miss = [k for k in mod_en if k not in cn_keyed]
        if miss:
            rows.append((len(miss), len(mod_en), d, pid))
    rows.sort(reverse=True)
    for miss, total, d, pid in rows[:40]:
        print(f"  {d} ({pid}): 缺 {miss}/{total}")

    # ---------- 导出完整确定错误清单（供逐条核对） ----------
    out_lines = []
    out_lines.append("# 简体中文翻译确定错误清单（启发式扫描，非游戏内报告）")
    out_lines.append("# 错误类型: baddef=DefInjected引用不存在def | ph=占位符/格式 | xml=XML解析失败 | badkey=key非法字符")
    for s in sorted(stats, key=w, reverse=True):
        if w(s) == 0:
            continue
        out_lines.append(f"\n## {s['name']}  (baddef={s['baddef']} ph={s['ph']} badkey={s['badkey']} xml={s['xml']})")
        for fn, kind, det in s["samples"]:
            out_lines.append(f"  [{kind}] {fn} :: {det}")
    out_path = Path(__file__).resolve().parent / "translation_errors.txt"
    out_path.write_text("\n".join(out_lines), encoding="utf-8")
    print(f"\n[已导出] 完整确定错误清单 -> {out_path} ({len(out_lines)} 行)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
