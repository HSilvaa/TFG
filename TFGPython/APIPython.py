from fastapi import FastAPI, Body
from pydantic import BaseModel
from typing import List
from FaissTry18_04 import obtener_contexto_mas_relevante, interactuar, resumir, buscar_resumen
from faiss_index_builder import construir_todos_los_indices_Unity, eliminar_todos_los_indices
import sys
sys.stdout.reconfigure(line_buffering=True)

app = FastAPI()
#uvicorn APIPython:app --reload
class QueryText(BaseModel):
    text: str

class ChatMessage(BaseModel):
    role: str
    content: str

class HistorialText(BaseModel):
    historial: str
    characterName: str
class MessageRequest(BaseModel):
    message: str
    epoca: str
    description: str
    #resumen: str
    name: str
    age:str
    id: int


@app.get("/status")
def get_status():
    print("PYTHON ESCRIBE COSAS")
    return {"status": "ok"}

@app.post("/GetEpoca")
def get_epoca(query: QueryText):
    print("EpocaMethod")
    try:
        epoca = obtener_contexto_mas_relevante(query.text)
        return {"epoca": epoca}
    except Exception as e:
        print("Error:", e)
        return {"error": "Época no encontrada"}

@app.post("/send")
def sendMessage(request: MessageRequest):
    try:
        #print(f"Interactuando... {request.message}, {request.epoca}, {request.description}, {request.resumen},{request.id}")
        resultado = interactuar(request.message, request.epoca, request.description, request.id, request.name, request.age)
        return {"response": resultado}

    except Exception as e:
        import traceback
        traceback.print_exc()
        return {"error": str(e)}


@app.post("/resumir")
def resumir_endpoint(historial: HistorialText):
    try:
        return { "resumen": resumir(historial.historial, historial.characterName)}
    except Exception as e:
        import traceback
        traceback.print_exc()
        return {"error": str(e)}

@app.post("/CrearContexto")
def create_contexto(query: QueryText):
    try:
        eliminar_todos_los_indices()
        epoca = construir_todos_los_indices_Unity(query.text)
        return {"contextCreated": "True"}
    except Exception as e:
        print("Error:", e)
        return {"error": "False"}
