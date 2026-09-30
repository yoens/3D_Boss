using UnityEngine;

public class HitState : IState
{
    private readonly EnemyController enemy;
    private readonly StateMachine stateMachine;
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
        hitTimer -= Time.deltaTime;
        if (hitTimer > 0f)
            return;

        if (enemy.CanHitPlayer())
        {
            stateMachine.ChangeState(enemy.AttackState);
        }
        else if (enemy.Player != null && enemy.GetDistanceToPlayer() <= enemy.ChaseLoseRange)
        {
            stateMachine.ChangeState(enemy.ChaseState);
        }
        else
        {
            stateMachine.ChangeState(enemy.IdleState);
        }
    }

    public void Exit() { }
}
