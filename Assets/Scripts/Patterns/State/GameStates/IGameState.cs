using System.Collections.Generic;
using UnityEngine;

public interface IGameState
{
    public bool IsGameStarted();
    public GameObject GetGameObject();
    public void SetState(IState state);
    public IState GetState();

}
