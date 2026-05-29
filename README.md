# Aigerim AI - Generación Procedimental de Diálogos para NPCs mediante RAG

Aigerim es un sistema inteligente diseñado para la generación procedimental de diálogos de personajes no jugables (NPCs) en entornos interactivos y videojuegos. La arquitectura del proyecto emplea un enfoque **RAG (Retrieval-Augmented Generation)**, combinando modelos de lenguaje masivos (**OpenAI API**) con almacenamiento vectorial (**FAISS**) para ofrecer interacciones contextuales, coherentes y dinámicas basadas en el trasfondo del mundo del juego y el historial de conversación.

El repositorio consta de dos componentes principales integrados al mismo nivel de directorio:
* **Cliente interactivo:** Desarrollado sobre el motor de videojuegos Unity.
* **Servidor Backend (`TFGPython`):** API REST construida en Python encargada del procesamiento de lenguaje natural (PLN), almacenamiento relacional de metadatos de personajes, vectorización de contexto y conexión con modelos de IA.

---

## 🛠️ Requisitos Previos

Antes de proceder con cualquiera de los métodos de instalación, asegúrese de cumplir con los siguientes requisitos:
1.  **Python:** Versión `3.10.11` o superior instalada en el sistema con sus correspondientes variables de entorno configuradas en el `PATH`.
2.  **Docker:** Tener instalado y en ejecución [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Recomendado para la integración limpia en producción/juego real).
3.  **Clonación:** Haber clonado de forma local el repositorio oficial de GitHub manteniendo la jerarquía de carpetas nativa.
4.  **Instalación de Dependencias:** Acceda a la carpeta raíz de `TFGPython` y ejecute el script automatizado `.bat` (`add installation.bat`). No cierre la ventana de la terminal hasta observar el mensaje explícito de finalización.

---

### Configuración de Credenciales 🔑

Antes de levantar el contenedor, es necesario configurar la API Key de OpenAI:

1. Duplique el archivo `.env.example`, que se encuentra en `/TFGPython` y renómbrelo a `.env`.
2. Abra el archivo `.env` y sustituya `tu_clave_de_openai_aqui` por su clave API válida de OpenAI.
3. Guarde el archivo.

## 🚀 Método 1: Ejecución en Local (Fase de Pruebas / Servidor Autónomo)

Este método permite levantar el ecosistema de forma rápida utilizando los ejecutables integrados y scripts de automatización locales en Windows.

### Guía paso a paso:
1.  **Iniciar la Aplicación:** Localice el archivo `.exe` compilado del proyecto dentro de los archivos del sistema y ejecútelo para iniciar la interfaz gráfica de Aigerim. 
    * *Nota Crítica:* Al abrir el ejecutable se instanciará una ventana de consola secundaria de Python. Este es el servidor corriendo en segundo plano y **NO debe cerrarse** bajo ninguna circunstancia. En caso de caída accidental, la aplicación cliente intentará relanzarlo automáticamente de forma interna.
2.  **Inyección y Guardado de Contexto:** Si desea inicializar un mundo o entorno interactivo nuevo, arrastre o agregue los archivos de conocimiento compatibles (`.pdf`, `.docx`, `.txt`, etc.) y seleccione la opción **Guardar contexto**. *Si es la primera vez que inicia la aplicación, ejecute este paso con los documentos de prueba suministrados para asentar los índices iniciales.*
3.  **Modelado del NPC:** Complete el formulario de creación de personaje rellenando los campos de *Nombre*, *Edad*, *Época/Mundo* (vínculo directo al contexto documental) y una *Descripción detallada*.
4.  **Interacción:** Una vez registrado el NPC, podrá interactuar en tiempo real con él a través del chat interactivo para verificar que su comportamiento, tono y conocimiento se alinean con las directrices deseadas antes de su integración final en el motor.

---

## 🐳 Método 2: Despliegue con Docker (Modo Integración en Juego)

Para implementar el sistema de IA de Aigerim de forma transparente y aislada dentro de su flujo de juego en Unity, se recomienda orquestar el backend mediante contenedores Docker.

### Instalación y Despliegue:
1.  Abra una ventana de terminal o consola de comandos.
2.  Navegue hasta la raíz del directorio backend donde se ubica el manifiesto de orquestación:
    ```bash
    cd TFGPython
    ```
3.  Construya y levante el contenedor aislado ejecutando:
    ```bash
    docker-compose up --build
    ```
    *Nota:* El proceso de compilación inicial puede demorar unos minutos debido a la descarga e instalación de las dependencias pesadas de IA (`FAISS`, `sentence-transformers`, procesamiento numérico, etc.). El contenedor estará completamente operativo y a la escucha en el puerto de red local `8000` en cuanto los logs reflejen la siguiente línea:  
    `INFO: Uvicorn running on http://0.0.0.0:8000`

---

## 🗺️ Guía de la API (AIGERIM AI API - V2)

A continuación, se especifican los puntos de acceso (endpoints) HTTP disponibles para que el cliente de Unity o herramientas externas invoquen los servicios del backend a través de peticiones REST (ejemplificadas mediante instrucciones `curl` estándar):

## 📡 Base URL
http://localhost:8000
## 🔍 Endpoints Disponibles
### 1. Verificación de Estado (Heartbeat)

Comprueba si el servidor está activo y disponible.

curl -X GET http://localhost:8000/status
### 2. Subir Archivos a una Carpeta de Contexto

Permite subir documentos (.txt, .pdf, .docx) asociados a una categoría o época concreta.

curl -X POST http://localhost:8000/files/upload \
  -F "folder_name=EM" \
  -F "files=@/ruta/al/documento.pdf"
| Campo | Tipo | Descripción |
| :--- | :--- | :--- |
| **folder_name** | `string` | Nombre de la carpeta o categoría |
| **files** | `file` | Archivo a subir |
### 3. Listar Carpetas de Contexto

Obtiene todas las carpetas únicas registradas en el sistema.

curl -X GET http://localhost:8000/files/folders
### 4. Listar Archivos de una Carpeta

Devuelve todos los documentos pertenecientes a una carpeta específica.

curl -X GET http://localhost:8000/files/folder/EM
### 5. Eliminar Archivo por ID

Elimina un documento del sistema mediante su identificador.

curl -X DELETE http://localhost:8000/files/{file_id}
## 👤 Gestión de Personajes (NPCs)
### 6. Crear un Nuevo NPC

Registra un personaje en la base de datos. Se REQUIERE que el campo epoca coincida con el nombre de la carpeta de contexto utilizada.

curl -X POST http://localhost:8000/characters \
  -H "Content-Type: application/json" \
  -d '{
    "name": "LUNA",
    "age": "30",
    "description": "Un personaje medieval de prueba",
    "epoca": "EM"
  }'
  
| Campo | Tipo | Descripción |
| :--- | :--- | :--- |
| **name** | `string` | Nombre del personaje |
| **age** | `string` | Edad del personaje |
| **description** | `string` | Descripción del NPC |
| **epoca** | `string` | Contexto o carpeta asociada |

Recupera todos los personajes registrados.

curl -X GET http://localhost:8000/characters
### 8. Obtener Datos de un NPC

Devuelve la información detallada de un personaje específico.

curl -X GET http://localhost:8000/characters/{char_id}
### 9. Eliminar NPC

Borra definitivamente un personaje y sus índices asociados.

curl -X DELETE http://localhost:8000/characters/{char_id}
## 💬 Sistema Conversacional (RAG)
### 10. Conversar con un NPC

Envía un mensaje al modelo IA asociado al personaje.

curl -X POST http://localhost:8000/characters/{char_id}/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Hola"
  }'

| Campo | Tipo | Descripción |
| :--- | :--- | :--- |
| **message** | `string` | Mensaje enviado al NPC |
### 11. Obtener Historial Conversacional

Recupera las conversaciones previas del personaje.

curl -X GET http://localhost:8000/characters/{char_id}/conversations
🧠 Indexación FAISS
### 12. Construir / Actualizar Índices

Procesa y vectoriza los documentos cargados para habilitar el sistema RAG.

curl -X POST http://localhost:8000/index
## ⚠️ Administración del Sistema
### 13. Reset Completo del Sistema

Elimina: 
1. Base de datos relaciona
2. Conversaciones
3. Personajes
4. Índices FAISS
5. Documentos procesados

⚠️ Acción irreversible.

curl -X POST http://localhost:8000/system/reset
## 🧩 Arquitectura General

El sistema está compuesto por:

FastAPI → Backend REST

FAISS → Búsqueda vectorial semántica

OpenAI API → Generación de respuestas con IA

SQLite  → Persistencia relacional

Unity Client → Integración con videojuegos

### Flujo Recomendado
Subir documentos → Construir índices FAISS → Crear NPC → Iniciar conversación → Recuperar historial conversacional

### 📦 Formatos de Archivo Compatibles
.txt
.pdf
.docx
## 🛠️ Estado del Proyecto
Versión actual: AIGERIM AI API - V2


