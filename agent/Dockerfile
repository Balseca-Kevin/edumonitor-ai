# Servicio de agente (FastAPI + LangGraph). Solo lo llama la API por la red interna (http://agent:8000); no publica puerto.
# Habla con la API (BACKEND_URL) y con Ollama (OLLAMA_BASE_URL). Nunca lee la base ni conoce el token de Moodle.

FROM python:3.11-slim
WORKDIR /app

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY app ./app

EXPOSE 8000
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
