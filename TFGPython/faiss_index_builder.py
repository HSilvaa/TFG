import os
import sys
import io
import traceback
from datetime import datetime

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
import json
from typing import List

import PyPDF2
import faiss
from sentence_transformers import SentenceTransformer

from docx import Document

# ========== CONFIGURACIÓN ==========
DATA_DIR = "docs"
INDEX_DIR = "indices"
os.makedirs(INDEX_DIR, exist_ok=True)

EPOCHS = [d for d in os.listdir(DATA_DIR) if os.path.isdir(os.path.join(DATA_DIR, d))]

model = SentenceTransformer("sentence-transformers/all-mpnet-base-v2")
def eliminar_todos_los_indices():
    print("Eliminando todos los índices...")

    for archivo in os.listdir(INDEX_DIR):
        if archivo.endswith(".index") or archivo.endswith("_docs.json"):
            ruta_archivo = os.path.join(INDEX_DIR, archivo)
            try:
                os.remove(ruta_archivo)
                print(f"Eliminado: {archivo}")
            except Exception as e:
                print(f"Error al eliminar {archivo}: {e}")

    print("impieza completa.")
def eliminar_saltos_de_linea(texto):
    # Reemplaza los saltos de línea dentro del texto por un espacio
    texto_sin_saltos = "".join(texto.splitlines())
    return texto_sin_saltos

# =======================
# Extrae un pdf y lo divide en chunks
# =======================
def extraer_texto_de_pdf_en_chunks(ruta_pdf: str, chunk_size: int = 1000) -> List[str]:
    chunks = []
    texto_completo = ""

    try:
        with open(ruta_pdf, 'rb') as file:
            reader = PyPDF2.PdfReader(file)

            for page in reader.pages:
                texto_pagina = page.extract_text()
                if texto_pagina:
                    texto_pagina = eliminar_saltos_de_linea(texto_pagina)
                    texto_completo += texto_pagina + "\n"

            # Dividir el texto completo en chunks
            for i in range(0, len(texto_completo), chunk_size):
                chunk = texto_completo[i:i + chunk_size].strip()
                if chunk:
                    chunks.append(chunk)

    except Exception as e:
        print(f"Error al procesar el PDF {ruta_pdf}: {e}")

    return chunks

# =======================
# Dividir texto plano en chunks
# =======================
def dividir_texto_en_chunks(texto: str, chunk_size: int = 500) -> List[str]: #Framentos pequeños funciona mejor la busqueda por semántica
    texto = eliminar_saltos_de_linea(texto)
    chunks = [texto[i:i + chunk_size].strip() for i in range(0, len(texto), chunk_size)]
    return [c for c in chunks if c]  # Eliminar chunks vacíos

# =======================
#
# =======================

def extraer_texto_docx(ruta_archivo):
    try:
        doc = Document(ruta_archivo)
        texto = "\n".join([p.text for p in doc.paragraphs])
        return dividir_texto_en_chunks(texto)
    except Exception as e:
        print(f"Error al procesar {ruta_archivo}: {e}")
        return []

def cargar_textos_de_epoca(epoca_path):
    documentos = []

    for nombre_archivo in os.listdir(epoca_path):
        ruta_completa = os.path.join(epoca_path, nombre_archivo)

        if not os.path.isfile(ruta_completa):
            continue  # Ignora subcarpetas si las hubiera

        if nombre_archivo.endswith(".txt"):
            try:
                with open(ruta_completa, "r", encoding="utf-8") as f:
                    texto = f.read().strip()
                    if texto:
                        documentos.extend(dividir_texto_en_chunks(texto))
            except Exception as e:
                print(f"Error leyendo TXT {ruta_completa}: {e}")

        elif nombre_archivo.endswith(".docx"):
            documentos.extend(extraer_texto_docx(ruta_completa))

        elif nombre_archivo.endswith(".pdf"):
            chunks = extraer_texto_de_pdf_en_chunks(ruta_completa)
            if chunks:
                documentos.extend(chunks)
        else:
            print(f"Formato no reconocido: {nombre_archivo}")

    return documentos

# =======================
#
# =======================
def crear_indice_si_no_existe(epoca, documentos):
    index_path = os.path.join(INDEX_DIR, f"{epoca}.index")
    docs_path = os.path.join(INDEX_DIR, f"{epoca}_docs.json")

    if os.path.exists(index_path) and os.path.exists(docs_path):
        print(f"🟡 Índice de '{epoca}' ya existe. Saltando.")
        return

    print(f"✅ Creando índice para la época: {epoca}")
    # Normalizar los embeddings
    embeddings = model.encode(documentos, convert_to_numpy=True, normalize_embeddings=True)

    dim = embeddings.shape[1]
    index = faiss.IndexFlatIP(dim)  # Cambiar a Inner Product
    index.add(embeddings.astype("float32"))
    faiss.write_index(index, index_path)

    with open(docs_path, "w", encoding="utf-8") as f:
        json.dump(documentos, f, ensure_ascii=False, indent=2)

    print(f"🧠 Guardado índice y documentos para '{epoca}' en {INDEX_DIR}/")


# =======================
#
# =======================
def _log_personaje(personajes_dir: str, personaje_name: str, mensaje: str):
    """
    Escribe una línea en el log del personaje con timestamp.
    """
    os.makedirs(personajes_dir, exist_ok=True)
    log_path = os.path.join(personajes_dir, f"personaje_{personaje_name}.log")
    timestamp = datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
    try:
        with open(log_path, "a", encoding="utf-8") as f:
            f.write(f"[{timestamp}] {mensaje}\n")
    except Exception:
        # Si el log falla, no interrumpe el flujo (pero podrías querer manejarlo)
        pass

def crear_o_actualizar_indice_personaje(personaje_name: str, str_resumen: str):
    """
        Añade el resumen (str_resumen) al índice semántico del personaje.
        Si ya existe el índice, lo actualiza rápidamente añadiendo el nuevo embedding.
        Si no existe, crea uno nuevo con este resumen.
        Además, guarda todos los resúmenes en un archivo JSON.
        """
    BASE_DIR = os.path.dirname(os.path.abspath(__file__))
    CHARACTERS_DIR = os.path.join(BASE_DIR, "characters")
    os.makedirs(CHARACTERS_DIR, exist_ok=True)

    try:
        os.makedirs(CHARACTERS_DIR, exist_ok=True)

        index_path = os.path.join(CHARACTERS_DIR, f"personaje_{personaje_name}.index")
        docs_path = os.path.join(CHARACTERS_DIR, f"personaje_{personaje_name}_summaries.json")

        time22 = datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
        nuevo_chunk = {
            "text": str_resumen,
            "timestamp": time22
        }

        # Crear embedding
        embedding_nuevo = model.encode([str_resumen], convert_to_numpy=True, normalize_embeddings=True)

        # Cargar o crear índice FAISS
        if os.path.exists(index_path):
            index = faiss.read_index(index_path)
        else:
            index = faiss.IndexFlatIP(embedding_nuevo.shape[1])

        index.add(embedding_nuevo.astype("float32"))
        faiss.write_index(index, index_path)

        # Guardar resumen en JSON
        if os.path.exists(docs_path):
            with open(docs_path, "r", encoding="utf-8") as f:
                documentos = json.load(f)
        else:
            documentos = []

        documentos.append(nuevo_chunk)

        with open(docs_path, "w", encoding="utf-8") as f:
            json.dump(documentos, f, ensure_ascii=False, indent=2)

        # Guardar log
        log_path = os.path.join(personajes_dir, f"personaje_{personaje_name}.log")
        with open(log_path, "a", encoding="utf-8") as f:
            f.write(f"[{time22}] Añadido resumen: {str_resumen}\n")

        print(f"✅ Índice y JSON actualizados para '{personaje_name}'")

    except Exception:
        print("❌ ERROR en crear_o_actualizar_indice_personaje:")
        traceback.print_exc()

# =======================
#
# =======================
def construir_todos_los_indices():
    for epoca in EPOCHS:
        epoca_path = os.path.join(DATA_DIR, epoca)
        documentos = cargar_textos_de_epoca(epoca_path)
        if documentos:
            crear_indice_si_no_existe(epoca, documentos)
        else:
            print(f"⚠️ No se encontraron documentos en {epoca_path}")

def construir_todos_los_indices_Unity(rootPath):
    if not os.path.exists(rootPath):
        print(f"Ruta no encontrada: {rootPath}")
        return

    epocas = [d for d in os.listdir(rootPath) if os.path.isdir(os.path.join(rootPath, d))]

    for epoca in epocas:
        epoca_path = os.path.join(rootPath, epoca)
        print(f"EPOCA PATH: {epoca_path}")
        documentos = cargar_textos_de_epoca(epoca_path)
        if documentos:
            crear_indice_si_no_existe(epoca, documentos)
        else:
            print(f"No se encontraron documentos en {epoca_path}")

if __name__ == "__main__":
    #construir_todos_los_indices()
    #eliminar_todos_los_indices()
    #construir_todos_los_indices_Unity("C://Users//hugop//Desktop//PRUEBAS")
    # prueba de log
    crear_o_actualizar_indice_personaje("Paco", "ESTE ES EL MENSAJE 2 PARA EL LOG.")