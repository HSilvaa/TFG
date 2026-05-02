using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChattingMenuState : AbstractMenuState
{
    private Transform IAContainer;
    private Transform ChatThings;
    public TMP_InputField messageField;
    public ScriptableCharacter currentCharacter;
    public GameObject mensajePrefab;
    private Button sendButt;
    private Button backButt;
    private ScrollRect scroll;

    private string messageToSend;

    // Variables para la gestión de mensajes (RAG/Historial)
    private List<ChatMessage> mensajesCargados = new List<ChatMessage>();
    private LinkedList<GameObject> mensajesVisibles = new LinkedList<GameObject>();
    private int currentStartIndex = -1;
    private bool isLoading = false;
    private int MaxVisibleMessages = 20;

    [System.Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;

        public ChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    private AbstractMenuState state;
    private APIManager api;

    public ChattingMenuState(IMenuState menu) : base(menu)
    {
        api = GameObject.FindObjectOfType<APIManager>();
    }

    public override void Enter()
    {
        try
        {
            ChatThings = GameObject.Find("ChatThings").transform;
            messageField = ChatThings.Find("MessageField").GetComponent<TMP_InputField>();
            sendButt = ChatThings.Find("SendButt").GetComponent<Button>();
            backButt = ChatThings.Find("BackButt").GetComponent<Button>();
            scroll = ChatThings.Find("Scroll View").GetComponent<ScrollRect>();
            IAContainer = scroll.transform.Find("Viewport").Find("IAContainer").transform;
            mensajePrefab = IAContainer.Find("MessagePrefab").gameObject;
            currentCharacter = GameObject.Find("ScriptableCharacter").GetComponent<HolderScriptable>().data;

            ChatThings.transform.SetAsLastSibling();

            // Limpieza inicial
            ClearVisibleMessages();
            mensajesCargados.Clear();

            // Configuración de UI
            sendButt.gameObject.SetActive(true);
            backButt.gameObject.SetActive(true);
            messageField.gameObject.SetActive(true);
            scroll.gameObject.SetActive(true);

            scroll.onValueChanged.AddListener(OnScrollValueChanged);

            // Cargar historial desde la nueva ruta de la API
            LoadCharacterConversationFromAPI();

            sendButt.onClick.AddListener(SendMessagePython);

            backButt.onClick.AddListener(() =>
            {
                state = new ChoseCharacterState(menu);
                TransicionExit();
            });
        }
        catch (Exception ex)
        {
            Debug.LogError("Error en Enter ChattingState: " + ex.Message);
        }
    }

    private void LoadCharacterConversationFromAPI()
    {
        if (isLoading) return;
        isLoading = true;

        api.GetConversations(currentCharacter.characterId, (conversaciones) =>
        {
            mensajesCargados.Clear();

            foreach (var item in conversaciones)
            {
                // El servidor devuelve "User: ... \nAssistant: ..." o solo "User: ..."
                // Usamos una partición más robusta para evitar errores si el string cambia
                string[] separators = { "\nAssistant: ", "\nAssistant:", "Assistant: " };
                string[] parts = item.Split(separators, StringSplitOptions.None);

                string userPart = parts[0].Replace("User: ", "").Trim();
                string assistantPart = parts.Length > 1 ? parts[1].Trim() : "";

                // Añadimos al historial local para la virtualización del scroll
                mensajesCargados.Add(new ChatMessage("user", userPart));
                if (!string.IsNullOrEmpty(assistantPart))
                    mensajesCargados.Add(new ChatMessage("assistant", assistantPart));
            }

            // Calculamos el índice para mostrar los últimos MaxVisibleMessages
            currentStartIndex = Mathf.Max(0, mensajesCargados.Count - MaxVisibleMessages);
            if (currentStartIndex % 2 != 0) currentStartIndex--; // Asegurar que empezamos por un mensaje de Usuario

            LoadVisibleMessages();
            isLoading = false;
        });
    }

    public void SendMessagePython()
    {
        messageToSend = messageField.text;
        if (string.IsNullOrEmpty(messageToSend)) return;

        // Instanciamos el par visual (User: texto, Assistant: ...)
        GameObject currentMessagePair = InstantiateMessagePair(messageToSend);
        messageField.text = "";

        // Petición a la nueva ruta jerárquica: /characters/{id}/chat
        api.SendChatMessage(currentCharacter.characterId, messageToSend, (respuestaNPC) =>
        {
            if (!string.IsNullOrEmpty(respuestaNPC))
            {
                // Limpiar comillas extras si las hay
                respuestaNPC = respuestaNPC.Trim('\"');

                FillAssistantResponse(currentMessagePair, respuestaNPC);

                // Actualizamos la lista interna para el sistema de scroll
                mensajesCargados.Add(new ChatMessage("user", messageToSend));
                mensajesCargados.Add(new ChatMessage("assistant", respuestaNPC));
            }
        });
    }

    private GameObject InstantiateMessagePair(string userMsg)
    {
        GameObject newPair = GameObject.Instantiate(mensajePrefab, IAContainer.transform);
        newPair.SetActive(true);

        newPair.transform.Find("User").GetComponent<TMP_Text>().text = userMsg;
        newPair.transform.Find("Assistant").GetComponent<TMP_Text>().text = "...";

        mensajesVisibles.AddLast(newPair);
        RefreshChatLayout();
        return newPair;
    }

    private void FillAssistantResponse(GameObject pair, string assistantMsg)
    {
        if (pair == null) return;
        pair.transform.Find("Assistant").GetComponent<TMP_Text>().text = assistantMsg;
        RefreshChatLayout();
    }

    private void LoadVisibleMessages()
    {
        // El prefab gestiona User y Assistant internamente, así que iteramos de 2 en 2
        for (int i = currentStartIndex; i < mensajesCargados.Count; i += 2)
        {
            if (mensajesCargados[i].role != "user") { i--; continue; } // Sincronización de roles

            GameObject go = GameObject.Instantiate(mensajePrefab, IAContainer.transform);
            go.SetActive(true);

            go.transform.Find("User").GetComponent<TMP_Text>().text = mensajesCargados[i].content;

            if (i + 1 < mensajesCargados.Count)
            {
                go.transform.Find("Assistant").GetComponent<TMP_Text>().text = mensajesCargados[i + 1].content;
            }
            else
            {
                go.transform.Find("Assistant").GetComponent<TMP_Text>().text = "";
            }

            mensajesVisibles.AddLast(go);
        }
        RefreshChatLayout();
    }

    private void TryLoadPreviousMessages()
    {
        if (isLoading || currentStartIndex <= 0) return;
        isLoading = true;

        int newStartIndex = Mathf.Max(0, currentStartIndex - MaxVisibleMessages);
        if (newStartIndex % 2 != 0) newStartIndex--;

        // Cargamos hacia atrás
        for (int i = currentStartIndex - 2; i >= newStartIndex; i -= 2)
        {
            GameObject go = GameObject.Instantiate(mensajePrefab, IAContainer.transform);
            go.SetActive(true);

            go.transform.Find("User").GetComponent<TMP_Text>().text = mensajesCargados[i].content;

            if (i + 1 < mensajesCargados.Count)
                go.transform.Find("Assistant").GetComponent<TMP_Text>().text = mensajesCargados[i + 1].content;

            go.transform.SetSiblingIndex(0); // Poner al principio del contenedor
            mensajesVisibles.AddFirst(go);
        }

        currentStartIndex = newStartIndex;
        isLoading = false;

        // Mantener la posición del scroll para que no pegue saltos
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(IAContainer.GetComponent<RectTransform>());
    }

    private void OnScrollValueChanged(Vector2 pos)
    {
        if (isLoading) return;
        if (pos.y >= 0.99f) TryLoadPreviousMessages();
    }

    private void RefreshChatLayout()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(IAContainer.GetComponent<RectTransform>());
        scroll.verticalNormalizedPosition = 0f;
    }

    private void ClearVisibleMessages()
    {
        foreach (var obj in mensajesVisibles) if (obj != null) GameObject.Destroy(obj);
        mensajesVisibles.Clear();
    }

    public override void TransicionExit()
    {
        sendButt.gameObject.SetActive(false);
        backButt.gameObject.SetActive(false);
        messageField.gameObject.SetActive(false);
        scroll.onValueChanged.RemoveAllListeners();

        ClearVisibleMessages();
        mensajesCargados.Clear();

        menu.SetState(state);
    }

    public override void Update() { }
    public override void FixedUpdate() { }
    public override void TransicionEnter() { }
    public override void Exit() { }
}