using System.Collections.Generic;
using UnityEngine;

public interface IUIObject 
{
    public GameObject GetGameObject();

    public void SetState(IState state);
    public IState GetState();

    public List<Canvas> GetCanvases();
}
