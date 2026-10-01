using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text countText;

    private Button button;
    private PlayerInventory inventory;
    private int index;

    public void Bind(PlayerInventory owner, int slotIndex)
    {
        inventory = owner;
        index = slotIndex;
        button = GetComponent<Button>();
        button.onClick.RemoveListener(Use);
        button.onClick.AddListener(Use);
        Refresh();
    }

    public void Refresh()
    {
        HealingItemData item = inventory.GetItem(index);
        button.interactable = item != null;

        if (icon != null)
        {
            icon.sprite = item != null ? item.Icon : null;
            icon.enabled = item != null && item.Icon != null;
        }

        if (itemNameText != null)
            itemNameText.text = item != null ?
                $"{item.DisplayName}\nHP +{item.HealAmount}" : "Empty Slot";

        if (countText != null)
            countText.text = item != null ? $"x{inventory.GetCount(index)}" : "";
    }

    private void Use()
    {
        inventory.UseSlot(index);
    }
}
