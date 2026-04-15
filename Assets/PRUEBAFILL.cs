using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
public class PRUEBAFILL : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    [Header("UI")]
    [Tooltip("La imagen de la barra con 'Image Type = Filled' y 'Fill Method = Horizontal'.")]
    public Image barra;

    [Header("Sprites")]
    [Tooltip("Sprite que se mostrará cuando la barra esté vacía (fillAmount = 0).")]
    public Sprite spriteVacio;
    [Tooltip("Sprite que se mostrará cuando la barra esté llena (fillAmount = 1).")]
    public Sprite spriteLleno;

    [Tooltip("Texto para timear la velocidad de la barra")]
    public TextMeshProUGUI _text;

    // ——— Internos ———
    float destino = 0f;           // 0 = vacío, 1 = lleno
    float Velocidad;

    private void Start()
    {
        float duracion = _text.GetComponent<HackerText>().GetTotalTime();
        Velocidad = 1f / duracion; 
    }
    void Reset()
    {
        barra = GetComponent<Image>();
    }

    private void OnDisable()
    {
        this.GetComponent<Image>().sprite = spriteVacio;
        barra.fillAmount = 0;
        destino = 0;
    }

    void Update()
    {
        // Desplazamos suavemente el fillAmount hacia el destino
        if (!Mathf.Approximately(barra.fillAmount, destino))
        {
            barra.fillAmount = Mathf.MoveTowards(barra.fillAmount, destino, Velocidad * Time.deltaTime);

            // Al llegar al extremo, cambiamos sprite si es necesario
            if (barra.fillAmount < 0.8f)
                this.GetComponent<Image>().sprite = spriteVacio;
            else if (Mathf.Approximately(barra.fillAmount, 1f))
                this.GetComponent<Image>().sprite = spriteLleno;
        }
    }

    // ——— Eventos del puntero ———
    public void OnPointerEnter(PointerEventData eventData)
    {
        destino = 1f;                    // Empieza a llenar
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        destino = 0f;                    // Empieza a vaciar
    }
}

