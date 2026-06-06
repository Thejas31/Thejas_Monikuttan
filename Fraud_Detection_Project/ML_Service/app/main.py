import os
import pickle
import numpy as np
import pandas as pd
# pyrefly: ignore [missing-import]
from fastapi import FastAPI, HTTPException
from typing import Dict, Any

# Define the expected feature list in exact order in case feature_list.pkl is missing
EXPECTED_FEATURES = [
    "TransactionAmt", "card1", "card2", "card3", "card5",
    "addr1", "addr2", "dist1", "dist2",
    "C1", "C2", "C3", "C4", "C5",
    "D1", "D2", "D3", "D4", "D5",
    "id_01", "id_02", "id_03", "id_04", "id_05", "id_06",
    "id_11", "id_13", "DeviceType", "DeviceInfo",
    "P_emaildomain", "R_emaildomain"
]

app = FastAPI(
    title="Donation Fraud Detection ML Inference Service",
    description="Real-time XGBoost inference with SHAP explanations for fraud detection",
    version="1.0.0"
)

# Global variables for model artifacts
model = None
feature_list = None
shap_explainer = None

@app.on_event("startup")
def load_artifacts():
    """Load model, feature list, and SHAP explainer once at startup to optimize latency."""
    global model, feature_list, shap_explainer
    
    models_dir = os.path.join(os.path.dirname(os.path.dirname(__file__)), "models")
    model_path = os.path.join(models_dir, "fraud_model.pkl")
    feature_list_path = os.path.join(models_dir, "feature_list.pkl")
    shap_explainer_path = os.path.join(models_dir, "shap_explainer.pkl")
    
    try:
        # 1. Load XGBoost Model
        if os.path.exists(model_path):
            with open(model_path, "rb") as f:
                model = pickle.load(f)
            print("Successfully loaded fraud_model.pkl")
        else:
            raise FileNotFoundError(f"Model file not found at {model_path}")
            
        # 2. Load Feature List
        if os.path.exists(feature_list_path):
            with open(feature_list_path, "rb") as f:
                feature_list = pickle.load(f)
            print("Successfully loaded feature_list.pkl")
        else:
            feature_list = EXPECTED_FEATURES
            print("feature_list.pkl not found. Falling back to default list.")
            
        # 3. Load SHAP Explainer
        if os.path.exists(shap_explainer_path):
            import joblib
            shap_explainer = joblib.load(shap_explainer_path)
            print("Successfully loaded shap_explainer.pkl")
        else:
            # pyrefly: ignore [missing-import]
            import shap
            if model is not None:
                shap_explainer = shap.TreeExplainer(model)
                print("Generated TreeExplainer from the loaded model.")
    except Exception as e:
        print(f"Error loading model artifacts: {e}")

@app.get("/health")
def health_check():
    """Health check endpoint to verify service and model status."""
    return {
        "status": "healthy",
        "model_loaded": model is not None,
        "explainer_loaded": shap_explainer is not None
    }

@app.post("/predict")
def predict(payload: Dict[str, Any]):
    """
    Accepts raw feature dictionary, processes features in exact order,
    and returns fraud probability, risk score, fraud flags, and top SHAP features.
    """
    if model is None or shap_explainer is None:
        raise HTTPException(
            status_code=503, 
            detail="Service unavailable: Model artifacts are not loaded on startup."
        )
        
    # Process features in correct order, replacing missing values with -999
    processed_features = []
    features_to_use = feature_list if feature_list is not None else EXPECTED_FEATURES
    for feat in features_to_use:
        val = payload.get(feat, -999)
        # Convert nulls or empty strings to -999
        if val is None or val == "":
            val = -999
        processed_features.append(float(val))
        
    # Convert to 2D DataFrame (required by XGBoost with feature names)
    X_df = pd.DataFrame([processed_features], columns=features_to_use)
    
    try:
        # Get probability of fraud (class 1)
        prob = float(model.predict_proba(X_df)[0][1])
    except Exception as e:
        raise HTTPException(
            status_code=500, 
            detail=f"Inference execution failed: {str(e)}"
        )
        
    # Calculate Risk Score (probability * 100)
    risk_score = int(round(prob * 100))
    is_fraud = prob >= 0.7  # Selected threshold = 0.7
    
    # Calculate SHAP values
    try:
        shap_res = shap_explainer.shap_values(X_df)
        
        # Handle different output shapes of TreeExplainer (list vs single array)
        if isinstance(shap_res, list):
            # For binary classification, index 1 is class 1 (Fraud)
            shap_vals = shap_res[1][0]
        else:
            if len(shap_res.shape) == 3: # (samples, classes, features)
                shap_vals = shap_res[0][1]
            elif len(shap_res.shape) == 2: # (samples, features)
                shap_vals = shap_res[0]
            else:
                shap_vals = shap_res
    except Exception as e:
        # Fallback if SHAP computation fails
        shap_vals = [0.0] * len(features_to_use)
        
    # Map features to SHAP values
    shap_dict = {feat: float(val) for feat, val in zip(features_to_use, shap_vals)}
    
    # Rank features by absolute SHAP contribution (descending)
    sorted_features = sorted(shap_dict.items(), key=lambda x: abs(x[1]), reverse=True)
    top_features = {feat: val for feat, val in sorted_features[:5]}
    
    return {
        "probability": prob,
        "riskScore": risk_score,
        "isFraud": is_fraud,
        "topFeatures": top_features
    }
