from pathlib import Path
import pandas as pd

def load_table(path: str | Path) -> pd.DataFrame:
    """Load CSV or Parquet without changing source values."""
    path = Path(path)
    if not path.exists():
        raise FileNotFoundError(f"Dataset not found: {path}")
    if path.suffix.lower() == ".csv":
        return pd.read_csv(path)
    if path.suffix.lower() in {".parquet", ".pq"}:
        return pd.read_parquet(path)
    raise ValueError(f"Unsupported format: {path.suffix}")
