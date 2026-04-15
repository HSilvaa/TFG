using System.Threading.Tasks;  // Necesario para las tareas asíncronas
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class CreateContext : IServiceResult<string>
{
    [System.Serializable]
    public class QueryText
    {
        public string text;
    }

    [System.Serializable]
    public class ContextoResponse
    {
        public string contextCreated;
    }

    public string Result { get; private set; }

    public async Task<bool> OpenServiceAsync()
    {
        Debug.Log("Servicio Abierto: GetEpoca");
        return true;
    }

    public async Task<bool> ExecuteServiceAsync(object[] info)
    {
        return await CrearContextoPython(info);
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
    private async Task<bool> CrearContextoPython(object[] info)
    {
        string contextPath = info[0] as string;
        string url = "http://127.0.0.1:8000/CrearContexto";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            QueryText query = new QueryText { text = contextPath };
            string jsonBody = JsonUtility.ToJson(query);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                ContextoResponse response = JsonUtility.FromJson<ContextoResponse>(json);
                Result = response.contextCreated;
                Debug.Log("ContextoCreado: " + Result);
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
