using System;
using System.Collections.Generic;
using UnityEngine;

public class ServiceLocator
{
    private static Dictionary<Type, object> services = new Dictionary<Type, object>();

    /// <summary>
    ///  Register the service into the ServiceLocator
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="service"></param>
    public static void Register<T>(T service)
    {
         Type type = service.GetType();
        if (!services.ContainsKey(type))
        {
            services[type] = service;
            //Debug.Log($"Servicio registrado: {type.Name}");
        }
    }

    /// <summary>
    /// Gets the service requied
    /// </summary>
    /// <typeparam name="T">Service type</typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static T Get<T>()
    {
        Type type = typeof(T);
        if (services.ContainsKey(type))
        {
            return (T)services[type];
        }
        throw new Exception($"El servicio {type.Name} no está registrado.");
    }
}
