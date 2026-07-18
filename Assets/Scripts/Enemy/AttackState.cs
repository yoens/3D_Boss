public class AttackState : IState
{
    private EnemyController enemy;
    private StateMachine stateMachine;

    private float attackCooldown = 2f;
    private float attackTimer;

    public AttackState(EnemyController enemy, StateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public void Enter()
    {
        attackTimer = 0f;
    }

    public void Update()
    {
        float distance = enemy.GetDistanceToPlayer();

        if(distance > enemy.AttackRange)
        {
            stateMachine.ChangeState(enemy.ChaseState);
            return;
        }
        enemy.LookAtPlayer();

        attackTimer -= UnityEngine.Time.deltaTime;

        if(attackTimer <= 0f)
        {
            enemy.Animator.SetTrigger("Attack");
            
            attackTimer = attackCooldown;
        }
    }

    public void Exit()
    {
        
    }

}