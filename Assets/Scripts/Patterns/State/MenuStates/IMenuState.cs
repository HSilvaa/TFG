using System;
using System.Collections.Generic;
using UnityEngine;

public interface IMenuState 
{
    public GameObject GetGameObject();

    public void SetState(IState state);
    public IState GetState();
    public List<Canvas> GetCanvases();
    public GameObject GetSuperParent();
}
