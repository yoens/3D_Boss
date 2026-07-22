using UnityEngine;

public class AttackState : IState
{
    private EnemyController enemy;
    private StateMachine stateMachine;

    private bool isAttacking;
    private bool isComboPlaying;

    public AttackState(EnemyController enemy, StateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public void Enter()
    {
        isAttacking = false;
        isComboPlaying = false;
    }

    public void Update()
    {
        enemy.LookAtPlayer();

        if (isAttacking) return;

        float distance = enemy.GetDistanceToPlayer();

        if (distance > enemy.AttackRange)
        {
            stateMachine.ChangeState(enemy.ChaseState);
            return;
        }


        if (!enemy.CanAttack) return;

        isAttacking = true;

        // 40% 확률로 콤보 공격 선택
        isComboPlaying = Random.value < 0.4f;

        enemy.Animator.SetTrigger(isComboPlaying ? "ComboAttack" : "Attack");
    }

    public void FinishAttack()
    {
        Debug.Log("FinishAttack 호출");
        isAttacking = false;
        isComboPlaying = false;
        enemy.ResetAttackCooldown(); 
    }

    public void Exit()
    {
        isAttacking = false;
        isComboPlaying = false;
    }
}