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
    {
        ServiceLocator.Register<GetEpoca>(new GetEpoca());
        ServiceLocator.Register<ChatWithNPC>(new ChatWithNPC());
        ServiceLocator.Register<KillProcessService>(new KillProcessService());
        ServiceLocator.Register<PythonLauncherService>(new PythonLauncherService());
        ServiceLocator.Register<CreateResumenService>(new CreateResumenService());
        ServiceLocator.Register<CreateContext>(new CreateContext());

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














//-------------------------------BARRA DE PROGRESO POCHA-------------------------------------
//using System.Collections.Generic;
//using System;
//using TMPro;
//using UnityEngine.UI;
//using System.Threading.Tasks;
//using UnityEngine;
//using DG.Tweening;

//public class LoaderState : AbstractMenuState
//{
//    public Slider progressBar;
//    public TMP_Text progressLabel;
//    public TMP_Text bienvenidoText;

//    private List<(string label, Func<Task> action)> loadingSteps;
//    private Dictionary<string, float> stepDurations = new();
//    private float elapsedTime = 0f;

//    private bool loadingStarted = false;
//    private bool transitionTriggered = false;

//    GameObject root;

//    GameObject loadBarMio;
//    public LoaderState(IMenuState menu) : base(menu) { }

//    public override void Enter()
//    {
//        //AudioManager.Instance.PlayClip("YouShouldntTryThat");

//        root = GameObject.Find("LoadingThings");
//        progressBar = root.transform.Find("LoadBar").GetComponent<Slider>();
//        progressLabel = root.transform.Find("LoadMessage").GetComponent<TMP_Text>();
//        bienvenidoText = root.transform.Find("Bienvenido").GetComponent<TMP_Text>();

//        loadBarMio = root.transform.Find("LOADERMIO").gameObject;

//        bienvenidoText.transform.DOScale(0.85f, 2f)
//            .SetEase(Ease.InOutSine)
//            .SetLoops(-1, LoopType.Yoyo);

//        //progressBar.gameObject.SetActive(true);
//        progressLabel.gameObject.SetActive(true);

//        TransicionEnter();

//        progressLabel.text = "Iniciando...";
//        SetupSteps();

//        // Por ejemplo, en cualquier script:
//        NotificationManager notifManager = GameObject.FindObjectOfType<NotificationManager>();
//        notifManager.ShowNotification("Cargando...", Color.yellow, 5f);
//    }

//    public override void Exit()
//    {
//        // Implementa transición a otro estado si es necesario
//        loadBarMio.SetActive(false);
//        progressLabel.gameObject.SetActive(false);
//    }

//    public override async void TransicionEnter()
//    {
//        //Transform parent = root.transform;
//        //parent.localScale = Vector3.zero;
//        //parent.DOScale(Vector3.one, 2f).SetEase(Ease.InOutSine);

//        //CanvasGroup labelGroup = progressLabel.GetComponent<CanvasGroup>();
//        //CanvasGroup barGroup = progressBar.GetComponent<CanvasGroup>();
//        //if (labelGroup == null) labelGroup = progressLabel.gameObject.AddComponent<CanvasGroup>();
//        //if (barGroup == null) barGroup = progressBar.gameObject.AddComponent<CanvasGroup>();

//        //labelGroup.alpha = 0;
//        //barGroup.alpha = 0;
//        //labelGroup.DOFade(1f, 2f);
//        //barGroup.DOFade(1f, 2f);

//    }

//    public override async void TransicionExit()
//    {
//        Sequence transitionSequence = DOTween.Sequence();
//        transitionSequence.Append(progressBar.transform.DOLocalMoveY(progressBar.transform.localPosition.y + 500f, 3f).SetEase(Ease.InOutSine));
//        //transitionSequence.Join(progressLabel.transform.DOLocalMoveY(progressLabel.transform.localPosition.y - 337f, 3f).SetEase(Ease.InOutSine));
//        transitionSequence.Join(bienvenidoText.transform.DOLocalMoveY(progressLabel.transform.localPosition.y + 700f, 3f).SetEase(Ease.InOutSine));
//        await transitionSequence.AsyncWaitForCompletion();

//        menu.SetState(new ChoseCharacterState(menu));
//    }
//    public override void FixedUpdate() { }

//    public override void Update()
//    {
//        if (!loadingStarted)
//        {
//            loadingStarted = true;
//            _ = ExecuteLoadingSteps();
//            return;
//        }

//        if (progressBar.value >= 1f && !transitionTriggered)
//        {
//            transitionTriggered = true;
//            progressLabel.text = "Carga completada!";

//            GameObject.FindAnyObjectByType<LoadingBatteries>().ActivarCargaCompleta();

//            TransicionExit();
//        }

//        elapsedTime += Time.deltaTime;
//    }

//    /// <summary>
//    /// Crea frase a mostrar + acción a ejecutar
//    /// </summary>
//    private void SetupSteps()
//    {
//        loadingSteps = new List<(string, Func<Task>)>
//        {
//            ("Registrando servicios...", RegisterServices),
//            ("Lanzando servidor Python...", StartPythonServer)
//        };

//        stepDurations["Registrando servicios..."] = 10f;
//        stepDurations["Lanzando servidor Python..."] = 60f;
//    }

//    /// <summary>
//    /// Ejecuta las acciones guardadas en LoadingSteps una a una. Solo una vez por acción y actualiza la barra de carga en consecuencia 
//    /// </summary>
//    /// <returns></returns>
//    private async Task ExecuteLoadingSteps()
//    {
//        float totalEstimated = 0f;
//        foreach (var step in loadingSteps)
//            totalEstimated += stepDurations[step.label];

//        float accumulatedProgress = 0f;

//        foreach (var (label, action) in loadingSteps)
//        {
//            progressLabel.text = label;
//            float stepDuration = stepDurations[label];
//            float targetProgress = accumulatedProgress + (stepDuration / totalEstimated);

//            var task = action();
//            float progressStart = progressBar.value;
//            float t = 0f;

//            while (!task.IsCompleted)
//            {
//                t += Time.deltaTime;
//                float progress = Mathf.Lerp(progressStart, targetProgress, Mathf.Clamp01(t / stepDuration));
//                progressBar.value = progress;
//                await Task.Yield();
//            }

//            await task;

//            while (progressBar.value < targetProgress)
//            {
//                progressBar.value = Mathf.MoveTowards(progressBar.value, targetProgress, Time.deltaTime * 0.5f);
//                await Task.Yield();
//            }

//            accumulatedProgress = targetProgress;
//        }

//        while (progressBar.value < 1f)
//        {
//            progressBar.value = Mathf.MoveTowards(progressBar.value, 1f, Time.deltaTime * 0.5f);
//            await Task.Yield();
//        }
//    }

//    /// <summary>
//    /// Registra los servicios de la app
//    /// </summary>
//    /// <returns></returns>
//    private Task RegisterServices()
//    {
//        ServiceLocator.Register<GetEpoca>(new GetEpoca());
//        ServiceLocator.Register<ChatWithNPC>(new ChatWithNPC());
//        ServiceLocator.Register<KillProcessService>(new KillProcessService());
//        ServiceLocator.Register<PythonLauncherService>(new PythonLauncherService());
//        ServiceLocator.Register<CreateResumenService>(new CreateResumenService());
//        ServiceLocator.Register<CreateContext>(new CreateContext());

//        return Task.CompletedTask;
//    }

//    /// <summary>
//    /// Lanza un nuevo servicio para LauchPythonServer
//    /// </summary>
//    /// <returns></returns>
//    private async Task StartPythonServer()
//    {
//        await Task.Delay(500); // pequeña espera 
//        bool isOpen = await ServiceLocatorManager.RunServiceWithResultAsync<PythonLauncherService, bool>("");
//        Debug.Log($"Servidor Python iniciado: {isOpen}");
//    }
//}

