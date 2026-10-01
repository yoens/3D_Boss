using UnityEngine;

[RequireComponent(typeof(EnemyController))]
public class EnemyLootDrop : MonoBehaviour
{
    [SerializeField] private WorldItemPickup pickupPrefab;
    [SerializeField, Range(0f, 1f)] private float dropChance = 0.5f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField, Min(0f)] private float groundOffset = 0.25f;

    private EnemyController enemy;
    private bool rolled;

    private void Awake()
    {
        enemy = GetComponent<EnemyController>();
    }

    public void Drop()
    {
        if (rolled || !isActiveAndEnabled || enemy == null || enemy.IsBoss || enemy.Hp > 0)
            return;

        rolled = true;
        if (pickupPrefab == null || dropChance <= 0f ||
            (dropChance < 1f && Random.value >= dropChance))
            return;

        Vector3 origin = transform.position + Vector3.up * 2f;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
            6f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            Debug.LogWarning($"{name}: 드랍 위치의 Ground를 찾지 못했습니다.", this);
            return;
        }

        Instantiate(pickupPrefab, hit.point + Vector3.up * groundOffset,
            Quaternion.identity);
    }
}
