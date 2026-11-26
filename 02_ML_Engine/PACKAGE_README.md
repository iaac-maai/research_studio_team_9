# SpecSynth AI — Data Consolidation Intake v1.0

## What this package is

A non-destructive staging package matching the existing `02_ML_Engine/00_Data` architecture. It collects the current master SQLite database, the 75-candidate Batches 01–15 reconciliation files, and earlier research/ML source files that were available.

**It does not perform production ingestion or claim that the 75 candidates are approved.**

## Folder layout

- `00_Data/raw/master_v1.5.0/` — SQLite baseline.
- `00_Data/raw/insulation_research/` — research staging and reconciliation inputs.
- `00_Data/raw/historical_ml/` — previous ML export and split metadata.
- `00_Data/processed/01_research_staging/` — reserved for normalized staging.
- `00_Data/processed/02_identity_reconciliation/` — reserved for adjudicated identity decisions.
- `00_Data/processed/03_consolidated_master/` — reserved for approved consolidated master candidate.
- `00_Data/processed/04_ml_ready/` — reserved for regenerated ML dataset.
- `00_Data/metadata/` — inventory, provenance, quality and split metadata.
- `02_Source/specsynth_ml/data/` — reserved for consolidation code.
- `07_Logs/` — reserved for pipeline logs.

## Current findings

The SQLite baseline contains these table counts:
- products: 182
- product_variants: 1,941
- sources: 150
- product_source_links: 208
- variant_source_links: 2,002
- ml_variant_records: 1,941
- manufacturer_assemblies: 77
- thermal_baseline: 4,753
- synthetic_variants: 216
- multiobjective_scenarios: 1,680
- epd_gwp_modules: 63

The supplied reconciliation files contain 75 candidate records. All 75 have `production_approval = NOT_APPROVED`; the deep review also marks all 75 `approval_status = NOT_APPROVED` and `ingestion_status = NOT_INGESTED`.

## Installation

Extract the ZIP to a temporary location. Merge the `02_ML_Engine` folder into your existing `06_AI_Performance_Engine/02_ML_Engine` folder. Compare before replacing any same-named file. This package is intended to add source files and metadata only; it does not replace your existing Python package, notebooks, dependency files, or models.

## Next stage

Implement and run a reconciliation pipeline that:
1. loads products, variants, and sources from SQLite read-only;
2. loads the 75 candidate records;
3. preserves the supplied identity and deep-review decisions;
4. produces a candidate-level review queue and field-level diff;
5. requires explicit approval before any write to a new master candidate;
6. exports an audit trail and never overwrites the source database.

Only after candidate adjudication should a new consolidated master and new ML-ready variants be generated.
