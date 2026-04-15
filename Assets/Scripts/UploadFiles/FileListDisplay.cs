using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Diagnostics;
using TMPro;
using NUnit.Framework;

public class FileListDisplay : MonoBehaviour
{
    public RectTransform contentPanel;          // Contenedor ScrollView para botones
    public GameObject fileButtonPrefab;         // Prefab con Button + Text
    public TMP_Text previewText;                     // Texto para mostrar contenido txt
    //public RawImage previewImage;                // RawImage para mostrar imágenes

    void Start()
    {
        LoadFileList();
    }

    void LoadFileList()
    {
        string folderPath = Application.dataPath + "/UploadedFiles";

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string[] files = Directory.GetFiles(folderPath);

        foreach (string filePath in files)
        {
            string fileName = Path.GetFileName(filePath);
            GameObject newButton = Instantiate(fileButtonPrefab, contentPanel);
            newButton.GetComponentInChildren<TMP_Text>().text = fileName;

            // Capturamos la variable para que no haya problema con el closure
            string capturedPath = filePath;

            newButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                OpenAndPreviewFile(capturedPath);
            });
        }
    }

    void OpenAndPreviewFile(string path)
    {
        string ext = Path.GetExtension(path).ToLower();

        previewText.text = "";
        //previewImage.texture = null;
        //previewImage.gameObject.SetActive(false);
        previewText.gameObject.SetActive(false);

        if (ext == ".txt")
        {
            string content = File.ReadAllText(path);
            previewText.gameObject.SetActive(true);
            previewText.text = content;
        }
        else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(fileData))
            {
                //previewImage.gameObject.SetActive(true);
                //previewImage.texture = tex;
            }
        }
        else
        {
            // Abrir con app predeterminada (solo en PC/Mac/Linux)
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            Process.Start("open", path);
#elif UNITY_STANDALONE_LINUX
            Process.Start("xdg-open", path);
#else
            Debug.LogWarning("Abrir archivos no soportado en esta plataforma");
#endif
        }
    }
}
