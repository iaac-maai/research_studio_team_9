# Source manifest — SpecSynth data consolidation intake v1.0

All files under `00_Data/raw/` are immutable source copies for this consolidation workflow.

| Source | Role | Merge decision |
|---|---|---|
| `SpecSynth_Master_v1.0.sqlite` | Existing relational catalogue and derived datasets | Baseline/reference; do not overwrite |
| `SpecSynth_Insulation_Batches01-15_v1.5.0_Reconciliation.csv` | 75 candidate records with v1.5.0 comparison | Reconciliation input only |
| `SpecSynth_Insulation_Batches01-15_v1.5.0_Deep_Reconciliation_Review.csv` | Detailed identity/variant decisions | Reconciliation input only |
| Batches 01–10 consolidated CSV | Earlier research staging | Source evidence; overlaps consolidated 01–15 |
| Research Pack v0.1 XLSX | Research source register and import rules | Evidence/supporting workbook |
| Historical ML variants v1.5.0 CSV | Existing ML baseline | Preserve separately; regenerate only after approval |
| Split/target JSON v1.5.0 | Historical split/target metadata | Historical metadata; do not reuse blindly after merge |

## Current reconciliation status

The 75 research candidates are all `NOT_APPROVED` for production ingestion and `NOT_INGESTED` in the deep review. The 75 must remain in staging until identity, source evidence, property-level verification, variant identity, and approval are resolved.

The reconciliation file reports 2 exact product + manufacturer matches, 2 exact product-name matches with manufacturer differences, 1 exact source URL match, 3 related-name candidates, and 67 with no direct product/URL match. The deep review classifies 62 as no direct match, 5 weak name similarity, 4 exact product-name match requiring manufacturer review, 3 possible aliases, and 1 exact source match requiring product-identity review. These are matching classifications, not ingestion approvals.

## Non-destructive policy

- Never append all research candidates directly to `products` or `product_variants`.
- Do not count batch 01–10 staging records twice when the consolidated 01–15 package includes them.
- Do not treat CSV/JSON exports of the SQLite database as additional independent products.
- Do not merge the material conductivity dataset with assembly U-value or multi-objective scenario datasets.
- Keep missing values missing; retain source URL, source reference, evidence status, and candidate record key.
- Regenerate product-grouped splits after any approved master/variant update.
