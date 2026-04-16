using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

public class ContinueGameState : AbstractMenuState
{

    private Transform buttonContainer;
    private Button buttonPrefab;
    private TMP_Text noCharactersYet;
    private ScriptableCharacter characterSelected;
    private Transform panelDeBotones;

    private Button ContinueButton;
    private Button BackButt;

    private List<GameObject> ActiveGO;

    private GameObject itemPrefab;

    private AbstractMenuState state;
    private APIManager api;

    public ContinueGameState(IMenuState menu) : base(menu)
    {
        api = GameObject.FindObjectOfType<APIManager>();
    }
    public async override void Enter()
    {
        ActiveGO = new List<GameObject>();
        // Obtener referencias necesarias
        buttonContainer = GameObject.Find("ContinueThings").transform; //Panel activo donde están todos estos elementos
        var viewport = buttonContainer.Find("Viewport");
        panelDeBotones = viewport.Find("BotonPanel"); //Panel con un vertical layout donde es´tán los personajes guardados

        itemPrefab = buttonContainer.Find("CharacterButtonPrefab").gameObject;

        noCharactersYet = buttonContainer.Find("NoCharactersText").GetComponent<TMP_Text>(); //Texto si no hay characters
        characterSelected = GameObject.Find("ScriptableCharacter").GetComponent<HolderScriptable>().data; //Scriptable para guardar el personaje actual selecionado
        ContinueButton = buttonContainer.Find("ContinueButton").GetComponent<Button>(); //Botton de continuar
        BackButt = buttonContainer.Find("BackButton").GetComponent<Button>(); //Boton de retroceder

        ActiveGO.Add(ContinueButton.gameObject);
        ActiveGO.Add(BackButt.gameObject);

        //Poner como ultimo hijo para que se pueda clickar bien
        buttonContainer.transform.SetAsLastSibling();

        //Limpiar listeners
        ContinueButton.onClick.RemoveAllListeners();
        BackButt.onClick.RemoveAllListeners();

        TransicionEnter();

        ContinueButton.interactable = true;
        BackButt.interactable = true;

        //Para continuar
        ContinueButton.gameObject.SetActive(false);

        ContinueButton.onClick.AddListener(() =>
        {
            state = new ChattingMenuState(menu);
            TransicionExit();
        });

        //Para volver atrás
        BackButt.gameObject.SetActive(true);

        BackButt.onClick.AddListener(() =>
        {
            BackButt.interactable = false;
            state = new ChoseCharacterState(menu);
            TransicionExit();
        });

        noCharactersYet.gameObject.SetActive(true);
        ActiveGO.Add(noCharactersYet.gameObject);

        CreateCharacterButtons();
    }
    public void CreateCharacterButtons()
    {
        foreach (Transform child in panelDeBotones) GameObject.Destroy(child.gameObject);

        api.GetAllCharacters((characters) => {

            if (characters == null || characters.Count == 0)
            {
                noCharactersYet.gameObject.SetActive(true);
                noCharactersYet.text = "No hay personajes guardados";
                return;
            }

            noCharactersYet.gameObject.SetActive(false);

            foreach (var character in characters)
            {
                GameObject newItem = GameObject.Instantiate(itemPrefab, panelDeBotones);
                newItem.SetActive(true);

                Transform selectTransform = newItem.transform.Find("SelectChar");
                Button selectBut = selectTransform.GetComponent<Button>();

                selectTransform.Find("Name").GetComponent<TMP_Text>().text = character.Name;
                selectBut.onClick.AddListener(() => OnCharacterButtonClick(character));

                Button deleteBut = newItem.transform.Find("Delete").GetComponent<Button>();
                deleteBut.onClick.AddListener(() => OnDeleteClick(character.Id));
            }
        });
    }

    public void OnCharacterButtonClick(APIManager.CharacterData character)
    {
        characterSelected.characterName = character.Name;
        characterSelected.characterAge = character.Age;
        characterSelected.characterDescription = character.Description;
        characterSelected.characterEpoca = character.Epoca;
        characterSelected.characterId = character.Id;

        ContinueButton.gameObject.SetActive(true);

    }

    public void OnDeleteClick(int id)
    {
        api.DeleteCharacter(id, () => {
            CreateCharacterButtons();
            ContinueButton.gameObject.SetActive(false);
        });
    }

    public override void Exit()
    {
        foreach (Transform child in panelDeBotones) GameObject.Destroy(child.gameObject);
        foreach (GameObject go in ActiveGO) go.SetActive(false);
    }

    public override void TransicionExit() => menu.SetState(state);

    public override void TransicionEnter()
    {
        buttonContainer.parent.DOScale(Vector3.one, 1f).SetEase(Ease.OutBack);
    }

    public override void Update() { }
    public override void FixedUpdate() { }
}


