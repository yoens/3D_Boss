public class ChaseState : IState
{
    private EnemyController enemy;
    private StateMachine stateMachine;

    public ChaseState(EnemyController enemy, StateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public void Enter()
    {
        enemy.Animator.SetFloat("Speed", 1f);
    }

    public void Update()
    {
        float distance = enemy.GetDistanceToPlayer();

        if(distance > enemy.DetectRange)
        {
            stateMachine.ChangeState(enemy.IdleState);
            return;
        }
        if(distance < enemy.AttackRange)
        {
            stateMachine.ChangeState(enemy.AttackState);
            return;
        }

        enemy.LookAtPlayer();
        enemy.MoveToPlayer();
    }

    public void Exit()
    {
        
    }
}