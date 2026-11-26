import argparse
import json
from pathlib import Path
import joblib
import pandas as pd

def main():
    parser = argparse.ArgumentParser(description="SpecSynth AI thermal conductivity prediction")
    parser.add_argument("--input", required=True, help="CSV with material feature columns")
    parser.add_argument("--output", required=True, help="Output CSV path")
    args = parser.parse_args()

    engine_root = Path(__file__).resolve().parents[1]
    model_dir = engine_root / "03_Models" / "thermal_prediction"
    model_path = model_dir / "thermal_conductivity_model.joblib"
    metadata_path = model_dir / "thermal_conductivity_model_metadata.json"
    if not model_path.exists() or not metadata_path.exists():
        raise FileNotFoundError("Model not found. Run the training module first.")

    data = pd.read_csv(args.input)
    metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
    features = metadata["features"]
    for column in features:
        if column not in data.columns:
            data[column] = pd.NA

    model = joblib.load(model_path)
    data["predicted_thermal_conductivity_W_mK"] = model.predict(data[features])
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    data.to_csv(output, index=False)
    print(f"Predictions saved: {output.resolve()}")

if __name__ == "__main__":
    main()
