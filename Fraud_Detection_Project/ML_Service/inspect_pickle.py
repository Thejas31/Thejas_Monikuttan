import os
import pickle

file_path = os.path.join(os.path.dirname(__file__), "models", "shap_explainer.pkl")

print(f"File path: {file_path}")
print(f"File exists: {os.path.exists(file_path)}")

if os.path.exists(file_path):
    print(f"File size: {os.path.getsize(file_path)} bytes")
    
    # 1. Read first 32 bytes to check the magic header
    with open(file_path, "rb") as f:
        header = f.read(32)
        print(f"Header hex: {header.hex()}")
        print(f"Header raw: {header}")
        
    # 2. Try standard pickle
    print("\n--- Testing pickle.load ---")
    try:
        with open(file_path, "rb") as f:
            obj = pickle.load(f)
            print("Successfully loaded with standard pickle!")
            print(f"Object type: {type(obj)}")
    except Exception as e:
        print(f"pickle.load failed: {e}")

    # 3. Try joblib (in case it was dumped with joblib)
    print("\n--- Testing joblib.load ---")
    try:
        import joblib
        obj = joblib.load(file_path)
        print("Successfully loaded with joblib!")
        print(f"Object type: {type(obj)}")
    except Exception as e:
        print(f"joblib.load failed: {e}")

    # 4. Try gzip compression
    print("\n--- Testing gzip decompression ---")
    try:
        import gzip
        with gzip.open(file_path, "rb") as f:
            obj = pickle.load(f)
            print("Successfully loaded with gzip + pickle!")
            print(f"Object type: {type(obj)}")
    except Exception as e:
        print(f"gzip failed: {e}")
