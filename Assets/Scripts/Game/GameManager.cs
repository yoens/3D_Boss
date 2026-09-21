using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {get; private set;}

    public GameState CurrentState {get; private set;}

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
        changeState(GameState.Playing);
    }

    public void changeState(GameState newState)
    {
        if(CurrentState == newState)
        {
            return;
        }
        CurrentState = newState;
        Debug.Log($"game state change: {CurrentState}");
        OnGameStateChanged?.Invoke(CurrentState);
    }

    private void OnDestroy()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }
}
