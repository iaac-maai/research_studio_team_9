from sklearn.ensemble import IsolationForest

def train_isolation_forest(X, contamination="auto", random_state=42):
    model = IsolationForest(n_estimators=300, contamination=contamination,
                            random_state=random_state, n_jobs=-1)
    return model.fit(X)
