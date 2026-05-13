using B83.Win32;
using DG.Tweening;
using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UploaderState : AbstractMenuState
{
    // Contenedores UI
    private Transform SubirArchivosRoot;
    private Transform PanelArchivos;
    public RectTransform contentPanel;
    public GameObject FileButtonPrefab;
    public Button Save;
    public TextMeshProUGUI PathText;
    public Button Atras;
    public Button Retroceder;
    public Button SubirArchivos;

    // Gestión de contexto y archivos
    public ContextMenuManager contextMenuScript;
    public static event Action<string> OnCurrentFolderChanged;
    public GameObject CreandoContextoPanel;

    private Dictionary<string, Sprite> iconDictionary;
    private string rootFolder = Path.Combine(Application.persistentDataPath, "UploadedFiles");
    private string _currentFolder = "";
    private float doubleClickTime = 0.3f;
    private Dictionary<string, float> lastClickTimeMap = new Dictionary<string, float>();

    private APIManager api;
    private NotificationManager notifManager;
    private AbstractMenuState newState;

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
        notifManager = GameObject.FindObjectOfType<NotificationManager>();
    }

    public override void Enter()
    {
        // 1. Inicialización de rutas y carpetas
        if (!Directory.Exists(rootFolder)) Directory.CreateDirectory(rootFolder);
        CurrentFolder = rootFolder;

        // 2. Localización de UI y Referencias
        SubirArchivosRoot = GameObject.Find("SubirArchivos").transform;
        PanelArchivos = SubirArchivosRoot.Find("PanelArchivos");
        contextMenuScript = PanelArchivos.Find("ContextButtonsContainer").GetComponent<ContextMenuManager>();
        CreandoContextoPanel = SubirArchivosRoot.Find("CreandoContexto").Find("CreandoContextoPanel").gameObject;
        FileButtonPrefab = PanelArchivos.Find("FilePrefab").gameObject;
        contentPanel = PanelArchivos.Find("Scroll View").Find("Viewport").Find("Content").GetComponent<RectTransform>();

        // 3. Configuración de Componentes e Interactuables
        Retroceder = SubirArchivosRoot.Find("Retroceder").GetComponent<Button>();
        Save = SubirArchivosRoot.Find("Guardar").GetComponent<Button>();
        PathText = PanelArchivos.Find("RutaInputField").GetComponent<TextMeshProUGUI>();
        Atras = PanelArchivos.Find("Atrás").GetComponent<Button>();
        SubirArchivos = SubirArchivosRoot.Find("SubirArchivosButt").GetComponent<Button>();
        notifManager = GameObject.FindObjectOfType<NotificationManager>();

        // 4. ACTIVACIÓN DE UI
        PanelArchivos.gameObject.SetActive(true);
        Retroceder.gameObject.SetActive(true);
        Save.gameObject.SetActive(true);
        SubirArchivos.gameObject.SetActive(true);
        Atras.gameObject.SetActive(true);

        // 5. LIMPIEZA Y ASIGNACIÓN DE LISTENERS (Evita duplicados si se re-entra al estado)
        Save.onClick.RemoveAllListeners();
        Atras.onClick.RemoveAllListeners();
        SubirArchivos.onClick.RemoveAllListeners();
        Retroceder.onClick.RemoveAllListeners();

        Save.onClick.AddListener(SaveChanges);
        Atras.onClick.AddListener(GoBack);
        SubirArchivos.onClick.AddListener(UploadFileAction);
        Retroceder.onClick.AddListener(() =>
        {
            newState = new ChoseCharacterState(menu);
            TransicionExit();
        });

        // 6. Configuración de fondo para click derecho
        EventTrigger backgroundTrigger = contentPanel.gameObject.GetComponent<EventTrigger>() ?? contentPanel.gameObject.AddComponent<EventTrigger>();
        backgroundTrigger.triggers.Clear(); // Limpiar triggers viejos
        EventTrigger.Entry bgEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        bgEntry.callback.AddListener((data) => {
            PointerEventData pData = (PointerEventData)data;
            if (pData.button == PointerEventData.InputButton.Right)
                contextMenuScript.ShowMenu(CurrentFolder, pData.position, false);
        });
        backgroundTrigger.triggers.Add(bgEntry);

        // 7. Carga de datos y visual
        LoadIcons();
        OnEnable();
        RefreshView();
        TransicionEnter();
    }


    // ==========================================================
    // LÓGICA DE GUARDADO Y SINCRONIZACIÓN CON EL SERVIDOR
    // ==========================================================
    public async void SaveChanges()
    {
        string[] allowedExts = { ".txt", ".pdf", ".doc", ".docx" };
        notifManager.ShowNotification("Validating structure...", Color.yellow, 1f);
        await Task.Delay(500);

        // 1. Validar que no haya archivos sueltos en la raíz (deben estar en carpetas/épocas)
        if (Directory.GetFiles(rootFolder).Length > 0)
        {
            notifManager.ShowNotification("Error: Files in root not allowed. Move them to a folder.", Color.red, 4f);
            return;
        }

        var contextFolders = Directory.GetDirectories(rootFolder);
        if (contextFolders.Length == 0)
        {
            notifManager.ShowNotification("Error: No context folders found.", Color.red, 3f);
            return;
        }

        showCreandoContextoPanel();
        notifManager.ShowNotification("Uploading contexts...", Color.cyan, 2f);

        try
        {
            api.ResetSystem(async (resetRes) =>
            {
                foreach (var folderPath in contextFolders)
                {
                    string folderName = Path.GetFileName(folderPath);

                    // Filtrar archivos válidos
                    List<string> filesInFolder = Directory.GetFiles(folderPath)
                        .Where(f => allowedExts.Contains(Path.GetExtension(f).ToLower()))
                        .ToList();

                    if (filesInFolder.Count == 0) continue;

                    bool uploadDone = false;

                    api.UploadContextFiles(folderName, filesInFolder, (uploadRes) => {
                        uploadDone = true;
                    });

                    float timeout = 0;
                    while (!uploadDone && timeout < 30f)
                    {
                        await Task.Delay(100);
                        timeout += 0.1f;
                    }

                    Debug.Log($"Carpeta {folderName} subida correctamente.");
                }

                // 3. Una vez subidas TODAS las carpetas, construir los índices una sola vez
                api.BuildAllIndices((indexRes) =>
                {
                    notifManager.ShowNotification("All contexts saved!", Color.green, 3f);
                    CancelarContextoPanel(false);
                });
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en la cascada de sincronización: {e.Message}");
            CancelarContextoPanel(true);
        }
    }

    // ==========================================================
    // GESTIÓN DEL EXPLORADOR DE ARCHIVOS LOCAL
    // ==========================================================

    void RefreshView()
    {
        PathText.text = CurrentFolder;
        foreach (Transform t in contentPanel) GameObject.Destroy(t.gameObject);

        string rootNormalized = Path.GetFullPath(rootFolder).Replace('\\', '/');
        string currentNormalized = Path.GetFullPath(CurrentFolder).Replace('\\', '/');

        foreach (var folder in Directory.GetDirectories(CurrentFolder))
        {
            string folderName = Path.GetFileName(folder);
            GameObject btn = GameObject.Instantiate(FileButtonPrefab, contentPanel);
            btn.SetActive(true);
            btn.GetComponentInChildren<TMP_Text>().text = folderName;
            btn.transform.Find("Icon").GetComponent<Image>().sprite = GetIconForExtension("folder");

            if (currentNormalized != rootNormalized) PintarBotonError(btn);

            btn.GetComponent<Button>().onClick.AddListener(() => HandleDoubleClick(folder, true));

            AddRightClickMenu(btn, folder, true);
        }

        string[] allowedExts = { ".txt", ".pdf", ".doc", ".docx" };
        foreach (var file in Directory.GetFiles(CurrentFolder))
        {
            string fileName = Path.GetFileName(file);
            string ext = Path.GetExtension(file).ToLower();
            GameObject btn = GameObject.Instantiate(FileButtonPrefab, contentPanel);
            btn.SetActive(true);
            btn.GetComponentInChildren<TMP_Text>().text = fileName;
            btn.transform.Find("Icon").GetComponent<Image>().sprite = GetIconForExtension(ext);

            bool isIllegalLocation = (currentNormalized == rootNormalized);
            bool isInvalidExt = !allowedExts.Contains(ext);
            if (isIllegalLocation || isInvalidExt) PintarBotonError(btn);

            btn.GetComponent<Button>().onClick.AddListener(() => HandleDoubleClick(file, false));

            AddRightClickMenu(btn, file, false);
        }
    }

    private void HandleDoubleClick(string path, bool isFolder)
    {
        float lastClickTime = lastClickTimeMap.ContainsKey(path) ? lastClickTimeMap[path] : -1f;
        if (Time.time - lastClickTime <= doubleClickTime)
        {
            if (isFolder) EnterFolder(path);
            else OpenFile(path);
            lastClickTimeMap[path] = -1f;
        }
        else lastClickTimeMap[path] = Time.time;
    }

    private void AddRightClickMenu(GameObject btn, string path, bool isFolder)
    {
        EventTrigger trigger = btn.GetComponent<EventTrigger>() ?? btn.AddComponent<EventTrigger>();

        // Limpiamos triggers previos para evitar duplicados al refrescar
        trigger.triggers.Clear();

        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener((data) => {
            PointerEventData pData = (PointerEventData)data;
            if (pData.button == PointerEventData.InputButton.Right)
            {
            
                contextMenuScript.ShowMenu(path, pData.position, true);

                EventSystem.current.SetSelectedGameObject(btn);
            }
        });
        trigger.triggers.Add(entry);
    }

    // ==========================================================
    // UTILIDADES Y MENÚS
    // ==========================================================

    public void UploadFileAction()
    {
        var files = StandaloneFileBrowser.OpenFilePanel("Select Files", "", "", true);
        foreach (string source in files)
        {
            if (string.IsNullOrEmpty(source)) continue;
            string dest = Path.Combine(CurrentFolder, Path.GetFileName(source));
            File.Copy(source, dest, true);
        }
        RefreshView();
    }

    void EnterFolder(string folder) { CurrentFolder = folder; RefreshView(); }

    public void GoBack()
    {
        if (Path.GetFullPath(CurrentFolder) == Path.GetFullPath(rootFolder)) return;
        CurrentFolder = Directory.GetParent(CurrentFolder).FullName;
        RefreshView();
    }

    private void PintarBotonError(GameObject btn)
    {
        Color err = new Color(1f, 0.3f, 0.3f);
        btn.GetComponent<Image>().color = err;
        ColorBlock cb = btn.GetComponent<Button>().colors;
        cb.normalColor = err;
        cb.selectedColor = err;
        btn.GetComponent<Button>().colors = cb;
    }

    Sprite GetIconForExtension(string ext) => iconDictionary.TryGetValue(ext, out Sprite s) ? s : iconDictionary["default"];

    public void LoadIcons()
    {
        iconDictionary = new Dictionary<string, Sprite> {
            { "folder", Resources.Load<Sprite>("Icons/FolderNormal") },
            { ".pdf", Resources.Load<Sprite>("Icons/PDF") },
            { ".txt", Resources.Load<Sprite>("Icons/TXT") },
            { "default", Resources.Load<Sprite>("Icons/Default") }
        };
    }

    private void showCreandoContextoPanel() { CreandoContextoPanel.SetActive(true); }
    private void CancelarContextoPanel(bool error)
    {
        CreandoContextoPanel.SetActive(false);
        RefreshView();
    }

    void OpenFile(string path) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });

    public override void Exit() { OnDisable(); }
    public override void FixedUpdate() { }
    public override void Update() { }
    public override void TransicionEnter() { PanelArchivos.gameObject.SetActive(true); }
    public override async void TransicionExit()
    {
        if (PanelArchivos != null) PanelArchivos.gameObject.SetActive(false);
        if (Retroceder != null) Retroceder.gameObject.SetActive(false);
        if (Save != null) Save.gameObject.SetActive(false);
        if (SubirArchivos != null) SubirArchivos.gameObject.SetActive(false);
        if (Atras != null) Atras.gameObject.SetActive(false);
        if (CreandoContextoPanel != null) CreandoContextoPanel.SetActive(false);

        if (Save != null) Save.onClick.RemoveAllListeners();
        if (Atras != null) Atras.onClick.RemoveAllListeners();
        if (SubirArchivos != null) SubirArchivos.onClick.RemoveAllListeners();
        if (Retroceder != null) Retroceder.onClick.RemoveAllListeners();

        OnDisable();
        menu.SetState(newState);
    }

    void OnEnable()
    {
        UnityDragAndDropHook.InstallHook();
        UnityDragAndDropHook.OnDroppedFiles += OnFiles;
        ContextMenuManager.OnRefreshRequested += RefreshView;
    }
    void OnDisable()
    {
        UnityDragAndDropHook.UninstallHook();
        UnityDragAndDropHook.OnDroppedFiles -= OnFiles;
        ContextMenuManager.OnRefreshRequested -= RefreshView;
    }
    void OnFiles(List<string> files, POINT pos)
    {
        foreach (var f in files) File.Copy(f, Path.Combine(CurrentFolder, Path.GetFileName(f)), true);
        RefreshView();
    }
}