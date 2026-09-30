using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text hpText;

    [SerializeField] private PlayerController player;
    [SerializeField] private EnemyController enemy;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
            hpSlider.wholeNumbers = false;
        }

        SetVisible(false);
    }

    private void OnEnable()
    {
        if (player != null)
        {
            player.OnHealthChanged += UpdateHP;
        }
        else if (enemy != null)
        {
            enemy.OnHealthChanged += UpdateHP;
            enemy.OnCombatStateChanged += HandleCombatStateChanged;
        }

        Refresh();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.OnHealthChanged -= UpdateHP;
        }
        else if (enemy != null)
        {
            enemy.OnHealthChanged -= UpdateHP;
            enemy.OnCombatStateChanged -= HandleCombatStateChanged;
        }

        SetVisible(false);
    }

    private void Refresh()
    {
        if (player != null)
        {
            UpdateHP(player.Hp, player.MaxHp);
            SetVisible(true);
            return;
        }

        if (enemy != null)
        {
            UpdateHP(enemy.Hp, enemy.MaxHp);

            SetVisible(
                enemy.IsBoss &&
                enemy.IsInCombat &&
                enemy.Hp > 0
            );

            return;
        }

        SetVisible(false);
    }

    private void HandleCombatStateChanged(bool inCombat)
    {
        if (enemy == null)
        {
            SetVisible(false);
            return;
        }

        UpdateHP(enemy.Hp, enemy.MaxHp);

        SetVisible(
            enemy.IsBoss &&
            inCombat &&
            enemy.Hp > 0
        );
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }
    }

    private void UpdateHP(int current, int max)
    {
        float hpRatio = max > 0
            ? Mathf.Clamp01((float)current / max)
            : 0f;

        if (hpSlider != null)
        {
            hpSlider.value = hpRatio;
        }

        if (hpText != null)
        {
            hpText.text = $"{current} / {max}";
        }
    }
}