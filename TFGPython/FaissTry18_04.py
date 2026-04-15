import os

from sentence_transformers import SentenceTransformer
import json
import faiss
import numpy as np
import sys

from faiss_index_builder import crear_o_actualizar_indice_personaje

sys.stdout.reconfigure(line_buffering=True)

# Cargar modelo
model = SentenceTransformer("sentence-transformers/all-mpnet-base-v2")

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
    index = faiss.read_index(INDEX_PATHS[epoca])
    with open(f"indices/{epoca}_docs.json", encoding="utf-8") as f:
        docs = json.load(f)

    # Embedding de la pregunta (NORMALIZADO)
    pregunta_emb = model.encode([pregunta], convert_to_numpy=True, normalize_embeddings=True)

    D, I = index.search(np.array(pregunta_emb).astype("float32"), k)

    # Recuperar documentos relevantes y sus distancias
    resultados = [(D[0][i], docs[I[0][i]]) for i in range(k)]
    return resultados

# ===========================
# Función para buscar en el indice del resumen del personaje
# ===========================
def buscar_resumen(characterName, pregunta, k=3):
    personajes_dir = os.path.join("C:/Users/hugop/Desktop/TFG/ProyectoUnity/AIGERIM-TFG/TFGPython", "characters")

    index_path = os.path.join(personajes_dir, f"personaje_{characterName}.index")
    docs_path = os.path.join(personajes_dir, f"personaje_{characterName}_summaries.json")

    print(index_path)

    if(os.path.exists(index_path)):
        # Cargar índice y documentos
        index = faiss.read_index(index_path)
        with open(docs_path, encoding="utf-8") as f:
            docs = json.load(f)

        # Embedding de la pregunta (NORMALIZADO)
        pregunta_emb = model.encode([pregunta], convert_to_numpy=True, normalize_embeddings=True)

        D, I = index.search(np.array(pregunta_emb).astype("float32"), k)

        # Recuperar documentos relevantes y sus distancias
        resumenes = [docs[I[0][i]] for i in range(k) if I[0][i] < len(docs)]

        # Si son dicts con "text", extraer solo el texto
        if resumenes and isinstance(resumenes[0], dict) and "text" in resumenes[0]:
            resumenes = [r["text"] for r in resumenes]

        # Unir todos en un único string separado por saltos de línea
        return "\n".join(resumenes)
    return "NoResumen"


# ===========================
# Generar respuesta basada en el contexto
# ===========================
def construir_prompt(contexto, pregunta, npc_descripcion, resumen, id, name, age, k=5, max_total_chars=1500): #Puesto que cada fragmento son 500, aqui guardaremos 3 fragmentos de informacion
    # Usamos solo los primeros k fragmentos
    try:
        contexto = contexto[:k]
        #personaje = obtener_personaje_by_id(id)
        #print(personaje[1])
        # print("---------------CONTEXTO [:K]---------------")
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
                     Vas a responder con naturalidad y coherencia, como si realmente vivieras en esa época.
                     Estos son fragmentos de conocimiento conocidos y existentes en ese tiempo. 
                     No significa que todo el mundo los conozca, pero son parte del saber de la época:
                     {contexto_str}
                    
                     Además, esto es un resumen de la conversación más relevante en base a la pregunta que has tenido con el jugador hasta ahora:
                     {resumen}
            
            
            Solo puedes usar este conocimiento para responder. Sigue las siguientes reglas:
            - Adapta tu lenguaje a la época o al idioma de tu personaje
            - No puedes acceder a información más allá de estos fragmentos.
            - Si el jugador menciona un concepto que no entiendes (por ejemplo: "coche", "ordenador", "teléfono"), **no intentes adivinarlo**. No tienes por qué saber lo que es.
            - No hagas inferencias modernas. Responde como lo haría alguien de tu época, según tu rol y entorno.
            - Evita contradicciones entre tu respuesta y lo que se encuentra en el resumen de la conversacion
            - Si no puedes responder, dilo de forma natural y coherente con tu personaje.
            
            Tu objetivo es mantener la ilusión de que realmente formas parte de ese mundo.
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
        # print("---------------Prompt---------------")
        # print(prompt)
        return prompt

    except Exception as e:
        print(f"Ha ocurrido un problema {e}")
        return[]
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
# configura tu API KEY aquí
openai.api_key = "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"
def responder_con_openai(prompt, temp):
    """Usa OpenAI ChatGPT para generar una respuesta"""
    try:
        response = openai.ChatCompletion.create(
            model="gpt-4o",
            messages=prompt,
            temperature = temp,
            max_tokens =  400
        )
        return response.choices[0].message.content.strip()
    except Exception as e:
        print("DESDE PYTHON: " + e)
        return f"[Error generando respuesta: {e}, prompt={prompt}]"
# ===========================
# Obtener el indice en base a la pregunta
# ===========================

def obtener_contexto_mas_relevante(descripcion, k=5):
    mejor_epoca = None
    mayor_similitud = -1.0  # porque la similitud coseno va de -1 a 1

    for epoca, index_path in INDEX_PATHS.items():
        print(epoca)
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

def interactuar(pregunta, epoca, npc_descripcion, id, name, age):
    """Función principal: recibe una pregunta y época, responde como NPC"""
    contexto = buscar_contexto(epoca, pregunta) #Buscamos como contestar a esa pregunta segun la informacion de esa época

    #Obtener la cercanía a la pregunta en los resúmenes
    resumen1 = buscar_resumen(name, pregunta)
    print(resumen1)

    fragmentos = [c[1] for c in contexto]
    prompt = construir_prompt(fragmentos, pregunta, npc_descripcion, resumen1, id, name, age) #Eres el personaje con id tal, desc tal y tienes que contestar a esta pregunta con esta informacion (fragmentos)

    print(prompt)

    respuesta = responder_con_openai(prompt, 0.7) #Respuestas originales, para evitar la sensación de "máquina"

    print(respuesta)

    return respuesta

def resumir(historial, characterName):
    resumen = responder_con_openai(hacer_resumen(historial), 0.3) #Menos originalidad. Queremos información NO inventada
    print("Vamos a crear o actualizar el indice")
    crear_o_actualizar_indice_personaje(characterName, resumen)
    print(characterName)
    return resumen

# ========== EJEMPLO DE USO ==========
if __name__ == "__main__":
    resumir("User: ¿Qué debes hacer mañana? Assistant: Presentarme en su casa con lirios que le encataban a su difunta madre para ganarme el honor de su padre", "Mario")
    #interactuar("Cuál es mi nombre?", "EM", "Eres un joven campesino que buscar una mujer para hacer crecer su familia. Realmente, te gustan los hombres, pero debes ocultarlo a toda costa o correrás el riesgo de morir.", 3, "Mario", 23)