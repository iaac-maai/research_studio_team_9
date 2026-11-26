"""
SpecSynth AI - ML dataset audit (read-only)
Run from the SpecSynth_AI project root with:
conda run -n arch_ai python .\06_AI_Performance_Engine\02_ML_Engine\01_Notebooks\audit_ml_dataset.py
"""
from __future__ import annotations

import json
from pathlib import Path

import pandas as pd

ROOT = Path.cwd()
MASTER = (
    ROOT
    / "04_Product_Dataset"
    / "10_Master_Database"
    / "SpecSynth_Master_Database_v1.0"
)
V15 = MASTER / "source_data" / "v1.5.0"
ML_DIR = V15 / "ml"

FILES = {
    "variants_csv": ML_DIR / "SpecSynth_ML_variants_v1.5.0.csv",
    "variants_parquet": ML_DIR / "SpecSynth_ML_variants_v1.5.0.parquet",
    "split_targets": ML_DIR / "SPLIT_AND_TARGETS_v1.5.0.json",
    "dataset_manifest": V15 / "data" / "DATASET_MANIFEST_v1.5.0.json",
    "data_quality_report": V15 / "data" / "DATA_QUALITY_REPORT_v1.5.0.json",
    "products_json": V15 / "data" / "Global_Products_v1.5.0.json",
    "variants_json": V15 / "data" / "Global_Product_Variants_v1.5.0.json",
    "sources_json": V15 / "data" / "Global_Sources_v1.5.0.json",
    "assemblies_reference_csv": ML_DIR / "SpecSynth_ML_assemblies_reference_U_v1.5.0.csv",
}

OUT = ROOT / "06_AI_Performance_Engine" / "02_ML_Engine" / "04_Evaluation" / "reports"
OUT.mkdir(parents=True, exist_ok=True)
REPORT = OUT / "dataset_audit_v1.5.0.txt"


def section(title: str, lines: list[str]) -> None:
    lines.extend(["", "=" * 88, title, "=" * 88])


def json_summary(path: Path, label: str, lines: list[str]) -> None:
    section(f"{label}: {path}", lines)
    if not path.exists():
        lines.append("FILE NOT FOUND")
        return
    try:
        obj = json.loads(path.read_text(encoding="utf-8-sig"))
    except Exception as exc:
        lines.append(f"JSON READ ERROR: {exc}")
        return
    lines.append(f"Root type: {type(obj).__name__}")
    if isinstance(obj, dict):
        lines.append(f"Top-level keys: {list(obj.keys())}")
        for key, value in obj.items():
            if isinstance(value, (dict, list)):
                lines.append(f"- {key}: {type(value).__name__}, length={len(value)}")
            else:
                lines.append(f"- {key}: {value!r}")
    elif isinstance(obj, list):
        lines.append(f"List length: {len(obj)}")
        if obj and isinstance(obj[0], dict):
            lines.append(f"First item keys: {list(obj[0].keys())}")


def dataframe_summary(path: Path, label: str, lines: list[str]) -> None:
    section(f"{label}: {path}", lines)
    if not path.exists():
        lines.append("FILE NOT FOUND")
        return
    try:
        if path.suffix.lower() == ".parquet":
            df = pd.read_parquet(path)
        else:
            df = pd.read_csv(path)
    except Exception as exc:
        lines.append(f"DATA READ ERROR: {type(exc).__name__}: {exc}")
        return

    lines.append(f"Shape: {df.shape[0]} rows x {df.shape[1]} columns")
    lines.append(f"Duplicate full rows: {int(df.duplicated().sum())}")
    lines.append("")
    lines.append("COLUMNS / TYPES / MISSING:")
    missing = df.isna().sum()
    for col in df.columns:
        lines.append(
            f"- {col} | dtype={df[col].dtype} | missing={int(missing[col])} "
            f"({missing[col] / max(len(df), 1):.1%}) | unique={df[col].nunique(dropna=True)}"
        )

    lines.append("")
    lines.append("FIRST 3 ROWS (truncated to 15 columns):")
    lines.append(df.iloc[:3, :15].to_string(index=False))

    candidate_cols = [
        c for c in df.columns
        if any(token in c.lower() for token in (
            "target", "label", "class", "split", "partition", "group",
            "provenance", "verification", "source", "conductivity",
            "thermal", "material_family", "category"
        ))
    ]
    lines.append("")
    lines.append(f"Potential target/group/provenance columns: {candidate_cols}")
    for col in candidate_cols:
        if df[col].nunique(dropna=True) <= 30:
            lines.append(f"Value counts [{col}]:")
            lines.append(df[col].value_counts(dropna=False).head(30).to_string())


def main() -> None:
    lines: list[str] = []
    section("SPECSYNTH AI - READ-ONLY DATASET AUDIT", lines)
    lines.append(f"Working directory: {ROOT}")
    lines.append("No source dataset files are modified.")

    for key in ("split_targets", "dataset_manifest", "data_quality_report"):
        json_summary(FILES[key], key, lines)

    dataframe_summary(FILES["variants_csv"], "PRIMARY PRODUCT-VARIANT ML CSV", lines)
    dataframe_summary(FILES["variants_parquet"], "PRODUCT-VARIANT ML PARQUET", lines)
    dataframe_summary(FILES["assemblies_reference_csv"], "ASSEMBLY REFERENCE CSV (SEPARATE DATASET)", lines)

    for key in ("products_json", "variants_json", "sources_json"):
        json_summary(FILES[key], key, lines)

    REPORT.write_text("\n".join(lines), encoding="utf-8")
    print("\n".join(lines))
    print(f"\n\nAUDIT REPORT SAVED TO:\n{REPORT}")


if __name__ == "__main__":
    main()
