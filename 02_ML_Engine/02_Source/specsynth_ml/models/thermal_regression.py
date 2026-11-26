from sklearn.ensemble import RandomForestRegressor
from sklearn.impute import SimpleImputer
from sklearn.pipeline import Pipeline

def build_thermal_regressor(random_state=42):
    """Baseline only; exclude target leakage before fitting."""
    return Pipeline([
        ("imputer", SimpleImputer(strategy="median")),
        ("model", RandomForestRegressor(n_estimators=400, random_state=random_state,
                                        min_samples_leaf=2, n_jobs=-1)),
    ])
