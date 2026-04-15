using System;
using System.Collections.Generic;
using UnityEngine;

public class MenuController : MonoBehaviour, IMenuState
{
    IState currentState;

    GameObject superParent;

    public List<Canvas> canvases;
    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public IState GetState()
    {
        return currentState;
    }

    public List<Canvas> GetCanvases()
    {
        return canvases;
    }

    public void SetState(IState state)
    {
        // SALE DEL ESTADO ACTUAL
        if (currentState != null)
        {
            currentState.Exit();
        }

        // SE METE EN EL NUEO ESTADO
        currentState = state;

        currentState.Enter();
    }

    public GameObject GetSuperParent()
    {
        return superParent;
    }

    private void Awake()
    {

    }

    void Start()
    {
        //SQLite.Instance.ResetDatabase();
        SetState(new LoaderState(this));
        superParent = GameObject.Find("BluePanel");
    }

    // Update is called once per frame
    void Update()
    {
        currentState.Update();
    }

    void FixedUpdate()
    {
        currentState.FixedUpdate();
    }
}
