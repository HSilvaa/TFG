using DG.Tweening;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

    public NewGameState(IMenuState menu) : base(menu)
    {
        api = GameObject.FindObjectOfType<APIManager>();
    }

    public async override void Enter()
    {
        NewThings = GameObject.Find("NewThings").transform;
        ActiveGO = new List<GameObject>();

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

        api.GetRootFolder((rutaServidor) => {
            FillWithEpochs(rutaServidor);
            ContinueButton.interactable = true;
        });

        foreach (GameObject go in ActiveGO) go.SetActive(true);

        NewThings.transform.SetAsLastSibling();
        TransicionEnter();

        ContinueButton.onClick.AddListener(() =>
        {
            // Primero guardamos y, cuando el servidor responda, cambiamos de estado
            AddCharacterToDataBase();
        });

        BackButt.onClick.AddListener(() =>
        {
            state = new ChoseCharacterState(menu);
            TransicionExit();
        });
    }

    private void FillWithEpochs(string rootPath)
    {
        // Validación: si el servidor no tiene ruta o no existe localmente
        if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
        {
            Debug.LogError("La ruta del servidor no es accesible localmente: " + rootPath);
            return;
        }

        try
        {
            // Obtener las carpetas (Épocas)
            var directories = Directory.GetDirectories(rootPath);
            List<string> folderNames = directories
                .Select(dir => Path.GetFileName(dir))
                .ToList();

            EpocaField.ClearOptions();
            EpocaField.AddOptions(folderNames);

            Debug.Log($"Épocas cargadas desde: {rootPath}");
        }
        catch (Exception e)
        {
            Debug.LogError("Error al leer carpetas de época: " + e.Message);
        }
    }

    private void AddCharacterToDataBase()
    {
        string name = NameField.text;
        string age = AgeField.text;
        string desc = DescriptionField.text;
        string epoca = EpocaField.options[EpocaField.value].text;

        // Validación simple
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(age) || string.IsNullOrEmpty(desc))
        {
            Debug.LogError("Faltan campos por rellenar");
            return;
        }

        api.CreateCharacter(name, age, desc, epoca, (newId) =>
        {
            NewCharacter.characterName = name;
            NewCharacter.characterAge = age;
            NewCharacter.characterDescription = desc;
            NewCharacter.characterEpoca = epoca;
            NewCharacter.characterId = newId;

            Debug.Log($"Personaje creado en Backend con ID: {newId}");

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
        EpocaField.options[EpocaField.value].text = "";

        foreach (GameObject go in ActiveGO) go.SetActive(false);
        ActiveGO.Clear();
    }

    public override void FixedUpdate() { }
    public override void TransicionEnter() { }
    public override void TransicionExit() => menu.SetState(state);
    public override void Update() { }
}