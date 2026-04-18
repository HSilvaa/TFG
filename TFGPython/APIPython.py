import sys
import traceback
from fastapi import FastAPI, HTTPException, Depends
from pydantic import BaseModel
from sqlalchemy.orm import Session

import crud
import database
from FaissTry18_04 import interactuar, resumir
from faiss_index_builder import construir_todos_los_indices_Unity, eliminar_todos_los_indices

# Configuración de salida
sys.stdout.reconfigure(line_buffering=True)

app = FastAPI(title="AIGERIM AI API")

# Inicializar Base de Datos (Crea tablas si no existen)
database.init_db()

# ========== MODELOS DE PETICIÓN (Pydantic) ==========

class QueryText(BaseModel):
    text: str  # Se usa para rutas de carpetas o textos generales

class CharacterCreate(BaseModel):
    Name: str
    Age: str
    Description: str
    Epoca: str

class MessageRequest(BaseModel):
    message: str
    id: int  # Character ID

class HistorialText(BaseModel):
    historial: str
    characterName: str

@app.get("/status")
def get_status():
    """
    Endpoint de control para que Unity sepa que el servidor está listo.
    """
    print("LOG: Unity ha consultado el estado - Servidor Activo")
    return {"status": "ok"}

# ========== ENDPOINTS DE CONTEXTO Y MUNDO ==========

@app.post("/CrearContexto")
def create_contexto(query: QueryText):
    """Limpia índices antiguos y genera nuevos desde la ruta especificada."""
    try:
        eliminar_todos_los_indices()
        construir_todos_los_indices_Unity(query.text)
        return {"contextCreated": "True", "path": query.text}
    except Exception as e:
        print(f"Error al crear contexto: {e}")
        return {"contextCreated": "False", "error": str(e)}


# ========== ENDPOINTS DE PERSONAJES (CRUD) ==========

@app.post("/characters")
def create_character(char_data: CharacterCreate, db: Session = Depends(database.get_db)):
    """Añade un nuevo personaje a la SQLite compartida."""
    try:
        new_char = crud.add_character(
            db, 
            name=char_data.Name, 
            age=char_data.Age, 
            description=char_data.Description, 
            epoca=char_data.Epoca
        )
        return {"status": "success", "id": new_char.Id}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/characters/{char_id}")
def get_character(char_id: int, db: Session = Depends(database.get_db)):
    """Obtiene los datos de un personaje por ID."""
    char = crud.get_character_by_id(db, char_id)
    if not char:
        raise HTTPException(status_code=404, detail="Character not found")
    return {
        "Id": char.Id,
        "Name": char.Name,
        "Age": char.Age,
        "Description": char.Description,
        "Epoca": char.Epoca
    }
@app.get("/characters")
def get_all_characters(db: Session = Depends(database.get_db)):
    """Obtiene la lista de todos los personajes guardados en la base de datos."""
    try:
        characters = crud.get_characters(db)
        return [
            {
                "Id": char.Id,
                "Name": char.Name,
                "Age": char.Age,
                "Description": char.Description,
                "Epoca": char.Epoca
            } for char in characters
        ]
    except Exception as e:
        print(f"Error al obtener personajes: {e}")
        raise HTTPException(status_code=500, detail="Error interno al listar personajes")

@app.delete("/characters/{char_id}")
def delete_character_endpoint(char_id: int, db: Session = Depends(database.get_db)):
    try:
        success = crud.delete_character(db, char_id)
        if success:
            # Opcional: Aquí podrías añadir lógica para borrar
            # físicamente los archivos .index del personaje si lo deseas.
            return {"status": "success", "message": f"Character {char_id} deleted"}
        raise HTTPException(status_code=404, detail="Character not found")
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

# ========== ENDPOINTS DE CONVERSACIÓN ==========

@app.post("/send")
def send_message(request: MessageRequest, db: Session = Depends(database.get_db)):
    """Endpoint principal para hablar con el NPC."""
    try:
        # La función interactuar ya gestiona FAISS, OpenAI y el guardado en DB
        resultado = interactuar(request.message, request.id)
        return {"response": resultado}
    except Exception as e:
        traceback.print_exc()
        return {"error": str(e)}

@app.get("/conversations/{char_id}")
def get_conversations(char_id: int, db: Session = Depends(database.get_db)):
    """Devuelve el historial literal de conversaciones."""
    convs = crud.get_conversations(db, char_id)
    return [c.Historial for c in convs]

@app.post("/resumir")
def resumir_endpoint(historial: HistorialText):
    """Genera un resumen de la conversación y actualiza el índice del personaje."""
    try:
        resumen_txt = resumir(historial.historial, historial.characterName)
        return {"resumen": resumen_txt}
    except Exception as e:
        return {"error": str(e)}

# ========== EXTRA: GESTIÓN DE CARPETAS ==========

@app.post("/UpdateRootFolder")
def update_root_folder(query: QueryText, db: Session = Depends(database.get_db)):
    try:
        folder = crud.update_or_create_root_folder(db, "RootFolder", query.text)

        construir_todos_los_indices_Unity(query.text)

        return {"status": "success", "route": folder.Route}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/folders/{folder_id}")
def get_folder(folder_id: int, db: Session = Depends(database.get_db)):
    folder = crud.get_folder_by_id(db, folder_id)
    if not folder:
        raise HTTPException(status_code=404, detail="Folder not found")
    return {
        "Id": folder.Id,
        "Name": folder.Name,
        "Route": folder.Route
    }

@app.post("/resetDB")
def reset_database_endpoint(db: Session = Depends(database.get_db)):
    """
    Borra todos los datos de la DB SQL y elimina los índices FAISS
    de mundo y personajes.
    """
    try:
        crud.reset_all_tables(db)
        eliminar_todos_los_indices()

        print("⚠️ Base de datos y archivos de índices reseteados por completo.")
        return {"status": "success", "message": "Base de datos y memorias FAISS reiniciadas"}

    except Exception as e:
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=f"Error al resetear la DB: {str(e)}")

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=5000)
    #python -m uvicorn APIPython:app --host 127.0.0.1 --port 8000