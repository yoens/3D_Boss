using UnityEngine;

public class ChaseState : IState
{
    private readonly EnemyController enemy;
    private readonly StateMachine stateMachine;

    public ChaseState(
        EnemyController enemy,
        StateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public void Enter()
    {
        enemy.ClearNavigationPath();
        enemy.Animator.SetFloat("Speed", 0f);
    }

    public void Update()
    {
        if (enemy.GetDistanceToPlayer() > enemy.ChaseLoseRange)
        {
            stateMachine.ChangeState(enemy.IdleState);
            return;
        }

        if (enemy.CanHitPlayer())
        {
            stateMachine.ChangeState(enemy.AttackState);
            return;
        }

        // 경로 방향으로의 회전도 이 메서드에서 처리합니다.
        enemy.MoveToPlayer();
    }

    public void Exit()
    {
        enemy.Move(Vector3.zero);
        enemy.Animator.SetFloat("Speed", 0f);
        enemy.ClearNavigationPath();
    }
}