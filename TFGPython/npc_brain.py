import os
import sys
import io
import json
import traceback
import numpy as np
import openai
from datetime import datetime

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

import crud
import database
import faiss
from sentence_transformers import SentenceTransformer
from dotenv import load_dotenv

load_dotenv()

model = SentenceTransformer("sentence-transformers/all-mpnet-base-v2")

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
CHARACTERS_DIR = os.path.join(BASE_DIR, "characters")
INDEX_DIR = os.path.join(BASE_DIR, "indices")

db = next(database.get_db())

openai.api_key = os.getenv("OPENAI_API_KEY")

# ===========================
# Búsqueda de Contexto del Mundo
# ===========================
def buscar_contexto(epoca, pregunta, k=4):
    """Busca en el índice de la época/contexto cargado con depuración de score."""
    index_path = os.path.join(INDEX_DIR, f"{epoca}.index")
    docs_path = os.path.join(INDEX_DIR, f"{epoca}_docs.json")

    if not os.path.exists(index_path):
        print(f"⚠️ [DEBUG-WORLD] No existe el índice para la época: {epoca}")
        return []

    index = faiss.read_index(index_path)
    with open(docs_path, encoding="utf-8") as f:
        docs = json.load(f)

    pregunta_emb = model.encode([pregunta], convert_to_numpy=True, normalize_embeddings=True)

    # D contiene las distancias (Coseno de similitud en FlatIP)
    # I contiene los índices de los documentos
    D, I = index.search(np.array(pregunta_emb).astype("float32"), k)

    print(f"\n🔍 [DEBUG-WORLD] Recuperando contexto para la época: {epoca}")
    print(f"   Pregunta: '{pregunta}'")

    resultados = []
    for i in range(len(I[0])):
        idx = I[0][i]
        score = D[0][i]

        if idx != -1 and idx < len(docs):
            item = docs[idx]
            texto = item["text"] if isinstance(item, dict) else item
            source = item.get("source", "Desconocido") if isinstance(item, dict) else "N/A"

            print(f"   📌 [Score: {score:.4f}] [Source: {source}] | Fragmento: {texto[:100]}...")
            resultados.append(texto)
        else:
            print(f"   ❌ [Score: {score:.4f}] Índice fuera de rango o vacío.")

    return resultados


# ===========================
# Memoria Semántica (Similitudes Pasadas)
# ===========================
def buscar_interacciones_similares(characterName, pregunta, k=3, umbral=0.40):
    """Recupera memorias del NPC con depuración de score y umbral."""
    index_path = os.path.join(CHARACTERS_DIR, f"personaje_{characterName}.index")
    docs_path = os.path.join(CHARACTERS_DIR, f"personaje_{characterName}_docs.json")

    if not os.path.exists(index_path) or not os.path.exists(docs_path):
        print(f"ℹ️ [DEBUG-MEM] Sin índice de memoria para {characterName}")
        return ""

    index = faiss.read_index(index_path)
    with open(docs_path, encoding="utf-8") as f:
        docs = json.load(f)

    pregunta_emb = model.encode([pregunta], convert_to_numpy=True, normalize_embeddings=True)
    D, I = index.search(np.array(pregunta_emb).astype("float32"), k)

    print(f"\n🧠 [DEBUG-MEM] Buscando recuerdos similares para {characterName}")

    recuerdos = []
    for i in range(len(I[0])):
        score = D[0][i]
        idx = I[0][i]

        if idx != -1 and idx < len(docs):
            item = docs[idx]
            estado_umbral = "✅ PASA" if score >= umbral else "descarta (bajo umbral)"

            print(f"   🕒 [Score: {score:.4f}] [{estado_umbral}] | Recuerdo: {item['text'][:100]}...")

            if score >= umbral:
                recuerdos.append(f"- {item['text']}")

    return "\n".join(recuerdos) if recuerdos else "No tienes recuerdos específicos sobre esto."

# ===========================
# Memoria Reciente (Short-Term)
# ===========================
def obtener_memoria_reciente(characterName, n=4):
    """Devuelve las últimas N interacciones literales para fluidez."""
    docs_path = os.path.join(CHARACTERS_DIR, f"personaje_{characterName}_docs.json")
    if not os.path.exists(docs_path):
        return "No hay interacciones previas."

    with open(docs_path, encoding="utf-8") as f:
        docs = json.load(f)

    # Últimas N
    recientes = docs[-n:]
    formateado = []
    for d in recientes:
        # Extraemos el diálogo crudo para la ventana de contexto
        formateado.append(f"Jugador: {d.get('raw_user', '')}\nTú: {d.get('raw_npc', '')}")

    return "\n---\n".join(formateado)


# ===========================
# Orquestación del Prompt
# ===========================
def construir_prompt(pregunta, npc, contexto_mundo, memorias_rel, memoria_reciente):
    contexto_str = "\n".join([f"* {c}" for c in contexto_mundo])

    system_content = f"""Eres {npc.name}, una persona de {npc.age} años viviendo en la época: {npc.epoca}.
Tu personalidad: {npc.description}

REGLAS DE ACTUACIÓN:
1. Sé fiel a tu época. Si no entiendes un concepto moderno, actúa con confusión o sospecha.
2. Usa el CONOCIMIENTO DEL MUNDO para dar veracidad a tus palabras.
3. Sé coherente con tus MEMORIAS PASADAS.
4. No eres un asistente. Tienes intereses propios y sentimientos.

CONOCIMIENTO DEL MUNDO (Lore):
{contexto_str}

MEMORIAS RELEVANTES (Lo que recuerdas de este tema):
{memorias_rel}

HISTORIAL RECIENTE (Lo que acabáis de hablar):
{memoria_reciente}
"""

    return [
        {"role": "system", "content": system_content},
        {"role": "user", "content": pregunta}
    ]


# ===========================
#  Interacción Principal
# ===========================
def interactuar(pregunta, character_id):
    try:
        print(f"\n{'=' * 60}")
        print(f"🚀 INICIANDO INTERACCIÓN - ID CHAR: {character_id}")

        # Obtener datos del NPC desde SQL
        character = crud.get_character_by_id(db, character_id)
        if not character:
            print("❌ Error: NPC no encontrado en la DB.")
            return "Error: NPC no encontrado."

        print(f"👤 NPC: {character.name} | Época: {character.epoca}")

        # Recuperar información
        contexto_mundo = buscar_contexto(character.epoca, pregunta)
        memorias_rel = buscar_interacciones_similares(character.name, pregunta)
        memoria_reciente = obtener_memoria_reciente(character.name)

        # Generar Respuesta
        prompt = construir_prompt(pregunta, character, contexto_mundo, memorias_rel, memoria_reciente)

        print(f"\n📡 Enviando a OpenAI (Modelo: gpt-4o)...")
        response = openai.ChatCompletion.create(
            model="gpt-4o",
            messages=prompt,
            temperature=0.7,
            max_tokens=250
        )
        respuesta_final = response.choices[0].message.content.strip()

        print(f"\n✨ RESPUESTA GENERADA:\n\"{respuesta_final}\"")
        print(f"{'=' * 60}\n")

        crud.add_conversation(db, character.id, pregunta, respuesta_final)
        from faiss_index_builder import actualizar_memoria_personaje
        actualizar_memoria_personaje(character.name, pregunta, respuesta_final)

        return respuesta_final

    except Exception as e:
        print(f"\n🔥 [ERROR CRÍTICO] interactuar: {e}")
        traceback.print_exc()
        return f"Mi mente se nubla... (Error técnico: {str(e)})"