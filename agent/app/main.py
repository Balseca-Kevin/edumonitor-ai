from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from app.graph import app_graph

app = FastAPI(title="EduMonitor AI - Servicio de Agente", version="0.1.0")

class ChatRequest(BaseModel):
    message: str

class ChatResponse(BaseModel):
    response: str

@app.post("/chat", response_model=ChatResponse)
async def chat_endpoint(request: ChatRequest):
    try:
        inputs = {"messages": [{"role": "user", "content": request.message}], "iterations": 0}
        result = app_graph.invoke(inputs)
        last_message = result["messages"][-1]
        content = last_message.get("content", "") if isinstance(last_message, dict) else last_message.content
        return ChatResponse(response=content)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))