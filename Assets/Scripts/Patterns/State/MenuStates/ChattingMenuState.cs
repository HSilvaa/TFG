using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEngine.RuleTile.TilingRuleOutput;
using Button = UnityEngine.UI.Button;
using Transform = UnityEngine.Transform;

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

    public List<ChatMessage> historialConversacion = new List<ChatMessage>();

    //Variables para la carga de mensajes 
    private List<ChatMessage> mensajesCargados = new List<ChatMessage>();
    private LinkedList<GameObject> mensajesVisibles = new LinkedList<GameObject>();
    private int currentStartIndex = -1; // -1 hasta que se cargue
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

    [System.Serializable]
    public class Wrapper
    {
        public List<ChatMessage> conversacion;
    }

    private AbstractMenuState state;
    private APIManager api;
    public ChattingMenuState(IMenuState menu) : base(menu)
    {
        api = GameObject.FindObjectOfType<APIManager>();
    }
    /// <summary>
    /// Obtiene referencias relevantes para el funcionamineto del estado
    /// </summary>
    public async override void Enter()
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
            scroll.onValueChanged.AddListener(OnScrollValueChanged);

            LoadCharacterConversationFromAPI();

            sendButt.gameObject.SetActive(true);
            backButt.gameObject.SetActive(true);
            messageField.gameObject.SetActive(true);
            scroll.gameObject.SetActive(true);

            sendButt.onClick.AddListener(() =>
            {
                SendMessagePython();
            });

            backButt.onClick.AddListener(() =>
            {
                state = new ChoseCharacterState(menu);
                TransicionExit();
            });
        }
        catch (Exception ex)
        {
            Debug.LogError("Error en Enter: " + ex.Message);
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
                 string[] parts = item.Split(new[] { "\nAssistant: " }, StringSplitOptions.None);
                 string userPart = parts[0].Replace("User: ", "");
                 string assistantPart = parts.Length > 1 ? parts[1] : "";

                 mensajesCargados.Add(new ChatMessage("user", userPart));
                 if (!string.IsNullOrEmpty(assistantPart))
                     mensajesCargados.Add(new ChatMessage("assistant", assistantPart));                
            }

            currentStartIndex = Mathf.Max(0, mensajesCargados.Count - MaxVisibleMessages);
            LoadVisibleMessages();
            isLoading = false;
        });
    }

    public override void Exit()
    {

    }

    public override void FixedUpdate()
    {
    }

    public override async void TransicionEnter()
    {
        //menu.SetState(state);
    }

    public override async void TransicionExit()
    {
        sendButt.gameObject.SetActive(false);
        backButt.gameObject.SetActive(false);
        messageField.gameObject.SetActive(false);

        ClearVisibleMessages();
        historialConversacion.Clear();

        menu.SetState(state);
    }

    public override void Update()
    {
        if (scroll.verticalNormalizedPosition >= 0.98f)
        {
            TryLoadPreviousMessages();
        }
    }

    /// <summary>
    /// Conecta con OpenAI y muestra el mensaje por pantalla cambiando el color segun el rol
    /// </summary>
    private GameObject currentMessagePair;
    public void SendMessagePython()
    {
        messageToSend = messageField.text;
        if (string.IsNullOrEmpty(messageToSend)) return;

        currentMessagePair = InstantiateMessagePair(messageToSend);

        messageField.text = "";

        api.SendChatMessage(messageToSend, currentCharacter.characterId, (respuestaNPC) =>
        {
            if (!string.IsNullOrEmpty(respuestaNPC))
            {
                respuestaNPC = respuestaNPC.Trim('\"');

                FillAssistantResponse(currentMessagePair, respuestaNPC);

                AddMessage("user", messageToSend);
                AddMessage("assistant", respuestaNPC);
            }
        });
    }


    private GameObject InstantiateMessagePair(string userMsg)
    {
        GameObject newPair = GameObject.Instantiate(mensajePrefab, IAContainer.transform);
        newPair.SetActive(true);

        TMP_Text userText = newPair.transform.Find("User").GetComponent<TMP_Text>();
        TMP_Text assistantText = newPair.transform.Find("Assistant").GetComponent<TMP_Text>();

        userText.text = userMsg;
        assistantText.text = "...";

        mensajesVisibles.AddLast(newPair);

        RefreshChatLayout();
        return newPair;
    }

    /// <summary>
    /// Rellena la parte del asistente en un prefab ya existente.
    /// </summary>
    private void FillAssistantResponse(GameObject pair, string assistantMsg)
    {
        if (pair == null) return;

        TMP_Text assistantText = pair.transform.Find("Assistant").GetComponent<TMP_Text>();
        assistantText.text = assistantMsg;

        RefreshChatLayout();
    }

    private void RefreshChatLayout()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(IAContainer.GetComponent<RectTransform>());
        scroll.verticalNormalizedPosition = 0f;
    }

    /// <summary>
    /// Añade un mensaje a la lista dee conversaciones
    /// </summary>
    /// <param name="role">User/Assistant</param>
    /// <param name="content">Convesation</param>
    private void AddMessage(string role, string content)
    {
        historialConversacion.Add(new ChatMessage(role, content));
    }

    /// <summary>
    /// Muestra los visibles (20 primeros)
    /// </summary>
    private void LoadVisibleMessages()
    {
        // Iteramos de 2 en 2 para llenar los pares (User + Assistant)
        for (int i = currentStartIndex; i < mensajesCargados.Count; i += 2)
        {
            ChatMessage userMsg = mensajesCargados[i];
            GameObject go = GameObject.Instantiate(mensajePrefab, IAContainer.transform);
            go.SetActive(true);

            go.transform.Find("User").GetComponent<TMP_Text>().text = userMsg.content;

            // Comprobamos si existe un mensaje de asistente después del de usuario
            if (i + 1 < mensajesCargados.Count)
            {
                ChatMessage assistantMsg = mensajesCargados[i + 1];
                go.transform.Find("Assistant").GetComponent<TMP_Text>().text = assistantMsg.content;
            }
            else
            {
                go.transform.Find("Assistant").GetComponent<TMP_Text>().text = "";
            }

            mensajesVisibles.AddLast(go);
        }
        RefreshChatLayout();
    }

    private void ClearVisibleMessages()
    {
        foreach (var obj in mensajesVisibles)
        {
            GameObject.Destroy(obj);
        }
        mensajesVisibles.Clear();
    }

    /// <summary>
    /// Metodo para instanciar los mensajes (similar to ShowMessage) but this one takes a ChatMessage as argument
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    private GameObject InstantiateMessage(ChatMessage msg)
    {
        Color color = msg.role == "user" ? Color.yellow : Color.green;

        GameObject newMessage = GameObject.Instantiate(mensajePrefab, IAContainer.transform);
        TMP_Text messageText = newMessage.GetComponentInChildren<TMP_Text>();

        newMessage.SetActive(true);
        messageText.text = msg.content;
        messageText.color = color;

        return newMessage;
    }

    /// <summary>
    /// Cuando cambia el valor del scroll
    /// </summary>
    /// <param name="pos"></param>
    private void OnScrollValueChanged(Vector2 pos)
    {
        if (isLoading) return;

        if (pos.y >= 0.98f)
        {
            TryLoadPreviousMessages();
        }
    }

    /// <summary>
    /// Intenta cargar mensajes màs antiguos
    /// </summary>
    private void TryLoadPreviousMessages()
    {
        if (isLoading || currentStartIndex <= 0) return;

        isLoading = true;

        int newStartIndex = Mathf.Max(0, currentStartIndex - MaxVisibleMessages);

        for (int i = currentStartIndex - 2; i >= newStartIndex; i -= 2)
        {
            ChatMessage userMsg = mensajesCargados[i];
            ChatMessage assistantMsg = (i + 1 < mensajesCargados.Count) ? mensajesCargados[i + 1] : null;

            GameObject go = GameObject.Instantiate(mensajePrefab, IAContainer.transform);
            go.SetActive(true);

            go.transform.Find("User").GetComponent<TMP_Text>().text = userMsg.content;
            TMP_Text assistantTMP = go.transform.Find("Assistant").GetComponent<TMP_Text>();

            if (assistantMsg != null)
                assistantTMP.text = assistantMsg.content;
            else
                assistantTMP.text = "";

            go.transform.SetSiblingIndex(0);

            mensajesVisibles.AddFirst(go);
        }

        currentStartIndex = newStartIndex;
        isLoading = false;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(IAContainer.GetComponent<RectTransform>());
    }
}