"""JSON inference entry point - deliberately not model-connected yet."""
import argparse
import json
from pathlib import Path

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--request", required=True)
    parser.add_argument("--result", required=True)
    args = parser.parse_args()
    request = json.loads(Path(args.request).read_text(encoding="utf-8"))
    response = {
        "status": "NOT_CONFIGURED",
        "message": "Complete dataset audit, model training, validation and JSON contracts before runtime integration.",
        "request_id": request.get("request_id"),
        "results": [],
    }
    result_path = Path(args.result)
    result_path.parent.mkdir(parents=True, exist_ok=True)
    result_path.write_text(json.dumps(response, indent=2), encoding="utf-8")
    print("SpecSynth ML inference placeholder completed.")

if __name__ == "__main__":
    main()
