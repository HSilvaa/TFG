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
    public ChoseCharacterState(IMenuState menu) : base(menu)
    {
    }
    public async override void Enter()
    {
        CreateContinue = GameObject.Find("NewContinueThings").transform;
        newButt = CreateContinue.Find("New").GetComponent<Button>();

        continueButt = CreateContinue.Find("Continue").GetComponent<Button>();

        contextButt = CreateContinue.Find("CrearContexto").GetComponent<Button>();

        //Poner como ultimo hijo para que se pueda clickar bien
        CreateContinue.transform.SetAsLastSibling();

        Event.RaiseMenuChanged();

        TransicionEnter();

        newButt.onClick.AddListener(() =>
        {
            newState = new NewGameState(menu);
            TransicionExit();
        });

        continueButt.onClick.AddListener(() =>
        {
            newState = new ContinueGameState(menu);
            TransicionExit();
        });

        contextButt.onClick.AddListener(() =>
        {
            newState = new UploaderState(menu);
            TransicionExit();
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
