public class IdleState : IState
{
    private EnemyController enemy;
    private StateMachine stateMachine;

    public IdleState(EnemyController enemy, StateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public void Enter()
    {

    }

    public void Update()
    {
        if(enemy.GetDistanceToPlayer() < enemy.DetectRange)
        {
            stateMachine.ChangeState(enemy.ChaseState);
        }
    }

    public void Exit()
    {
        
    }
}