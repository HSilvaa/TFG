using System.Collections.Generic;
using System;
using TMPro;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

public class LoaderState : AbstractMenuState
{
    private List<(string label, Func<Task> action)> loadingSteps;
    private Dictionary<string, float> stepDurations = new();
    private float elapsedTime = 0f;

    private bool loadingStarted = false;
    private bool transitionTriggered = false;

    GameObject root;

    GameObject loadBarMio;
    public LoaderState(IMenuState menu) : base(menu) { }

    public override void Enter()
    {
        root = GameObject.Find("LoadingThings");
        loadBarMio = root.transform.Find("LOADERMIO").gameObject;

        TransicionEnter();
        SetupSteps();
    }

    public override void Exit()
    {
        loadBarMio.SetActive(false);
    }

    public override async void TransicionEnter() { }

    public override async void TransicionExit()
    {
        menu.SetState(new ChoseCharacterState(menu));
    }

    public override void FixedUpdate() { }

    public override void Update()
    {
        if (!loadingStarted)
        {
            loadingStarted = true;
            _ = ExecuteLoadingSteps();
            return;
        }

        elapsedTime += Time.deltaTime;
    }

    private void SetupSteps()
    {
        loadingSteps = new List<(string, Func<Task>)>
        {
            ("Registrando servicios...", RegisterServices),
            ("Lanzando servidor Python...", StartPythonServer)
        };

        stepDurations["Registrando servicios..."] = 10f;
        stepDurations["Lanzando servidor Python..."] = 200f;
    }

    private async Task ExecuteLoadingSteps()
    {
        foreach (var (_, action) in loadingSteps)
        {
            await action();
        }
    }

    private Task RegisterServices()
    {;
        ServiceLocator.Register<KillProcessService>(new KillProcessService());
        ServiceLocator.Register<PythonLauncherService>(new PythonLauncherService());

        return Task.CompletedTask;
    }

    private async Task StartPythonServer()
    {
        await Task.Delay(500);
        bool isOpen = await ServiceLocatorManager.RunServiceWithResultAsync<PythonLauncherService, bool>("");
        GameObject.FindAnyObjectByType<LoadingBatteries>().ActivarCargaCompleta();

        await Task.Delay(3000); // opcional si deseas mantener algo de pausa

        TransicionExit();
        Debug.Log($"Servidor Python iniciado: {isOpen}");
    }
}