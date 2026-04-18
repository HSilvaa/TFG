using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    public GameObject notificationPrefab;
    public Transform notificationParent; 

    public void ShowNotification(string message, Color color, float duration = 2f)
    {
        GameObject notif = Instantiate(notificationPrefab, notificationParent);
        Notification notificationScript = notif.GetComponent<Notification>();
        notificationScript.Setup(message, color, duration);
    }
}
