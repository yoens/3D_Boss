using UnityEngine;

public class DeadState : IState
{
    private readonly EnemyController enemy;

    public DeadState(
        EnemyController enemy,
        StateMachine stateMachine)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        Debug.Log("Enemy down");

        enemy.Move(Vector3.zero);

        enemy.Animator.SetFloat("Speed", 0f);
        enemy.Animator.ResetTrigger("Attack");
        enemy.Animator.ResetTrigger("Hit");
        enemy.Animator.SetTrigger("Death");
    }

    public void Update()
    {
    }

    public void Exit()
    {
    }
}