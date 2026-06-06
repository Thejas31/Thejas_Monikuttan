import requests
import json

# Define URL of the running FastAPI service
URL = "http://localhost:8000/predict"

# Sample 31-feature payload corresponding to the expected features
sample_payload = {
    "TransactionAmt": 1500.0,
    "card1": 12345.0,
    "card2": 222.0,
    "card3": 150.0,
    "card5": 100.0,
    "addr1": 456.0,
    "addr2": 840.0,
    "dist1": 12.5,
    "dist2": -999.0,
    "C1": 1.0,
    "C2": 1.0,
    "C3": 1.0,
    "C4": -999.0,
    "C5": -999.0,
    "D1": 5.2,
    "D2": -999.0,
    "D3": -999.0,
    "D4": -999.0,
    "D5": -999.0,
    "id_01": 0.0,
    "id_02": 0.0,
    "id_03": -999.0,
    "id_04": -999.0,
    "id_05": -999.0,
    "id_06": -999.0,
    "id_11": 100.0,
    "id_13": -999.0,
    "DeviceType": 1.0,
    "DeviceInfo": 1.0,
    "P_emaildomain": 12.0,
    "R_emaildomain": 12.0
}

def test_prediction():
    print("Sending request to FastAPI service...")
    try:
        response = requests.post(URL, json=sample_payload)
        if response.status_code == 200:
            print("Response Status: 200 OK")
            print("Response Payload:")
            print(json.dumps(response.json(), indent=2))
        else:
            print(f"Error Response Status: {response.status_code}")
            print(response.text)
    except Exception as e:
        print(f"Failed to connect to FastAPI service: {e}")
        print("Make sure uvicorn is running: uvicorn app.main:app --reload --port 8000")

if __name__ == "__main__":
    test_prediction()
