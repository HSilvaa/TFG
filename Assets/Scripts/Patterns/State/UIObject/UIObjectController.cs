using System.Collections.Generic;
using UnityEngine;

public class UIObjectController : MonoBehaviour, IUIObject
{
    IState currentState;

    public List<Canvas> canvases;


    private void Awake()
    {
        canvases.Add(GameObject.Find("FrontScreen").GetComponent<Canvas>());
        canvases.Add(GameObject.Find("LeftScreen").GetComponent<Canvas>());
        canvases.Add(GameObject.Find("RightScreen").GetComponent<Canvas>());

        //SetState(new BienvenidoState(this));
    }


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

    void Start()
    {
        
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
