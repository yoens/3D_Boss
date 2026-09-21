
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text hpText;

    [SerializeField] private PlayerController player;
    [SerializeField] private EnemyController enemy;

    private void OnEnable()
    {
        if (player != null)
        {
            player.OnHealthChanged += UpdatePlayerHP;

            UpdatePlayerHP(player.Hp, player.MaxHp);
        }

        if (enemy != null)
        {
            enemy.OnHealthChanged += UpdateEnemyHP;

            UpdateEnemyHP(enemy.Hp, enemy.MaxHp);
        }
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.OnHealthChanged -= UpdatePlayerHP;
        }

        if (enemy != null)
        {
            enemy.OnHealthChanged -= UpdateEnemyHP;
        }
    }

    private void UpdatePlayerHP(int current, int max)
    {
        UpdateHP(current, max);
    }

    private void UpdateEnemyHP(int current, int max)
    {
        UpdateHP(current, max);
    }

    private void UpdateHP(int current, int max)
    {
        if (hpSlider != null)
        {
            hpSlider.value = max > 0
                ? current / max
                : 0;
        }

        if (hpText != null)
        {
            hpText.text = $"{current:0} / {max:0}";
        }
    }
}