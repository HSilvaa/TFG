

using System.Threading.Tasks;

public abstract class AbstractUIObjectState : IState
{
    protected IUIObject obj; 

    public AbstractUIObjectState(IUIObject obj) 
    {
        this.obj = obj;
    }

    public abstract void Enter();
    public abstract void Exit();
    public abstract void Update();
    public abstract void FixedUpdate();
}
