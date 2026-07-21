using UnityEngine;

public class AttackState : IState
{
    private EnemyController enemy;
    private StateMachine stateMachine;

    private float attackCooldown = 1f;
    private float attackTimer;

    private bool isAttacking;
    private bool isComboPlaying;

    public AttackState(EnemyController enemy, StateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public void Enter()
    {
        attackTimer = 0f;
        isAttacking = false;
        isComboPlaying = false;
    }

    public void Update()
    {
        float distance = enemy.GetDistanceToPlayer();

        if (distance > enemy.AttackRange)
        {
            stateMachine.ChangeState(enemy.ChaseState);
            return;
        }

        enemy.LookAtPlayer();

        attackTimer -= Time.deltaTime;

        if (attackTimer > 0f || isAttacking)
        {
            return;
        }

        isAttacking = true;

        // 40% 확률로 콤보 공격 선택
        isComboPlaying = Random.value < 0.4f;

        if (isComboPlaying)
        {
            enemy.Animator.SetTrigger("ComboAttack");
        }
        else
        {
            enemy.Animator.SetTrigger("Attack");
        }
    }

    public void FinishAttack()
    {
        Debug.Log("FinishAttack 호출");
        isAttacking = false;
        isComboPlaying = false;
        attackTimer = attackCooldown;
    }

    public void Exit()
    {
        isAttacking = false;
        isComboPlaying = false;
    }
}