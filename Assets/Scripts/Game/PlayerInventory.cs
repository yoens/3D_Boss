using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(PlayerController))]
public class PlayerInventory : MonoBehaviour
{
    [SerializeField, Min(1)] private int slotCount = 8;

    private HealingItemData[] items;
    private int[] counts;
    private PlayerController player;
    private int closedFrame = -1;
    private bool usingItem;

    public bool IsOpen { get; private set; }
    public int SlotCount { get { EnsureSlots(); return items.Length; } }
    public bool BlocksGameplayInput => IsOpen || closedFrame == Time.frameCount ||
        (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame);

    public event Action OnChanged;
    public event Action<bool> OnOpenChanged;
    public event Action<string> OnMessage;

    private void Awake()
    {
        EnsureSlots();
        player = GetComponent<PlayerController>();
    }

    private void EnsureSlots()
    {
        if (items != null)
            return;

        items = new HealingItemData[Mathf.Max(1, slotCount)];
        counts = new int[items.Length];
    }

    private void Update()
    {
        if (player == null || !player.CanUseInventory)
        {
            Close();
            return;
        }

        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            SetOpen(!IsOpen);
    }

    private void OnDisable()
    {
        Close();
    }

    private void SetOpen(bool value)
    {
        if (IsOpen == value)
            return;

        IsOpen = value;
        if (!value)
            closedFrame = Time.frameCount;

        OnOpenChanged?.Invoke(value);
    }

    public void Close()
    {
        SetOpen(false);
    }

    public HealingItemData GetItem(int index)
    {
        EnsureSlots();
        return index >= 0 && index < items.Length ? items[index] : null;
    }

    public int GetCount(int index)
    {
        EnsureSlots();
        return index >= 0 && index < counts.Length ? counts[index] : 0;
    }

    public bool TryAdd(HealingItemData item, int amount)
    {
        EnsureSlots();
        if (item == null || amount <= 0)
            return false;

        long capacity = 0;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                capacity += item.MaxStack;
            else if (items[i] == item)
                capacity += Mathf.Max(0, item.MaxStack - counts[i]);
        }

        if (capacity < amount)
            return false;

        int remaining = amount;
        for (int i = 0; i < items.Length && remaining > 0; i++)
        {
            if (items[i] != item)
                continue;

            int added = Mathf.Min(remaining, Mathf.Max(0, item.MaxStack - counts[i]));
            counts[i] += added;
            remaining -= added;
        }

        for (int i = 0; i < items.Length && remaining > 0; i++)
        {
            if (items[i] != null)
                continue;

            int added = Mathf.Min(remaining, item.MaxStack);
            items[i] = item;
            counts[i] = added;
            remaining -= added;
        }

        OnChanged?.Invoke();
        return true;
    }

    public void UseSlot(int index)
    {
        if (!IsOpen || usingItem || player == null || !player.CanUseInventory)
            return;

        HealingItemData item = GetItem(index);
        if (item == null || counts[index] <= 0)
            return;

        usingItem = true;
        try
        {
            int before = player.Hp;
            if (!player.TryHeal(item.HealAmount))
            {
                Notify("life is full or can't use now.");
                return;
            }

            counts[index]--;
            if (counts[index] == 0)
                items[index] = null;

            OnChanged?.Invoke();
            Notify($"{item.DisplayName} USE · HP +{player.Hp - before}");
        }
        finally
        {
            usingItem = false;
        }
    }

    public void Notify(string message)
    {
        OnMessage?.Invoke(message);
    }
}
