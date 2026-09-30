using UnityEngine;

public class IdleState : IState
{
    private readonly EnemyController enemy;
    private readonly StateMachine stateMachine;
    private Vector3 wanderCenter;
    private Vector3 destination;
    private bool hasDestination;
    private float nextSearchTime;
    private float moveTimer;

    public IdleState(EnemyController enemy, StateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public void Enter()
    {
        enemy.SetInCombat(false);
        wanderCenter = enemy.transform.position;
        hasDestination = false;
        nextSearchTime = 0f;
        moveTimer = 0f;
        enemy.ClearNavigationPath();
        enemy.Move(Vector3.zero);
        enemy.Animator.SetFloat("Speed", 0f);
    }

    public void Update()
    {
        if (enemy.Player != null && enemy.GetDistanceToPlayer() <= enemy.DetectRange)
        {
            stateMachine.ChangeState(enemy.ChaseState);
            return;
        }

        if (hasDestination)
        {
            moveTimer += Time.deltaTime;
            if (enemy.HasReachedDestination(destination) || moveTimer >= 15f)
            {
                hasDestination = false;
                nextSearchTime = 0f;
                enemy.ClearNavigationPath();
            }
        }

        if (!hasDestination)
        {
            if (Time.time < nextSearchTime)
                return;
            nextSearchTime = Time.time + 0.5f;
            if (!enemy.TryGetWanderDestination(wanderCenter, out destination))
                return;
            hasDestination = true;
            moveTimer = 0f;
            enemy.ClearNavigationPath();
        }

        enemy.MoveToPosition(destination, true);
    }

    public void Exit()
    {
        hasDestination = false;
        enemy.Move(Vector3.zero);
        enemy.Animator.SetFloat("Speed", 0f);
        enemy.ClearNavigationPath();
    }
}
