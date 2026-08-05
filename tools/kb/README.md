# Knowledge Base Toolchain (`tools/kb/`)

Scripts that keep `docs/knowledge/` in sync with the actual mod folders. Pure
Python 3 standard library — no dependencies, works on Windows/Linux/macOS.

Run from the repo root (or anywhere; paths are resolved relative to the repo root).

## Scripts

| Script | Purpose |
|---|---|
| `scan_mods.py` | Scan every `NN-*` mod folder, parse `About/About.xml`, probe structure (version dirs, `Common/`, `Source/`, `Languages/`, `docs/`, `Tests/`, `_PublisherPlus.xml`, `LoadFolders.xml`, README status). Writes `docs/knowledge/kb-index.json` (machine-readable) and the auto section of `docs/knowledge/mod-index.md`. |
| `check_links.py` | Verify every relative link in `AGENTS.md` + `docs/knowledge/*.md` resolves, mod numbers are unique (55 is expected duplicated), and manual refs point to real files. Exit code 0 = OK, 1 = problems. |
| `validate_readme.py` | Classify each README as `ok` / `template` (placeholder) / `stub` (empty) / `missing`; list rewrite backlog; verify single mods; track batch progress. |

## Usage

```powershell
# Windows (from repo root)
python tools/kb/scan_mods.py            # regenerate index (preserves manual section)
python tools/kb/scan_mods.py --check    # report what changed vs last run (no rewrite)
python tools/kb/scan_mods.py --json-only
python tools/kb/check_links.py          # link/reference validation
python tools/kb/validate_readme.py --list
python tools/kb/validate_readme.py --todo        # mods still needing README rewrite
python tools/kb/validate_readme.py --verify 01-AlertUtility
python tools/kb/validate_readme.py --progress
```

```bash
# Linux/macOS
python3 tools/kb/scan_mods.py
```

## Workflow: keeping the knowledge base current

See `docs/knowledge/CONTRIBUTING.md` for the full update checklist. Summary:

1. After adding/modifying a mod, run `scan_mods.py` (regenerates the metadata table).
2. If anything in the report looks off, update the affected topic docs in
   `docs/knowledge/`.
3. Always finish with `check_links.py` — it must pass (exit 0).

## Design rules

- **Generated vs manual separation**: `mod-index.md` has an auto section
  (metadata table, rewritten on every scan) and a manual section (`<!-- MANUAL -->`,
  human-maintained notes/links, never touched by the scanner).
- **Topic docs are hand-written** (`harmony-patching.md`, etc.). The scripts never
  generate prose — they only scan, emit metadata, and validate.
- `kb-progress.json` (this folder) tracks README rewrite batches; it is the only
  state the toolchain keeps outside `docs/knowledge/`.

## CI (optional)

Add to any CI job:

```yaml
- run: python tools/kb/check_links.py
- run: python tools/kb/validate_readme.py --todo
```
