import os

from requests import Session
from sentence_transformers import SentenceTransformer
import json
import faiss
import numpy as np
import sys

import crud
import database
from faiss_index_builder import crear_o_actualizar_indice_personaje

sys.stdout.reconfigure(line_buffering=True)

# Cargar modelo
model = SentenceTransformer("sentence-transformers/all-mpnet-base-v2")

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
CHARACTERS_DIR = os.path.join(BASE_DIR, "characters")

os.makedirs(CHARACTERS_DIR, exist_ok=True)

db: Session = next(database.get_db())

# Tus rutas
INDEX_PATHS = {
    "Pre_EM": "indices/pre_EM.index",
    "EM": "indices/EM.index",
    "Post_EM": "indices/post_EM.index",
    "Act": "indices/ACT.index",
    "Fut_Real": "indices/futuro_real.index",
    "Fut_Dist": "indices/futuro_distopico.index"
}


# ===========================
# Función para buscar en el indice
# ===========================
def buscar_contexto(epoca, pregunta, k=3):
    # Cargar índice y documentos
    index = faiss.read_index(
        f"indices/{epoca}.index")  # ------------------------------> SI ESTO NO TIRA PONER INDEX_PATHS[epoca]
    with open(f"indices/{epoca}_docs.json", encoding="utf-8") as f:
        docs = json.load(f)

    # Embedding de la pregunta (NORMALIZADO)
    pregunta_emb = model.encode([pregunta], convert_to_numpy=True, normalize_embeddings=True)

    D, I = index.search(np.array(pregunta_emb).astype("float32"), k)

    # Recuperar documentos relevantes y sus distancias
    resultados = [(D[0][i], docs[I[0][i]]) for i in range(k)]
    return resultados


# ===========================
# Buscar y guardar interacciones FAISS
# ===========================
def buscar_interacciones(characterName, pregunta, k=3, umbral=0.45):
    """
    Busca interacciones pasadas en el índice del personaje y devuelve solo aquellas
    cuya similitud supere un umbral mínimo.

    - characterName: nombre del personaje
    - pregunta: lo que pregunta el usuario
    - k: número de resultados a recuperar
    - umbral: valor mínimo de similitud para considerar el resultado válido
    """
    index_path = os.path.join(CHARACTERS_DIR, f"personaje_{characterName}.index")
    docs_path = os.path.join(CHARACTERS_DIR, f"personaje_{characterName}_summaries.json")

    if not os.path.exists(index_path) or not os.path.exists(docs_path):
        return []

    # Cargar índice FAISS y documentos
    index = faiss.read_index(index_path)
    with open(docs_path, encoding="utf-8") as f:
        docs = json.load(f)

    # Embedding de la pregunta
    pregunta_emb = model.encode([pregunta], convert_to_numpy=True, normalize_embeddings=True)
    D, I = index.search(np.array(pregunta_emb).astype("float32"), k)

    interacciones = []
    for i in range(k):
        similitud = D[0][i]
        idx = I[0][i]

        if idx < len(docs) and similitud >= umbral:
            item = docs[idx]

            user_text = item.get("User", "")
            assistant_text = item.get("Assistant", "")

            # Normalizamos prefijos
            if user_text.startswith("User: "):
                user_text = user_text[len("User: "):]
            if assistant_text.startswith("Assistant: "):
                assistant_text = assistant_text[len("Assistant: "):]

            interacciones.append(
                f"(Similitud {similitud:.3f})\nUser: {user_text}\nAssistant: {assistant_text}"
            )

    print(f"[DEBUG] Interacciones relevantes para {characterName}: {interacciones}")
    return interacciones


def guardar_interaccion(character, pregunta, respuesta):
    historial = f"User: {pregunta}\nAssistant: {respuesta}"

    # Guardar en la base de datos
    crud.add_conversation(db, character.Id, historial)

    crear_o_actualizar_indice_personaje(character.Name, pregunta, respuesta)


# ===========================
# Obtener las últimas interacciones literales
# ===========================
def ultimas_interacciones(characterName, n=4):
    """
    Devuelve siempre las últimas `n` interacciones con un personaje,
    leyendo directamente del archivo JSON (orden cronológico).
    """
    docs_path = os.path.join(CHARACTERS_DIR, f"personaje_{characterName}_summaries.json")

    if not os.path.exists(docs_path):
        return []

    with open(docs_path, encoding="utf-8") as f:
        docs = json.load(f)

    # Tomamos las últimas n interacciones
    ultimas = docs[-n:] if len(docs) >= n else docs

    interacciones = []
    for item in ultimas:
        user_text = item.get("User", "")
        assistant_text = item.get("Assistant", "")

        if user_text.startswith("User: "):
            user_text = user_text[len("User: "):]
        if assistant_text.startswith("Assistant: "):
            assistant_text = assistant_text[len("Assistant: "):]

        interacciones.append(f"User: {user_text}\nAssistant: {assistant_text}")

    print(f"[DEBUG] Últimas {len(interacciones)} interacciones para {characterName}: {interacciones}")
    return interacciones


# ===========================
# Generar respuesta basada en el contexto
# ===========================
def construir_prompt(contexto, pregunta, npc_descripcion, converSimilares, converRecientes, id, name, age, k=5,
                     max_total_chars=1500):  # Puesto que cada fragmento son 500, aqui guardaremos 3 fragmentos de informacion
    # Usamos solo los primeros k fragmentos
    try:
        contexto = contexto[:k]
        print(contexto)

        contexto_final = []
        total_chars = 0
        for c in contexto:
            fragmento = c.strip()
            if total_chars + len(fragmento) > max_total_chars:
                break
            contexto_final.append(f"- {fragmento}")
            total_chars += len(fragmento)

        contexto_str = "\n".join(contexto_final)

        messages = [
            {
                "role": "system",
                "content": f"""Eres un personaje del mundo del juego llamado {name} de {age} años.
                     Vas a responder con naturalidad y coherencia, como si realmente vivieras en esa época. Solo puedes usar el siguiente conocimiento para responder.
                     Estos son fragmentos de conocimiento conocidos y existentes en ese tiempo. 
                     No significa que todo el mundo los conozca, pero son parte del saber de la época:
                     {contexto_str}

                     Estas son conversaciones relevantes que has tenido con el jugador en el pasado y que pueden tener relación con la pregunta actual:
                     {converSimilares}
                     Estas son las últimas 4 conversaciones que has tenido con el jugador. Muy probablemente estén directamente relacionadas con su pregunta actual:
                     {converRecientes}


            Sigue las siguientes reglas:
                - Habla como alguien de tu época, fiel a tu trasfondo.
                - No puedes acceder a información fuera de este contexto y memorias.
                - Si el jugador menciona un concepto extraño (ej: "coche", "ordenador", "teléfono"), no intentes adivinarlo.
                - No hagas inferencias modernas.
                - Sé coherente con tus recuerdos: no contradigas conversaciones pasadas.
                - Si no puedes responder, dilo de forma natural como lo haría tu personaje.
                - Tu objetivo es mantener la ilusión de que eres un habitante real de este mundo.
                - Evita ser servicial, pues eres un ser libre con sus propios intereses
            """
            },
            {
                "role": "user",
                "content": f"""Tu personaje es el siguiente:
            {npc_descripcion}

            Pregunta del jugador: "{pregunta}"
            """
            }
        ]

        return messages

    except Exception as e:
        print(f"Ha ocurrido un problema {e}")
        return []


def hacer_resumen(historial):
    messages = [
        {
            "role": "system",
            "content": (
                "Estás teniendo una conversación con otro ser inteligente como tú. "
                "Tu tarea es quedarte con los fragmentos más relevantes para recordar "
                "tanto a corto plazo (detalles inmediatos útiles para la siguiente interacción) "
                "como a largo plazo (información que defina al jugador, al personaje o la relación entre ambos). "
                "Debes resumir la interacción de forma concisa, sin inventar nada que no se haya dicho. A pesar de ser un resumen,"
                "usa información específica de la conversación para recordar con mayor precisión. "
            )
        },
        {
            "role": "user",
            "content": (
                "A partir del siguiente historial, genera un resumen en lenguaje natural que incluya exclusivamente:\n"
                "- La intención principal del jugador (qué busca, quiere saber o conseguir).\n"
                "- Las respuestas o información significativa que dio tu personaje (conocimiento, opiniones, confesiones, etc.).\n"
                "- Cualquier emoción o actitud expresada por tu personaje.\n"
                "- Cambios en la relación entre el jugador y el personaje (amistad, desconfianza, admiración, etc.).\n"
                "- Preguntas pendientes importantes o temas abiertos.\n\n"
                "Ignora frases de saludo, despedida, dudas sobre el sistema o repeticiones triviales. "
                "No repitas frases textuales del diálogo, sintetiza. "
                f"Historial:\n{historial}"
            )
        }
    ]
    return messages


import openai

# ========== CONFIGURACIÓN CHAT GPT==========
openai.api_key = "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"


def responder_con_openai(prompt, temp):
    """Usa OpenAI ChatGPT para generar una respuesta"""
    try:
        response = openai.ChatCompletion.create(
            model="gpt-4o",
            messages=prompt,
            temperature=temp,
            max_tokens=230  # 400 antes
        )
        return response.choices[0].message.content.strip()
    except Exception as e:
        return f"[Error generando respuesta: {e}, prompt={prompt}]"


# ===========================
# Obtener el indice en base a la pregunta
# ===========================

def obtener_contexto_mas_relevante(descripcion,
                                   k=5):  # --> debe recibir epoca y en vez de buscar en todas, busca en una
    mejor_epoca = None
    mayor_similitud = -1.0  # porque la similitud coseno va de -1 a 1

    for epoca, index_path in INDEX_PATHS.items():
        try:
            resultados = buscar_contexto(epoca, descripcion)
            similitud_mas_alta = resultados[0][0]  # primer resultado (más cercano)

            print(f"[{epoca}] Similitud más alta: {similitud_mas_alta} : {resultados[0][1]}")

            if similitud_mas_alta > mayor_similitud:
                mayor_similitud = similitud_mas_alta
                mejor_epoca = epoca
        except Exception as e:
            print(f"Error al procesar el índice '{epoca}': {e}")

    print(f"Época más relevante: {mejor_epoca}")
    return mejor_epoca


def interactuar(pregunta, character_id):
    """Función principal: recibe una pregunta y época, responde como NPC"""

    character = crud.get_character_by_id(db, character_id)

    contexto = buscar_contexto(character.Epoca, pregunta)

    # Memoria semántica (por similitud)
    interacciones_similares = buscar_interacciones(character.Name, pregunta)

    # Memoria reciente (últimas 4 interacciones)
    interacciones_recientes = ultimas_interacciones(character.Name, n=4)

    # Combinar ambas memorias
    # todas_interacciones = interacciones_recientes + interacciones_similares

    fragmentos = [c[1] for c in contexto]
    prompt = construir_prompt(fragmentos, pregunta, character.Description, interacciones_similares,
                              interacciones_recientes, character_id, character.Name, character.Age)

    respuesta = responder_con_openai(prompt, 0.7)

    guardar_interaccion(character, pregunta, respuesta)

    return respuesta


def resumir(historial, characterName):
    resumen = responder_con_openai(hacer_resumen(historial),
                                   0.3)  # Menos originalidad. Queremos información NO inventada
    crear_o_actualizar_indice_personaje(characterName, resumen)
    return resumen


# ========== EJEMPLO DE USO ==========
if __name__ == "__main__":
    resumir(
        "User: ¿Qué debes hacer mañana? Assistant: Presentarme en su casa con lirios que le encataban a su difunta madre para ganarme el honor de su padre",
        "Mario")
    # interactuar("Cuál es mi nombre?", "EM", "Eres un joven campesino que buscar una mujer para hacer crecer su familia. Realmente, te gustan los hombres, pero debes ocultarlo a toda costa o correrás el riesgo de morir.", 3, "Mario", 23)