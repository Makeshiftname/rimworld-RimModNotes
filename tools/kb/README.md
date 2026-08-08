# Knowledge Base Toolchain (`tools/kb/`)

Scripts that keep `docs/knowledge/` in sync with the actual mod folders. Pure
Python 3 standard library — no dependencies, works on Windows/Linux/macOS.

Run from the repo root (or anywhere; paths are resolved relative to the repo root).

## Scripts

| Script | Purpose |
|---|---|
| `scan_mods.py` | Scan every mod folder under `自建/` and `收集/` (`NN-功能-名称`), parse `About/About.xml`, probe structure (version dirs, `Common/`, `Source/`, `Languages/`, `docs/`, `Tests/`, `_PublisherPlus.xml`, `LoadFolders.xml`, README status). Writes `docs/knowledge/kb-index.json` (machine-readable) and the auto section of `docs/knowledge/mod-index.md` (with a 大类 column). |
| `check_links.py` | Verify every relative link in `AGENTS.md` + `docs/knowledge/*.md` resolves, and mod numbers are unique within each category (自建/收集 each start at 01). Exit code 0 = OK, 1 = problems. |
| `validate_readme.py` | Classify each README as `ok` / `template` (placeholder) / `stub` (empty) / `missing`; list rewrite backlog; verify single mods; track batch progress. |
| `check_repo_hygiene.py` | Acceptance gate for repo structure: leftover `.bak`, git-tracked `.pdb`, bad `NN-功能-名称` dir names, stray top-level mod dirs, placeholder packageIds (warning), naming aliases (warning). Exit code 0 = OK. |
| `scan_translations.py` | Chinese translation error audit against a local game install (bad def refs / placeholders / XML / missing keys). Requires `--mods/--data/--config` pointing at the game. |
| `gen_rename_map.py` | (one-shot, 2026-08-08) Generate the 自建/收集 × 功能 rename mapping table (dry-run / `--json` / `--review`). |
| `apply_rename.py` | (one-shot, 2026-08-08) Apply the rename mapping via `git mv` (`--apply`; default dry-run). |
| `migrate_links.py` | (one-shot, 2026-08-08) Rewrite old `NN-名称` dir references in markdown links / inline text to the new `大类/NN-功能-名称` paths (`--text` for inline text, excludes `mod-index.md`). |

## Usage

```powershell
# Windows (from repo root)
python tools/kb/scan_mods.py            # regenerate index (preserves manual section)
python tools/kb/scan_mods.py --check    # report what changed vs last run (no rewrite)
python tools/kb/scan_mods.py --json-only
python tools/kb/check_links.py          # link/reference validation
python tools/kb/check_repo_hygiene.py   # structure hygiene gate (.bak/.pdb/命名)
python tools/kb/validate_readme.py --list
python tools/kb/validate_readme.py --todo        # mods still needing README rewrite
python tools/kb/validate_readme.py --verify 01-standalone-AlertUtility
python tools/kb/validate_readme.py --progress
python tools/kb/scan_translations.py    # translation audit (needs --mods/--data/--config)
```

```bash
# Linux/macOS
python3 tools/kb/scan_mods.py
```

## Workflow: keeping the knowledge base current

See `docs/knowledge/CONTRIBUTING.md` for the full update checklist. Summary:

1. After adding/modifying a mod (place it under `自建/` or `收集/`, named `NN-功能-名称`),
   run `scan_mods.py` (regenerates the metadata table).
2. If anything in the report looks off, update the affected topic docs in
   `docs/knowledge/`.
3. Always finish with `check_links.py` AND `check_repo_hygiene.py` — both must
   pass (exit 0).

## Design rules

- **Generated vs manual separation**: `mod-index.md` has an auto section
  (metadata table, rewritten on every scan) and a manual section (`<!-- MANUAL -->`,
  human-maintained notes/links, never touched by the scanner).
- **Topic docs are hand-written** (`harmony-patching.md`, etc.). The scripts never
  generate prose — they only scan, emit metadata, and validate.
- `kb-progress.json` (this folder) tracks README rewrite batches; it is the only
  state the toolchain keeps outside `docs/knowledge/`.

## CI

`.github/workflows/kb-ci.yml` runs on every push/PR:

```yaml
- run: python tools/kb/check_links.py
- run: python tools/kb/check_repo_hygiene.py
- run: python tools/kb/scan_mods.py --check
- run: python tools/kb/validate_readme.py --todo
```

CI cannot build the C# mods (no game assemblies here); it only enforces the
knowledge-base and structure gates.
