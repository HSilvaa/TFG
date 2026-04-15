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

    private AbstractMenuState state;

    public ContinueGameState(IMenuState menu) : base(menu)
    {
    }
    public async override void Enter()
    {
        ActiveGO = new List<GameObject>();
        // Obtener referencias necesarias
        buttonContainer = GameObject.Find("ContinueThings").transform; //Panel activo donde están todos estos elementos
        panelDeBotones = buttonContainer.Find("BotonPanel"); //Panel con un vertical layout donde es´tán los personajes guardados

        buttonPrefab = buttonContainer.Find("CharacterButtonPrefab").GetComponent<Button>(); //Prefab del boton
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

    public override void Exit()
    {
        foreach (Transform child in panelDeBotones)
            GameObject.Destroy(child.gameObject);

        foreach (GameObject go in ActiveGO)
        {
            go.SetActive(false);
        }
    }

    public override void FixedUpdate()
    {
    }

    public override void TransicionExit()
    {
        menu.SetState(state);
    }

    public override void TransicionEnter()
    {
        CanvasGroup backGroup = BackButt.GetComponent<CanvasGroup>();

        // Si no tienen CanvasGroup, se lo añadimos para el fade
        if (backGroup == null) backGroup = BackButt.gameObject.AddComponent<CanvasGroup>();

        backGroup.alpha = 0;
        backGroup.DOFade(1f, 2f);

        // Animación del panel
        buttonContainer.parent.DOScale(Vector3.one, 2f).SetEase(Ease.InOutSine);
    }

    public override void Update()
    {

    }

    /// <summary>
    /// Borra los botones existentens y los crea de nuevo
    /// </summary>
    public void CreateCharacterButtons()
    {
        List<SQLite.Character> characters = SQLite.Instance.GetCharacters();

        if (characters.Count == 0) { noCharactersYet.text = characters.Count == 0 ? "No hay personajes para seleccionar" : ""; return; }

        // Limpiar botones anteriores
        foreach (Transform child in panelDeBotones)
            GameObject.Destroy(child.gameObject);

        //Por cada personaje en la BD, crea un boton
        foreach (var character in characters)
        {
            Button newButton = GameObject.Instantiate(buttonPrefab, panelDeBotones);

            newButton.gameObject.SetActive(true);

            TMP_Text textName = newButton.transform.Find("Name").GetComponent<TMP_Text>();
            textName.text = character.Name;

            TMP_Text textDesc = newButton.transform.Find("Desc").GetComponent<TMP_Text>();
            textDesc.text = character.Description;

            newButton.onClick.AddListener(() => OnCharacterButtonClick(character));

            Button delteBut = newButton.transform.Find("Delete").GetComponent<Button>();
            delteBut.onClick.AddListener(() =>
            {
                OnDeleteClick(character.Id);
            });
        }
    }

    /// <summary>
    /// Cuando se hace click en un botón de personaje, se guarda en el SO la info
    /// </summary>
    /// <param name="character"></param>
    public void OnCharacterButtonClick(SQLite.Character character)
    {
        characterSelected.characterName = character.Name;
        characterSelected.characterAge = character.Age;
        characterSelected.characterDescription = character.Description;
        characterSelected.characterEpoca = character.Epoca;
        characterSelected.characterId = character.Id;

        ContinueButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Cuando se borra un personaje, se elimina de la BD
    /// </summary>
    /// <param name="id"></param>
    public void OnDeleteClick(int id)
    {
        SQLite.Instance.DeleteCharById(id);

        // Recargar personajes
        CreateCharacterButtons();

        // Ocultar botón de continuar
        ContinueButton.gameObject.SetActive(false);
    }
}


