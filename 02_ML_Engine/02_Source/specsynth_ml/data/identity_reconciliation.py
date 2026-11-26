from pathlib import Path
import sqlite3
import json
import re
import difflib
import pandas as pd

ENGINE_ROOT = Path(__file__).resolve().parents[3]
RAW = ENGINE_ROOT / "00_Data" / "raw"
DEEP_REVIEW = RAW / "insulation_research" / "reconciliation" / (
    "SpecSynth_Insulation_Batches01-15_v1.5.0_Deep_Reconciliation_Review.csv"
)
DB_PATH = RAW / "master_v1.5.0" / "SpecSynth_Master_v1.0.sqlite"
OUT_DIR = ENGINE_ROOT / "00_Data" / "processed" / "02_identity_reconciliation"
REPORT_DIR = ENGINE_ROOT / "00_Data" / "metadata" / "quality_reports"


def norm(value):
    if pd.isna(value):
        return ""
    value = str(value).lower().strip()
    value = re.sub(r"[^a-z0-9]+", " ", value)
    return re.sub(r"\s+", " ", value).strip()


def read_master():
    if not DB_PATH.exists():
        raise FileNotFoundError(f"Master database not found: {DB_PATH}")
    con = sqlite3.connect(f"file:{DB_PATH.as_posix()}?mode=ro", uri=True)
    try:
        products = pd.read_sql_query(
            "SELECT product_id, manufacturer, product_name, category, material_family, data_status "
            "FROM products", con
        )
        variants = pd.read_sql_query(
            "SELECT variant_id, product_id, thickness_mm, lambda_W_mK, "
            "thermal_resistance_m2K_W, data_status FROM product_variants", con
        )
        sources = pd.read_sql_query(
            "SELECT source_id, publisher, document, url, source_type, evidence_role FROM sources", con
        )
        product_links = pd.read_sql_query(
            "SELECT product_id, source_id FROM product_source_links", con
        )
    finally:
        con.close()
    return products, variants, sources, product_links


def main():
    if not DEEP_REVIEW.exists():
        raise FileNotFoundError(
            f"Deep reconciliation CSV not found: {DEEP_REVIEW}\n"
            "Copy the supplied deep review CSV into "
            "00_Data/raw/insulation_research/reconciliation/."
        )

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    REPORT_DIR.mkdir(parents=True, exist_ok=True)

    candidates = pd.read_csv(DEEP_REVIEW)
    products, variants, sources, product_links = read_master()

    # Build normalized lookup fields from master records.
    products = products.copy()
    products["__name_norm"] = products["product_name"].map(norm)
    products["__mfr_norm"] = products["manufacturer"].map(norm)
    sources = sources.copy()
    sources["__url_norm"] = sources["url"].fillna("").astype(str).str.strip().str.rstrip("/").str.lower()

    output = []
    for _, row in candidates.iterrows():
        name = norm(row.get("product_name"))
        manufacturer = norm(row.get("manufacturer"))
        url = str(row.get("source_url") or "").strip().rstrip("/").lower()

        exact_name = products[products["__name_norm"] == name] if name else products.iloc[0:0]
        exact_name_mfr = exact_name[exact_name["__mfr_norm"] == manufacturer] if manufacturer else exact_name.iloc[0:0]

        # Similarity scores are triage hints only; they are not identity decisions.
        fuzzy_candidates = []
        if name:
            for _, master_row in products.iterrows():
                score = difflib.SequenceMatcher(None, name, master_row["__name_norm"]).ratio()
                if score >= 0.55:
                    fuzzy_candidates.append((score, master_row))
        fuzzy_candidates.sort(key=lambda x: x[0], reverse=True)
        top = fuzzy_candidates[:3]

        exact_url_sources = sources[sources["__url_norm"] == url] if url else sources.iloc[0:0]
        url_product_ids = []
        if not exact_url_sources.empty:
            source_ids = set(exact_url_sources["source_id"].astype(str))
            links = product_links[product_links["source_id"].astype(str).isin(source_ids)]
            url_product_ids = sorted(set(links["product_id"].astype(str)))

        thickness = row.get("thickness_mm")
        same_thickness_matches = []
        if not exact_name_mfr.empty and pd.notna(thickness):
            ids = set(exact_name_mfr["product_id"].astype(str))
            vv = variants[variants["product_id"].astype(str).isin(ids)]
            same_thickness_matches = vv[
                pd.to_numeric(vv["thickness_mm"], errors="coerce").sub(float(thickness)).abs() < 0.01
            ]["variant_id"].astype(str).tolist()

        result = {
            "record_key": row.get("record_key"),
            "research_batch": row.get("research_batch"),
            "candidate_product_name": row.get("product_name"),
            "candidate_manufacturer": row.get("manufacturer"),
            "candidate_material_family": row.get("material_family"),
            "candidate_thickness_mm": thickness,
            "candidate_lambda_W_mK": row.get("thermal_conductivity_W_mK"),
            "candidate_source_url": row.get("source_url"),
            "original_reconciliation_decision": row.get("reconciliation_decision"),
            "original_approval_status": row.get("approval_status"),
            "exact_name_match_count": int(len(exact_name)),
            "exact_name_and_manufacturer_match_count": int(len(exact_name_mfr)),
            "exact_name_match_product_ids": ";".join(exact_name["product_id"].astype(str).tolist()),
            "exact_name_and_manufacturer_product_ids": ";".join(exact_name_mfr["product_id"].astype(str).tolist()),
            "exact_source_match_count": int(len(exact_url_sources)),
            "source_linked_product_ids": ";".join(url_product_ids),
            "same_thickness_variant_ids": ";".join(same_thickness_matches),
            "fuzzy_top1_product_id": str(top[0][1]["product_id"]) if top else "",
            "fuzzy_top1_product_name": str(top[0][1]["product_name"]) if top else "",
            "fuzzy_top1_manufacturer": str(top[0][1]["manufacturer"]) if top else "",
            "fuzzy_top1_score": round(float(top[0][0]), 4) if top else None,
            "fuzzy_top2_product_id": str(top[1][1]["product_id"]) if len(top) > 1 else "",
            "fuzzy_top2_product_name": str(top[1][1]["product_name"]) if len(top) > 1 else "",
            "fuzzy_top2_score": round(float(top[1][0]), 4) if len(top) > 1 else None,
            "manual_review_status": "PENDING",
            "reviewer_decision": "NOT_REVIEWED",
            "approved_product_id": "",
            "approved_variant_id": "",
            "reviewer_notes": ""
        }
        output.append(result)

    review = pd.DataFrame(output)
    review_path = OUT_DIR / "identity_match_results.csv"
    review.to_csv(review_path, index=False)

    # Separate approval register is deliberately blank; no candidate is auto-approved.
    approval_cols = [
        "record_key", "research_batch", "candidate_product_name", "candidate_manufacturer",
        "reviewer_decision", "approved_product_id", "approved_variant_id",
        "reviewer_name", "review_date", "reviewer_notes"
    ]
    approval = review[["record_key", "research_batch", "candidate_product_name", "candidate_manufacturer"]].copy()
    for col in approval_cols[4:]:
        approval[col] = ""
    approval["reviewer_decision"] = "PENDING"
    approval.to_csv(OUT_DIR / "approval_register.csv", index=False)

    report = {
        "candidate_count": int(len(review)),
        "master_product_count": int(len(products)),
        "master_variant_count": int(len(variants)),
        "exact_name_match_candidates": int((review["exact_name_match_count"] > 0).sum()),
        "exact_name_and_manufacturer_match_candidates": int((review["exact_name_and_manufacturer_match_count"] > 0).sum()),
        "exact_source_match_candidates": int((review["exact_source_match_count"] > 0).sum()),
        "fuzzy_similarity_candidates_score_ge_0_80": int((review["fuzzy_top1_score"].fillna(0) >= 0.80).sum()),
        "all_candidates_pending_manual_review": True,
        "auto_approvals": 0,
        "database_open_mode": "read-only",
        "database_modified": False,
        "outputs": [str(review_path), str(OUT_DIR / "approval_register.csv")]
    }
    report_path = REPORT_DIR / "identity_reconciliation_report.json"
    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))
    print(f"\nIdentity results: {review_path}")
    print(f"Approval register: {OUT_DIR / 'approval_register.csv'}")
    print(f"Report: {report_path}")


if __name__ == "__main__":
    main()
