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

    private TMP_Text resumenText;
    private TMP_Text historialText;

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

    public ChattingMenuState(IMenuState menu) : base(menu)
    {
    }
    /// <summary>
    /// Obtiene referencias relevantes para el funcionamineto del estado
    /// </summary>
    public async override void Enter()
    {
        // Obtener referencias necesarias
        try
        {
            ChatThings = GameObject.Find("ChatThings").transform; //Panel activo donde están todos estos elementos

            messageField = ChatThings.Find("MessageField").GetComponent<TMP_InputField>();
            sendButt = ChatThings.Find("SendButt").GetComponent<Button>(); //Boton de enviar
            backButt = ChatThings.Find("BackButt").GetComponent<Button>(); //Boton de enviar

            scroll = ChatThings.Find("Scroll View").GetComponent<ScrollRect>(); //Boton de enviar

            IAContainer = scroll.transform.Find("Viewport").Find("IAContainer").transform;
            mensajePrefab = IAContainer.Find("MessagePrefab").gameObject;
            currentCharacter = GameObject.Find("ScriptableCharacter").GetComponent<HolderScriptable>().data;

            //Poner como ultimo hijo para que se pueda clickar bien
            ChatThings.transform.SetAsLastSibling();

            scroll.onValueChanged.AddListener(OnScrollValueChanged);

            //await LoadCharacterConversation(); //AHORA TENGO QUE CARGAR TODAS

            sendButt.gameObject.SetActive(true);
            backButt.gameObject.SetActive(true);
            messageField.gameObject.SetActive(true);
            scroll.gameObject.SetActive(true);

            sendButt.onClick.AddListener(() =>
            {
                SendMessagePython();
                scroll.verticalNormalizedPosition = 0f;
            });

            backButt.onClick.AddListener(() =>
            {
                state = new ChoseCharacterState(menu);
                TransicionExit();
            });
        }
        catch (Exception ex)
        {
        }
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
        // Detecta scroll arriba para cargar mensajes más antiguos
        if (scroll.verticalNormalizedPosition >= 0.98f)
        {
            TryLoadPreviousMessages();
        }
    }

    /// <summary>
    /// Conecta con OpenAI y muestra el mensaje por pantalla cambiando el color segun el rol
    /// </summary>
    public async void SendMessagePython()
    {
        messageToSend = messageField.text;
        if (string.IsNullOrEmpty(messageToSend))
        {
            return;
        }

        ShowMessage(messageToSend, "user", Color.yellow);

        LayoutRebuilder.ForceRebuildLayoutImmediate(IAContainer.GetComponent<RectTransform>());

        string respuestaNPC = await ServiceLocatorManager.RunServiceWithResultAsync<ChatWithNPC, string>(messageToSend, currentCharacter);

        //Si ha habido respuesta, la muestra y la guarda
        if (!string.IsNullOrEmpty(respuestaNPC))
        {
            respuestaNPC = respuestaNPC.Trim('\"'); //Quitamos las comillas al mensaje

            ShowMessage(respuestaNPC, "assistant", Color.green);

            LayoutRebuilder.ForceRebuildLayoutImmediate(IAContainer.GetComponent<RectTransform>());

            SaveConversation(messageToSend, respuestaNPC);
        }
    }

    /// <summary>
    /// Muestra el mensaje por pantalla
    /// </summary>
    public void ShowMessage(string msg, string role, Color color)
    {
        msg = msg.Trim();

        if (!string.IsNullOrEmpty(msg))
        {
            scroll.verticalNormalizedPosition = 0f;

            GameObject newMessage = GameObject.Instantiate(mensajePrefab, IAContainer.transform);
            TMP_Text messageText = newMessage.GetComponentInChildren<TMP_Text>();

            mensajesVisibles.AddLast(newMessage);

            newMessage.SetActive(true);

            // Cambiar color del texto según el rol
            messageText.text = msg;
            messageText.color = color;  // Color azul para el usuario

            AddMessage(role, msg);

            messageField.text = "";
            Canvas.ForceUpdateCanvases();

            LayoutRebuilder.ForceRebuildLayoutImmediate(IAContainer.GetComponent<RectTransform>());

            scroll.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// Guarda cada par de 2 conversaciones en la BD
    /// </summary>
    private async void SaveConversation(string messageToSend, string respuestaNPC)
    {
        string completePair;
        messageToSend = "User: " + messageToSend + "\n";
        respuestaNPC = "Assistant: " + respuestaNPC;

        completePair = messageToSend + respuestaNPC;

        // Crear resumen para este par (opcional)
        //string resumen = await ServiceLocatorManager
        //    .RunServiceWithResultAsync<CreateResumenService, string>(completePair, currentCharacter.characterName);

        //SQLite.Instance.AddResumen(currentCharacter.characterId, resumen);

        // Guardar como registro independiente en la BD
        SQLite.Instance.AddConversation(currentCharacter.characterId, completePair);
    }

    /// <summary>
    /// Extra los ultimos 4 mensajes de conversacion con el NPC
    /// </summary>
    /// <param name="historialConversacion"></param>
    /// <param name="maxPairs"></param>
    /// <returns></returns>
    private Wrapper GetLastMessagesLiteral(int maxPairs = 2)
    {
        var lastMessages = new List<ChatMessage>();

        // Contamos desde el final hacia atrás, tomando pares de user/assistant
        int count = 0;
        for (int i = historialConversacion.Count - 1; i >= 0 && count < maxPairs * 2; i--)
        {
            var msg = historialConversacion[i];
            lastMessages.Insert(0, msg); // insert at beginning to preserve order
            count++;
        }

        Wrapper wrapper = new Wrapper { conversacion = lastMessages };
        return wrapper;
        //return JsonUtility.ToJson(wrapper);
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
    /// Carga los 20 primeros mensajes de la conversacion de la base de datos 
    /// </summary>
    /// <returns></returns>
    private async Task LoadCharacterConversation()
    {
        isLoading = true;

        var conversacionGuardada = SQLite.Instance.GetConversations(currentCharacter.characterId).FirstOrDefault();

        if (conversacionGuardada != null)
        {
            var wrapper = JsonUtility.FromJson<Wrapper>(conversacionGuardada.Historial);
            if (wrapper?.conversacion != null)
            {
                mensajesCargados = wrapper.conversacion;

                // Empezamos por los más recientes (final)
                currentStartIndex = mensajesCargados.Count - MaxVisibleMessages;
                if (currentStartIndex < 0) currentStartIndex = 0;

                LoadVisibleMessages();
            }
        }

        isLoading = false;
    }

    /// <summary>
    /// Muestra los visibles (20 primeros)
    /// </summary>
    private void LoadVisibleMessages()
    {
        //ClearVisibleMessages();

        int end = Mathf.Min(currentStartIndex + MaxVisibleMessages, mensajesCargados.Count);

        for (int i = currentStartIndex; i < end; i++)
        {
            ChatMessage msg = mensajesCargados[i];
            GameObject go = InstantiateMessage(msg);
            mensajesVisibles.AddLast(go);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(IAContainer.GetComponent<RectTransform>());

        scroll.verticalNormalizedPosition = 0f;
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

        // Si estamos en la parte superior
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
        int end = currentStartIndex; // el límite actual

        for (int i = newStartIndex; i < end; i++)
        {
            ChatMessage msg = mensajesCargados[i];
            GameObject go = InstantiateMessage(msg);

            // Insertar al principio visualmente
            go.transform.SetSiblingIndex(0);
            mensajesVisibles.AddFirst(go);
        }

        currentStartIndex = newStartIndex;
        isLoading = false;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(IAContainer.GetComponent<RectTransform>());
    }
}