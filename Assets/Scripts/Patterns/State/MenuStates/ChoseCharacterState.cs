using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

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

    public override void Enter()
    {
        CreateContinue = GameObject.Find("NewContinueThings").transform;
        newButt = CreateContinue.Find("New").GetComponent<Button>();
        continueButt = CreateContinue.Find("Continue").GetComponent<Button>();
        contextButt = CreateContinue.Find("CrearContexto").GetComponent<Button>();

        newButt.gameObject.SetActive(true);
        continueButt.gameObject.SetActive(true);
        contextButt.gameObject.SetActive(true);

        newButt.interactable = true;
        continueButt.interactable = true;
        contextButt.interactable = true;

        CreateContinue.transform.SetAsLastSibling();

        newButt.onClick.RemoveAllListeners();
        continueButt.onClick.RemoveAllListeners();
        contextButt.onClick.RemoveAllListeners();

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

        Event.RaiseMenuChanged();
        TransicionEnter();
    }

    private void CheckIfContextIsCreated()
    {
        newButt.interactable = false;

        api.GetAllCharacters((listaPersonajes) => {
            if (listaPersonajes != null)
            {
                Debug.Log("Conexión con servidor establecida. Procediendo a nuevo juego.");
                newState = new NewGameState(menu);
                TransicionExit();
            }
            else
            {
                Debug.LogWarning("No se puede iniciar juego: Error de comunicación o contexto no inicializado.");
                notifManager.ShowNotification("Server not ready or Context missing", Color.red, 2f);
                newButt.interactable = true; // Reactivar si falla
            }
        });
    }

    public override async void TransicionExit()
    {
        if (newButt != null) newButt.gameObject.SetActive(false);
        if (continueButt != null) continueButt.gameObject.SetActive(false);
        if (contextButt != null) contextButt.gameObject.SetActive(false);

        if (newButt != null) newButt.onClick.RemoveAllListeners();
        if (continueButt != null) continueButt.onClick.RemoveAllListeners();
        if (contextButt != null) contextButt.onClick.RemoveAllListeners();

        menu.SetState(newState);
    }

    public override void Exit() { }
    public override void FixedUpdate() { }
    public override void Update() { }
    public override void TransicionEnter() { }
}