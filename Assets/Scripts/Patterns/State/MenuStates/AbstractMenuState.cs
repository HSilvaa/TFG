

using System.Threading.Tasks;

public abstract class AbstractMenuState : IState
{
    protected IMenuState menu; 

    public AbstractMenuState(IMenuState menu) 
    {
        this.menu = menu;
    }

    public abstract void Enter();
    public abstract void Exit();
    public abstract void Update();
    public abstract void FixedUpdate();
    public abstract void TransicionEnter();
    public abstract void TransicionExit();
}
