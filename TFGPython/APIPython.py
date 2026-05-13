import shutil
import sys
import os
import traceback
from io import BytesIO
from typing import List, Union
from fastapi import FastAPI, HTTPException, Depends, UploadFile, File, Form
from pydantic import BaseModel
from sqlalchemy.orm import Session
from starlette.responses import StreamingResponse

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

@app.post("/files/upload")
async def upload_files(
        folder_name: str = Form(...),
        files: List[UploadFile] = File(...),
        db: Session = Depends(database.get_db)
):
    ALLOWED_EXTENSIONS = {".txt", ".pdf", ".docx"}
    ALLOWED_MIME_TYPES = {
        "text/plain",
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    }

    try:
        saved_files = []

        if not isinstance(files, list):
            files = [files]

        for file in files:
            extension = os.path.splitext(file.filename)[1].lower()

            if extension not in ALLOWED_EXTENSIONS or file.content_type not in ALLOWED_MIME_TYPES:
                raise HTTPException(
                    status_code=400,
                    detail=f"Archivo '{file.filename}' no permitido. Solo se aceptan TXT, PDF y DOCX."
                )

            file_data = await file.read()

            saved_file = crud.save_file(
                db=db,
                folder_name=folder_name,
                filename=file.filename,
                content_type=file.content_type,
                data=file_data
            )

            saved_files.append({
                "id": saved_file.id,
                "filename": saved_file.filename
            })

        return {
            "status": "success",
            "folder": folder_name,
            "total_files": len(saved_files),
            "files": saved_files
        }

    except HTTPException as he:
        raise he
    except Exception as e:
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(e))
@app.get("/files/{file_id}")
def get_file(
    file_id: int,
    db: Session = Depends(database.get_db)
):
    try:

        stored_file = crud.get_file_by_id(db, file_id)

        if not stored_file:
            raise HTTPException(
                status_code=404,
                detail="File not found"
            )

        return StreamingResponse(
            BytesIO(stored_file.data),
            media_type=stored_file.content_type,
            headers={
                "Content-Disposition":
                    f"attachment; filename={stored_file.filename}"
            }
        )

    except Exception as e:
        traceback.print_exc()

        raise HTTPException(
            status_code=500,
            detail=str(e)
        )
@app.get("/files/folder/{folder_name}")
def get_files_by_folder(
    folder_name: str,
    db: Session = Depends(database.get_db)
):
    try:

        files = crud.get_files_by_folder(db, folder_name)

        return [
            {
                "id": f.id,
                "filename": f.filename,
                "content_type": f.content_type,
                "created_at": f.created_at
            }
            for f in files
        ]

    except Exception as e:
        traceback.print_exc()

        raise HTTPException(
            status_code=500,
            detail=str(e)
        )
@app.delete("/files/{file_id}")
def delete_file(
    file_id: int,
    db: Session = Depends(database.get_db)
):
    try:

        deleted = crud.delete_file(db, file_id)

        if not deleted:
            raise HTTPException(
                status_code=404,
                detail="File not found"
            )

        return {
            "status": "success",
            "message": f"File {file_id} deleted"
        }

    except Exception as e:
        traceback.print_exc()

        raise HTTPException(
            status_code=500,
            detail=str(e)
        )

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
    char = crud.get_character_by_id(db, char_id)
    if not char:
        raise HTTPException(status_code=404, detail="Character not found")

    char_name = char.name
    char_folder = "characters"

    files_to_remove = [
        os.path.join(char_folder, f"personaje_{char_name}.index"),
        os.path.join(char_folder, f"personaje_{char_name}_docs.json")
    ]

    for file_path in files_to_remove:
        try:
            if os.path.exists(file_path):
                os.remove(file_path)
                print(f"LOG: Archivo eliminado: {file_path}")
        except Exception as e:
            print(f"Error eliminando archivo {file_path}: {e}")

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

        return {"status": "success", "message": "System reset complete"}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/index")
def build_index():

    construir_todos_los_indices_Unity()

    return {
        "status": "success"
    }


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