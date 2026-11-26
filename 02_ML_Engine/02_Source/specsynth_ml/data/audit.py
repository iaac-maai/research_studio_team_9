import json
from pathlib import Path
from .dataset import load_dataset, project_root

TARGET = "target_thermal_conductivity_W_mK"
SPLIT = "recommended_split"
GROUP = "split_group_id"

def run_audit(dataset_path=None):
    df = load_dataset(dataset_path)
    missing = [c for c in (TARGET, SPLIT, GROUP) if c not in df.columns]
    if missing:
        raise ValueError(f"Required columns missing: {missing}")

    labelled = df.loc[df[TARGET].notna() & df[SPLIT].notna() & df[GROUP].notna()].copy()
    group_counts = labelled.groupby(GROUP)[SPLIT].nunique()
    multi_split = int((group_counts > 1).sum())

    report = {
        "dataset_path": str(dataset_path or (project_root() / "00_Data/processed/SpecSynth_ML_variants_v1.5.0.csv")),
        "rows_total": int(len(df)),
        "columns": int(len(df.columns)),
        "labelled_rows": int(len(labelled)),
        "target": TARGET,
        "target_min": float(labelled[TARGET].min()),
        "target_max": float(labelled[TARGET].max()),
        "target_mean": float(labelled[TARGET].mean()),
        "split_counts": {str(k): int(v) for k, v in labelled[SPLIT].value_counts().items()},
        "unique_products": int(df["product_id"].nunique()) if "product_id" in df else None,
        "unique_split_groups": int(labelled[GROUP].nunique()),
        "groups_in_multiple_splits": multi_split,
        "data_status_counts": {str(k): int(v) for k, v in df["data_status"].value_counts(dropna=False).items()} if "data_status" in df else {}
    }

    out_dir = project_root() / "04_Evaluation" / "reports"
    out_dir.mkdir(parents=True, exist_ok=True)
    out_path = out_dir / "thermal_conductivity_dataset_audit.json"
    out_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))
    print(f"\nAudit report: {out_path}")
    if multi_split:
        raise RuntimeError("Group leakage check failed: one or more split groups occur in multiple partitions.")
    return report

if __name__ == "__main__":
    run_audit()
