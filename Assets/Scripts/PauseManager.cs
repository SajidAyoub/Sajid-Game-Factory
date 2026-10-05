using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PauseManager : MonoBehaviour
{
    // Ownership of the one global time scale, not a general manager singleton.
    private static PauseManager pauseOwner;
    private bool paused;
    private float previousTimeScale = 1f;
    public event Action<bool> PauseStateChanged;

    public void PauseGame()
    {
        if (!isActiveAndEnabled || paused || pauseOwner != null || Time.timeScale <= 0f ||
            float.IsNaN(Time.timeScale) || float.IsInfinity(Time.timeScale)) return;
        previousTimeScale = Time.timeScale;
        pauseOwner = this;
        paused = true;
        Time.timeScale = 0f;
        GameFactoryEvents.Raise(PauseStateChanged, true, this);
    }

    public void ResumeGame()
    {
        if (!paused) return;
        paused = false;
        if (pauseOwner == this)
        {
            pauseOwner = null;
            // Preserve a later override made by another time-scale controller.
            if (Time.timeScale == 0f) Time.timeScale = previousTimeScale;
        }
        GameFactoryEvents.Raise(PauseStateChanged, false, this);
    }

    public void TogglePause()
    {
        if (paused) ResumeGame();
        else PauseGame();
    }

    public bool IsPaused() => paused;

    private void Update()
    {
        if (paused && Time.timeScale != 0f) ResumeGame();
    }

    private void OnDisable() => ResumeGame();
}
