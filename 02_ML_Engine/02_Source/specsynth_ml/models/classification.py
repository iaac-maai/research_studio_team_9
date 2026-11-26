from sklearn.ensemble import RandomForestClassifier
from sklearn.impute import SimpleImputer
from sklearn.pipeline import Pipeline

def build_material_classifier(random_state=42):
    """Baseline only; final feature handling follows the dataset audit."""
    return Pipeline([
        ("imputer", SimpleImputer(strategy="median")),
        ("model", RandomForestClassifier(n_estimators=400, random_state=random_state,
                                         class_weight="balanced", n_jobs=-1)),
    ])
