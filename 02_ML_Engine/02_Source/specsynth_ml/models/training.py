import json
import warnings
from pathlib import Path

import joblib
import pandas as pd
from sklearn.compose import ColumnTransformer
from sklearn.ensemble import ExtraTreesRegressor, RandomForestRegressor, GradientBoostingRegressor
from sklearn.impute import SimpleImputer
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import OneHotEncoder

from ..data.dataset import load_dataset, project_root
from ..evaluation.metrics import regression_metrics

CONFIG_PATH = Path(__file__).resolve().parents[1] / "utils" / "thermal_conductivity_config.json"

def _one_hot():
    try:
        return OneHotEncoder(handle_unknown="ignore", sparse_output=False)
    except TypeError:
        return OneHotEncoder(handle_unknown="ignore", sparse=False)

def _build_pipeline(estimator, numeric, categorical):
    numeric_pipe = Pipeline([("imputer", SimpleImputer(strategy="median"))])
    categorical_pipe = Pipeline([
        ("imputer", SimpleImputer(strategy="constant", fill_value="Unknown")),
        ("onehot", _one_hot())
    ])
    prep = ColumnTransformer([
        ("numeric", numeric_pipe, numeric),
        ("categorical", categorical_pipe, categorical)
    ], remainder="drop")
    return Pipeline([("preprocessor", prep), ("model", estimator)])

def train(dataset_path=None):
    config = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
    df = load_dataset(dataset_path)

    target = config["target"]
    split_col = config["split_column"]
    group_col = config["group_column"]
    numeric = config["numeric_features"]
    categorical = config["categorical_features"]
    features = numeric + categorical

    required = [target, split_col, group_col] + features
    missing = [col for col in required if col not in df.columns]
    if missing:
        raise ValueError(f"Dataset is missing required columns: {missing}")

    df[target] = pd.to_numeric(df[target], errors="coerce")
    df = df.loc[df[target].notna() & df[split_col].notna() & df[group_col].notna()].copy()

    split_group_counts = df.groupby(group_col)[split_col].nunique()
    bad_groups = int((split_group_counts > 1).sum())
    if bad_groups:
        raise RuntimeError(
            f"{bad_groups} split groups occur in multiple partitions. "
            "Training stopped to prevent group leakage."
        )

    split_values = df[split_col].astype(str).str.lower()
    train_df = df[split_values == "train"]
    val_df = df[split_values.isin(["validation", "valid", "val"])]
    test_df = df[split_values == "test"]
    if min(len(train_df), len(val_df), len(test_df)) == 0:
        raise ValueError(
            f"Expected train/validation/test partitions; got "
            f"{len(train_df)}/{len(val_df)}/{len(test_df)}."
        )

    X_train, y_train = train_df[features], train_df[target]
    X_val, y_val = val_df[features], val_df[target]
    X_test, y_test = test_df[features], test_df[target]

    candidates = {
        "RandomForest": RandomForestRegressor(
            n_estimators=400, min_samples_leaf=2, max_features=0.9,
            random_state=42, n_jobs=-1
        ),
        "ExtraTrees": ExtraTreesRegressor(
            n_estimators=400, min_samples_leaf=2, max_features=0.9,
            random_state=42, n_jobs=-1
        ),
        "GradientBoosting": GradientBoostingRegressor(
            n_estimators=200, learning_rate=0.04, max_depth=2,
            loss="huber", random_state=42
        )
    }

    fitted = {}
    validation_rows = []
    print(f"Rows: {len(df)} | train={len(train_df)} | validation={len(val_df)} | test={len(test_df)}")
    print(f"Independent split groups: {df[group_col].nunique()}")
    for name, estimator in candidates.items():
        print(f"\nTraining {name}...")
        pipeline = _build_pipeline(estimator, numeric, categorical)
        with warnings.catch_warnings():
            warnings.simplefilter("ignore")
            pipeline.fit(X_train, y_train)
        val_pred = pipeline.predict(X_val)
        score = regression_metrics(y_val, val_pred)
        fitted[name] = pipeline
        validation_rows.append({"model": name, **score})
        print(f"Validation MAE={score['mae']:.6f}; RMSE={score['rmse']:.6f}; R2={score['r2']}")

    comparison = pd.DataFrame(validation_rows).sort_values("mae").reset_index(drop=True)
    best_name = str(comparison.iloc[0]["model"])
    best_model = fitted[best_name]

    # The test set is held out from model selection and is evaluated only once here.
    test_pred = best_model.predict(X_test)
    test_score = regression_metrics(y_test, test_pred)

    root = project_root()
    model_dir = root / "03_Models" / "thermal_prediction"
    eval_dir = root / "04_Evaluation" / "reports"
    model_dir.mkdir(parents=True, exist_ok=True)
    eval_dir.mkdir(parents=True, exist_ok=True)

    model_path = model_dir / "thermal_conductivity_model.joblib"
    metadata_path = model_dir / "thermal_conductivity_model_metadata.json"
    joblib.dump(best_model, model_path)

    metadata = {
        "model_name": best_name,
        "target": target,
        "features": features,
        "numeric_features": numeric,
        "categorical_features": categorical,
        "dataset_file": Path(dataset_path).name if dataset_path else "SpecSynth_ML_variants_v1.5.0.csv",
        "split_column": split_col,
        "group_column": group_col,
        "training_rows": int(len(train_df)),
        "validation_rows": int(len(val_df)),
        "test_rows": int(len(test_df)),
        "split_group_count": int(df[group_col].nunique()),
        "excluded_leakage_columns": config["excluded_leakage_columns"],
        "warning": "Research prototype only. Do not replace manufacturer-verified declared values."
    }
    metadata_path.write_text(json.dumps(metadata, indent=2), encoding="utf-8")
    comparison.to_csv(eval_dir / "thermal_conductivity_validation_comparison.csv", index=False)

    prediction_cols = [c for c in ["record_id", "product_id", "variant_id", group_col, target] if c in test_df]
    test_output = test_df[prediction_cols].copy()
    test_output["predicted_thermal_conductivity_W_mK"] = test_pred
    test_output["absolute_error_W_mK"] = (test_output[target] - test_output["predicted_thermal_conductivity_W_mK"]).abs()
    test_output.to_csv(eval_dir / "thermal_conductivity_test_predictions.csv", index=False)

    report = {
        "selected_model": best_name,
        "selection_rule": "Lowest validation MAE",
        "validation_comparison": validation_rows,
        "held_out_test_metrics": test_score,
        "group_leakage_check": {"groups_spanning_multiple_splits": bad_groups, "passed": True},
        "model_path": str(model_path)
    }
    (eval_dir / "thermal_conductivity_metrics.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print("\nSelected model:", best_name)
    print("Held-out test metrics:", json.dumps(test_score, indent=2))
    print("Saved model:", model_path)
    print("Saved reports:", eval_dir)
    return report

if __name__ == "__main__":
    train()
