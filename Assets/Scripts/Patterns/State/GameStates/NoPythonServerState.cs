using System;
using System.Net.Http;
using UnityEngine;

public class NoPythonServerState : AbstractGameControllerState
{
    GameObject emergencyGameObject;
    string clipName = "NotPythonLaunched";

    float checkInterval = 5f;
    float checkTimer = 0f;
    bool isChecking = false;
    bool isEmergencySet = false;

    int maxRetries = 3;
    int currentRetries = 0;

    private string serverUrl = "http://127.0.0.1:8000/status";

    private static readonly HttpClient httpClient = new HttpClient();

    public NoPythonServerState(IGameState game) : base(game) { }

    public override void Enter()
    {
        checkTimer = 0f;
        currentRetries = 0;
        Debug.Log("Esperando al servidor Python...");
    }

    public override void Exit()
    {
       Debug.Log(" Conectado con Python. Emergencia terminada.");
    }

    public override void FixedUpdate() { }

    public override void Update()
    {
        checkTimer += Time.deltaTime;

        if (checkTimer >= checkInterval && !isChecking)
        {
            checkTimer = 0f;
            isChecking = true;
            CheckPythonConnection();
        }
    }

    private async void CheckPythonConnection()
    {
        bool isServerUp = await IsPythonServerRunning();

        if (isServerUp)
        {
            isEmergencySet = false;
            game.SetState(new NoProblemState(game));
        }
        else
        {
            Debug.LogWarning($"Python no responde. Intentando lanzarlo... (Intento {currentRetries + 1})");

            if (!isEmergencySet)
            {
                isEmergencySet = true;
            }

            bool launched = await TryLaunchPythonServer();
            currentRetries++;

        }

        isChecking = false;
    }

    private async System.Threading.Tasks.Task<bool> IsPythonServerRunning()
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync(serverUrl);
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody.Contains("\"status\":\"ok\"");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Error al contactar el servidor Python: " + ex.Message);
        }
        return false;
    }

    private async System.Threading.Tasks.Task<bool> TryLaunchPythonServer()
    {
        try
        {
            return await ServiceLocatorManager.RunServiceWithResultAsync<PythonLauncherService, bool>("");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error al intentar lanzar Python: " + ex.Message);
            return false;
        }
    }
}
