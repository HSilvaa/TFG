using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using System.IO;

public class APIManager : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:8000";

    // ========== MODELOS DE DATOS (Matching Python V2) ==========

    [Serializable]
    public class CharacterData
    {
        public int id;
        public string name;
        public string age;
        public string description;
        public string epoca;
    }

    [Serializable]
    public class ContextosResponse
    {
        public List<string> contextos;
    }

    [Serializable]
    public class MessageRequest
    {
        public string message;
    }

    [Serializable]
    public class FolderData
    {
        public int id;
        public string name;
        public string route;
    }

    // ========== WRAPPERS PARA RESPUESTAS JSON ==========
    [Serializable] public class ResponseString { public string response; }
    [Serializable] public class ResponseStatus { public string status; public string message; }

    // ========== MÉTODOS DE CONTEXTO (ARCHIVOS) ==========

    public void UploadContextFiles(string folderName, List<string> filePaths, Action<string> onSuccess = null)
    {
        StartCoroutine(PostUploadFiles($"/context/{folderName}/upload", filePaths, onSuccess));
    }

    public void GetContextos(Action<List<string>> onSuccess)
    {
        StartCoroutine(GetRequest("/contexts", (json) => {
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var res = JsonUtility.FromJson<ContextosResponse>(json);
                    onSuccess?.Invoke(res.contextos);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error parseando contextos: " + e.Message);
                    onSuccess?.Invoke(new List<string>());
                }
            }
            else
            {
                onSuccess?.Invoke(new List<string>());
            }
        }));
    }

    public void BuildContextIndex(string folderName, Action<string> onSuccess = null)
    {
        StartCoroutine(PostRequest($"/context/{folderName}/save", "{}", onSuccess));
    }

    // ========== MÉTODOS DE PERSONAJES ==========

    public void CreateCharacter(string name, string age, string desc, string epoca, Action<CharacterData> onSuccess = null)
    {
        CharacterData data = new CharacterData { name = name, age = age, description = desc, epoca = epoca };
        StartCoroutine(PostRequest("/characters", JsonUtility.ToJson(data), (json) => {
            var res = JsonUtility.FromJson<CharacterData>(json);
            onSuccess?.Invoke(res);
        }));
    }

    public void GetAllCharacters(Action<List<CharacterData>> onSuccess)
    {
        StartCoroutine(GetRequest("/characters", (json) => {
            string newJson = "{ \"items\": " + json + "}";
            Wrapper<CharacterData> wrapper = JsonUtility.FromJson<Wrapper<CharacterData>>(newJson);
            onSuccess?.Invoke(wrapper.items);
        }));
    }

    public void GetCharacter(int charId, Action<CharacterData> onSuccess)
    {
        StartCoroutine(GetRequest($"/characters/{charId}", (json) => {
            var res = JsonUtility.FromJson<CharacterData>(json);
            onSuccess?.Invoke(res);
        }));
    }

    public void DeleteCharacter(int charId, Action onSuccess)
    {
        StartCoroutine(DeleteRequest($"/characters/{charId}", (json) => {
            onSuccess?.Invoke();
        }));
    }

    // ========== MÉTODOS DE CHAT ==========

    public void SendChatMessage(int charId, string msg, Action<string> onReply)
    {
        MessageRequest data = new MessageRequest { message = msg };
        StartCoroutine(PostRequest($"/characters/{charId}/chat", JsonUtility.ToJson(data), (json) => {
            var res = JsonUtility.FromJson<ResponseString>(json);
            onReply?.Invoke(res.response);
        }));
    }

    public void GetConversations(int charId, Action<List<string>> onSuccess)
    {
        StartCoroutine(GetRequest($"/characters/{charId}/conversations", (json) => {
            string newJson = "{ \"items\": " + json + "}";
            Wrapper<string> wrapper = JsonUtility.FromJson<Wrapper<string>>(newJson);
            onSuccess?.Invoke(wrapper.items);
        }));
    }

    // ========== MANTENIMIENTO ==========

    public void ResetSystem()
    {
        StartCoroutine(PostRequest("/system/reset", "{}"));
    }

    public void CheckStatus(Action<bool> onResult)
    {
        StartCoroutine(GetRequest("/status", (json) => {
            onResult?.Invoke(json.Contains("\"status\":\"ok\""));
        }));
    }

    // ========== MOTORES DE PETICIÓN ==========

    IEnumerator PostUploadFiles(string endpoint, List<string> filePaths, Action<string> callback)
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

        foreach (string path in filePaths)
        {
            if (File.Exists(path))
            {
                byte[] fileData = File.ReadAllBytes(path);
                string fileName = Path.GetFileName(path);
                // "files" es el nombre del parámetro que espera FastAPI: List[UploadFile] = File(...)
                formData.Add(new MultipartFormFileSection("files", fileData, fileName, "text/plain"));
            }
        }

        UnityWebRequest request = UnityWebRequest.Post(baseUrl + endpoint, formData);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            callback?.Invoke(request.downloadHandler.text);
        else
            Debug.LogError($"Error Uploading: {request.error}");
    }

    IEnumerator PostRequest(string endpoint, string json, Action<string> callback = null)
    {
        var request = new UnityWebRequest(baseUrl + endpoint, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            callback?.Invoke(request.downloadHandler.text);
        else
            Debug.LogError($"Error POST {endpoint}: {request.error} | {request.downloadHandler.text}");
    }

    IEnumerator GetRequest(string endpoint, Action<string> callback)
    {
        var request = UnityWebRequest.Get(baseUrl + endpoint);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            callback?.Invoke(request.downloadHandler.text);
        else
            callback?.Invoke("");
    }

    IEnumerator DeleteRequest(string endpoint, Action<string> callback)
    {
        var request = UnityWebRequest.Delete(baseUrl + endpoint);
        request.downloadHandler = new DownloadHandlerBuffer();
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            callback?.Invoke(request.downloadHandler.text);
        else
            Debug.LogError($"Error DELETE {endpoint}: {request.error}");
    }

    [Serializable] private class Wrapper<T> { public List<T> items; }
}