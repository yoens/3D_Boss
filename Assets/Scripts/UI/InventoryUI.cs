using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform slotParent;
    [SerializeField] private InventorySlotUI slotPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text messageText;

    private readonly List<InventorySlotUI> slots = new List<InventorySlotUI>();
    private float messageUntil;
    private bool cursorOverridden;
    private bool savedCursorVisible;
    private CursorLockMode savedCursorLock;

    private void Awake()
    {
        if (inventory == null || panel == null || slotParent == null || slotPrefab == null ||
            transform == panel.transform || transform.IsChildOf(panel.transform))
        {
            Debug.LogError("InventoryUI 참조를 연결하고, 컴포넌트는 Panel 밖의 활성 오브젝트에 붙여 주세요.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            InventorySlotUI slot = Instantiate(slotPrefab, slotParent);
            slot.gameObject.SetActive(true);
            slot.Bind(inventory, i);
            slots.Add(slot);
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(inventory.Close);

        panel.SetActive(false);
        if (messageText != null)
            messageText.text = "";
    }

    private void OnEnable()
    {
        if (inventory == null || panel == null)
            return;

        inventory.OnChanged += Refresh;
        inventory.OnOpenChanged += SetVisible;
        inventory.OnMessage += ShowMessage;
        Refresh();
        SetVisible(inventory.IsOpen);
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnChanged -= Refresh;
            inventory.OnOpenChanged -= SetVisible;
            inventory.OnMessage -= ShowMessage;
            inventory.Close();
        }

        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (closeButton != null && inventory != null)
            closeButton.onClick.RemoveListener(inventory.Close);
    }

    private void Update()
    {
        if (messageText != null && Time.unscaledTime >= messageUntil)
            messageText.text = "";
    }

    private void Refresh()
    {
        foreach (InventorySlotUI slot in slots)
            slot.Refresh();
    }

    private void SetVisible(bool visible)
    {
        if (visible && !cursorOverridden)
        {
            savedCursorVisible = Cursor.visible;
            savedCursorLock = Cursor.lockState;
            cursorOverridden = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (!visible && cursorOverridden)
        {
            Cursor.lockState = savedCursorLock;
            Cursor.visible = savedCursorVisible;
            cursorOverridden = false;
        }

        if (panel != null)
            panel.SetActive(visible);
    }

    private void ShowMessage(string message)
    {
        if (messageText == null)
            return;

        messageText.text = message;
        messageUntil = Time.unscaledTime + 2f;
    }
}
