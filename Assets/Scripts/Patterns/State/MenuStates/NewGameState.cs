using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewGameState : AbstractMenuState
{
    private Transform NewThings;
    public TMP_InputField DescriptionField;
    public TMP_InputField NameField;
    public TMP_InputField AgeField;
    private ScriptableCharacter NewCharacter;
    public TMP_Dropdown EpocaField;

    private Button ContinueButton;
    private Button BackButt;

    private List<GameObject> ActiveGO;
    private AbstractMenuState state;

    private APIManager api;
    private NotificationManager notifManager;

    public NewGameState(IMenuState menu) : base(menu)
    {
        api = GameObject.FindObjectOfType<APIManager>();
        notifManager = GameObject.FindObjectOfType<NotificationManager>();
    }

    public override void Enter()
    {
        NewThings = GameObject.Find("NewThings").transform;
        ActiveGO = new List<GameObject>();

        // Localizar componentes
        DescriptionField = NewThings.Find("DescriptionField").GetComponent<TMP_InputField>();
        ActiveGO.Add(DescriptionField.gameObject);
        NameField = NewThings.Find("NameField").GetComponent<TMP_InputField>();
        ActiveGO.Add(NameField.gameObject);
        AgeField = NewThings.Find("AgeField").GetComponent<TMP_InputField>();
        ActiveGO.Add(AgeField.gameObject);
        EpocaField = NewThings.Find("EpocaField").GetComponent<TMP_Dropdown>();
        ActiveGO.Add(EpocaField.gameObject);

        NewCharacter = GameObject.Find("ScriptableCharacter").GetComponent<HolderScriptable>().data;
        ContinueButton = NewThings.Find("ContinueButtNewGame").GetComponent<Button>();
        ActiveGO.Add(ContinueButton.gameObject);
        BackButt = NewThings.Find("BackButtNewGame").GetComponent<Button>();
        ActiveGO.Add(BackButt.gameObject);

        // Desactivar botón hasta que carguen los contextos
        ContinueButton.interactable = false;

        // NUEVA LÓGICA: Consultar contextos disponibles al servidor
        api.GetContextos((listaContextos) => {
            if (listaContextos != null && listaContextos.Count > 0)
            {
                FillWithEpochs(listaContextos);
                ContinueButton.interactable = true;
            }
            else
            {
                Debug.LogWarning("No se encontraron contextos en el servidor.");
                notifManager.ShowNotification("Create a Context first!", Color.yellow, 3f);
            }
        });

        foreach (GameObject go in ActiveGO) go.SetActive(true);

        NewThings.transform.SetAsLastSibling();
        TransicionEnter();

        ContinueButton.onClick.AddListener(() =>
        {
            AddCharacterToDataBase();
        });

        BackButt.onClick.AddListener(() =>
        {
            state = new ChoseCharacterState(menu);
            TransicionExit();
        });
    }

    // Adaptado para recibir la lista directamente desde la API
    private void FillWithEpochs(List<string> contextos)
    {
        EpocaField.ClearOptions();
        EpocaField.AddOptions(contextos);
        Debug.Log($"Contextos cargados desde el servidor: {contextos.Count}");
    }

    private void AddCharacterToDataBase()
    {
        string name = NameField.text;
        string age = AgeField.text;
        string desc = DescriptionField.text;

        if (EpocaField.options.Count == 0) return;
        string epoca = EpocaField.options[EpocaField.value].text;

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(age) || string.IsNullOrEmpty(desc))
        {
            notifManager.ShowNotification("Please fill all fields", Color.yellow, 2f);
            return;
        }

        api.CreateCharacter(name, age, desc, epoca, (newChar) =>
        {
            NewCharacter.characterName = newChar.name;
            NewCharacter.characterAge = newChar.age;
            NewCharacter.characterDescription = newChar.description;
            NewCharacter.characterEpoca = newChar.epoca;
            NewCharacter.characterId = newChar.id;

            Debug.Log($"Personaje creado con éxito. ID: {newChar.id}");

            state = new ChattingMenuState(menu);
            TransicionExit();
        });
    }

    public override void Exit()
    {
        ContinueButton.onClick.RemoveAllListeners();
        BackButt.onClick.RemoveAllListeners();

        NameField.text = "";
        AgeField.text = "";
        DescriptionField.text = "";

        foreach (GameObject go in ActiveGO) go.SetActive(false);
        ActiveGO.Clear();
    }

    public override void FixedUpdate() { }
    public override void TransicionEnter() { }
    public override void TransicionExit() => menu.SetState(state);
    public override void Update() { }
}