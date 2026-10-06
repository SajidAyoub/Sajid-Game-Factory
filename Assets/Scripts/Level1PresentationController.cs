using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Scene-owned adapter: presentation, navigation and best-score persistence only.
// No movement, currency grants, ad provider, automatic missions or scene searches.
[DisallowMultipleComponent]
public sealed class Level1PresentationController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private PauseManager pauseManager;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private SettingsManager settingsManager;
    [SerializeField] private RewardManager rewardManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private HUDController hudController;
    [SerializeField] private Transform player;
    [SerializeField] private Transform finish;
    [SerializeField] private Coin[] coins = new Coin[0];
    [SerializeField, Min(1)] private int displayLevelNumber = 1;
    [SerializeField] private Text rewardText;
    [SerializeField] private Text gameOverScoreText;
    [SerializeField] private Text gameOverBestText;
    [SerializeField] private Text completeScoreText;
    [SerializeField] private Text musicText;
    [SerializeField] private Text sfxText;
    [SerializeField] private Text vibrationText;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private ParticleSystem coinCollectVFX;
    [SerializeField] private ParticleSystem playerHitVFX;
    [SerializeField] private ParticleSystem finishVFX;
    [Header("Optional audio assets — silence is safe")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip coinSFX;
    [SerializeField] private AudioClip hitSFX;
    [SerializeField] private AudioClip finishSFX;
    [SerializeField] private AudioClip uiClickSFX;

    private GameManager boundGame;
    private ScoreManager boundScore;
    private PauseManager boundPause;
    private RewardManager boundRewards;
    private SettingsManager boundSettings;
    private Coin[] boundCoins;
    private Action<int>[] coinCallbacks;
    private int coinCount;
    private bool started;
    private bool settingsOpen;
    private bool pausedBeforeSettings;

    private void OnEnable()
    {
        Unsubscribe();
        boundGame = gameManager; boundScore = scoreManager; boundPause = pauseManager;
        boundRewards = rewardManager; boundSettings = settingsManager;
        if (boundGame != null)
        {
            boundGame.StateChanged += OnStateChanged;
            boundGame.GameOverTriggered += OnGameOver;
            boundGame.LevelCompleted += OnLevelComplete;
        }
        if (boundScore != null) boundScore.ScoreChanged += OnScoreChanged;
        if (boundPause != null) boundPause.PauseStateChanged += OnPauseChanged;
        if (boundRewards != null) boundRewards.RewardBalanceChanged += OnRewardChanged;
        if (boundSettings != null)
        {
            boundSettings.MusicEnabledChanged += OnSettingChanged;
            boundSettings.SFXEnabledChanged += OnSettingChanged;
            boundSettings.VibrationEnabledChanged += OnSettingChanged;
        }
        boundCoins = coins != null ? (Coin[])coins.Clone() : new Coin[0];
        coinCallbacks = new Action<int>[boundCoins.Length];
        for (int i = 0; i < boundCoins.Length; i++)
        {
            Coin coin = boundCoins[i];
            bool duplicate = false;
            for (int j = 0; j < i; j++) if (boundCoins[j] == coin) duplicate = true;
            if (coin == null || duplicate) continue;
            coinCallbacks[i] = value => OnCoinCollected(coin);
            coin.Collected += coinCallbacks[i];
        }
        if (started) Synchronize();
    }

    private void Start()
    {
        started = true;
        Synchronize(); // All initial Awake preferences/state have now been loaded.
        if (audioManager != null && backgroundMusic != null) audioManager.PlayMusic(backgroundMusic);
    }

    private void OnDisable() => Unsubscribe();

    private void Unsubscribe()
    {
        if (boundGame != null)
        {
            boundGame.StateChanged -= OnStateChanged;
            boundGame.GameOverTriggered -= OnGameOver;
            boundGame.LevelCompleted -= OnLevelComplete;
        }
        if (boundScore != null) boundScore.ScoreChanged -= OnScoreChanged;
        if (boundPause != null) boundPause.PauseStateChanged -= OnPauseChanged;
        if (boundRewards != null) boundRewards.RewardBalanceChanged -= OnRewardChanged;
        if (boundSettings != null)
        {
            boundSettings.MusicEnabledChanged -= OnSettingChanged;
            boundSettings.SFXEnabledChanged -= OnSettingChanged;
            boundSettings.VibrationEnabledChanged -= OnSettingChanged;
        }
        if (boundCoins != null && coinCallbacks != null)
            for (int i = 0; i < boundCoins.Length; i++)
                if (boundCoins[i] != null && coinCallbacks[i] != null) boundCoins[i].Collected -= coinCallbacks[i];
        boundGame = null; boundScore = null; boundPause = null; boundRewards = null; boundSettings = null;
        boundCoins = null; coinCallbacks = null;
    }

    private void Synchronize()
    {
        if (saveManager != null) saveManager.LoadData();
        OnScoreChanged(scoreManager != null ? scoreManager.GetScore() : 0);
        if (hudController != null)
        {
            // Build index may be 1 because SampleScene precedes MainGame.
            hudController.UpdateLevel(Mathf.Max(1, displayLevelNumber));
            hudController.UpdateCoinCount(coinCount);
        }
        OnRewardChanged(rewardManager != null ? rewardManager.GetRewardBalance() : 0);
        RefreshSettings();
        ApplyState();
    }

    private void OnScoreChanged(int score)
    {
        int best = Mathf.Max(score, saveManager != null ? saveManager.BestScore : 0);
        if (hudController != null) { hudController.UpdateScore(score); hudController.UpdateBestScore(best); }
        SetText(gameOverScoreText, "Score  " + score);
        SetText(gameOverBestText, "Best  " + best);
        SetText(completeScoreText, "Score  " + score);
    }

    private void OnCoinCollected(Coin coin)
    {
        if (coinCount < int.MaxValue) coinCount++;
        if (hudController != null) hudController.UpdateCoinCount(coinCount);
        if (coin != null) PlayCoinCollect(coin.transform.position);
        PlaySound(coinSFX);
    }

    private void OnRewardChanged(int balance) => SetText(rewardText, "Rewards  " + Mathf.Max(0, balance));
    private void OnStateChanged(GameManager.GameState state) => ApplyState();
    private void OnPauseChanged(bool paused) => ApplyState();
    private void OnSettingChanged(bool enabled) => RefreshSettings();

    private void OnGameOver()
    {
        settingsOpen = false;
        PersistBest();
        if (pauseManager != null) pauseManager.ResumeGame();
        PlayPlayerHit(player != null ? player.position : transform.position);
        PlaySound(hitSFX);
        ApplyState();
    }

    private void OnLevelComplete()
    {
        settingsOpen = false;
        PersistBest();
        if (pauseManager != null) pauseManager.ResumeGame();
        PlayFinish(finish != null ? finish.position + Vector3.up : transform.position);
        PlaySound(finishSFX);
        ApplyState();
    }

    private void PersistBest()
    {
        if (saveManager != null && scoreManager != null) saveManager.UpdateBestScore(scoreManager.GetScore());
        OnScoreChanged(scoreManager != null ? scoreManager.GetScore() : 0);
    }

    private void ApplyState()
    {
        if (uiManager == null || gameManager == null) return;
        if (gameManager.CurrentState == GameManager.GameState.GameOver) uiManager.ShowGameOver();
        else if (gameManager.CurrentState == GameManager.GameState.LevelComplete) uiManager.ShowLevelComplete();
        else if (settingsOpen) uiManager.ShowSettings();
        else if (pauseManager != null && pauseManager.IsPaused()) uiManager.ShowPauseMenu();
        else uiManager.ShowGameplayHUD();
        if (nextLevelButton != null) nextLevelButton.interactable = HasNextLevel();
    }

    public void Pause()
    {
        PlaySound(uiClickSFX);
        if (gameManager != null && gameManager.CurrentState == GameManager.GameState.Playing && pauseManager != null) pauseManager.PauseGame();
    }

    public void Resume()
    {
        PlaySound(uiClickSFX);
        settingsOpen = false;
        if (pauseManager != null) pauseManager.ResumeGame();
        ApplyState(); // Never replace a terminal panel with HUD.
    }

    public void Restart()
    {
        PlaySound(uiClickSFX);
        if (gameManager != null) gameManager.RestartGame(); // Existing loader resumes the assigned pause owner.
    }

    public void OpenSettings()
    {
        if (settingsOpen) return;
        PlaySound(uiClickSFX);
        if (gameManager == null || gameManager.CurrentState != GameManager.GameState.Playing) return;
        pausedBeforeSettings = pauseManager != null && pauseManager.IsPaused();
        settingsOpen = true;
        if (pauseManager != null) pauseManager.PauseGame();
        RefreshSettings(); ApplyState();
    }

    public void CloseSettings()
    {
        PlaySound(uiClickSFX);
        settingsOpen = false;
        if (!pausedBeforeSettings && pauseManager != null) pauseManager.ResumeGame();
        ApplyState();
    }

    public void NextLevel()
    {
        PlaySound(uiClickSFX);
        if (!HasNextLevel()) { Debug.Log("Next level not configured.", this); return; }
        if (levelManager != null) levelManager.LoadNextLevel();
    }

    private bool HasNextLevel()
    {
        if (levelManager == null || gameManager == null || gameManager.CurrentState != GameManager.GameState.LevelComplete) return false;
        int index = levelManager.GetCurrentLevel();
        return index >= 0 && index < SceneManager.sceneCountInBuildSettings - 1;
    }

    public void ToggleMusic() { PlaySound(uiClickSFX); if (settingsManager != null) settingsManager.ToggleMusic(); }
    public void ToggleSFX() { PlaySound(uiClickSFX); if (settingsManager != null) settingsManager.ToggleSFX(); }
    public void ToggleVibration() { PlaySound(uiClickSFX); if (settingsManager != null) settingsManager.ToggleVibration(); }

    private void RefreshSettings()
    {
        if (settingsManager == null) return;
        SetText(musicText, "Music  " + (settingsManager.MusicEnabled ? "ON" : "OFF"));
        SetText(sfxText, "SFX  " + (settingsManager.SFXEnabled ? "ON" : "OFF"));
        SetText(vibrationText, "Vibration  " + (settingsManager.VibrationEnabled ? "ON" : "OFF"));
    }

    private void PlaySound(AudioClip clip) { if (audioManager != null && clip != null) audioManager.PlaySFX(clip); }
    private static void SetText(Text target, string value) { if (target != null) target.text = value; }
    public void PlayCoinCollect(Vector3 position) => Burst(coinCollectVFX, position, 10);
    public void PlayPlayerHit(Vector3 position) => Burst(playerHitVFX, position, 16);
    public void PlayFinish(Vector3 position) => Burst(finishVFX, position, 24);

    private static void Burst(ParticleSystem effect, Vector3 position, int count)
    {
        if (effect == null || !effect.gameObject.activeInHierarchy) return;
        if (!effect.isPlaying) effect.Play(false);
        // World-space particles keep previous bursts in place when another coin is collected.
        for (int i = 0; i < count; i++)
        {
            var particle = new ParticleSystem.EmitParams { position = position, velocity = UnityEngine.Random.onUnitSphere * 2.2f };
            effect.Emit(particle, 1);
        }
    }
}
