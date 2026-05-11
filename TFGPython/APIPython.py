import shutil
import sys
import os
import traceback
from typing import List
from fastapi import FastAPI, HTTPException, Depends, UploadFile, File
from pydantic import BaseModel
from sqlalchemy.orm import Session

import crud
import database
from FaissTry18_04 import interactuar
from faiss_index_builder import construir_todos_los_indices_Unity, eliminar_todos_los_indices

sys.stdout.reconfigure(line_buffering=True)
UPLOAD_BASE_DIR = "uploads"
os.makedirs(UPLOAD_BASE_DIR, exist_ok=True)

app = FastAPI(title="AIGERIM AI API - V2")
database.init_db()


# ========== MODELOS ==========
class CharacterCreate(BaseModel):
    name: str
    age: str
    description: str
    epoca: str


class MessageRequest(BaseModel):
    message: str


# ========== ENDPOINTS DE ARCHIVOS Y CONTEXTO ==========

@app.get("/contexts")
def get_all_contextos():
    """
    Devuelve la lista de todos los contextos (carpetas) disponibles en el servidor.
    """
    try:
        if not os.path.exists(UPLOAD_BASE_DIR):
            return []
        folders = [f for f in os.listdir(UPLOAD_BASE_DIR) if os.path.isdir(os.path.join(UPLOAD_BASE_DIR, f))]
        return {"contextos": folders}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/context/{folder_name}/upload")
async def upload_files_to_folder(folder_name: str, files: List[UploadFile] = File(...)):
    target_path = os.path.join(UPLOAD_BASE_DIR, folder_name)
    os.makedirs(target_path, exist_ok=True)

    saved_files = []
    for file in files:
        if "text" not in file.content_type and not file.filename.endswith('.txt'):
            continue

        file_location = os.path.join(target_path, file.filename)
        with open(file_location, "wb") as buffer:
            shutil.copyfileobj(file.file, buffer)
        saved_files.append(file.filename)

    return {"status": "files_persisted", "folder": folder_name, "total": len(saved_files)}

@app.post("/context/{folder_name}/save")
def build_index_from_persisted_folder(folder_name: str, db: Session = Depends(database.get_db)):
    try:
        folder = crud.update_or_create_root_folder(db, folder_name)

        construir_todos_los_indices_Unity(folder.route)

        return {
            "status": "success",
            "message": f"Index added/updated for: {folder_name}",
            "route": folder.route
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error building index: {str(e)}")


# ========== ENDPOINTS DE PERSONAJES (JERÁRQUICOS) ==========

@app.get("/characters")
def get_characters(db: Session = Depends(database.get_db)):
    """Lista todos los personajes."""
    chars = crud.get_characters(db)
    return chars


@app.post("/characters")
def post_character(char_data: CharacterCreate, db: Session = Depends(database.get_db)):
    """Crea un nuevo personaje."""
    try:
        new_char = crud.add_character(
            db,
            name=char_data.name,
            age=char_data.age,
            description=char_data.description,
            epoca=char_data.epoca
        )
        return new_char
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.get("/characters/{char_id}")
def get_character_by_id(char_id: int, db: Session = Depends(database.get_db)):
    """Obtiene el JSON de un personaje específico."""
    char = crud.get_character_by_id(db, char_id)
    if not char:
        raise HTTPException(status_code=404, detail="Character not found")
    return char


@app.delete("/characters/{char_id}")
def delete_character(char_id: int, db: Session = Depends(database.get_db)):
    """Borra un personaje."""
    success = crud.delete_character(db, char_id)
    if not success:
        raise HTTPException(status_code=404, detail="Character not found")
    return {"status": "success", "message": f"Character {char_id} deleted"}


# ========== ENDPOINTS DE CHAT  ==========

@app.post("/characters/{char_id}/chat")
def character_chat(char_id: int, request: MessageRequest):
    try:
        # interactuar ya gestiona la lógica de recuperación RAG
        resultado = interactuar(request.message, char_id)
        return {"response": resultado}
    except Exception as e:
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(e))


@app.get("/characters/{char_id}/conversations")
def get_character_conversations(char_id: int, db: Session = Depends(database.get_db)):
    try:
        convs = crud.get_conversations(db, char_id)

        if not convs:
            return []

        return [f"User: {c.message_user}\nAssistant: {c.message_npc}" for c in convs]

    except Exception as e:
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(e))


# ========== MANTENIMIENTO ==========

@app.post("/system/reset")
def reset_system(db: Session = Depends(database.get_db)):
    try:
        crud.reset_all_tables(db)
        eliminar_todos_los_indices()

        if os.path.exists(UPLOAD_BASE_DIR):
            shutil.rmtree(UPLOAD_BASE_DIR)
        os.makedirs(UPLOAD_BASE_DIR, exist_ok=True)

        return {"status": "success", "message": "System reset complete"}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/status")
def get_status():
    """
    Endpoint de control para que Unity sepa que el servidor está listo.
    """
    print("LOG: Unity ha consultado el estado - Servidor Activo")
    return {"status": "ok"}


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=8080)