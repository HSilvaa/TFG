using UnityEngine;

public abstract class AbstractGameControllerState : IState
{
    protected IGameState game;

    public AbstractGameControllerState(IGameState game)
    {
        this.game = game;
    }

    public abstract void Enter();
    public abstract void Exit();
    public abstract void Update();
    public abstract void FixedUpdate();
}
