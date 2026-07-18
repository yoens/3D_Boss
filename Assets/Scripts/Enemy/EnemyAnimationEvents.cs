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
}