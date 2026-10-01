using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class WorldItemPickup : MonoBehaviour
{
    [SerializeField] private HealingItemData item;
    [SerializeField, Min(1)] private int amount = 1;

    private bool collecting;
    private bool collected;

    public bool IsAvailable => isActiveAndEnabled && !collected && !collecting &&
        item != null && amount > 0;
    public string Prompt => item != null ? $"[E] {item.DisplayName} x{amount} 줍기" : "";

    private void Reset()
    {
        GetComponent<SphereCollider>().isTrigger = true;
    }

    public bool TryCollect(PlayerInventory inventory)
    {
        if (!IsAvailable || inventory == null)
            return false;

        collecting = true;
        if (!inventory.TryAdd(item, amount))
        {
            collecting = false;
            inventory.Notify("인벤토리 공간이 부족합니다.");
            return false;
        }

        collected = true;
        inventory.Notify($"{item.DisplayName} x{amount} 획득");
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }
}
