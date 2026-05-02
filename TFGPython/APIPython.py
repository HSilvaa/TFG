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
UPLOAD_DIR = "uploads/context_files"
os.makedirs(UPLOAD_DIR, exist_ok=True)

app = FastAPI(title="AIGERIM AI API - V2")
database.init_db()


# ========== MODELOS ==========
class CharacterCreate(BaseModel):
    Name: str
    Age: str
    Description: str
    Epoca: str


class MessageRequest(BaseModel):
    message: str


# ========== ENDPOINTS DE ARCHIVOS Y CONTEXTO ==========

# --- ENDPOINT 1: SUBIR Y PERSISTIR ---
@app.post("/context/{folder_name}/upload")
async def upload_files_to_folder(folder_name: str, files: List[UploadFile] = File(...)):
    """
    Recibe archivos y los guarda en una subcarpeta dentro de 'uploads/'.
    Solo acepta archivos de texto (mimetype text/plain).
    """
    target_path = os.path.join("uploads", folder_name)
    os.makedirs(target_path, exist_ok=True)

    saved_files = []
    for file in files:
        # Validación de tipo de contenido
        if file.content_type != "text/plain":
            continue

        file_location = os.path.join(target_path, file.filename)
        with open(file_location, "wb") as buffer:
            shutil.copyfileobj(file.file, buffer)
        saved_files.append(file.filename)

    return {"status": "files_persisted", "folder": folder_name, "total": len(saved_files)}


# --- ENDPOINT 2: CONSTRUIR ÍNDICE (SAVE) ---
@app.post("/context/{folder_name}/save")
def build_index_from_persisted_folder(folder_name: str, db: Session = Depends(database.get_db)):
    """
    Toma los archivos ya persistidos en la carpeta especificada y construye FAISS.
    """
    try:
        # 1. Registrar en la DB que esta es la carpeta activa
        folder = crud.update_or_create_root_folder(db, folder_name)

        # 2. Construir índices usando la ruta interna generada
        eliminar_todos_los_indices()
        construir_todos_los_indices_Unity(folder.route)

        return {
            "status": "success",
            "message": f"Index created from folder: {folder_name}",
            "files_processed_from": folder.route
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
            name=char_data.Name,
            age=char_data.Age,
            description=char_data.Description,
            epoca=char_data.Epoca
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


# ========== ENDPOINTS DE CHAT (RECURSOS HIJOS) ==========

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
    convs = crud.get_conversations(db, char_id)
    return [c.Historial for c in convs]


# ========== MANTENIMIENTO ==========

@app.post("/system/reset")
def reset_database(db: Session = Depends(database.get_db)):
    """Borra todo (DB y FAISS)."""
    try:
        crud.reset_all_tables(db)
        eliminar_todos_los_indices()
        # Opcional: borrar archivos de UPLOAD_DIR
        for f in os.listdir(UPLOAD_DIR):
            os.remove(os.path.join(UPLOAD_DIR, f))

        return {"status": "success", "message": "System reset complete"}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=8080)