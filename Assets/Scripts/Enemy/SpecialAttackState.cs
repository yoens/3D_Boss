using UnityEngine;

public class SpecialAttackState : IState
{
    private readonly EnemyController enemy;
    private readonly StateMachine stateMachine;
    private float elapsed;
    private float recoveryTimer;
    private bool hitProcessed;
    private bool animationFinished;

    public SpecialAttackState(EnemyController enemy, StateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public void Enter()
    {
        elapsed = 0f;
        recoveryTimer = 0f;
        hitProcessed = false;
        animationFinished = false;
        enemy.Sfx?.PlaySpecialImpact();
        enemy.BeginSpecialAttack();
    }

    public void Update()
    {
        elapsed += Time.deltaTime;

        if (hitProcessed)
        {
            recoveryTimer += Time.deltaTime;
            if (recoveryTimer >= 0.25f)
                enemy.HideSpecialIndicator();

            if (animationFinished && recoveryTimer >= enemy.SpecialRecoveryDuration)
            {
                ReturnToCombat();
                return;
            }
        }

        if (elapsed >= enemy.SpecialTimeout)
        {
            Debug.LogWarning($"{enemy.name}: 특수 공격 시간 초과. 애니메이션 전환/이벤트와 Special Timeout을 확인하세요.");
            ReturnToCombat();
        }
    }

    public void Hit()
    {
        if (hitProcessed || animationFinished)
            return;

        hitProcessed = true;
        recoveryTimer = 0f;
        
        enemy.ApplySpecialAttackDamage();
    }

    public void FinishAnimation()
    {
        if (animationFinished)
            return;

        animationFinished = true;
        if (!hitProcessed)
        {
            Debug.LogWarning($"{enemy.name}: OnSpecialAttackHit 없이 특수 공격이 종료됐습니다. 이벤트 순서를 확인하세요.");
            ReturnToCombat();
        }
    }

    private void ReturnToCombat()
    {
        if (enemy.Player == null || enemy.GetDistanceToPlayer() > enemy.ChaseLoseRange)
        {
            stateMachine.ChangeState(enemy.IdleState);
        }
        else if (enemy.CanHitPlayer())
        {
            stateMachine.ChangeState(enemy.AttackState);
        }
        else
        {
            stateMachine.ChangeState(enemy.ChaseState);
        }
    }

    public void Exit()
    {
        enemy.EndSpecialAttack();
    }
}
