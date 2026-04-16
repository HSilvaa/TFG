using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;

public class APIManager : MonoBehaviour
{
    private string baseUrl = "http://127.0.0.1:5000";

    // ========== MODELOS DE DATOS (Matching Pydantic) ==========
    [Serializable] public class QueryText { public string text; }

    [Serializable]
    public class CharacterData
    {
        public int Id;
        public string Name;
        public string Age;
        public string Description;
        public string Epoca;
    }

    [Serializable]
    public class MessageRequest
    {
        public string message;
        public int id;
    }

    [Serializable]
    public class HistorialText
    {
        public string historial;
        public string characterName;
    }

    // ========== WRAPPERS PARA RESPUESTAS JSON ==========
    [Serializable] public class ResponseString { public string response; }
    [Serializable] public class ResponseResumen { public string resumen; }
    [Serializable] public class ResponseStatus { public string status; public int id; }

    // ========== MÉTODOS DE LA API ==========

    public void UpdateRootFolder(string path)
    {
        QueryText data = new QueryText { text = path };
        StartCoroutine(PostRequest("/UpdateRootFolder", JsonUtility.ToJson(data)));
    }

    public void CreateCharacter(string name, string age, string desc, string epoca, Action<int> onSuccess = null)
    {
        CharacterData data = new CharacterData { Name = name, Age = age, Description = desc, Epoca = epoca };
        StartCoroutine(PostRequest("/characters", JsonUtility.ToJson(data), (json) => {
            var res = JsonUtility.FromJson<ResponseStatus>(json);
            onSuccess?.Invoke(res.id);
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

    public void SendChatMessage(string msg, int charId, Action<string> onReply)
    {
        MessageRequest data = new MessageRequest { message = msg, id = charId };
        StartCoroutine(PostRequest("/send", JsonUtility.ToJson(data), (json) => {
            var res = JsonUtility.FromJson<ResponseString>(json);
            onReply?.Invoke(res.response);
        }));
    }

    public void GetConversations(int charId, Action<List<string>> onSuccess)
    {
        StartCoroutine(GetRequest($"/conversations/{charId}", (json) => {
            string newJson = "{ \"items\": " + json + "}";
            Wrapper<string> wrapper = JsonUtility.FromJson<Wrapper<string>>(newJson);
            onSuccess?.Invoke(wrapper.items);
        }));
    }

    public void Summarize(string history, string charName, Action<string> onDone)
    {
        HistorialText data = new HistorialText { historial = history, characterName = charName };
        StartCoroutine(PostRequest("/resumir", JsonUtility.ToJson(data), (json) => {
            var res = JsonUtility.FromJson<ResponseResumen>(json);
            onDone?.Invoke(res.resumen);
        }));
    }

    public void ResetDatabase()
    {
        StartCoroutine(PostRequest("/resetDB", "{}"));
    }

    // ========== MOTORES DE PETICIÓN ==========

    IEnumerator PostRequest(string endpoint, string json, Action<string> callback = null)
    {
        var request = new UnityWebRequest(baseUrl + endpoint, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"{endpoint}: " + request.downloadHandler.text);
            callback?.Invoke(request.downloadHandler.text);
        }
        else
        {
            Debug.LogError($"Error en {endpoint}: " + request.error);
        }
    }

    IEnumerator GetRequest(string endpoint, Action<string> callback)
    {
        var request = UnityWebRequest.Get(baseUrl + endpoint);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            callback?.Invoke(request.downloadHandler.text);
        }
        else
        {
            Debug.LogError($"Error en GET {endpoint}: " + request.error);
        }
    }

    // Helper para listas JSON
    [Serializable] private class Wrapper<T> { public List<T> items; }
}