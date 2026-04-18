using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class PythonLauncherService : IServiceResult<bool>
{
    public bool Result { get; private set; }

    public Process pythonProcess;

    private APIManager api; 
    public async Task<bool> OpenServiceAsync()
    {
        api = GameObject.FindObjectOfType<APIManager>();
        await KillProccess(); //mate a cualquier proceso antes por si acaso
        return true;
    }

    /// <summary>
    /// Crea un proceso y lo ejecuta. Obtenemos los logs del sercvidor con OutputDataReceived y ErrorDataReceived
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public async Task<bool> ExecuteServiceAsync(object[] info)
    {
        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = "python";
        start.Arguments = "-u -m uvicorn APIPython:app --host 127.0.0.1 --port 8000";

        string unityProjectPath = Application.dataPath; // Esto da la ruta hasta ...\AIGERIM-TFG 3D\Assets
        string serverPath = Path.Combine(Directory.GetParent(unityProjectPath).FullName, "TFGPython");
        start.WorkingDirectory = serverPath;


        //start.WorkingDirectory = @"D:\Python Projects\TFG";
        start.UseShellExecute = false;
        start.CreateNoWindow = false;

        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.StandardOutputEncoding = Encoding.UTF8;
        start.StandardErrorEncoding = Encoding.UTF8;


        pythonProcess = new Process();
        pythonProcess.StartInfo = start;

        pythonProcess.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                UnityEngine.Debug.Log("[Python STDOUT] " + args.Data);
            }
        };

        pythonProcess.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                UnityEngine.Debug.LogWarning("[Python STDERR] " + args.Data);
            }
        };

        pythonProcess.Start();
        pythonProcess.BeginOutputReadLine();
        pythonProcess.BeginErrorReadLine();

        bool serverReady = await WaitForServerAsync();

        if (serverReady)
        {
            OnServerReady();
        }

        return serverReady;
    }

    /// <summary>
    /// Intenta abrir el servidor cada X tiempo. Si en "timeoutSeconds" no lo ha conseguido, sale. El servidor se abrá abierto cuando se conecte correctamente con /status
    /// </summary>
    /// <param name="timeoutSeconds">Seconds trying to open the service</param>
    /// <returns></returns>
    private async Task<bool> WaitForServerAsync(int timeoutSeconds = 30)
    {
        float currentTime = 0;

        UnityEngine.Debug.Log("Esperando a que el servidor Python despierte...");

        while (currentTime < timeoutSeconds)
        {
            var tcs = new TaskCompletionSource<bool>();

            // Llamamos al CheckStatus del APIManager
            api.CheckStatus((success) => {
                tcs.TrySetResult(success);
            });

            bool isReady = await tcs.Task;

            if (isReady)
            {
                Result = true;
                return true;
            }

            await Task.Delay(1000);
            currentTime++;
        }

        UnityEngine.Debug.LogError("Timeout: El servidor Python nunca respondió en el puerto 8000.");
        return false;
    }

    /// <summary>
    /// Hacer algo cuando el servido haya cargado
    /// </summary>
    private void OnServerReady()
    {
        //Momentaneo 
    }

    public async Task<bool> CloseServiceAsync()
    {
        return true;
    }

    /// <summary>
    /// Mata al proceso. Será llamado desde otro servicio
    /// </summary>
    /// <returns></returns>
    public async Task<bool> KillProccess()
    {
        try
        {
            Process killProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = "/F /IM python.exe /T",
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };

            killProcess.Start();
            killProcess.WaitForExit();
            return true;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("Error al cerrar el servidor: " + ex.Message);
            return false;
        }
    }
}
