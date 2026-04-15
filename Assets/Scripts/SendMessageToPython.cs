using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class SendMessageToPython : MonoBehaviour ///////////////////////// OLDDDDDDDDDDDDDDDDDDDDDD
{
    public TMP_InputField messageField;
    public ScriptableCharacter currentCharacter;
    public Transform content;
    public GameObject mensajePrefab;

    private string messageToSend;

    public List<ChatMessage> historialConversacion = new List<ChatMessage>();

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


    private void AddMessage(string mensajeUsuario, string respuestaAsistente)
    {
        historialConversacion.Add(new ChatMessage(mensajeUsuario, respuestaAsistente));
    }


    public async void SendMessagePython()
    {
        messageToSend = messageField.text;
        ShowMessage();
        string respuestaNPC  = await ServiceLocatorManager.RunServiceWithResultAsync<ChatWithNPC, string>(messageToSend, currentCharacter);

        respuestaNPC = respuestaNPC.Trim('\"'); //Quitamos las comillas al mensaje

        GameObject newMessage = Instantiate(mensajePrefab, content);
        TMP_Text messageText = newMessage.GetComponentInChildren<TMP_Text>();

        // Cambiar color del texto según el rol
        messageText.text = respuestaNPC;
        messageText.color = Color.green;  // Color verde para el asistente

        AddMessage("assistant", respuestaNPC);
        SaveConversation();
    }


    public void ShowMessage()
    {
        string message = messageField.text.Trim();

        if (!string.IsNullOrEmpty(message))
        {
            GameObject newMessage = Instantiate(mensajePrefab, content);
            TMP_Text messageText = newMessage.GetComponentInChildren<TMP_Text>();

            // Cambiar color del texto según el rol
            messageText.text = message;
            messageText.color = Color.blue;  // Color azul para el usuario

            AddMessage("user", message);

            messageField.text = "";
            Canvas.ForceUpdateCanvases();
        }
    }

    private void SaveConversation()
    {
        //Guardamos el la conver cada 20 mensajes
        if(historialConversacion.Count()%20 == 0)
        {
            //Llamar a la IA para que haga un resumen
            //Gestionar que se guarda al salirse
            string historialJson = JsonUtility.ToJson(new Wrapper { conversacion = historialConversacion }, true);
            //SQLite.Instance.AddConversation(currentCharacter.characterId, historialJson, "NoResumen");
        }
    }
}
