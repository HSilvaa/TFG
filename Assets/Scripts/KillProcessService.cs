using System.Diagnostics;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class KillProcessService : IServiceResult<string>
{
    public string Result { get; private set; }
    public async Task<bool> CloseServiceAsync()
    {
        return true;
    }

    /// <summary>
    /// Intenta cerrar el servidor de python
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<bool> ExecuteServiceAsync(object[] info)
    {
        try
        {
            // Obtener la instancia del servicio PythonLauncherService
            PythonLauncherService pythonLauncherService = ServiceLocator.Get<PythonLauncherService>();

            if (pythonLauncherService != null && pythonLauncherService.pythonProcess != null)
            {
                Process pythonProcess = pythonLauncherService.pythonProcess;
                if (!pythonProcess.HasExited)
                {
                    try
                    {
                        // Cerramos el proceso Python
                        await pythonLauncherService.KillProccess();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error: " + ex.Message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al intentar matar el proceso: {ex.Message}");
        }

        return true;

    }


    public async Task<bool> OpenServiceAsync()
    {
        return true;
    }
}
