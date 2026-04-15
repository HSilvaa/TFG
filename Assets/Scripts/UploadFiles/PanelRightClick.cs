using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PanelRightClick : MonoBehaviour
{
    public RectTransform PanelArchivos; //Scroll contentn
    public ContextMenuManager contextMenuScript;
    public TMP_InputField PathInputField;  //Ruta editable barra de navegacion --> puesto que no se puede cambiar, sustituir por un text y ya

    private void OnEnable()
    {
        AddRightClickToPanel();
    }
    private void AddRightClickToPanel()
    {
        EventTrigger trigger = PanelArchivos.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = PanelArchivos.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry rightClickEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };

        rightClickEntry.callback.AddListener((eventData) =>
        {
            PointerEventData pointerData = (PointerEventData)eventData;

            // Solo mostrar si es botón derecho y no se hizo sobre otro objeto interactivo
            if (pointerData.button == PointerEventData.InputButton.Right) //&& !IsPointerOverUIElement())
            {
                Vector2 screenPos = pointerData.position;
                Debug.Log("Clic derecho en fondo del panel");
                contextMenuScript.ShowMenuSimple(PathInputField.text, screenPos);
            }
        });

        trigger.triggers.Add(rightClickEntry);
    }

    bool IsPointerOverUIElement()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        // Excluir el fondo (PanelArchivos) si es el único bajo el mouse
        return results.Any(r => r.gameObject != PanelArchivos.gameObject && r.gameObject.GetComponent<Button>() != null);
    }
}
