from pathlib import Path
import sqlite3, json
import pandas as pd

ENGINE_ROOT = Path(__file__).resolve().parents[3]
RAW = ENGINE_ROOT / "00_Data" / "raw"
OUT = ENGINE_ROOT / "00_Data" / "processed" / "02_identity_reconciliation"
REPORTS = ENGINE_ROOT / "00_Data" / "metadata" / "quality_reports"

DB = RAW / "master_v1.5.0" / "SpecSynth_Master_v1.0.sqlite"
DEEP = RAW / "insulation_research" / "reconciliation" / "SpecSynth_Insulation_Batches01-15_v1.5.0_Deep_Reconciliation_Review.csv"
RECON = RAW / "insulation_research" / "reconciliation" / "SpecSynth_Insulation_Batches01-15_v1.5.0_Reconciliation.csv"

def main():
    for p in (DB, DEEP, RECON):
        if not p.exists():
            raise FileNotFoundError(f"Required input missing: {p}")

    OUT.mkdir(parents=True, exist_ok=True)
    REPORTS.mkdir(parents=True, exist_ok=True)

    con = sqlite3.connect(DB)
    try:
        tables = pd.read_sql(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name",
            con
        )["name"].tolist()
        counts = {}
        for table in tables:
            # table names originate from sqlite_master, quoted to avoid SQL parsing issues.
            counts[table] = int(pd.read_sql(f'SELECT COUNT(*) AS n FROM "{table}"', con).iloc[0]["n"])
        products = pd.read_sql(
            "SELECT product_id, manufacturer, product_name, category, material_family, data_status, record_origin FROM products",
            con
        )
        variants = pd.read_sql(
            "SELECT variant_id, product_id, thickness_mm, lambda_W_mK, thermal_resistance_m2K_W, data_status FROM product_variants",
            con
        )
    finally:
        con.close()

    deep = pd.read_csv(DEEP)
    recon = pd.read_csv(RECON)

    # Existing reconciliation fields are retained. This script does not approve or ingest candidates.
    queue_cols = [
        c for c in [
            "research_batch", "record_key", "product_name", "manufacturer",
            "material_family", "thermal_conductivity_W_mK", "thickness_mm",
            "evidence_status", "training_eligible", "linked_product_id",
            "top1_id", "top1_name", "top1_score", "top2_id", "top2_name",
            "top2_score", "reconciliation_decision", "approval_status",
            "ingestion_status", "candidate_thickness_already_in_master",
            "source_reference", "source_url", "notes"
        ] if c in deep.columns
    ]
    queue = deep[queue_cols].copy()
    queue.to_csv(OUT / "candidate_review_queue.csv", index=False)

    summary = {
        "master_database": str(DB),
        "master_table_counts": counts,
        "master_products": int(len(products)),
        "master_variants": int(len(variants)),
        "research_candidates": int(len(deep)),
        "reconciliation_decisions": {
            str(k): int(v) for k, v in deep["reconciliation_decision"].value_counts(dropna=False).items()
        } if "reconciliation_decision" in deep else {},
        "approval_status": {
            str(k): int(v) for k, v in deep["approval_status"].value_counts(dropna=False).items()
        } if "approval_status" in deep else {},
        "ingestion_status": {
            str(k): int(v) for k, v in deep["ingestion_status"].value_counts(dropna=False).items()
        } if "ingestion_status" in deep else {},
        "reconciliation_csv_rows": int(len(recon)),
        "database_modified": False,
        "candidate_ingestion_performed": False,
        "next_action": "Manually review candidate_review_queue.csv; only approved rows may enter a future master-candidate build."
    }
    (REPORTS / "consolidation_preflight_report.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(json.dumps(summary, indent=2))
    print(f"\nReview queue: {OUT / 'candidate_review_queue.csv'}")
    print(f"Preflight report: {REPORTS / 'consolidation_preflight_report.json'}")

if __name__ == "__main__":
    main()
