# SpecSynth AI - Machine Learning Engine

Product-level ML research and runtime components. This is separate from the existing ThermalEngine and assembly-optimisation workflows.

## Capabilities
1. Anomaly detection - Isolation Forest
2. Material clustering - K-Means
3. Material classification - Random Forest Classifier
4. Thermal prediction - Random Forest Regressor

## Structure
- `00_Data`: local datasets and metadata.
- `01_Notebooks`: research, exploration, training and validation.
- `02_Source/specsynth_ml`: reusable Python package shared by notebooks and runtime.
- `03_Models`: saved model artifacts, grouped by capability.
- `04_Evaluation`: metrics, reports and figures.
- `05_Inference`: JSON inference entry point and request/response examples.
- `06_Integration`: C#/Python JSON contracts.
- `07_Logs`: local logs.

## Governance
Never include the target column among input features. Keep product identity, source,
verification status and provenance available for audit. Split related variants by
product/group where appropriate to prevent leakage. Do not present deterministic
ThermalEngine calculations as measured outcomes. Synthetic or surrogate records
must not be silently mixed into empirical training.

## Setup
Install dependencies with `python -m pip install -r requirements.txt`, then run
`python -m pip install -e .`. Confirm actual dataset paths, feature names, targets,
units and split metadata before training.
