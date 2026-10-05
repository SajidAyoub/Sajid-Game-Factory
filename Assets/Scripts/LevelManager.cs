using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    private int currentLevelIndex;

    public event Action<int> CurrentLevelCompleted;

    private void Awake()
    {
        currentLevelIndex = gameObject.scene.buildIndex;
    }

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.LevelCompleted += HandleLevelCompleted;
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.LevelCompleted -= HandleLevelCompleted;
        }
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

        if (currentLevelIndex < 0)
        {
            Debug.LogWarning("Add the current level to the build scene list.", this);
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
        CurrentLevelCompleted?.Invoke(currentLevelIndex);
    }

    private void LoadLevel(int buildIndex)
    {
        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("The requested level is not in the build scene list.", this);
            return;
        }

        SceneManager.LoadScene(buildIndex);
    }
}
