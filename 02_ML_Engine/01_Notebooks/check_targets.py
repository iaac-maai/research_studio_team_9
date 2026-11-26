"""
SpecSynth AI — Dataset Target and Quality Validation
Dataset Version: v1.5.0

Purpose:
1. Read the product ML dataset.
2. Inspect target distributions.
3. Verify predefined train/validation/test splits.
4. Check thermal conductivity target leakage.
5. Review material family and category distributions.
6. Inspect dataset quality summary and known limitations.

This script is read-only. It does not modify the source dataset.
"""

import json
from pathlib import Path

import pandas as pd


# ============================================================
# 1. PROJECT AND DATASET PATHS
# ============================================================

# Locate the SpecSynth_AI project root automatically.
# check_targets.py is inside:
# SpecSynth_AI/06_AI_Performance_Engine/02_ML_Engine/01_Notebooks/

PROJECT_ROOT = Path(__file__).resolve().parents[3]

DATASET_ROOT = (
    PROJECT_ROOT
    / "04_Product_Dataset"
    / "10_Master_Database"
    / "SpecSynth_Master_Database_v1.0"
    / "source_data"
    / "v1.5.0"
)

CSV_PATH = (
    DATASET_ROOT
    / "ml"
    / "SpecSynth_ML_variants_v1.5.0.csv"
)

SPLIT_PATH = (
    DATASET_ROOT
    / "ml"
    / "SPLIT_AND_TARGETS_v1.5.0.json"
)

QUALITY_PATH = (
    DATASET_ROOT
    / "data"
    / "DATA_QUALITY_REPORT_v1.5.0.json"
)


# ============================================================
# 2. CHECK FILE AVAILABILITY
# ============================================================

print("\n" + "=" * 65)
print("SPECSYNTH AI — DATASET VALIDATION")
print("=" * 65)

print("\nPROJECT ROOT:")
print(PROJECT_ROOT)

print("\nDATASET ROOT:")
print(DATASET_ROOT)

required_files = {
    "Product ML CSV": CSV_PATH,
    "Split metadata": SPLIT_PATH,
    "Data quality report": QUALITY_PATH,
}

for name, path in required_files.items():
    if path.exists():
        print(f"[FOUND] {name}: {path}")
    else:
        raise FileNotFoundError(
            f"\n[MISSING] {name}\nExpected location: {path}"
        )


# ============================================================
# 3. LOAD DATA
# ============================================================

print("\n" + "=" * 65)
print("LOADING DATA")
print("=" * 65)

df = pd.read_csv(CSV_PATH)

with open(SPLIT_PATH, "r", encoding="utf-8-sig") as file:
    split = json.load(file)

with open(QUALITY_PATH, "r", encoding="utf-8-sig") as file:
    quality = json.load(file)

print("\nProduct ML dataset loaded successfully.")
print("Rows:", len(df))
print("Columns:", len(df.columns))


# ============================================================
# 4. TARGET COUNTS
# ============================================================

print("\n" + "=" * 65)
print("TARGET COUNTS")
print("=" * 65)

target_counts = split.get("target_row_counts", {})

if target_counts:
    print(json.dumps(target_counts, indent=4))
else:
    print("No target_row_counts field found in split metadata.")


# ============================================================
# 5. TRAIN / VALIDATION / TEST SPLITS
# ============================================================

print("\n" + "=" * 65)
print("SPLIT COUNTS")
print("=" * 65)

print("\nROW COUNTS:")

print(
    json.dumps(
        split.get("counts_rows", {}),
        indent=4
    )
)

print("\nGROUP COUNTS:")

print(
    json.dumps(
        split.get("counts_groups", {}),
        indent=4
    )
)

print("\nSPLIT METHOD:")

print(
    split.get(
        "split_method",
        split.get("method", "Not specified")
    )
)

print("\nSYNTHETIC RECORDS IN SPLITS:")

print(
    split.get(
        "synthetic_records_in_any_split",
        "Not specified"
    )
)

print("\nLEAKAGE CHECK:")

print(
    split.get(
        "leakage_check_groups_in_multiple_splits",
        "Not specified"
    )
)


# ============================================================
# 6. THERMAL CONDUCTIVITY TARGET LEAKAGE
# ============================================================

print("\n" + "=" * 65)
print("CONDUCTIVITY LEAKAGE CHECK")
print("=" * 65)

input_column = "thermal_conductivity_W_mK"
target_column = "target_thermal_conductivity_W_mK"

if input_column in df.columns and target_column in df.columns:

    input_values = pd.to_numeric(
        df[input_column],
        errors="coerce"
    )

    target_values = pd.to_numeric(
        df[target_column],
        errors="coerce"
    )

    comparable = (
        input_values.notna()
        & target_values.notna()
    )

    identical = (
        input_values[comparable]
        == target_values[comparable]
    )

    different = (
        input_values[comparable]
        != target_values[comparable]
    )

    print("Comparable records:", int(comparable.sum()))
    print("Identical values:", int(identical.sum()))
    print("Different values:", int(different.sum()))

    if different.sum() == 0 and comparable.sum() > 0:
        print(
            "\nWARNING: Input conductivity and target conductivity "
            "are identical for all comparable records."
        )
        print(
            "Using the input column to predict the target would "
            "cause direct target leakage."
        )

    elif different.sum() > 0:
        print(
            "\nInput and target conductivity contain different values."
        )
        print(
            "Further investigation is required before selecting "
            "the prediction features."
        )

    else:
        print(
            "\nNo comparable conductivity values were found."
        )

else:
    print("Required conductivity columns were not found.")


# ============================================================
# 7. MATERIAL FAMILY DISTRIBUTION
# ============================================================

print("\n" + "=" * 65)
print("MATERIAL FAMILY DISTRIBUTION")
print("=" * 65)

if "material_family" in df.columns:

    print(
        df["material_family"]
        .value_counts(dropna=False)
        .to_string()
    )

else:
    print("material_family column not found.")


# ============================================================
# 8. MATERIAL CATEGORY DISTRIBUTION
# ============================================================

print("\n" + "=" * 65)
print("MATERIAL CATEGORY DISTRIBUTION")
print("=" * 65)

if "material_category_group" in df.columns:

    print(
        df["material_category_group"]
        .value_counts(dropna=False)
        .to_string()
    )

else:
    print("material_category_group column not found.")


# ============================================================
# 9. DATA QUALITY SUMMARY
# ============================================================

print("\n" + "=" * 65)
print("DATA QUALITY SUMMARY")
print("=" * 65)

summary = quality.get("summary", {})

print(
    json.dumps(
        summary,
        indent=4
    )
)


# ============================================================
# 10. KNOWN LIMITATIONS
# ============================================================

print("\n" + "=" * 65)
print("KNOWN LIMITATIONS")
print("=" * 65)

limitations = quality.get("known_limitations", [])

if limitations:

    for index, item in enumerate(limitations, start=1):
        print(f"{index}. {item}")

else:
    print("No known_limitations field or no limitations listed.")


# ============================================================
# 11. DATASET COLUMN INVENTORY
# ============================================================

print("\n" + "=" * 65)
print("DATASET COLUMN INVENTORY")
print("=" * 65)

for index, column in enumerate(df.columns, start=1):

    missing = int(df[column].isna().sum())
    missing_percent = (missing / len(df)) * 100

    print(
        f"{index:02d}. {column:<45} "
        f"Missing: {missing:>5} "
        f"({missing_percent:>5.1f}%)"
    )


# ============================================================
# 12. COMPLETION
# ============================================================

print("\n" + "=" * 65)
print("DATASET VALIDATION COMPLETED")
print("=" * 65)

print("\nDataset:", CSV_PATH.name)
print("Records:", len(df))
print("Features:", len(df.columns))

print("\nNo source dataset files were modified.")