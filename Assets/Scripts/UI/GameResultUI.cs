
using UnityEngine;

public class GameResultUI : MonoBehaviour
{
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;

    private void Start()
    {
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);

        GameManager.Instance.OnGameStateChanged +=
            HandleGameStateChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -=
                HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.Victory)
        {
            victoryPanel.SetActive(true);
        }
        else if (newState == GameState.Defeat)
        {
            defeatPanel.SetActive(true);
        }
    }
}