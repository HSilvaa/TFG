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

    // ========== MODELOS DE DATOS ==========

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

    // ========== WRAPPERS PARA RESPUESTAS JSON ==========
    [Serializable]
    public class ResponseString { public string response; }

    [Serializable]
    public class Wrapper<T>
    {
        public T[] items; 
    }

    // ========== MÉTODOS DE CONTEXTO (ARCHIVOS) ==========

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
            else onSuccess?.Invoke(new List<string>());
        }));
    }

    public void UploadContextFiles(string folderName, List<string> filePaths, Action<string> onSuccess = null)
    {
        StartCoroutine(PostUploadFiles($"/context/{folderName}/upload", filePaths, onSuccess));
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
            if (string.IsNullOrEmpty(json)) { onSuccess?.Invoke(new List<CharacterData>()); return; }

            string newJson = "{ \"items\": " + json + "}";
            Wrapper<CharacterData> wrapper = JsonUtility.FromJson<Wrapper<CharacterData>>(newJson);
            onSuccess?.Invoke(new List<CharacterData>(wrapper.items));
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
            if (string.IsNullOrEmpty(json) || json == "[]" || json == "null")
            {
                onSuccess?.Invoke(new List<string>());
                return;
            }

            try
            {
                json = json.Trim();
                string newJson = "{ \"items\": " + json + "}";

                Wrapper<string> wrapper = JsonUtility.FromJson<Wrapper<string>>(newJson);

                if (wrapper != null && wrapper.items != null)
                    onSuccess?.Invoke(new List<string>(wrapper.items));
                else
                    onSuccess?.Invoke(new List<string>());
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parseando historial: {e.Message}. JSON: {json}");
                onSuccess?.Invoke(new List<string>());
            }
        }));
    }

    // ========== MANTENIMIENTO Y STATUS ==========

    public void CheckStatus(Action<bool> onResult)
    {
        StartCoroutine(GetRequest("/status", (json) => {
            onResult?.Invoke(!string.IsNullOrEmpty(json) && json.Contains("\"status\":\"ok\""));
        }));
    }

    public void ResetSystem(Action<string> callback = null)
    {
        StartCoroutine(PostRequest("/system/reset", "{}", callback));
    }

    // ========== MOTORES DE PETICIÓN (IEnumerator) ==========

    IEnumerator PostUploadFiles(string endpoint, List<string> filePaths, Action<string> callback)
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        foreach (string path in filePaths)
        {
            if (File.Exists(path))
            {
                byte[] fileData = File.ReadAllBytes(path);
                string fileName = Path.GetFileName(path);
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
}