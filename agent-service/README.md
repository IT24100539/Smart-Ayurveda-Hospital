# Internal-only FastAPI + LangGraph service.
# Bind to 127.0.0.1. Never publish this port.

python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
python run.py
