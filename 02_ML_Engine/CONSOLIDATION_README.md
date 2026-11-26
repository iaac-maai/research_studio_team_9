# SpecSynth Data Consolidation Workflow v1.1

## Purpose
This package organises the actual source files and adds a read-only consolidation preflight script. It does not modify the SQLite master, approve candidates, or create a new released dataset.

## Merge into existing project
Extract the ZIP and merge the top-level `02_ML_Engine` folder into your existing:
`06_AI_Performance_Engine/02_ML_Engine`

Do not replace existing project files. This package only adds files in the listed folders.

## Inputs included
- SQLite master database (`SpecSynth_Master_v1.0.sqlite`)
- Batches 01–15 reconciliation CSV
- Deep reconciliation review CSV
- Batches 01–10 consolidated research CSV and research workbook
- Historical ML v1.5.0 CSV and split/target JSON

The SQLite file is copied as a source snapshot. Keep the original elsewhere as a backup too.

## What the preflight script does
Run from `02_ML_Engine`:

PowerShell:
`$env:PYTHONPATH = "$PWD\02_Source"`
`python -m specsynth_ml.data.consolidation_preflight`

It:
1. Reads SQLite table counts and product/variant counts.
2. Reads the two reconciliation CSVs.
3. Writes `candidate_review_queue.csv`.
4. Writes `consolidation_preflight_report.json`.
5. Explicitly does not write to the SQLite database or approve/ingest any candidate.

## Dependencies
Requires `pandas`. Uses Python's built-in `sqlite3` and `json`.
Do not replace your existing requirements files. Add pandas only if it is not already installed.

## Important data governance
- The 75 research candidates remain NOT_APPROVED / NOT_INGESTED according to the supplied deep review file.
- Similarity matches are not proof of product identity.
- Do not append batch files to consolidated staging if the same records are already represented.
- Do not mix material-level conductivity targets with assembly-level U-value datasets.
- The historical v1.5.0 ML dataset remains a separate derived dataset until new master candidates are approved and a fresh ML export is generated.
- The database is not modified by this package.

## Output paths
- `00_Data/processed/02_identity_reconciliation/candidate_review_queue.csv`
- `00_Data/metadata/quality_reports/consolidation_preflight_report.json`
