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
        enemy.Attack();
    }
    public void OnComboHit(int comboIndex)   
    {
        enemy.ComboAttack(comboIndex);
    }
    public void OnAttackFinished()
    {
        enemy.FinishAttack();
    }
    
}