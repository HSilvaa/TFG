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

---

## 🚀 Método 1: Ejecución en Local (Fase de Pruebas / Servidor Autónomo)

Este método permite levantar el ecosistema de forma rápida utilizando los ejecutables integrados y scripts de automatización locales en Windows.

### Guía paso a paso:
1.  **Instalación de Dependencias:** Acceda a la carpeta raíz de `TFGPython` y ejecute el script automatizado `.bat` (`add installation.bat`). No cierre la ventana de la terminal hasta observar el mensaje explícito de finalización.
2.  **Iniciar la Aplicación:** Localice el archivo `.exe` compilado del proyecto dentro de los archivos del sistema y ejecútelo para iniciar la interfaz gráfica de Aigerim. 
    * *Nota Crítica:* Al abrir el ejecutable se instanciará una ventana de consola secundaria de Python. Este es el servidor corriendo en segundo plano y **NO debe cerrarse** bajo ninguna circunstancia. En caso de caída accidental, la aplicación cliente intentará relanzarlo automáticamente de forma interna.
3.  **Inyección y Guardado de Contexto:** Si desea inicializar un mundo o entorno interactivo nuevo, arrastre o agregue los archivos de conocimiento compatibles (`.pdf`, `.docx`, `.txt`, etc.) y seleccione la opción **Guardar contexto**. *Si es la primera vez que inicia la aplicación, ejecute este paso con los documentos de prueba suministrados para asentar los índices iniciales.*
4.  **Modelado del NPC:** Complete el formulario de creación de personaje rellenando los campos de *Nombre*, *Edad*, *Época/Mundo* (vínculo directo al contexto documental) y una *Descripción detallada*.
5.  **Interacción:** Una vez registrado el NPC, podrá interactuar en tiempo real con él a través del chat interactivo para verificar que su comportamiento, tono y conocimiento se alinean con las directrices deseadas antes de su integración final en el motor.

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

### 1. Verificación de Estado (Heartbeat)
Utilizado de forma preventiva por los subsistemas de Unity para comprobar la disponibilidad y estado del servidor Docker antes de habilitar los componentes de comunicación en el motor de juego.
```bash
curl -X GET http://localhost:8000/status
