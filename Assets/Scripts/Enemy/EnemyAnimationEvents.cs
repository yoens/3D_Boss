using UnityEngine;

public class EnemyAnimationEvents : MonoBehaviour
{
    private EnemyController enemy;

    private void Awake()
    {
        enemy = GetComponentInParent<EnemyController>();
    }

    public void OnAttackHit()
    {
        if (enemy != null)
            enemy.Attack();
    }

    public void OnAttackFinished()
    {
        if (enemy != null)
            enemy.FinishAttack();
    }

    public void OnSpecialAttackHit()
    {
        if (enemy != null)
            enemy.SpecialAttackHit();
    }

    public void OnSpecialAttackFinished()
    {
        if (enemy != null)
            enemy.FinishSpecialAttack();
    }

    public void OnDeathFinished()
    {
        if (enemy != null)
            enemy.FinishDeath();
    }
}
