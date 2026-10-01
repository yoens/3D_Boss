using UnityEngine;

[CreateAssetMenu(menuName = "Game/Items/Healing Item", fileName = "HealingPotion")]
public class HealingItemData : ScriptableObject
{
    [SerializeField] private string displayName = "회복 물약";
    [SerializeField] private Sprite icon;
    [SerializeField, Min(1)] private int healAmount = 30;
    [SerializeField, Min(1)] private int maxStack = 10;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public int HealAmount => Mathf.Max(1, healAmount);
    public int MaxStack => Mathf.Max(1, maxStack);
}
