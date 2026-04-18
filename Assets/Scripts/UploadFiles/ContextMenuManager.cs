using B83.Win32;
using DG.Tweening;
using SFB;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Unity.VisualScripting;
using Image = UnityEngine.UI.Image;
using UnityEngine.InputSystem.LowLevel;
using System;
using UnityEngine.EventSystems;
using System.Threading.Tasks;
public class ContextMenuManager : MonoBehaviour
{
    public GameObject contextMenu;
    public Button eliminarBtn;
    public Button copiarBtn;
    public Button pegarBtn;
    public Button crearCarpetaBtn;
    public Button renombrarBtn;

    public TMP_InputField renameInputField;
    public Button confirmRenameBtn;

    private string selectedPath; // ruta del archivo o carpeta seleccionada
    private string copiedPath;
    private bool isFileCopied;
    private string pathBeingRenamed;
    private bool isRenamingNewFolder = false;

    public static event Action OnRefreshRequested;
    public static event Action<GameObject> OnRenameRequested;

    private string currentFolder;

    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;

    void Start()
    {
        pegarBtn.interactable = false;

        eliminarBtn.onClick.AddListener(() => DeleteSelected());
        copiarBtn.onClick.AddListener(() => CopySelected());
        pegarBtn.onClick.AddListener(() => PasteToCurrentFolder());
        crearCarpetaBtn.onClick.AddListener(() => BeginCreateNewFolder());
        renombrarBtn.onClick.AddListener(() => BeginRename());

        contextMenu.gameObject.SetActive(false);

        renameInputField.gameObject.SetActive(false);
        confirmRenameBtn.gameObject.SetActive(false);

        confirmRenameBtn.onClick.AddListener(OnConfirmRename);
    }

    void OnEnable()
    {
        UploaderState.OnCurrentFolderChanged += UpdateCurrentFolder;
    }

    void OnDisable()
    {
        UploaderState.OnCurrentFolderChanged -= UpdateCurrentFolder;
    }
    void BeginCreateNewFolder()
    {
        // Ya no ocultamos el menú
        // HideMenu();

        string newFolderPath = Path.Combine(currentFolder, "NuevaCarpeta");
        int count = 1;
        while (Directory.Exists(newFolderPath))
            newFolderPath = Path.Combine(currentFolder, "NuevaCarpeta" + count++);

        pathBeingRenamed = newFolderPath;
        isRenamingNewFolder = true;

        ShowRenameInputAtContextMenu();
    }

    void BeginRename()
    {
        if (string.IsNullOrEmpty(selectedPath))
            return;

        // Ya no ocultamos el menú
        // HideMenu();

        pathBeingRenamed = selectedPath;
        isRenamingNewFolder = false;

        ShowRenameInputAtContextMenu();
    }

    void ShowRenameInputAtContextMenu()
    {
        // Posicionamos el input y botón a la derecha del contextMenu con un offset X de 10
        Vector3 menuPos = contextMenu.transform.position;
        renameInputField.transform.position = menuPos + new Vector3(contextMenu.GetComponent<RectTransform>().rect.width / 2 + 30f, -35f, 0f);
        confirmRenameBtn.transform.position = renameInputField.transform.position + new Vector3(renameInputField.GetComponent<RectTransform>().rect.width / 2 + 0f, 0f, 0f);

        renameInputField.gameObject.SetActive(true);
        confirmRenameBtn.gameObject.SetActive(true);

        string nameToShow;

        if (Directory.Exists(pathBeingRenamed))
        {
            // Es carpeta: solo el nombre de la carpeta
            nameToShow = Path.GetFileName(pathBeingRenamed);
        }
        else if (File.Exists(pathBeingRenamed))
        {
            // Es archivo: nombre sin extensión
            nameToShow = Path.GetFileNameWithoutExtension(pathBeingRenamed);
        }
        else
        {
            // Por si acaso (nuevo folder no creado aún)
            nameToShow = Path.GetFileName(pathBeingRenamed);
        }

        renameInputField.text = nameToShow;


        renameInputField.ActivateInputField();
        renameInputField.Select();
    }

    void OnConfirmRename()
    {
        string newName = renameInputField.text.Trim();

        if (string.IsNullOrEmpty(newName))
        {
            HideRenameInput();
            return;
        }

        string directory = Path.GetDirectoryName(pathBeingRenamed);

        if (File.Exists(pathBeingRenamed))
        {
            string originalExtension = Path.GetExtension(pathBeingRenamed);
            if (!newName.EndsWith(originalExtension, StringComparison.OrdinalIgnoreCase))
            {
                newName += originalExtension;
            }
        }

        string newPath = Path.Combine(directory, newName);

        try
        {
            if (isRenamingNewFolder)
            {
                if (!Directory.Exists(pathBeingRenamed))
                {
                    Directory.CreateDirectory(newPath);
                }
                else
                {
                    Directory.Move(pathBeingRenamed, newPath);
                }
            }
            else
            {
                if (Directory.Exists(pathBeingRenamed))
                    Directory.Move(pathBeingRenamed, newPath);
                else if (File.Exists(pathBeingRenamed))
                    File.Move(pathBeingRenamed, newPath);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error al renombrar: {ex.Message}");
        }

        HideRenameInput();
        HideMenu();
        OnRefreshRequested?.Invoke();
    }

    void HideRenameInput()
    {
        renameInputField.gameObject.SetActive(false);
        confirmRenameBtn.gameObject.SetActive(false);
    }

    void UpdateCurrentFolder(string newPath)
    {
        currentFolder = newPath;

        pegarBtn.interactable = !string.IsNullOrEmpty(copiedPath);
    }

    public void ShowMenu(string path, Vector2 screenPosition, bool isFileOrFolder = true)
    {
        selectedPath = path;
        contextMenu.SetActive(true);

        // Si es un archivo o carpeta, activamos las acciones de edición
        eliminarBtn.interactable = isFileOrFolder;
        copiarBtn.interactable = isFileOrFolder;
        renombrarBtn.interactable = isFileOrFolder;

        crearCarpetaBtn.interactable = true;
        pegarBtn.interactable = !string.IsNullOrEmpty(copiedPath);

        HideRenameInput();
        contextMenu.transform.position = screenPosition;
    }

    public void ShowMenuSimple(string path, Vector2 screenPosition)
    {
        selectedPath = path;
        contextMenu.SetActive(true);

        // Si clicamos en el FONDO: Deshabilitamos acciones de selección
        eliminarBtn.interactable = false;
        copiarBtn.interactable = false;
        renombrarBtn.interactable = false;

        // Acciones de fondo: Crear carpeta siempre, Pegar solo si hay algo
        crearCarpetaBtn.interactable = true;
        pegarBtn.interactable = !string.IsNullOrEmpty(copiedPath);

        HideRenameInput();
        contextMenu.transform.position = screenPosition;
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Click izquierdo
        {
            if (!IsPointerOverUIObject(contextMenu) && !IsPointerOverUIObject(renameInputField.gameObject) && !IsPointerOverUIObject(confirmRenameBtn.gameObject))
            {
                HideMenu();
            }
        }
    }

    private bool IsPointerOverUIObject(GameObject target)
    {
        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == target || result.gameObject.transform.IsChildOf(target.transform))
            {
                return true;
            }
        }

        return false;
    }

    public void HideMenu()
    {
        contextMenu.SetActive(false);
        renameInputField.gameObject.SetActive(false);
    }

    void DeleteSelected()
    {
        try
        {
            if (Directory.Exists(selectedPath))
            {
                // Revisa si es un enlace simbólico
                var dirInfo = new DirectoryInfo(selectedPath);
                if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    Debug.LogWarning($"'{selectedPath}' es un enlace simbólico, no se elimina recursivamente.");
                    dirInfo.Delete(); // Borra solo el enlace
                    return;
                }

                // Borrado seguro sin caer en loops
                DeleteDirectorySafely(selectedPath);
            }
            else if (File.Exists(selectedPath))
            {
                File.Delete(selectedPath);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error al eliminar '{selectedPath}': {ex.Message}");
            return;
        }

        HideMenu();
        OnRefreshRequested?.Invoke();
    }

    void DeleteDirectorySafely(string path)
    {
        foreach (string dir in Directory.GetDirectories(path))
        {
            var subDir = new DirectoryInfo(dir);
            if ((subDir.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                Debug.LogWarning($"Se saltó enlace simbólico: {dir}");
                continue;
            }

            DeleteDirectorySafely(dir);
        }

        foreach (string file in Directory.GetFiles(path))
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        Directory.Delete(path, false);
    }

    void CopySelected()
    {
        copiedPath = selectedPath;
        isFileCopied = File.Exists(copiedPath);
        pegarBtn.interactable = true;
        HideMenu();
    }

    async void PasteToCurrentFolder()
    {
        if (string.IsNullOrEmpty(copiedPath))
            return;

        string dest = Path.Combine(currentFolder, Path.GetFileName(copiedPath));

        try
        {
            if (isFileCopied)
            {
                dest = GetUniqueFilePath(dest);
                File.Copy(copiedPath, dest, false);
            }
            else
            {
                dest = GetUniqueFolderPath(dest);

                // Protección contra copias recursivas
                if (IsSubdirectory(copiedPath, dest))
                {
                    GameObject.FindObjectOfType<NotificationManager>().ShowNotification("Error: La carpeta destino es una subcarpeta de la carpeta origen", Color.red, 5f);
                    return;
                }

                CopyFolderRecursive(copiedPath, dest);
            }

            copiedPath = null;
            isFileCopied = false;
            pegarBtn.interactable = false;

            OnRefreshRequested?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error al pegar: {ex.Message}");
        }

        HideMenu();
    }

    bool IsSubdirectory(string parentPath, string childPath)
    {
        var parentUri = new Uri(parentPath.EndsWith("\\") ? parentPath : parentPath + "\\");
        var childUri = new Uri(childPath.EndsWith("\\") ? childPath : childPath + "\\");

        return parentUri.IsBaseOf(childUri);
    }

    string GetUniqueFilePath(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath);
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        string extension = Path.GetExtension(filePath);

        int count = 1;
        string newPath = filePath;

        while (File.Exists(newPath))
        {
            newPath = Path.Combine(directory, $"{fileNameWithoutExt}({count}){extension}");
            count++;
        }

        return newPath;
    }

    string GetUniqueFolderPath(string folderPath)
    {
        int count = 1;
        string newPath = folderPath;

        while (Directory.Exists(newPath))
        {
            newPath = folderPath + $"({count})";
            count++;
        }

        return newPath;
    }

    System.Collections.IEnumerator InvokeRenameOnNextFrame(string folderPath)
    {
        yield return null; // Espera 1 frame
                           // Llama al evento con la nueva ruta
        GameObject newFolderButton = FindButtonByPath(folderPath);
        if (newFolderButton != null)
            OnRenameRequested?.Invoke(newFolderButton);
    }

    private GameObject FindButtonByPath(string path)
    {
        foreach (Transform child in GameObject.Find("ContentPanel").transform)
        {
            string childPath = Path.Combine(currentFolder, child.GetComponentInChildren<TMP_Text>().text);
            if (childPath == path)
                return child.gameObject;
        }
        return null;
    }

    void CopyFolderRecursive(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);

        foreach (var dir in Directory.GetDirectories(sourceDir))
            CopyFolderRecursive(dir, Path.Combine(destDir, Path.GetFileName(dir)));
    }
}
