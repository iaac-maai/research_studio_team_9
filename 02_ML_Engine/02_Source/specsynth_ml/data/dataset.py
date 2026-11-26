from pathlib import Path
import pandas as pd


def project_root() -> Path:
    # dataset.py is located at:
    # 02_ML_Engine/02_Source/specsynth_ml/data/dataset.py
    # parents[3] resolves to the existing 02_ML_Engine directory.
    return Path(__file__).resolve().parents[3]


def default_dataset_path() -> Path:
    return (
        project_root()
        / "00_Data"
        / "processed"
        / "SpecSynth_ML_variants_v1.5.0.csv"
    )


def load_dataset(path=None) -> pd.DataFrame:
    dataset_path = Path(path) if path else default_dataset_path()

    if not dataset_path.exists():
        raise FileNotFoundError(
            f"Dataset not found: {dataset_path}\n"
            "Place the approved ML-ready variants CSV in "
            "02_ML_Engine/00_Data/processed/."
        )

    return pd.read_csv(dataset_path)
