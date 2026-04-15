using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections;
using TMPro;

public class Notification : MonoBehaviour, IPointerClickHandler
{
    public TMP_Text messageText;
    public Image backgroundImage;

    private RectTransform rectTransform;
    private float animationDuration = 0.4f; // duración de entrada/salida

    private bool isExiting = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        messageText = GetComponentInChildren<TMP_Text>();
    }

    public void Setup(string message, Color color, float duration = 1.5f)
    {
        messageText.text = message;
        backgroundImage.color = color;

        // Posición inicial (fuera de pantalla hacia arriba)
        Vector2 startPos = rectTransform.anchoredPosition;
        //startPos.y += 200f;
        //rectTransform.anchoredPosition = startPos;

        // Animación de entrada (bajar)
        //rectTransform.DOAnchorPosY(startPos.y - 200f, animationDuration).SetEase(Ease.OutCubic);

        // Esperar y luego animar salida
        StartCoroutine(ExitAfterDelay(duration));
    }

    private IEnumerator ExitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ExitAndDestroy();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ExitAndDestroy();
    }

    private void ExitAndDestroy()
    {
        if (isExiting) return;
        isExiting = true;

        // Animación de salida (subir) y destruir
        rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y + 200f, animationDuration)
                     .SetEase(Ease.InCubic)
                     .OnComplete(() => Destroy(gameObject));
    }
}
