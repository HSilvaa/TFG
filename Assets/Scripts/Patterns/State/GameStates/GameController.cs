using System;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour, IGameState
{
    IState currentState;

    bool gameStarted;

    private void Awake()
    {
        gameStarted = false;
    }

    void Start()
    {
        //Se suscribe 
        Event.OnGameStarted += StartGame;

        SetState(new NoProblemState(this));
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public IState GetState()
    {
        return currentState;
    }

    private void StartGame()
    {
        gameStarted = true;

        //Segun empiece el juego, se desuscribe
        Event.OnGameStarted -= StartGame;
    }

    public bool IsGameStarted()
    {
        return gameStarted;
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
