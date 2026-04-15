using System.Threading.Tasks;
using UnityEngine;

public interface IService 
{
    /// <summary>
    /// Open a service
    /// </summary>
    /// <returns>true if service was oppened</returns>
    Task<bool> OpenServiceAsync();

    /// <summary>
    /// Execute a service
    /// </summary>
    /// <returns>If the execution was correct</returns>
    Task<bool> ExecuteServiceAsync(object[] info);

    /// <summary>
    /// Closes a service
    /// </summary>
    /// <returns>If the close was correct</returns>
    Task<bool> CloseServiceAsync();
}
