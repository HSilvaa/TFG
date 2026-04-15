using System.Threading.Tasks;  // Necesario para las tareas asíncronas
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class GetEpoca : IServiceResult<string>
{
    [System.Serializable]
    public class QueryText
    {
        public string text;
    }

    [System.Serializable]
    public class EpocaResponse
    {
        public string epoca;
    }

    public string Result { get; private set; }

    public async Task<bool> OpenServiceAsync()
    {
        Debug.Log("Servicio Abierto: GetEpoca");
        return true;
    }

    public async Task<bool> ExecuteServiceAsync(object[] info)
    {
        return await GetEpocaPython(info);
    }

    public async Task<bool> CloseServiceAsync()
    {
        Debug.Log("Servicio Cerrado: GetEpoca");
        return true;
    }

    /// <summary>
    /// Hace una peticion a python para que te de la epoca del personaje id según su descripción
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    private async Task<bool> GetEpocaPython(object[] info)
    {
        ScriptableCharacter character = info[0] as ScriptableCharacter;
        string url = "http://127.0.0.1:8000/GetEpoca";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            QueryText query = new QueryText { text = character.characterDescription };
            string jsonBody = JsonUtility.ToJson(query);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                EpocaResponse response = JsonUtility.FromJson<EpocaResponse>(json);
                Result = response.epoca;
                Debug.Log("Época detectada: " + Result);
                character.characterEpoca = Result;
                return true;
            }
            else
            {
                Debug.LogError($"Error en la petición: {request.error}");
                Debug.LogError($"Código HTTP: {request.responseCode}");
                Debug.LogError($"Respuesta del servidor: {request.downloadHandler.text}");
                Result = null;
                return false;
            }
        }
    }
}
