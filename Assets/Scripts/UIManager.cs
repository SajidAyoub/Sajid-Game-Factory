using System;
using UnityEngine;

[DisallowMultipleComponent]
public class UIManager : MonoBehaviour
{
    public enum UIState
    {
        None,
        MainMenu,
        GameplayHUD,
        PauseMenu,
        GameOver,
        LevelComplete,
        Settings
    }

    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameplayHUDPanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private UIState initialState = UIState.MainMenu;

    private bool stateSelected;

    public UIState CurrentState { get; private set; } = UIState.None;
    public event Action<UIState> UIStateChanged;

    private void Start()
    {
        // Preserve any explicit state selected by another component before Start.
        if (!stateSelected)
        {
            ShowState(initialState);
        }
    }

    public void ShowMainMenu() => ShowState(UIState.MainMenu);
    public void ShowGameplayHUD() => ShowState(UIState.GameplayHUD);
    public void ShowPauseMenu() => ShowState(UIState.PauseMenu);
    public void ShowGameOver() => ShowState(UIState.GameOver);
    public void ShowLevelComplete() => ShowState(UIState.LevelComplete);
    public void ShowSettings() => ShowState(UIState.Settings);
    public void HideAllPanels() => ShowState(UIState.None);

    private void ShowState(UIState state)
    {
        if (!Enum.IsDefined(typeof(UIState), state))
        {
            Debug.LogWarning("Invalid UI state; showing the main menu.", this);
            state = UIState.MainMenu;
        }
        GameObject activePanel = GetPanel(state);
        stateSelected = true;
        if (mainMenuPanel != activePanel) SetPanelActive(mainMenuPanel, false);
        if (gameplayHUDPanel != activePanel) SetPanelActive(gameplayHUDPanel, false);
        if (pauseMenuPanel != activePanel) SetPanelActive(pauseMenuPanel, false);
        if (gameOverPanel != activePanel) SetPanelActive(gameOverPanel, false);
        if (levelCompletePanel != activePanel) SetPanelActive(levelCompletePanel, false);
        if (settingsPanel != activePanel) SetPanelActive(settingsPanel, false);
        SetPanelActive(activePanel, true);

        if (CurrentState != state)
        {
            CurrentState = state;
            GameFactoryEvents.Raise(UIStateChanged, state, this);
        }
    }

    private GameObject GetPanel(UIState state)
    {
        switch (state)
        {
            case UIState.MainMenu: return mainMenuPanel;
            case UIState.GameplayHUD: return gameplayHUDPanel;
            case UIState.PauseMenu: return pauseMenuPanel;
            case UIState.GameOver: return gameOverPanel;
            case UIState.LevelComplete: return levelCompletePanel;
            case UIState.Settings: return settingsPanel;
            default: return null;
        }
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null && panel.activeSelf != active)
        {
            panel.SetActive(active);
        }
    }
}
