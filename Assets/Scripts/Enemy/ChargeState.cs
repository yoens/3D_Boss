using UnityEngine;

public class ChargeState : IState
{
    private readonly EnemyController enemy;
    private readonly StateMachine stateMachine;
    private readonly BossChargeAttack charge;

    public ChargeState(EnemyController enemy, StateMachine stateMachine,
        BossChargeAttack charge)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
        this.charge = charge;
    }

    public void Enter()
    {
        enemy.Move(Vector3.zero);
        enemy.ClearNavigationPath();
        charge.Begin();
    }

    public void Update()
    {
        if (charge != null && charge.isActiveAndEnabled)
            charge.Tick(Time.deltaTime);

        if (charge != null && charge.isActiveAndEnabled && !charge.IsFinished)
            return;

        if (enemy.Player == null ||
            enemy.GetDistanceToPlayer() > enemy.ChaseLoseRange)
            stateMachine.ChangeState(enemy.IdleState);
        else if (enemy.CanHitPlayer())
            stateMachine.ChangeState(enemy.AttackState);
        else
            stateMachine.ChangeState(enemy.ChaseState);
    }

    public void Exit()
    {
        if (charge != null)
            charge.Cancel();
        enemy.Move(Vector3.zero);
        enemy.ClearNavigationPath();
        enemy.ResetAttackCooldown();
    }
}
