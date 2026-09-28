using UnityEngine;

public class AttackState : IState
{
    private readonly EnemyController enemy;
    private readonly StateMachine stateMachine;

    private bool isAttacking;

    public bool IsAttacking => isAttacking;

    public AttackState(
        EnemyController enemy,
        StateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public void Enter()
    {
        isAttacking = false;
        enemy.Animator.SetFloat("Speed", 0f);
    }

    public void Update()
    {
        
        enemy.LookAtPlayer();

        if (isAttacking)
            return;

        if (enemy.GetDistanceToPlayer() > enemy.AttackRange)
        {
            stateMachine.ChangeState(enemy.ChaseState);
            return;
        }

        if (!enemy.CanAttack)
            return;

        isAttacking = true;
        enemy.Animator.SetTrigger("Attack");
    }

    public void FinishAttack()
    {
        if (!isAttacking)
            return;

        isAttacking = false;
        enemy.ResetAttackCooldown();
    }

    public void Exit()
    {
        isAttacking = false;
        enemy.Animator.ResetTrigger("Attack");
    }
}