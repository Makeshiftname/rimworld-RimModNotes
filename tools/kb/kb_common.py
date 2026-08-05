"""Shared helpers for the RimWorld mod knowledge base toolchain (tools/kb/).

Pure Python standard library, works on Windows/Linux/macOS.
"""
from __future__ import annotations

import re
import xml.etree.ElementTree as ET
from pathlib import Path

# tools/kb/kb_common.py -> repo root
REPO_ROOT = Path(__file__).resolve().parent.parent.parent

MOD_DIR_RE = re.compile(r"^\d+[-_]")
NUMBER_RE = re.compile(r"^(\d+)")
VERSION_DIR_RE = re.compile(r"^1\.\d+$")

# Markers that identify the GitHub template placeholder README
TEMPLATE_MARKERS = [
    "Rimworld Mod Template Monodevelop and Linux",
    "This repository is generated from github template",
    "Rimworld Mod Template",
]

KB_DIR = REPO_ROOT / "docs" / "knowledge"
INDEX_JSON = KB_DIR / "kb-index.json"
INDEX_MD = KB_DIR / "mod-index.md"
AUTO_START = "<!-- AUTO-GENERATED-START -->"
AUTO_END = "<!-- AUTO-GENERATED-END -->"
MANUAL_START = "<!-- MANUAL-START -->"
MANUAL_END = "<!-- MANUAL-END -->"


def mod_dirs() -> list[Path]:
    """Return top-level mod directories (named NN-xxx), sorted by name."""
    return sorted(
        (p for p in REPO_ROOT.iterdir()
         if p.is_dir() and MOD_DIR_RE.match(p.name)),
        key=lambda p: p.name,
    )


def _child_text(elem: ET.Element, tag: str) -> str:
    node = elem.find(tag)
    return node.text.strip() if node is not None and node.text else ""


def _strip(text: str | None) -> str:
    return " ".join((text or "").split())


def parse_about(about_path: Path) -> dict:
    """Parse an About/About.xml into a dict. Falls back to text scanning on parse errors."""
    data = {
        "package_id": "", "name": "", "author": "",
        "supported_versions": [], "deps": [], "load_after": [], "description": "",
    }
    if not about_path.exists():
        return data
    try:
        root = ET.parse(about_path, parser=ET.XMLParser(encoding="utf-8")).getroot()
    except ET.ParseError:
        return _parse_about_text(about_path, data)
    data["package_id"] = _child_text(root, "packageId")
    data["name"] = _child_text(root, "name")
    data["author"] = _child_text(root, "author")
    data["description"] = _strip(_child_text(root, "description"))
    for li in root.findall("supportedVersions/li"):
        if li.text and li.text.strip():
            data["supported_versions"].append(li.text.strip())
    for li in root.findall("modDependencies/li"):
        pid = _child_text(li, "packageId")
        if pid:
            data["deps"].append(pid)
    for li in root.findall("loadAfter/li"):
        if li.text and li.text.strip():
            data["load_after"].append(li.text.strip())
    return data


def _parse_about_text(about_path: Path, data: dict) -> dict:
    raw = about_path.read_text(encoding="utf-8", errors="replace")
    for key, tag in (("package_id", "packageId"), ("name", "name"),
                     ("author", "author"), ("description", "description")):
        m = re.search(r"<%s>\s*(.*?)\s*</%s>" % (tag, tag), raw, re.S)
        if m:
            data[key] = _strip(m.group(1))
    sv = re.search(r"<supportedVersions>(.*?)</supportedVersions>", raw, re.S)
    if sv:
        data["supported_versions"] = re.findall(r"<li>\s*(1\.\d+)\s*</li>", sv.group(1))
    data["deps"] = re.findall(
        r"<modDependencies>(.*?)</modDependencies>", raw, re.S)[0:] and re.findall(
        r"<packageId>\s*([^<\s]+)\s*</packageId>",
        re.search(r"<modDependencies>(.*?)</modDependencies>", raw, re.S).group(1)) \
        if re.search(r"<modDependencies>(.*?)</modDependencies>", raw, re.S) else []
    la = re.search(r"<loadAfter>(.*?)</loadAfter>", raw, re.S)
    if la:
        data["load_after"] = re.findall(r"<li>\s*([^<\s]+)\s*</li>", la.group(1))
    return data


def readme_status(readme_path: Path) -> str:
    """Classify a mod's README: ok / template / stub / missing."""
    if not readme_path.exists():
        return "missing"
    try:
        text = readme_path.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return "missing"
    low = text.lower()
    if any(marker.lower() in low for marker in TEMPLATE_MARKERS):
        return "template"
    if len(text.strip()) < 300:
        return "stub"
    return "ok"


def probe_mod(mod_dir: Path) -> dict:
    """Collect structural facts about one mod directory."""
    m = NUMBER_RE.match(mod_dir.name)
    number = m.group(1) if m else ""
    title = NUMBER_RE.sub("", mod_dir.name, count=1).lstrip("-_ ") or mod_dir.name

    about_top = mod_dir / "About" / "About.xml"
    about_backup = mod_dir / "backup" / "About" / "About.xml"
    has_about = about_top.exists()
    in_backup = about_backup.exists() and not has_about
    about = parse_about(about_top if has_about else about_backup) if (has_about or in_backup) else {}

    versions = sorted(
        d.name for d in mod_dir.iterdir()
        if d.is_dir() and VERSION_DIR_RE.match(d.name)
    )
    backup_dir = mod_dir / "backup"
    if not versions and backup_dir.is_dir():
        versions = sorted(
            d.name for d in backup_dir.iterdir()
            if d.is_dir() and VERSION_DIR_RE.match(d.name)
        )

    cs_files = sorted(p for p in mod_dir.rglob("*.cs")
                      if "Source" in p.parts and ".roo" not in p.parts)
    cs_count = len(cs_files)

    lang_candidates = [mod_dir / "Languages"] + [mod_dir / v / "Languages" for v in versions]
    if backup_dir.is_dir():
        lang_candidates += [backup_dir / "Common" / "Languages",
                            backup_dir / "Languages"] + [
            backup_dir / v / "Languages" for v in versions]
    has_languages = any(p.is_dir() for p in lang_candidates) or \
        (mod_dir / "Common" / "Languages").is_dir()
    has_docs = (mod_dir / "docs").is_dir()
    has_tests = any(p.is_dir() for p in [mod_dir / "Tests", mod_dir / "tests"])
    has_publisher_plus = any(mod_dir.glob("_PublisherPlus.xml"))
    has_loadfolders = (mod_dir / "LoadFolders.xml").exists()
    has_loadfolders_bak = (mod_dir / "LoadFolders.xml.bak").exists()
    has_agents = (mod_dir / "AGENTS.md").exists()

    readme_path = mod_dir / "README.md"
    status = readme_status(readme_path)

    notes_files = sorted(
        str(p.relative_to(REPO_ROOT)).replace("\\", "/")
        for p in mod_dir.rglob("*.md")
        if p.name != "README.md" and ".roo" not in p.parts
        and not any(part.startswith(".") for part in p.relative_to(REPO_ROOT).parts)
    )

    mod = {
        "dir": mod_dir.name,
        "number": number,
        "title": title,
        "has_about": has_about,
        "in_backup": in_backup,
        **about,
        "versions": versions,
        "has_common": (mod_dir / "Common").is_dir() or (backup_dir / "Common").is_dir(),
        "has_csharp": cs_count > 0,
        "cs_count": cs_count,
        "has_languages": has_languages,
        "has_docs": has_docs,
        "has_tests": has_tests,
        "has_publisher_plus": has_publisher_plus,
        "has_loadfolders": has_loadfolders,
        "has_loadfolders_bak": has_loadfolders_bak,
        "has_agents": has_agents,
        "readme_status": status,
        "notes_files": notes_files,
    }
    mod["type"], mod["type_conf"] = classify(mod)
    return mod


def classify(mod: dict) -> tuple[str, str]:
    """Heuristic type classification. Confidence: high/medium/low.

    Patch vs standalone is ambiguous from structure alone; verify manually
    (the manual section of mod-index.md is the source of truth).
    """
    if not mod.get("has_about") and not mod.get("in_backup"):
        return "empty", "high"
    dirname = mod["dir"].lower()
    if re.search(r"zh", dirname):
        return "translation", "high"
    if not mod["has_csharp"]:
        return "xml", "medium"
    third_party = [d for d in mod.get("deps", []) if not d.startswith("brrainz.harmony")]
    if third_party:
        return "patch", "medium"
    name = (mod.get("name") or mod["title"]).lower()
    if "gui" in name or "lib" in name:
        return "lib", "medium"
    return "standalone", "low"


def scan_all() -> list[dict]:
    return [probe_mod(d) for d in mod_dirs()]


def extract_manual(md_text: str) -> str | None:
    """Extract the MANUAL section body (without the markers) from existing mod-index.md."""
    start = md_text.find(MANUAL_START)
    end = md_text.find(MANUAL_END)
    if start == -1 or end == -1 or end <= start:
        return None
    return md_text[start + len(MANUAL_START):end].strip("\n")
