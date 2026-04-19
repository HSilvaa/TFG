using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

public class ChoseCharacterState : AbstractMenuState
{
    private Transform CreateContinue;

    private Button newButt;
    private Button continueButt;
    private Button contextButt;

    AbstractMenuState newState;
    private APIManager api;
    NotificationManager notifManager;


    public ChoseCharacterState(IMenuState menu) : base(menu)
    {
        api = GameObject.FindObjectOfType<APIManager>();
        notifManager = GameObject.FindObjectOfType<NotificationManager>();
    }
    public async override void Enter()
    {
        CreateContinue = GameObject.Find("NewContinueThings").transform;
        newButt = CreateContinue.Find("New").GetComponent<Button>();
        continueButt = CreateContinue.Find("Continue").GetComponent<Button>();
        contextButt = CreateContinue.Find("CrearContexto").GetComponent<Button>();

        CreateContinue.transform.SetAsLastSibling();
        Event.RaiseMenuChanged();
        TransicionEnter();

        newButt.interactable = true;
        continueButt.interactable = true;
        contextButt.interactable = true;

        newButt.onClick.AddListener(() => {
            CheckIfContextIsCreated();
        });

        continueButt.onClick.AddListener(() => {
            newState = new ContinueGameState(menu);
            TransicionExit();
        });

        contextButt.onClick.AddListener(() => {
            newState = new UploaderState(menu);
            TransicionExit();
        });
    }

    private void CheckIfContextIsCreated()
    {
        api.GetRootFolder((rutaServidor) => {
            if (string.IsNullOrEmpty(rutaServidor))
            {
                Debug.LogWarning("Contexto no detectado (Ruta vacía o error de red)");
                notifManager.ShowNotification("Context not created yet", Color.red, 2f);
            }
            else
            {
                Debug.Log("Contexto detectado: " + rutaServidor);
                newState = new NewGameState(menu);
                TransicionExit();
            }
        });
    }

    public override void Exit() //Reset por si vuelve atrás
    {

    }

    public override void FixedUpdate()
    {
    }

    public override async void TransicionEnter()
    {
        newButt.gameObject.SetActive(true);
        continueButt.gameObject.SetActive(true);
        contextButt.gameObject.SetActive(true);


        newButt.onClick.RemoveAllListeners();
        continueButt.onClick.RemoveAllListeners();
        contextButt.onClick.RemoveAllListeners();
    }
    public override async void TransicionExit()
    {
        newButt.gameObject.SetActive(false);
        continueButt.gameObject.SetActive(false);
        contextButt.gameObject.SetActive(false);

        menu.SetState(newState);
    }
    public override void Update()
    {

    }

}
