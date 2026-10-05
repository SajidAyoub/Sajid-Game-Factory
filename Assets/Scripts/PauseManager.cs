using System;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    private bool paused;
    private float previousTimeScale = 1f;

    public event Action<bool> PauseStateChanged;

    public void PauseGame()
    {
        // Do not take ownership of a pause established by another system.
        if (paused || Time.timeScale <= 0f)
        {
            return;
        }

        previousTimeScale = Time.timeScale;
        paused = true;
        Time.timeScale = 0f;
        PauseStateChanged?.Invoke(true);
    }

    public void ResumeGame()
    {
        if (!paused)
        {
            return;
        }

        paused = false;
        Time.timeScale = previousTimeScale;
        PauseStateChanged?.Invoke(false);
    }

    public void TogglePause()
    {
        if (paused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public bool IsPaused()
    {
        return paused;
    }

    private void OnDisable()
    {
        // Time scale is global and otherwise survives a scene reload.
        ResumeGame();
    }
}
