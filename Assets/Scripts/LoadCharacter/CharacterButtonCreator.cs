using UnityEngine;
using UnityEngine.UI; // Para trabajar con UI
using TMPro; // Para usar TextMeshPro si deseas personalizar texto
using System.Collections.Generic;

public class CharacterButtonCreator : MonoBehaviour
{
    public Transform buttonContainer; // Este será el contenedor donde se generarán los botones.
    public Button buttonPrefab; // El prefab del botón que se clonará para cada personaje
    public TMP_Text noCharactersYet; // El texto que se asociará a cada botón
    public ScriptableCharacter characterSelected;

    public Button ContinueButton; // El prefab del botón que se clonará para cada personaje
    void Start()
    {
        ContinueButton.gameObject.SetActive(false);
        CreateCharacterButtons();
    }

    /// <summary>
    /// Crea un boton por cada personaje en la base de datos
    /// </summary>
    public void CreateCharacterButtons()
    {
        // Primero obtenemos la lista de personajes de la base de datos
        List<SQLite.Character> characters = SQLite.Instance.GetCharacters();

        foreach (SQLite.Character character in characters)
        {
            // Creamos un nuevo botón a partir del prefab
            Button newButton = Instantiate(buttonPrefab, buttonContainer);

            // Establecemos el texto del botón con el nombre del personaje
            TMP_Text textName = newButton.transform.Find("Name").GetComponent<TMP_Text>();
            textName.text = character.Name;

            TMP_Text textDesc = newButton.transform.Find("Desc").GetComponent<TMP_Text>();
            textDesc.text = character.Description;

            // Agregamos un evento para manejar lo que sucede cuando se hace clic en el botón
            newButton.onClick.AddListener(() => OnCharacterButtonClick(character));

            Button delteBut = newButton.transform.Find("Delete").GetComponent<Button>();
            delteBut.onClick.AddListener(() => OnDeleteClick(character.Id));
        }

        if(characters.Count == 0)
        {
            noCharactersYet.text = "No hay personajes para seleccionar";
        }
        else
        {
            noCharactersYet.text = "";
        }
    }

    /// <summary>
    /// Evento que se le asocia cuando clickas en el personaje
    /// </summary>
    /// <param name="character"></param>
    public void OnCharacterButtonClick(SQLite.Character character)
    {
        // Asignamos la información del personaje al ScriptableObject
        characterSelected.characterName = character.Name;
        characterSelected.characterAge = character.Age;
        characterSelected.characterDescription = character.Description;
        characterSelected.characterEpoca = character.Epoca;

        ContinueButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Borra un personaje de la base de datos
    /// </summary>
    /// <param name="id"></param>
    public void OnDeleteClick(int id)
    {
        SQLite.Instance.DeleteCharById(id);

        // Limpiar botones actuales
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // Regenerar los botones actualizados
        CreateCharacterButtons();

        // Ocultar el botón de continuar en caso de que el personaje seleccionado ya no exista
        ContinueButton.gameObject.SetActive(false);
    }
}
