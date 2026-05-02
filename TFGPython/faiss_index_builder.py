import os
import sys
import io
import json
import re
from datetime import datetime
from typing import List, Dict

# Evitar problemas de encoding en servidores externos
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

import faiss
import PyPDF2
from docx import Document
from sentence_transformers import SentenceTransformer

# ========== CONFIGURACIÓN ==========
INDEX_DIR = "indices"
CHARACTERS_DIR = "characters"
os.makedirs(INDEX_DIR, exist_ok=True)
os.makedirs(CHARACTERS_DIR, exist_ok=True)

# Modelo global optimizado para rendimiento
model = SentenceTransformer("sentence-transformers/all-mpnet-base-v2")


# =======================
# Utilidades de Procesamiento Optimizado
# =======================

def limpiar_texto(texto: str) -> str:
    """Normaliza el texto para mejorar la calidad del embedding."""
    # Eliminar saltos de línea y tabulaciones
    texto = re.sub(r'[\r\n\t]+', ' ', texto)
    # Eliminar espacios múltiples
    texto = re.sub(r'\s+', ' ', texto)
    return texto.strip()


def dividir_texto_en_chunks_optimizados(texto: str, size: int = 600, overlap: int = 100) -> List[str]:
    """
    Divide el texto con solapamiento (overlap) para no perder contexto en los cortes.
    """
    texto = limpiar_texto(texto)
    if len(texto) <= size:
        return [texto]

    chunks = []
    start = 0
    while start < len(texto):
        end = start + size
        chunk = texto[start:end]
        chunks.append(chunk)
        # El inicio del siguiente chunk retrocede la cantidad de 'overlap'
        start += (size - overlap)

        # Evitar bucles infinitos si el tamaño es menor al overlap
        if size <= overlap: break

    return [c for c in chunks if len(c) > 50]  # Ignorar chunks residuales muy cortos


# =======================
# Extractores de Archivos con Trazabilidad
# =======================

def extraer_datos_directorio(path_directorio: str) -> List[Dict[str, str]]:
    """
    Lee archivos y devuelve una lista de diccionarios con contenido y origen (metadatos).
    """
    datos_procesados = []
    if not os.path.exists(path_directorio):
        return datos_procesados

    for nombre_archivo in os.listdir(path_directorio):
        ruta_completa = os.path.join(path_directorio, nombre_archivo)
        if not os.path.isfile(ruta_completa): continue

        texto_archivo = ""
        try:
            if nombre_archivo.endswith(".txt"):
                with open(ruta_completa, "r", encoding="utf-8") as f:
                    texto_archivo = f.read()
            elif nombre_archivo.endswith(".docx"):
                doc = Document(ruta_completa)
                texto_archivo = " ".join([p.text for p in doc.paragraphs])
            elif nombre_archivo.endswith(".pdf"):
                with open(ruta_completa, 'rb') as file:
                    reader = PyPDF2.PdfReader(file)
                    texto_archivo = " ".join([page.extract_text() or "" for page in reader.pages])

            if texto_archivo:
                chunks = dividir_texto_en_chunks_optimizados(texto_archivo)
                for c in chunks:
                    datos_procesados.append({
                        "text": c,
                        "source": nombre_archivo
                    })
        except Exception as e:
            print(f"Error procesando {nombre_archivo}: {e}")

    return datos_procesados


# =======================
# Gestión de Índices FAISS
# =======================

def crear_indice_faiss_avanzado(nombre_indice: str, datos: List[Dict[str, str]]):
    """Crea índice FAISS y guarda JSON con metadatos."""
    if not datos: return

    index_path = os.path.join(INDEX_DIR, f"{nombre_indice}.index")
    docs_path = os.path.join(INDEX_DIR, f"{nombre_indice}_docs.json")

    textos = [d["text"] for d in datos]

    print(f"Generando embeddings de alta precisión para: {nombre_indice}")
    embeddings = model.encode(textos, convert_to_numpy=True, normalize_embeddings=True)

    dim = embeddings.shape[1]
    # IndexFlatIP es excelente para similitud de coseno con vectores normalizados
    index = faiss.IndexFlatIP(dim)
    index.add(embeddings.astype("float32"))

    faiss.write_index(index, index_path)

    # Guardamos la lista de diccionarios (texto + origen)
    with open(docs_path, "w", encoding="utf-8") as f:
        json.dump(datos, f, ensure_ascii=False, indent=2)
    print(f"Índice optimizado '{nombre_indice}' guardado con éxito.")


# =======================
# Memoria de Personaje (Short-Term & Long-Term Memory)
# =======================

def actualizar_memoria_personaje(personaje_name: str, pregunta: str, respuesta: str):
    """
    Optimizado: Guarda las interacciones con metadatos temporales.
    """
    index_path = os.path.join(CHARACTERS_DIR, f"personaje_{personaje_name}.index")
    docs_path = os.path.join(CHARACTERS_DIR, f"personaje_{personaje_name}_docs.json")

    timestamp = datetime.utcnow().strftime("%Y-%m-%d %H:%M:%S")
    # Formato de memoria narrativa para que el modelo lo entienda mejor al recuperar
    texto_memoria = f"El día {timestamp}, el usuario preguntó: '{pregunta}' y yo respondí: '{respuesta}'"

    nuevo_dato = {
        "text": texto_memoria,
        "raw_user": pregunta,
        "raw_npc": respuesta,
        "date": timestamp
    }

    embedding = model.encode([texto_memoria], convert_to_numpy=True, normalize_embeddings=True)

    if os.path.exists(index_path):
        index = faiss.read_index(index_path)
    else:
        index = faiss.IndexFlatIP(embedding.shape[1])

    index.add(embedding.astype("float32"))
    faiss.write_index(index, index_path)

    documentos = []
    if os.path.exists(docs_path):
        with open(docs_path, "r", encoding="utf-8") as f:
            documentos = json.load(f)

    documentos.append(nuevo_dato)
    with open(docs_path, "w", encoding="utf-8") as f:
        json.dump(documentos, f, ensure_ascii=False, indent=2)


# =======================
# Integración con API Unity
# =======================

def construir_todos_los_indices_Unity(rootPath: str):
    """Punto de entrada principal para el servidor remoto."""
    if not os.path.exists(rootPath):
        print(f"Ruta de uploads no válida: {rootPath}")
        return

    nombre_contexto = os.path.basename(os.path.normpath(rootPath))
    print(f"Iniciando optimización de contexto para: {nombre_contexto}")

    datos = extraer_datos_directorio(rootPath)
    if datos:
        crear_indice_faiss_avanzado(nombre_contexto, datos)
    else:
        print(f"No hay contenido para indexar en {rootPath}")


def eliminar_todos_los_indices():
    """Limpieza profunda de memoria."""
    for d in [INDEX_DIR, CHARACTERS_DIR]:
        if os.path.exists(d):
            for f in os.listdir(d):
                try:
                    os.remove(os.path.join(d, f))
                except:
                    pass
    print("Memoria global FAISS purgada.")