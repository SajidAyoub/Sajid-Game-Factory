using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, GameOver, LevelComplete }

    [SerializeField] private PlayerController playerController;
    [SerializeField] private PauseManager pauseManager;
    private bool transitioning;
    private bool loadingScene;

    public GameState CurrentState { get; private set; } = GameState.Playing;
    public event Action<GameState> StateChanged;
    public event Action GameOverTriggered;
    public event Action LevelCompleted;

    private void Awake()
    {
        if (playerController == null) Debug.LogError("Assign the PlayerController to GameManager.", this);
        StartGame();
    }

    public void StartGame() => SetState(GameState.Playing);

    public void TriggerGameOver()
    {
        if (CurrentState == GameState.Playing) SetState(GameState.GameOver, GameOverTriggered);
    }

    public void TriggerLevelComplete()
    {
        if (CurrentState == GameState.Playing) SetState(GameState.LevelComplete, LevelCompleted);
    }

    public void RestartGame()
    {
        if (loadingScene) return;
        int index = gameObject.scene.buildIndex;
        if (index < 0 || index >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError("Add the gameplay scene to the build scene list to restart it.", this);
            return;
        }
        loadingScene = true;
        try
        {
            if (pauseManager != null) pauseManager.ResumeGame();
            SceneManager.LoadScene(index);
        }
        catch (Exception exception)
        {
            loadingScene = false;
            Debug.LogException(exception, this);
        }
    }

    private void SetState(GameState state, Action terminalEvent = null)
    {
        // Notifications cannot reenter and reverse a partially published transition.
        if (transitioning) return;
        transitioning = true;
        try
        {
            if (playerController != null) playerController.enabled = state == GameState.Playing;
            if (CurrentState == state) return;
            CurrentState = state;
            GameFactoryEvents.Raise(StateChanged, state, this);
            GameFactoryEvents.Raise(terminalEvent, this);
        }
        finally { transitioning = false; }
    }
}
