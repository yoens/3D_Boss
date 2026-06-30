using UnityEngine;

public class Dummy : MonoBehaviour, IDamageable
{
    [SerializeField] private int hp = 30;

    public void TakeDamage(int amount)
    {
        hp -= amount;
        Debug.Log($"{name} 피격! 남은 HP : {hp}");
        if(hp <= 0)
            Destroy(gameObject);
    }
}
