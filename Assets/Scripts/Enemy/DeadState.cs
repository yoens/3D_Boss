public class DeadState : IState
{
    private EnemyController enemy;
    private StateMachine stateMachine;

    public DeadState(EnemyController enemy, StateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public void Enter()
    {
        UnityEngine.Debug.Log("Enemy down");
    }

    public void Update()
    {

    }

    public void Exit()
    {

    }
}