using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class CreateResumenService : IServiceResult<string>
{
    public string Result { get; private set; }

    [Serializable]
    private class HistorialText
    {
        public string historial;
        public string characterName;
    }

    [Serializable]
    private class ResumenResponse
    {
        public string resumen;
        public string error;
    }

    public Task<bool> OpenServiceAsync()
    {
        Debug.Log("Servicio Abierto: CreateResumen");
        return Task.FromResult(true);
    }

    public Task<bool> CloseServiceAsync()
    {
        Debug.Log("Servicio cerrado: CreateResumen");
        return Task.FromResult(true);
    }

    public Task<bool> ExecuteServiceAsync(object[] info) => ResumirAsync(info);

    private async Task<bool> ResumirAsync(object[] info)
    {
        if (info.Length == 0 || info[0] is not string completePair)
        {
            Debug.LogError("CreateResumenService: historial vacío o null");
            return false;
        }

        string charName = info[1] as string;

        // Prepara el JSON esperado por el backend
        string json = JsonUtility.ToJson(new HistorialText { historial = completePair, characterName = charName});
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest("http://127.0.0.1:8000/resumir", "POST")
        {
            uploadHandler = new UploadHandlerRaw(jsonBytes),
            downloadHandler = new DownloadHandlerBuffer(),
            disposeDownloadHandlerOnDispose = true
        };
        request.SetRequestHeader("Content-Type", "application/json");

        try
        {
            var op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<ResumenResponse>(request.downloadHandler.text);
                if (!string.IsNullOrEmpty(response.error))
                {
                    Debug.LogError($"Backend error: {response.error}");
                    return false;
                }

                Result = response.resumen ?? "";
                return true;
            }

            Debug.LogError($"HTTP {request.responseCode}: {request.error}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"CreateResumenService exception: {ex}");
            return false;
        }
    }
}
