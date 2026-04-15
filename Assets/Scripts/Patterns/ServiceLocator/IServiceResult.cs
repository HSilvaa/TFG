using UnityEngine;

public interface IServiceResult<T> : IService
{
    T Result { get; }
}
