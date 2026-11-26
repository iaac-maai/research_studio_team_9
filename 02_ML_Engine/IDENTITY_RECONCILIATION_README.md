# SpecSynth Identity Reconciliation v1.2

## Purpose
This step performs a second, read-only comparison of the 75 research candidates against the master SQLite product, variant, and source tables. It creates a manual review queue. It does not approve, ingest, or modify any records.

## Prerequisites
The previous Data Consolidation Workflow v1.1 must already be merged into the existing `02_ML_Engine`, including:
- `00_Data/raw/master_v1.5.0/SpecSynth_Master_v1.0.sqlite`
- `00_Data/raw/insulation_research/reconciliation/SpecSynth_Insulation_Batches01-15_v1.5.0_Deep_Reconciliation_Review.csv`

## Install
Extract and merge this package into the existing `02_ML_Engine`. Do not overwrite existing files.

## Run from the `02_ML_Engine` directory
PowerShell:
`$env:PYTHONPATH = "$PWD\02_Source"`
`python -m specsynth_ml.data.identity_reconciliation`

Requires pandas. SQLite access uses Python's built-in sqlite3.

## Outputs
- `00_Data/processed/02_identity_reconciliation/identity_match_results.csv`
- `00_Data/processed/02_identity_reconciliation/approval_register.csv`
- `00_Data/metadata/quality_reports/identity_reconciliation_report.json`

## Interpretation
- Exact name/manufacturer and exact URL matches are evidence for review, not automatic identity proof.
- Fuzzy name similarity is only a triage hint.
- The script does not approve candidates.
- Every candidate starts with `manual_review_status=PENDING` and `reviewer_decision=NOT_REVIEWED`.
- The SQLite database is opened in read-only mode.

## Reviewer decisions
Use the `approval_register.csv` to record one of:
- `APPROVE_NEW_PRODUCT`
- `LINK_EXISTING_PRODUCT`
- `ADD_NEW_VARIANT`
- `POSSIBLE_DUPLICATE`
- `REJECT`
- `NEEDS_MORE_EVIDENCE`

Do not fill approved IDs until the identity has been verified against the source evidence.
