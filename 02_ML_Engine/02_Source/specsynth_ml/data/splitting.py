from sklearn.model_selection import GroupShuffleSplit

def grouped_train_test_split(X, y, groups, test_size=0.2, random_state=42):
    """Split by group to reduce leakage between related product variants."""
    splitter = GroupShuffleSplit(n_splits=1, test_size=test_size, random_state=random_state)
    train_idx, test_idx = next(splitter.split(X, y, groups=groups))
    return train_idx, test_idx
