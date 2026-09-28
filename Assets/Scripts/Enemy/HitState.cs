using UnityEngine;

public class HitState : IState
{
    private EnemyController enemy;
    private StateMachine stateMachine;

    private float hitTimer;

    public HitState(EnemyController enemy, StateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public void Enter()
    {
        hitTimer = enemy.HitDuration;

        enemy.Move(Vector3.zero);
        enemy.Animator.SetFloat("Speed", 0f);

        enemy.Animator.ResetTrigger("Attack");
        enemy.Animator.ResetTrigger("Hit");
        enemy.Animator.SetTrigger("Hit");
    
    }
    public void Update()
    {
        hitTimer -= UnityEngine.Time.deltaTime;

        if (hitTimer > 0f)
        {
            return;
        }

        float distance = enemy.GetDistanceToPlayer();
        
        if (enemy.CanHitPlayer())
        {
            stateMachine.ChangeState(enemy.AttackState);
            return;
        }

        if (distance <= enemy.AttackRange)
        {
            stateMachine.ChangeState(enemy.AttackState);
            return;
        }

        if (distance <= enemy.DetectRange)
        {
            stateMachine.ChangeState(enemy.ChaseState);
            return;
        }

        stateMachine.ChangeState(enemy.IdleState);
    }

    public void Exit()
    {
    }
}