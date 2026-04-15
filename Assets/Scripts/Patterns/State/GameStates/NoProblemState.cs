using UnityEngine;
using UnityEngine.Networking;

public class NoProblemState : AbstractGameControllerState
{

    public NoProblemState(IGameState game) : base(game)
    {
    }

    public override void Enter()
    {

    }

    public override void Exit()
    {

    }

    public override void FixedUpdate() { }

    public override void Update()
    {
        if (game.IsGameStarted()) //SI EL JUEGO HA EMPEZADO, COMIENZO A COMPROBAR LOS ESTADOS DE LAS CONEXIONES
        {
            game.SetState(new NoInternetConnexionState(game));
        }
    }
}
