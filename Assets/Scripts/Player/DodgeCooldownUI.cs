using UnityEngine;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class DodgeCooldownUI : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private TMP_Text cooldownText;

    private CanvasGroup canvasGroup;
    private int lastDisplayedSeconds = -1;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void LateUpdate()
    {
        float remaining = player != null
            ? player.DodgeCooldownRemaining
            : 0f;

        bool visible = remaining > 0f;
        canvasGroup.alpha = visible ? 1f : 0f;

        int seconds = visible
            ? Mathf.CeilToInt(remaining)
            : 0;

        if (seconds == lastDisplayedSeconds)
            return;

        lastDisplayedSeconds = seconds;

        if (cooldownText != null)
        {
            cooldownText.text = visible
                ? seconds.ToString()
                : string.Empty;
        }
    }
}