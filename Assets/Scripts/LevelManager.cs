using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class LevelManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PauseManager pauseManager;

    private int currentLevelIndex;
    private GameManager subscribedGameManager;
    private bool loadingScene;

    public event Action<int> CurrentLevelCompleted;

    private void Awake()
    {
        currentLevelIndex = gameObject.scene.buildIndex;
    }

    private void OnEnable()
    {
        if (subscribedGameManager != null)
        {
            subscribedGameManager.LevelCompleted -= HandleLevelCompleted;
        }
        subscribedGameManager = gameManager;
        if (subscribedGameManager != null) subscribedGameManager.LevelCompleted += HandleLevelCompleted;
    }

    private void OnDisable()
    {
        if (subscribedGameManager != null)
        {
            subscribedGameManager.LevelCompleted -= HandleLevelCompleted;
        }
        subscribedGameManager = null;
    }

    public bool CompleteCurrentLevel()
    {
        if (gameManager == null)
        {
            Debug.LogWarning("Assign a GameManager to LevelManager.", this);
            return false;
        }

        if (gameManager.CurrentState != GameManager.GameState.Playing)
        {
            return false;
        }

        gameManager.TriggerLevelComplete();
        return true;
    }

    public void LoadNextLevel()
    {
        if (gameManager == null ||
            gameManager.CurrentState != GameManager.GameState.LevelComplete)
        {
            return;
        }

        if (currentLevelIndex < 0 || currentLevelIndex >= SceneManager.sceneCountInBuildSettings - 1)
        {
            Debug.LogWarning("No next level is available in the build scene list.", this);
            return;
        }

        LoadLevel(currentLevelIndex + 1);
    }

    public void RestartCurrentLevel()
    {
        LoadLevel(currentLevelIndex);
    }

    public int GetCurrentLevel()
    {
        return currentLevelIndex;
    }

    private void HandleLevelCompleted()
    {
        GameFactoryEvents.Raise(CurrentLevelCompleted, currentLevelIndex, this);
    }

    private void LoadLevel(int buildIndex)
    {
        if (loadingScene) return;
        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("The requested level is not in the build scene list.", this);
            return;
        }

        loadingScene = true;
        try
        {
            if (pauseManager != null) pauseManager.ResumeGame();
            SceneManager.LoadScene(buildIndex);
        }
        catch (Exception exception)
        {
            loadingScene = false;
            Debug.LogException(exception, this);
        }
    }
}
