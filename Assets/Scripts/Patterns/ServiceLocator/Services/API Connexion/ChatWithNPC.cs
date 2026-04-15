using System.Threading.Tasks;  // Necesario para las tareas asíncronas
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Unity.VisualScripting;
using System;
using System.Linq;
using static SQLite;
using static ChattingMenuState;
using System.Collections.Generic;

public class ChatWithNPC : IServiceResult<string>
{
    public string Result { get; private set; }

    //Eventos
    public static event Action<string> OnEmergencyActive;

    [System.Serializable]
    public class CharacterInteractionRequest
    {
        public string message;
        public string epoca;
        public string description;
        //public string resumen;
        public string name;
        public string age;
        public int id;
    }

    [System.Serializable]
    public class AIResponse
    {
        public string response;
    }


    public async Task<bool> OpenServiceAsync()
    {
        Debug.Log("Servicio Abierto: ChatWithNPC");
        return true;
    }

    public async Task<bool> ExecuteServiceAsync(object[] info)
    {
        return await InteractuarAsync(info);
    }

    public async Task<bool> CloseServiceAsync()
    {
        Debug.Log("Servicio cerrado: ChatWithNPC");
        return true;
    }

    //tasklist | findstr uvicorn --> para ver procesos activos en la cmd (esto son HIJOS)
    //taskkill /F /IM uvicorn.exe --> para ver matar todos los procesos en uvicorn
    //tasklist | findstr python --> (ver procesos padre)
    //taskkill /F /IM python.exe /T --> (matar proceso padre)

    /// <summary>
    /// Obtiene el resumen actual y se comunica con la API para mantener una conversacion con gpt-4o
    /// </summary>
    /// <param name="info">info[0] = mensaje del jugador || info[1] = información del personaje que interpreta la ia</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private async Task<bool> InteractuarAsync(object[] info)
    {
        string message = info[0] as string;
        ScriptableCharacter character = info[1] as ScriptableCharacter;

        string resumenStr = "NoResumen";

        var resumen = SQLite.Instance?.GetResumenById(character.characterId).FirstOrDefault();

        if (resumen != null)
        {
            resumenStr = resumen.ResumenText; //en teoria solo debe haber una conversacion por personaje
        }

        CharacterInteractionRequest requestData = new CharacterInteractionRequest
        {
            message = message ?? "",
            epoca = character.characterEpoca ?? "",
            description = character.characterDescription ?? "",
            //resumen = resumenStr ?? "NoResumen", //NO PASARLE EL RESUMEN, Y DESDE EL SEND DE PYTHON, ACCEDER AL RESUMEN RELEVANTE DEL MENSAJE QUE LE HE PASADO Y QUE USE ESE PARA CREAR LA RESPUESTA
            name = character.characterName,
            age = character.characterAge,
            id = character.characterId
        };
        string json = JsonUtility.ToJson(requestData);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);


        Debug.Log("JSON Enviado: " + json);

        using (UnityWebRequest request = new UnityWebRequest("http://127.0.0.1:8000/send", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(jsonBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            try
             {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {

                    var rawJson = request.downloadHandler.text;
                    AIResponse response = JsonUtility.FromJson<AIResponse>(rawJson);
                    Result = response.response;
                    return true;
                }
                else
                {
                    switch (request.result)
                    {
                        case UnityWebRequest.Result.ConnectionError:
                            Debug.LogError("Error de conexión: " + request.error);

                            //OnEmergencyActive.Invoke("NoInternetConnexion"); //prob should trigger emerency state somehow here

                            break;
                        case UnityWebRequest.Result.ProtocolError:
                            Debug.LogError("Error de protocolo HTTP: " + request.responseCode);

                            //OnEmergencyActive.Invoke("NotPythonLaunched");

                            break;
                        case UnityWebRequest.Result.DataProcessingError:
                            Debug.LogError("Error procesando la respuesta: " + request.error);

                            //OnEmergencyActive.Invoke("NotPythonLaunched");

                            break;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al enviar la petición: " + ex);
            }
        }
    }
}