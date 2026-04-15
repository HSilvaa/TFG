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
    public TextMeshProUGUI PathText;

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

            if (pointerData.button == PointerEventData.InputButton.Right)
            {
                Vector2 screenPos = pointerData.position;
                contextMenuScript.ShowMenuSimple(PathText.text, screenPos);
            }
        });

        trigger.triggers.Add(rightClickEntry);
    }
}
