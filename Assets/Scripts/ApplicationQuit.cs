using UnityEngine;

public class ApplicationQuit : MonoBehaviour
{
    public static ApplicationQuit Instance;

    // Asegurarse de que solo haya una instancia del servicio y que no se destruya al cambiar de escena
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Mantener el objeto a través de escenas
        }
        else
        {
            Destroy(gameObject); // Destruir el objeto si ya existe una instancia
        }
    }

    /// <summary>
    /// Cuando cierro la app, se cierra el servidor de python
    /// </summary>
    private void OnApplicationQuit() //-- > NO LO MATA
    {
        // Llamar a la ejecución del servicio de matar procesos
        ServiceLocatorManager.RunServiceWithResultAsync<KillProcessService, string>("hola");
    }
}
