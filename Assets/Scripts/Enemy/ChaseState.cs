using UnityEngine;

public class ChaseState : IState
{
    private readonly EnemyController enemy;
    private readonly StateMachine stateMachine;

    public ChaseState(EnemyController enemy, StateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public void Enter()
    {
        enemy.SetInCombat(true);
        enemy.ClearNavigationPath();
        enemy.Animator.SetFloat("Speed", 0f);
    }

    public void Update()
    {
        if (enemy.Player == null || enemy.GetDistanceToPlayer() > enemy.ChaseLoseRange)
        {
            stateMachine.ChangeState(enemy.IdleState);
            return;
        }

        if (enemy.TryStartChargeAttack())
            return;

        if (enemy.TryStartSpecialAttack())
            return;

        if (enemy.CanHitPlayer())
        {
            stateMachine.ChangeState(enemy.AttackState);
            return;
        }

        enemy.MoveToPlayer();
    }

    public void Exit()
    {
        enemy.Move(Vector3.zero);
        enemy.Animator.SetFloat("Speed", 0f);
        enemy.ClearNavigationPath();
    }
}
