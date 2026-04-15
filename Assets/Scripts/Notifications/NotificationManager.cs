using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    public GameObject notificationPrefab;
    public Transform notificationParent; // Un empty GameObject dentro del Canvas para contener las notificaciones

    // Método para crear una notificación
    public void ShowNotification(string message, Color color, float duration = 2f)
    {
        GameObject notif = Instantiate(notificationPrefab, notificationParent);
        Notification notificationScript = notif.GetComponent<Notification>();
        notificationScript.Setup(message, color, duration);
    }
}
