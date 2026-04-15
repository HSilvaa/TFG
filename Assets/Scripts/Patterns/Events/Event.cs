using System;
using UnityEngine;

public class Event : MonoBehaviour
{
    public static event Action OnGameStarted;
    public static void RaiseMenuChanged()
    {
        OnGameStarted?.Invoke();
    }
}
