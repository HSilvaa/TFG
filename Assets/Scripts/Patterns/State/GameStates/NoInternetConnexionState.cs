using UnityEngine;
using UnityEngine.Networking;

public class NoInternetConnexionState : AbstractGameControllerState
{
    /// <summary>
    /// Checks if there is internet connexion or not. If not, starts the emergency
    /// </summary>
    GameObject emergencyGameObject;
    string clipName = "NoInternetConnexion";

    float checkInterval = 5f; // Intervalo entre comprobaciones en segundos
    float checkTimer = 0f;
    bool isChecking = false;
    bool isEmergencySet = false;
    NotificationManager notifManager;


    public NoInternetConnexionState(IGameState game) : base(game)
    {
        notifManager = GameObject.FindObjectOfType<NotificationManager>();
    }

    public override void Enter()
    {
        checkTimer = 0f;
    }

    public override void Exit()
    {
    }

    public override void FixedUpdate() { }

    public override void Update()
    {
        checkTimer += Time.deltaTime;

        if (checkTimer >= checkInterval && !isChecking)
        {
            checkTimer = 0f;
            isChecking = true;
            CheckInternetConnection();
        }
    }

    private void CheckInternetConnection()
    {
        UnityWebRequest request = new UnityWebRequest("https://clients3.google.com/generate_204");
        request.method = UnityWebRequest.kHttpVerbGET;
        request.timeout = 5;
        request.downloadHandler = new DownloadHandlerBuffer();

        var operation = request.SendWebRequest();
        operation.completed += (asyncOp) =>
        {
            bool isOnline = !request.isNetworkError && !request.isHttpError && request.responseCode == 204;

            if (isOnline)
            {
                if(isEmergencySet == true)
                {
                    notifManager.ShowNotification("Connection Established", Color.green, 5f);
                }

                isEmergencySet = false;
                game.SetState(new NoPythonServerState(game)); // Goes over all possible emergencies
            }
            else if(!isEmergencySet)
            {
                isEmergencySet = true;
                notifManager.ShowNotification("No Internet Connection. Reconnecting...", Color.red, 5f);
                Debug.Log("Sin conexión todavía...");
            }

            isChecking = false;
            request.Dispose();
        };
    }
}
