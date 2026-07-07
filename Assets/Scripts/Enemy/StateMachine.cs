public class StateMachine
{

    private IState currentState;
    public IState CurrentState => currentState;

    public void ChangeState(IState newState)
    {
        if(currentState != null)
        {
            currentState.Exit();
        }

        currentState = newState;

        if(currentState != null)
        {
            currentState.Enter();
        }
    }

    public void Update()
    {
        if(currentState != null)
        {
            currentState.Update();
        }
    }
}