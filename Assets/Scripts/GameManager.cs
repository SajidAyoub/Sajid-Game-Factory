using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Playing,
        GameOver,
        LevelComplete
    }

    [SerializeField] private PlayerController playerController;

    public GameState CurrentState { get; private set; } = GameState.Playing;

    public event Action<GameState> StateChanged;
    public event Action GameOverTriggered;
    public event Action LevelCompleted;

    private void Awake()
    {
        if (playerController == null)
        {
            Debug.LogError("Assign the PlayerController to GameManager.", this);
        }

        StartGame();
    }

    public void StartGame()
    {
        SetState(GameState.Playing);
    }

    public void TriggerGameOver()
    {
        if (CurrentState != GameState.Playing)
        {
            return;
        }

        SetState(GameState.GameOver);
        GameOverTriggered?.Invoke();
    }

    public void TriggerLevelComplete()
    {
        if (CurrentState != GameState.Playing)
        {
            return;
        }

        SetState(GameState.LevelComplete);
        LevelCompleted?.Invoke();
    }

    public void RestartGame()
    {
        // Reload the scene to restore the player, collected coins and score.
        Scene scene = gameObject.scene;
        if (scene.buildIndex < 0)
        {
            Debug.LogError("Add the gameplay scene to the build scene list to restart it.", this);
            return;
        }

        SceneManager.LoadScene(scene.buildIndex);
    }

    private void SetState(GameState state)
    {
        if (playerController != null)
        {
            playerController.enabled = state == GameState.Playing;
        }

        if (CurrentState == state)
        {
            return;
        }

        CurrentState = state;
        StateChanged?.Invoke(state);
    }
}
