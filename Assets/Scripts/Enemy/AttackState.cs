using UnityEngine;

public class AttackState : IState
{
    private readonly EnemyController enemy;
    private readonly StateMachine stateMachine;
    private bool isAttacking;
    private bool hitProcessed;

    public bool IsAttacking => isAttacking;

    public AttackState(EnemyController enemy, StateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public void Enter()
    {
        isAttacking = false;
        hitProcessed = false;
        enemy.SetInCombat(true);
        enemy.Move(Vector3.zero);
        enemy.Animator.SetFloat("Speed", 0f);
    }

    public void Update()
    {
        enemy.LookAtPlayer();

        if (isAttacking)
            return;

        if (enemy.TryStartSpecialAttack())
            return;

        if (!enemy.CanHitPlayer())
        {
            stateMachine.ChangeState(enemy.ChaseState);
            return;
        }

        if (!enemy.CanAttack)
            return;

        isAttacking = true;
        hitProcessed = false;
        enemy.Animator.SetTrigger("Attack");
    }

    public bool TryConsumeHit()
    {
        if (!isAttacking || hitProcessed)
            return false;

        hitProcessed = true;
        return true;
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
        hitProcessed = false;
        enemy.Animator.ResetTrigger("Attack");
    }
}
