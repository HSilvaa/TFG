using DG.Tweening;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

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

    public NewGameState(IMenuState menu) : base(menu)
    {
    }
    public async override void Enter()
    {
        // Obtener referencias necesarias
        NewThings = GameObject.Find("NewThings").transform; //Panel activo donde están todos estos elementos

        ActiveGO = new List<GameObject>();

        DescriptionField = NewThings.Find("DescriptionField").GetComponent<TMP_InputField>(); //Escribir la descripcion
        ActiveGO.Add(DescriptionField.gameObject);
        NameField = NewThings.Find("NameField").GetComponent<TMP_InputField>(); //Escribir el nombre
        ActiveGO.Add(NameField.gameObject);
        AgeField = NewThings.Find("AgeField").GetComponent<TMP_InputField>(); //Escribir la edad
        ActiveGO.Add(AgeField.gameObject);
        EpocaField = NewThings.Find("EpocaField").GetComponent<TMP_Dropdown>(); //Determinar la epoca (desplegable)
        ActiveGO.Add(EpocaField.gameObject);

        NewCharacter = GameObject.Find("ScriptableCharacter").GetComponent<HolderScriptable>().data; //Scriptable para guardar el personaje actual selecionado
        ContinueButton = NewThings.Find("ContinueButtNewGame").GetComponent<Button>(); //Botton de continuar
        ActiveGO.Add(ContinueButton.gameObject);
        BackButt = NewThings.Find("BackButtNewGame").GetComponent<Button>(); //Boton de retroceder
        ActiveGO.Add(BackButt.gameObject);

        foreach (GameObject go in ActiveGO)
        {
            go.SetActive(true);
        }

        //Poner como ultimo hijo para que se pueda clickar bien
        NewThings.transform.SetAsLastSibling();

        TransicionEnter();

        ContinueButton.onClick.AddListener(() =>
        {
            state = new ChattingMenuState(menu);

            AddCharacterToDataBase();

            TransicionExit();
        });

        BackButt.onClick.AddListener(() =>
        {
            state = new ChoseCharacterState(menu);

            TransicionExit();
        });

        List<string> epochs = FillWithEpochs();
        EpocaField.ClearOptions();
        EpocaField.AddOptions(epochs); 
    }

    private List<string> FillWithEpochs()
    {
        // Obtener la ruta raíz desde SQLite
        string rootPath = SQLite.Instance.GetFolder(1).Route;

        // Validación básica
        if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
            return new List<string>();

        // Obtener todas las carpetas directas dentro de rootPath
        var directories = Directory.GetDirectories(rootPath);

        // Obtener solo los nombres (no las rutas completas)
        List<string> folderNames = directories
            .Select(dir => Path.GetFileName(dir))
            .ToList();

        return folderNames;
    }


    private async void AddCharacterToDataBase()
    {
        List<string> values = new List<string>();
        values.Add(NameField.text);
        values.Add(AgeField.text);
        values.Add(DescriptionField.text);
        values.Add(EpocaField.options[EpocaField.value].text);

        //Comprobamos que no haya valores nulos/empty
        foreach (string value in values)
        {
            if (!string.IsNullOrEmpty(value))
            {
                continue;
            }
            else
            {
                throw new Exception("Todos los campos deben contener un valor");
            }
        }

        NewCharacter.characterName = NameField.text;
        NewCharacter.characterAge = AgeField.text;
        NewCharacter.characterDescription = DescriptionField.text;

        NewCharacter.characterEpoca = EpocaField.options[EpocaField.value].text;

        //Si está en auto, llamamos al servicio para que nos detecte la epoca
        //if (NewCharacter.characterEpoca.Equals("AUTO"))
        //{
        //    NewCharacter.characterEpoca = FindEpoca(EpocaField.options[EpocaField.value].text).ToString();
        //}

        int charId = SQLite.Instance.AddCharacter(NewCharacter.characterName, NewCharacter.characterAge, NewCharacter.characterDescription, NewCharacter.characterEpoca);

        NewCharacter.characterId = charId;

        values.Clear();
    }
    private async Task<string> FindEpoca(string epoca)
    {
        return await ServiceLocatorManager.RunServiceWithResultAsync<GetEpoca, string>(NewCharacter);
    }

    public override void Exit()
    {

        foreach (GameObject go in ActiveGO)
        {
            go.SetActive(false);
        }

        ActiveGO.Clear();
    }

    public override void FixedUpdate()
    {
    }

    public override async void TransicionEnter()
    {

    }

    public override async void TransicionExit()
    {
        menu.SetState(state);
    }

    public override void Update()
    {

    }
}


