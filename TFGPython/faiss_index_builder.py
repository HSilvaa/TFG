import os
import sys
import io
import json
import re
from collections import defaultdict
from datetime import datetime
from typing import List, Dict

from database import SessionLocal
from schemas import StoredFile

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

import faiss
import PyPDF2
from docx import Document
from sentence_transformers import SentenceTransformer

INDEX_DIR = "indices"
CHARACTERS_DIR = "characters"
os.makedirs(INDEX_DIR, exist_ok=True)
os.makedirs(CHARACTERS_DIR, exist_ok=True)

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

def extraer_datos_desde_bd(files) -> Dict[str, List[Dict[str, str]]]:
    """
    Agrupa archivos por folder_name y extrae chunks.
    """

    datos_por_folder = defaultdict(list)

    for file in files:

        texto_archivo = ""

        try:

            # TXT
            if file.filename.endswith(".txt"):

                texto_archivo = file.data.decode("utf-8")

            # DOCX
            elif file.filename.endswith(".docx"):

                doc = Document(io.BytesIO(file.data))

                texto_archivo = " ".join(
                    [p.text for p in doc.paragraphs]
                )

            # PDF
            elif file.filename.endswith(".pdf"):

                pdf_stream = io.BytesIO(file.data)

                reader = PyPDF2.PdfReader(pdf_stream)

                texto_archivo = " ".join([
                    page.extract_text() or ""
                    for page in reader.pages
                ])

            if texto_archivo:

                chunks = dividir_texto_en_chunks_optimizados(
                    texto_archivo
                )

                for c in chunks:

                    datos_por_folder[file.folder_name].append({
                        "text": c,
                        "source": file.filename
                    })

        except Exception as e:

            print(f"Error procesando {file.filename}: {e}")

    return datos_por_folder


# =======================
# Gestión de Índices FAISS
# =======================

def crear_indice_faiss_avanzado(nombre_indice: str, datos: List[Dict[str, str]]):
    """
    Crea o Reconstruye (Update) el índice FAISS y el JSON de metadatos.
    """
    if not datos:
        print(f"Advertencia: No hay datos para procesar en el índice '{nombre_indice}'")
        return

    index_path = os.path.join(INDEX_DIR, f"{nombre_indice}.index")
    docs_path = os.path.join(INDEX_DIR, f"{nombre_indice}_docs.json")

    # Verificación de existencia para log
    if os.path.exists(index_path):
        print(f"Actualizando/Reconstruyendo índice existente: {nombre_indice}")
    else:
        print(f"Creando nuevo índice: {nombre_indice}")

    textos = [d["text"] for d in datos]

    print(f"Generando embeddings de alta precisión para {len(textos)} chunks...")
    # normalize_embeddings=True junto con IndexFlatIP equivale a Similitud de Coseno
    embeddings = model.encode(textos, convert_to_numpy=True, normalize_embeddings=True)

    dim = embeddings.shape[1]

    # Creamos un nuevo objeto de índice
    # Usamos FlatIP porque es el más preciso para comparaciones de documentos
    index = faiss.IndexFlatIP(dim)
    index.add(embeddings.astype("float32"))

    # Guardar el índice (esto sobreescribe el archivo si ya existe)
    faiss.write_index(index, index_path)

    # Guardar los metadatos (esto sobreescribe el JSON si ya existe)
    with open(docs_path, "w", encoding="utf-8") as f:
        json.dump(datos, f, ensure_ascii=False, indent=2)

    print(f"Operación finalizada: Índice '{nombre_indice}' sincronizado con éxito.")

# =======================
# Memoria de Personaje (Short-Term y Long-Term Memory)
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

def limpiar_indices_obsoletos(carpetas_activas: List[str]):
    """
    Borra los archivos .index y _docs.json de carpetas que
    ya no existen en la base de datos.
    """
    if not os.path.exists(INDEX_DIR):
        return

    # Obtenemos todos los archivos actuales en el directorio de índices
    archivos_en_disco = os.listdir(INDEX_DIR)

    for archivo in archivos_en_disco:
        # Extraemos el nombre de la carpeta/personaje del nombre del archivo
        # Ejemplo: "Asgar.index" -> "Asgar" o "Asgar_docs.json" -> "Asgar"
        nombre_base = archivo.replace(".index", "").replace("_docs.json", "")

        if nombre_base not in carpetas_activas:
            archivo_path = os.path.join(INDEX_DIR, archivo)
            try:
                os.remove(archivo_path)
                print(f"LOG: Limpieza - Índice obsoleto eliminado: {archivo}")
            except Exception as e:
                print(f"Error eliminando {archivo}: {e}")


def construir_todos_los_indices_Unity():
    """
    Sincroniza los índices FAISS con la BD:
    1. Actualiza o crea índices para carpetas con archivos.
    2. Borra índices de carpetas que ya no tienen archivos en la BD.
    """
    db = SessionLocal()

    try:
        files = db.query(StoredFile).all()

        datos_por_folder = extraer_datos_desde_bd(files)

        carpetas_activas = list(datos_por_folder.keys())

        if not datos_por_folder:
            print("LOG: No hay archivos en la BD.")
        else:
            for folder_name, datos in datos_por_folder.items():
                print(f"Sincronizando índice para: {folder_name}")
                crear_indice_faiss_avanzado(folder_name, datos)

        print("LOG: Verificando índices obsoletos...")
        limpiar_indices_obsoletos(carpetas_activas)

    finally:
        db.close()


def eliminar_todos_los_indices():
    """Limpieza profunda de memoria."""
    for d in [INDEX_DIR, CHARACTERS_DIR]:
        if os.path.exists(d):
            for f in os.listdir(d):
                try:
                    os.remove(os.path.join(d, f))
                except:
                    pass
    print("Memoria global FAISS eliminada.")