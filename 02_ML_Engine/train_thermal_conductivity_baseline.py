"""
SpecSynth thermal conductivity exploratory baseline training.
Run from 02_ML_Engine with:
python train_thermal_conductivity_baseline.py --dataset 00_Data/processed/thermal_conductivity/thermal_conductivity_modeling_view_v1.6.0.csv
"""
import argparse, json
from pathlib import Path
import joblib
import pandas as pd
from sklearn.compose import ColumnTransformer
from sklearn.preprocessing import OneHotEncoder
from sklearn.impute import SimpleImputer
from sklearn.pipeline import Pipeline
from sklearn.ensemble import RandomForestRegressor, ExtraTreesRegressor
from sklearn.metrics import mean_absolute_error, mean_squared_error, r2_score

NUM = ['thickness_mm','density_kg_m3','specific_heat_J_kgK','vapour_resistance_factor_mu',
       'compressive_strength_kPa','tensile_strength_perpendicular_kPa']
CAT = ['material_category_group','material_family','category','manufacturer','market','lambda_level',
       'reaction_to_fire','euroclass_main']
TARGET = 'target_thermal_conductivity_W_mK'

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('--dataset',required=True)
    ap.add_argument('--output',default='.')
    args=ap.parse_args()
    df=pd.read_csv(args.dataset)
    df=df[df[TARGET].notna()].copy()
    splits={s:df[df.recommended_split==s] for s in ['train','validation','test']}
    prep=ColumnTransformer([
      ('num',Pipeline([('impute',SimpleImputer(strategy='median',add_indicator=True))]),NUM),
      ('cat',Pipeline([('impute',SimpleImputer(strategy='most_frequent')),
                       ('onehot',OneHotEncoder(handle_unknown='ignore'))]),CAT)])
    candidates={
      'RandomForest':RandomForestRegressor(n_estimators=500,min_samples_leaf=2,max_features=.8,random_state=42,n_jobs=-1),
      'ExtraTrees':ExtraTreesRegressor(n_estimators=500,min_samples_leaf=2,max_features=.8,random_state=42,n_jobs=-1)}
    scores={}
    for name,est in candidates.items():
      pipe=Pipeline([('preprocess',prep),('model',est)])
      pipe.fit(splits['train'][NUM+CAT],splits['train'][TARGET])
      p=pipe.predict(splits['validation'][NUM+CAT])
      scores[name]={'MAE':mean_absolute_error(splits['validation'][TARGET],p),
                    'RMSE':mean_squared_error(splits['validation'][TARGET],p)**.5,
                    'R2':r2_score(splits['validation'][TARGET],p)}
    selected=min(scores,key=lambda k:scores[k]['MAE'])
    Est=candidates[selected].__class__
    final=Pipeline([('preprocess',prep),('model',Est(n_estimators=500,min_samples_leaf=2,
                  max_features=.8,random_state=42,n_jobs=-1))])
    tv=pd.concat([splits['train'],splits['validation']])
    final.fit(tv[NUM+CAT],tv[TARGET])
    p=final.predict(splits['test'][NUM+CAT])
    report={'selected_model':selected,'validation':scores,
            'test':{'MAE':mean_absolute_error(splits['test'][TARGET],p),
                    'RMSE':mean_squared_error(splits['test'][TARGET],p)**.5,
                    'R2':r2_score(splits['test'][TARGET],p)},
            'note':'Exploratory baseline. Do not interpret as production validation.'}
    out=Path(args.output); out.mkdir(parents=True,exist_ok=True)
    joblib.dump(final,out/'thermal_conductivity_model.joblib')
    (out/'training_metrics.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report,indent=2))
if __name__=='__main__': main()
