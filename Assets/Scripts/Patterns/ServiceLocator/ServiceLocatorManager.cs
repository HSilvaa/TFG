using System;
using System.Threading.Tasks;
using UnityEngine;

public static class ServiceLocatorManager   
{
    public static async Task<TOut> RunServiceWithResultAsync<TService, TOut>(params object[] parameters)
    where TService : IServiceResult<TOut>
    {
        try
        {
            TService service = ServiceLocator.Get<TService>();

            if (await service.OpenServiceAsync())
            {
                if (await service.ExecuteServiceAsync(parameters))
                {
                    await service.CloseServiceAsync();
                    return service.Result;
                }
            }

            await service.CloseServiceAsync();
            return default;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error running service {typeof(TService).Name}: {e.Message}");
            return default;
        }
    }

}
