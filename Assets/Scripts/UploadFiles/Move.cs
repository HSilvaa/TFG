using UnityEngine;
using DG.Tweening;
using UnityEngine.UI; // ← este es el correcto para UI en Unity

public class Move : MonoBehaviour
{
    public float moveDistance = 1f;   // Distancia del movimiento
    public float moveDuration = 0.3f; // Duración del movimiento y rotación
    public RectTransform targetToMove; // El objeto que se mueve (puede ser un panel)
    public RectTransform arrowButton;  // El botón con la flecha (imagen)

    private bool isExpanded = false;  // Estado actual (expandido o contraído)

    public void ToggleMove()
    {
        if (!targetToMove) return;

        // Alternar el estado
        isExpanded = !isExpanded;

        // Mover hacia arriba o abajo según el estado
        float direction = isExpanded ? moveDistance : -moveDistance;
        targetToMove.DOMoveY(targetToMove.position.y + direction, moveDuration);

        // Rotar la flecha 0° ↔ 180°
        float targetRotation = isExpanded ? 90f : -90f;
        arrowButton.DORotate(new Vector3(0, 0, targetRotation), moveDuration);
    }
}
