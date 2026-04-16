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
using UnityEngine.EventSystems;
using System;
using System.Threading.Tasks;

public class UploaderState : AbstractMenuState
{
    // Parent Containers
    private Transform SubirArchivosRoot;
    private Transform PanelArchivos;

    //Other variables UI
    [Header("UI")]
    public RectTransform contentPanel; //Scroll contentn
    public GameObject FileButtonPrefab; //FileButtonPrefab
    public Button Save;                 // Boton de guardar
    public TextMeshProUGUI PathText;
    public Button SetRuta;  //Ruta editable boton
    public Button Atras;  //Go Back In Files
    public Button Retroceder;  //Go Back In Files
    public Button SubirArchivos;  //UploadFiles from Explorador de archivos

    // Menu contextual
    public ContextMenuManager contextMenuScript;
    public static event Action<string> OnCurrentFolderChanged;

    private float doubleClickTime = 0.3f; // Tiempo máximo entre clics para considerarse doble clic
    private Dictionary<string, float> lastClickTimeMap = new Dictionary<string, float>();

    //CargandoContexto
    public GameObject CreandoContextoPanel; //FileButtonPrefab
    bool entrenando = false;

    // Icon variable
    [Header("Iconos")]
    private Dictionary<string, Sprite> iconDictionary;

    // Rutas
    string rootFolder = Path.Combine(Application.persistentDataPath, "UploadedFiles");

    private APIManager api;

    private string _currentFolder = "";

    //Notificaciones
    NotificationManager notifManager;

    public string CurrentFolder
    {
        get => _currentFolder;
        set
        {
            if (_currentFolder != value)
            {
                _currentFolder = value;
                OnCurrentFolderChanged?.Invoke(_currentFolder);
            }
        }
    }

    public UploaderState(IMenuState menu) : base(menu)
    {
        api = GameObject.FindObjectOfType<APIManager>();
    }

    public async override void Enter()
    {
        SubirArchivosRoot = GameObject.Find("SubirArchivos").transform; //Root

        PanelArchivos = SubirArchivosRoot.Find("PanelArchivos"); //Panel con elementos del explorador de archivos

        contextMenuScript = PanelArchivos.Find("ContextButtonsContainer").GetComponent<ContextMenuManager>();

        CreandoContextoPanel = SubirArchivosRoot.Find("CreandoContexto").Find("CreandoContextoPanel").gameObject;

        TransicionEnter();
        LoadIcons();

        FileButtonPrefab = PanelArchivos.Find("FilePrefab").gameObject;

        contentPanel = PanelArchivos.Find("Scroll View").Find("Viewport").Find("Content").GetComponent<RectTransform>();

        //INTERACTUABLES
        Retroceder = PanelArchivos.Find("Retroceder").GetComponent<Button>();

        Save = PanelArchivos.Find("Guardar").GetComponent<Button>();
        Save.onClick.AddListener(saveChanges);

        PathText = PanelArchivos.Find("RutaInputField").GetComponent<TextMeshProUGUI>();

        Atras = PanelArchivos.Find("Atrás").GetComponent<Button>();
        Atras.onClick.AddListener(GoBack);

        SubirArchivos = PanelArchivos.Find("SubirArchivosButt").GetComponent<Button>();
        SubirArchivos.onClick.AddListener(UploadFile);

        Retroceder.onClick.AddListener(() =>
        {
            menu.SetState(new ChoseCharacterState(menu));
            TransicionExit();
        });

        notifManager = GameObject.FindObjectOfType<NotificationManager>();

        api.GetRootFolder((rutaServidor) => {
            if (!string.IsNullOrEmpty(rutaServidor))
            {
                rootFolder = rutaServidor;
            }
            else
            {
                if (!Directory.Exists(rootFolder))
                    Directory.CreateDirectory(rootFolder);
            }

            CurrentFolder = rootFolder;
            RefreshView();
        });

        CurrentFolder = rootFolder;

        OnEnable();

        RefreshView();
    }

    public override void Exit() //Reset por si vuelve atrás
    {

        Save.onClick.RemoveAllListeners();

        Atras.onClick.RemoveAllListeners();

        SubirArchivos.onClick.RemoveAllListeners();
    }

    public override void FixedUpdate()
    {
    }

    public override async void TransicionEnter()
    {
        PanelArchivos.gameObject.SetActive(true);
    }
    public override async void TransicionExit()
    {
        PanelArchivos.gameObject.SetActive(false);
        OnDisable();
    }
    public override void Update()
    {
    }

    void OnEnable()
    {
        UnityDragAndDropHook.InstallHook();
        UnityDragAndDropHook.OnDroppedFiles += OnFiles;
        ContextMenuManager.OnRenameRequested += StartRenameForButton;

        ContextMenuManager.OnRefreshRequested += RefreshView;

    }

    void OnDisable()
    {
        UnityDragAndDropHook.UninstallHook();
        UnityDragAndDropHook.OnDroppedFiles -= OnFiles;
        ContextMenuManager.OnRenameRequested -= StartRenameForButton;

        ContextMenuManager.OnRefreshRequested -= RefreshView;
    }

    public async void saveChanges()
    {
        string[] allowedExts = new[] { ".txt", ".pdf", ".doc", ".docx" };
        notifManager.ShowNotification("Comprobando...", Color.yellow, 0.5f);
        await Task.Delay(1000);
        // Obtener todas las carpetas y archivos en rootFolder
        var allDirectories = Directory.GetDirectories(rootFolder, "*", SearchOption.AllDirectories);
        var allFiles = Directory.GetFiles(rootFolder, "*", SearchOption.AllDirectories);

        // Detectar carpetas anidadas (nivel 3 o más)
        var nestedFolders = allDirectories.Where(dir =>
        {
            string relativeDir = Path.GetRelativePath(rootFolder, dir);
            var parts = relativeDir.Split(Path.DirectorySeparatorChar);
            return parts.Length >= 2; // Nivel 3 o más: parts.Length>=2 porque raíz no se cuenta, ejemplo: Carpeta1/Carpeta2 (2 partes)
        }).ToList();

        if (nestedFolders.Count > 0)
        {
            foreach (var folder in nestedFolders)
            {
                string shortPath = GetShortenedPath(folder, rootFolder);
                notifManager.ShowNotification("Error: Subcarpetas no están permitidas" + shortPath, Color.red, 5f);
            }
            return; // Cancelar guardado por carpeta anidada
        }

        // Detectar archivos en nivel 3 o más (carpeta dentro de carpeta dentro de carpeta)
        var filesInRoot = allFiles.Where(file =>
        {
            string relativeFile = Path.GetRelativePath(rootFolder, file);
            var parts = relativeFile.Split(Path.DirectorySeparatorChar);
            return parts.Length == 1; // Solo el archivo, sin carpetas
        }).ToList();

        if (filesInRoot.Count > 0)
        {
            foreach (var file in filesInRoot)
            {
                string shortPath = GetShortenedPath(file, rootFolder);
                notifManager.ShowNotification("Error: Archivos en la raíz no permitidos " + shortPath, Color.red, 5f);

                GameObject btn = FindButtonByPath(file);
                if (btn != null)
                {
                    // 1. Cambiamos el color base del componente Image (el fondo)
                    var btnImage = btn.GetComponent<UnityEngine.UI.Image>();
                    if (btnImage != null)
                    {
                        btnImage.color = Color.red;
                    }

                    // 2. Opcional: Ajustar el ColorBlock para que al pasar el ratón no se vea raro
                    var btnComp = btn.GetComponent<Button>();
                    if (btnComp != null)
                    {
                        ColorBlock colors = btnComp.colors;
                        colors.normalColor = Color.red;
                        colors.selectedColor = new Color(0.8f, 0f, 0f); // Rojo más oscuro al seleccionar
                        btnComp.colors = colors;
                    }
                }
            }

            return;
        }
       
        // Validar extensiones permitidas
        var invalidFiles = allFiles.Where(file => !allowedExts.Contains(Path.GetExtension(file).ToLower())).ToList();


        if (invalidFiles.Count > 0)
        {
            foreach (var filePath in invalidFiles)
            {
                GameObject btn = FindButtonByPath(filePath);

                string shortenedPath = GetShortenedPath(filePath, rootFolder);
                notifManager.ShowNotification("Archivo no compatible: " + shortenedPath, Color.red, 5f);

                if (btn != null)
                {
                    var btnComp = btn.GetComponent<Button>();
                    if (btnComp != null)
                    {
                        ColorBlock colors = btnComp.colors;
                        colors.normalColor = Color.red;
                        btnComp.colors = colors;
                    }
                }
            }

            return; // Cancelar el guardado si hay archivos inválidos
        }

        // Guardar cambios en la base de datos
        api.UpdateRootFolder(rootFolder);

        notifManager.ShowNotification("Entrenando con archivos en la ruta ../UploadedFiles", Color.yellow, 5f);

        showCreandoContextoPanel();

        string respuesta = await ServiceLocatorManager.RunServiceWithResultAsync<CreateContext, string>(rootFolder);

        if (respuesta.Equals("True"))
        {
            await Task.Delay(2000);
            CancelarContextoPanel(false);
        }
    }

    private void showCreandoContextoPanel()
    {
        CreandoContextoPanel.SetActive(true);

        foreach (Transform child in CreandoContextoPanel.transform)
        {
            child.gameObject.SetActive(true);
        }
    }

    private void CancelarContextoPanel(bool error)
    {
        if (error)
        {
            notifManager.ShowNotification("Entrenamiento Cancelado. Cambios no persistidos", Color.red, 5f);
        }
        else
        {
            notifManager.ShowNotification("Entrenamiento completado", Color.green, 5f);

        }

        foreach (Transform child in CreandoContextoPanel.transform)
            {
                child.gameObject.SetActive(false);
            }

        CreandoContextoPanel.SetActive(false);
    }

    string GetShortenedPath(string fullPath, string root)
    {
        try
        {
            string relativePath = Path.GetRelativePath(root, fullPath);
            string[] parts = relativePath.Split(Path.DirectorySeparatorChar);

            if (parts.Length <= 2)
            {
                return relativePath.Replace('\\', '/'); // carpeta1/archivo
            }
            else
            {
                return ".../" + string.Join("/", parts.Skip(parts.Length - 2)); // .../carpeta2/archivo
            }
        }
        catch
        {
            return Path.GetFileName(fullPath); // fallback
        }
    }

    private GameObject FindButtonByPath(string path)
    {
        string normPath1 = Path.GetFullPath(path).ToLowerInvariant();

        foreach (Transform child in contentPanel)
        {
            string childPath = Path.Combine(CurrentFolder, child.GetComponentInChildren<TMP_Text>().text);
            string normPath2 = Path.GetFullPath(childPath).ToLowerInvariant();
            if (normPath1 == normPath2)
                return child.gameObject;
        }
        return null;
    }

    void StartRenameForButton(GameObject button)
    {
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true); // busca también inactivos
        TMP_InputField input = button.GetComponentInChildren<TMP_InputField>(true);

        if (label == null || input == null)
        {
            Debug.LogWarning("No se encontró TMP_Text o TMP_InputField en el botón.");
            return;
        }

        string oldName = label.text;

        // Mostrar el input, ocultar el label
        input.gameObject.SetActive(true);
        label.gameObject.SetActive(false);

        input.text = oldName;
        input.ActivateInputField();
        input.Select();

        input.onEndEdit.RemoveAllListeners();
        input.onEndEdit.AddListener((newName) =>
        {
            if (!string.IsNullOrWhiteSpace(newName) && newName != oldName)
            {
                string oldPath = Path.Combine(CurrentFolder, oldName);
                string newPath = Path.Combine(CurrentFolder, newName);

                try
                {
                    if (Directory.Exists(oldPath))
                    {
                        Directory.Move(oldPath, newPath);
                    }
                    else if (File.Exists(oldPath))
                    {
                        File.Move(oldPath, newPath);
                    }

                    RefreshView();
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error al renombrar: " + ex.Message);
                }
            }

            // Ocultar input, mostrar label
            input.gameObject.SetActive(false);
            label.gameObject.SetActive(true);
        });
    }

    public void LoadIcons()
    {
        iconDictionary = new Dictionary<string, Sprite>
    {
        { "folder", Resources.Load<Sprite>("Icons/FolderNormal") },
        { ".pdf", Resources.Load<Sprite>("Icons/PDF") },
        { ".doc", Resources.Load<Sprite>("Icons/DOC") },
        { ".docx", Resources.Load<Sprite>("Icons/DOC") },
        { ".txt", Resources.Load<Sprite>("Icons/TXT") },
        { ".png", Resources.Load<Sprite>("Icons/Default") },
        { ".jpg", Resources.Load<Sprite>("Icons/Default") },
        { ".jpeg", Resources.Load<Sprite>("Icons/Default") },
        { "default", Resources.Load<Sprite>("Icons/Default") }
    };
    }


    public void SelectRootFolder()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        var folders = StandaloneFileBrowser.OpenFolderPanel("Selecciona carpeta raíz", "", false);
        if (folders.Length > 0 && !string.IsNullOrEmpty(folders[0]))
        {
            rootFolder = folders[0];
            CurrentFolder = rootFolder;
            RefreshView();
        }
#endif
    }

    void OnFiles(List<string> files, POINT pos)
    {
        foreach (var file in files)
        {
            if (File.Exists(file))
            {
                string destPath = Path.Combine(CurrentFolder, Path.GetFileName(file));
                try
                {
                    File.Copy(file, destPath, true);
                    Debug.Log($"Archivo arrastrado copiado: {file} → {destPath}");
                }
                catch (IOException ex)
                {
                    Debug.LogError($"Error al copiar: {ex.Message}");
                }
            }
        }

        RefreshView();
    }


    void EnterFolder(string folder)
    {
        CurrentFolder = folder;
        RefreshView();
    }

    public void GoBack()
    {
        string currentNormalized = Path.GetFullPath(CurrentFolder).Replace('\\', '/').TrimEnd('/');
        string rootNormalized = Path.GetFullPath(rootFolder).Replace('\\', '/').TrimEnd('/');

        if (string.Equals(currentNormalized, rootNormalized, StringComparison.OrdinalIgnoreCase))
            return;

        CurrentFolder = Directory.GetParent(CurrentFolder).FullName;
        RefreshView();
    }

    /// <summary>
    /// Permite subir carpetas o archivos individuales
    /// </summary>
    public void UploadFile()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        // Subir archivos
        var files = StandaloneFileBrowser.OpenFilePanel("Selecciona archivos", "", "", true);
        foreach (string source in files)
        {
            if (!string.IsNullOrEmpty(source))
            {
                string dest = Path.Combine(CurrentFolder, Path.GetFileName(source));
                try
                {
                    File.Copy(source, dest, true);
                    Debug.Log($"Archivo subido: {source} → {dest}");
                }
                catch (IOException ex)
                {
                    Debug.LogError($"Error al copiar archivo {source}: {ex.Message}");
                }
            }
        }

        // Subir carpetas
        //var folders = StandaloneFileBrowser.OpenFolderPanel("Selecciona carpetas", "", true);
        //foreach (string folder in folders)
        //{
        //    if (!string.IsNullOrEmpty(folder))
        //    {
        //        string folderName = Path.GetFileName(folder);
        //        string destFolder = Path.Combine(CurrentFolder, folderName);
        //        try
        //        {
        //            CopyFolderRecursive(folder, destFolder);
        //            Debug.Log($"Carpeta subida: {folder} → {destFolder}");
        //        }
        //        catch (IOException ex)
        //        {
        //            Debug.LogError($"Error al copiar carpeta {folder}: {ex.Message}");
        //        }
        //    }
        //}

        RefreshView();
#endif
    }

    /// <summary>
    /// Copia el contenido de la carpeta
    /// </summary>
    /// <param name="sourceDir"></param>
    /// <param name="targetDir"></param>
    void CopyFolderRecursive(string sourceDir, string targetDir)
    {
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        // Copiar archivos
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        // Copiar subcarpetas
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            string destSubDir = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyFolderRecursive(dir, destSubDir);
        }
    }

    /// <summary>
    /// Refresca la vista cada vez que haya una actualización
    /// </summary>
    void RefreshView()
    {
        PathText.text = CurrentFolder;

        foreach (Transform t in contentPanel)
            GameObject.Destroy(t.gameObject);

        // Carpetas hijas
        var folders = Directory.GetDirectories(CurrentFolder);
        foreach (var folder in folders)
        {
            string folderName = Path.GetFileName(folder);
            GameObject btn = GameObject.Instantiate(FileButtonPrefab, contentPanel);

            var textComp = btn.GetComponentInChildren<TMP_Text>();
            textComp.text = folderName;

            btn.SetActive(true);

            var iconObj = btn.transform.Find("Icon");
            if (iconObj != null)
            {
                // Obtiene el componente Image del hijo "Icon"
                var imgComp = iconObj.GetComponent<UnityEngine.UI.Image>();
                if (imgComp != null)
                {
                    // Asigna el sprite que quieres mostrar en el icono
                    imgComp.sprite = GetIconForExtension("folder"); ; // aquí pones la imagen que quieras
                }
            }

            string relativeDir = Path.GetRelativePath(rootFolder, folder);
            var folderParts = relativeDir.Split(Path.DirectorySeparatorChar);

            if (folderParts.Length >= 2)
            {
                PintarBotonError(btn);
            }

            string fCopy = folder;
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                float lastClickTime;
                if (!lastClickTimeMap.TryGetValue(fCopy, out lastClickTime))
                    lastClickTime = -1f;

                float timeSinceLastClick = Time.time - lastClickTime;

                if (timeSinceLastClick <= doubleClickTime)
                {
                    EnterFolder(fCopy); // Doble clic detectado
                    lastClickTimeMap[fCopy] = -1f; // Reset
                }
                else
                {
                    lastClickTimeMap[fCopy] = Time.time;
                }
            });

            EventTrigger trigger = btn.AddComponent<EventTrigger>();

            EventTrigger.Entry rightClick = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };

            rightClick.callback.AddListener((eventData) =>
            {
                PointerEventData pointerData = (PointerEventData)eventData;
                if (pointerData.button == PointerEventData.InputButton.Right)
                {
                    Vector2 screenPos = pointerData.position;
                    // Pasamos 'true' porque es un elemento (carpeta)
                    contextMenuScript.ShowMenu(fCopy, screenPos, true);
                    EventSystem.current.SetSelectedGameObject(btn.gameObject);
                }
            });

            trigger.triggers.Add(rightClick);

        }

        // Archivos
        var allowedExts = new[] { ".txt", ".pdf", ".doc", ".docx" };
        var files = Directory.GetFiles(CurrentFolder); // TODOS los archivos


        foreach (var file in files)
        {
            string fileName = Path.GetFileName(file);
            GameObject btn = GameObject.Instantiate(FileButtonPrefab, contentPanel);

            var textComp = btn.GetComponentInChildren<TMP_Text>();
            textComp.text = fileName;

            btn.SetActive(true);
            string ext = Path.GetExtension(file).ToLower();
            string relativeFile = Path.GetRelativePath(rootFolder, file);
            var fileParts = relativeFile.Split(Path.DirectorySeparatorChar);

            bool ubicacionIlegal = fileParts.Length == 1 || fileParts.Length >= 3;
            bool extensionInvalida = !allowedExts.Contains(ext);

            if (ubicacionIlegal || extensionInvalida)
            {
                PintarBotonError(btn);
            }

            var iconObj = btn.transform.Find("Icon");
            if (iconObj != null)
            {
                // Obtiene el componente Image del hijo "Icon"
                var imgComp = iconObj.GetComponent<Image>();
                if (imgComp != null)
                {
                    // Asigna el sprite que quieres mostrar en el icono
                    imgComp.sprite = GetIconForExtension(Path.GetExtension(file).ToLower());

                    imgComp.sprite = GetIconForExtension(ext);

                    // Si la extensión NO está en la lista permitida, pintar el botón naranja
                    if (!allowedExts.Contains(ext))
                    {
                        var btnComp = btn.GetComponent<Button>();
                        if (btnComp != null)
                        {
                            ColorBlock colors = btnComp.colors;
                            colors.normalColor = new Color(1f, 0.5f, 0f); // naranja
                            btnComp.colors = colors;
                        }
                    }
                }
            }

            string fCopy = file;
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                float lastClickTime;
                if (!lastClickTimeMap.TryGetValue(fCopy, out lastClickTime))
                    lastClickTime = -1f;

                float timeSinceLastClick = Time.time - lastClickTime;

                if (timeSinceLastClick <= doubleClickTime)
                {
                    OpenFile(fCopy); // Doble clic detectado
                    lastClickTimeMap[fCopy] = -1f; // Reset
                }
                else
                {
                    lastClickTimeMap[fCopy] = Time.time;
                }
            });

            EventTrigger trigger = btn.AddComponent<EventTrigger>();

            EventTrigger.Entry rightClick = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };

            rightClick.callback.AddListener((eventData) =>
            {
                PointerEventData pointerData = (PointerEventData)eventData;
                if (pointerData.button == PointerEventData.InputButton.Right)
                {
                    Vector2 screenPos = pointerData.position;
                    // Pasamos 'true' porque es un elemento (archivo)
                    contextMenuScript.ShowMenu(fCopy, screenPos, true);
                    EventSystem.current.SetSelectedGameObject(btn.gameObject);
                }
            });

            trigger.triggers.Add(rightClick);
        }
    }

    private void PintarBotonError(GameObject btn)
    {
        Color colorError = new Color(1f, 0.2f, 0.2f, 1f); // Rojo intenso

        // 1. Cambiar el color de la imagen de fondo
        var btnImage = btn.GetComponent<UnityEngine.UI.Image>();
        if (btnImage != null)
        {
            btnImage.color = colorError;
        }

        // 2. Cambiar los colores de transición del botón para que no vuelva a azul al quitar el mouse
        var btnComp = btn.GetComponent<Button>();
        if (btnComp != null)
        {
            ColorBlock colors = btnComp.colors;
            colors.normalColor = colorError;
            colors.highlightedColor = new Color(1f, 0.4f, 0.4f, 1f); // Rojo claro al pasar mouse
            colors.pressedColor = new Color(0.7f, 0f, 0f, 1f);       // Rojo oscuro al clicar
            colors.selectedColor = colorError;
            btnComp.colors = colors;
        }
    }

    Sprite GetIconForExtension(string ext)
    {
        return iconDictionary.TryGetValue(ext, out Sprite icon)
            ? icon
            : iconDictionary["default"];
    }

    void OpenFile(string path)
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        System.Diagnostics.Process.Start("open", path);
#elif UNITY_STANDALONE_LINUX
        System.Diagnostics.Process.Start("xdg-open", path);
#else
        Debug.LogWarning("Abrir archivos no soportado en esta plataforma");
#endif
    }
}
