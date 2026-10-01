using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInventory), typeof(PlayerController))]
public class PlayerItemInteractor : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float pickupRadius = 2f;
    [SerializeField] private LayerMask pickupLayer;
    [SerializeField] private LayerMask obstructionLayer;
    [SerializeField] private TMP_Text promptText;

    private PlayerInventory inventory;
    private PlayerController player;
    private WorldItemPickup nearest;
    private float nextScanTime;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (!player.CanUseInventory || inventory.BlocksGameplayInput)
        {
            SetPrompt("");
            return;
        }

        bool pressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        if (Time.time >= nextScanTime || pressed)
        {
            nextScanTime = Time.time + 0.1f;
            FindNearest();
        }

        if (pressed && nearest != null && nearest.IsAvailable)
        {
            nearest.TryCollect(inventory);
            FindNearest();
        }

        SetPrompt(nearest != null && nearest.IsAvailable ? nearest.Prompt : "");
    }

    private void FindNearest()
    {
        nearest = null;
        float bestDistance = float.PositiveInfinity;
        Vector3 origin = transform.position + Vector3.up * 0.75f;

        Collider[] hits = Physics.OverlapSphere(origin, pickupRadius,
            pickupLayer, QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            WorldItemPickup pickup = hit.GetComponentInParent<WorldItemPickup>();
            if (pickup == null || !pickup.IsAvailable)
                continue;

            Vector3 destination = pickup.transform.position;
            float distance = (destination - origin).sqrMagnitude;
            if (distance >= bestDistance ||
                Physics.Linecast(origin, destination, obstructionLayer,
                    QueryTriggerInteraction.Ignore))
                continue;

            bestDistance = distance;
            nearest = pickup;
        }
    }

    private void SetPrompt(string text)
    {
        if (promptText != null)
            promptText.text = text;
    }

    private void OnDisable()
    {
        SetPrompt("");
    }
}
