
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    public event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        ChangeState(GameState.Playing);
        GameSettings.ApplySettings();
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        CurrentState = newState;

        Debug.Log($"게임 상태 변경: {CurrentState}");

        OnGameStateChanged?.Invoke(CurrentState);
    }

    public void Victory()
    {
        if (CurrentState != GameState.Playing)
        {
            return;
        }

        ChangeState(GameState.Victory);
    }

    public void Defeat()
    {
        if (CurrentState != GameState.Playing)
        {
            return;
        }

        ChangeState(GameState.Defeat);
    }

    public void Retry()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("MainMenu");
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing)
        {
            return;
        }

        ChangeState(GameState.Paused);

        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused)
        {
            return;
        }

        Time.timeScale = 1f;

        ChangeState(GameState.Playing);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}