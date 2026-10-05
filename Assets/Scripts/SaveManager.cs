using UnityEngine;

public class SaveManager : MonoBehaviour
{
    private const string BestScoreKey = "Runner.BestScore";
    private const string HighestUnlockedLevelKey = "Runner.HighestUnlockedLevel";
    private const string MusicEnabledKey = "Runner.MusicEnabled";
    private const string SfxEnabledKey = "Runner.SfxEnabled";
    private const string VibrationEnabledKey = "Runner.VibrationEnabled";

    public int BestScore { get; private set; }
    // Level indices are zero-based, matching LevelManager and SceneManager.
    public int HighestUnlockedLevel { get; private set; }
    public bool MusicEnabled { get; private set; } = true;
    public bool SfxEnabled { get; private set; } = true;
    public bool VibrationEnabled { get; private set; } = true;

    private void Awake()
    {
        LoadData();
    }

    public void LoadData()
    {
        BestScore = Mathf.Max(0, PlayerPrefs.GetInt(BestScoreKey, 0));
        HighestUnlockedLevel = Mathf.Max(0, PlayerPrefs.GetInt(HighestUnlockedLevelKey, 0));
        MusicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) != 0;
        SfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) != 0;
        VibrationEnabled = PlayerPrefs.GetInt(VibrationEnabledKey, 1) != 0;
    }

    public void SaveData()
    {
        // Preserve progress even if another component saved newer values.
        BestScore = Mathf.Max(BestScore, PlayerPrefs.GetInt(BestScoreKey, 0));
        HighestUnlockedLevel = Mathf.Max(
            HighestUnlockedLevel, PlayerPrefs.GetInt(HighestUnlockedLevelKey, 0));
        PlayerPrefs.SetInt(BestScoreKey, BestScore);
        PlayerPrefs.SetInt(HighestUnlockedLevelKey, HighestUnlockedLevel);
        PlayerPrefs.SetInt(MusicEnabledKey, MusicEnabled ? 1 : 0);
        PlayerPrefs.SetInt(SfxEnabledKey, SfxEnabled ? 1 : 0);
        PlayerPrefs.SetInt(VibrationEnabledKey, VibrationEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void UpdateBestScore(int score)
    {
        if (score > BestScore)
        {
            BestScore = score;
            SaveData();
        }
    }

    public void UpdateHighestUnlockedLevel(int levelIndex)
    {
        if (levelIndex > HighestUnlockedLevel)
        {
            HighestUnlockedLevel = levelIndex;
            SaveData();
        }
    }

    public void SetMusicEnabled(bool enabled)
    {
        MusicEnabled = enabled;
        SaveData();
    }

    public void SetSfxEnabled(bool enabled)
    {
        SfxEnabled = enabled;
        SaveData();
    }

    public void SetVibrationEnabled(bool enabled)
    {
        VibrationEnabled = enabled;
        SaveData();
    }

    public void ResetData()
    {
        // Reset only this system's keys, leaving other PlayerPrefs intact.
        PlayerPrefs.DeleteKey(BestScoreKey);
        PlayerPrefs.DeleteKey(HighestUnlockedLevelKey);
        PlayerPrefs.DeleteKey(MusicEnabledKey);
        PlayerPrefs.DeleteKey(SfxEnabledKey);
        PlayerPrefs.DeleteKey(VibrationEnabledKey);
        PlayerPrefs.Save();
        LoadData();
    }
}
