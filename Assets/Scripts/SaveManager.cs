using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SaveManager : MonoBehaviour
{
    private const string BestScoreKey = "Runner.BestScore";
    private const string HighestUnlockedLevelKey = "Runner.HighestUnlockedLevel";
    private const string MusicEnabledKey = "Runner.MusicEnabled";
    private const string SfxEnabledKey = "Runner.SfxEnabled";
    private const string VibrationEnabledKey = "Runner.VibrationEnabled";
    private bool progressDirty;

    public int BestScore { get; private set; }
    public int HighestUnlockedLevel { get; private set; }
    public bool MusicEnabled { get; private set; } = true;
    public bool SfxEnabled { get; private set; } = true;
    public bool VibrationEnabled { get; private set; } = true;

    private void Awake() => LoadData();

    public void LoadData()
    {
        BestScore = Mathf.Max(0, PlayerPrefs.GetInt(BestScoreKey, 0));
        HighestUnlockedLevel = Mathf.Max(0, PlayerPrefs.GetInt(HighestUnlockedLevelKey, 0));
        MusicEnabled = ReadSetting(MusicEnabledKey);
        SfxEnabled = ReadSetting(SfxEnabledKey);
        VibrationEnabled = ReadSetting(VibrationEnabledKey);
    }

    public void SaveData()
    {
        if (!progressDirty)
        {
            // A flush from an old instance must not resurrect progress after ResetData.
            LoadData();
            try { PlayerPrefs.Save(); }
            catch (Exception exception) { Debug.LogException(exception, this); }
            return;
        }
        // Merge progress only. Preference setters own their individual keys so a
        // stale progress save cannot overwrite newer music/SFX/vibration settings.
        BestScore = Mathf.Max(BestScore, Mathf.Max(0, PlayerPrefs.GetInt(BestScoreKey, 0)));
        HighestUnlockedLevel = Mathf.Max(HighestUnlockedLevel,
            Mathf.Max(0, PlayerPrefs.GetInt(HighestUnlockedLevelKey, 0)));
        try
        {
            PlayerPrefs.SetInt(BestScoreKey, BestScore);
            PlayerPrefs.SetInt(HighestUnlockedLevelKey, HighestUnlockedLevel);
            PlayerPrefs.Save();
            progressDirty = false;
        }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }

    public void UpdateBestScore(int score)
    {
        LoadData();
        if (score <= BestScore) return;
        BestScore = score;
        progressDirty = true;
        SaveData();
    }

    public void UpdateHighestUnlockedLevel(int levelIndex)
    {
        LoadData();
        if (levelIndex <= HighestUnlockedLevel) return;
        HighestUnlockedLevel = levelIndex;
        progressDirty = true;
        SaveData();
    }

    public void SetMusicEnabled(bool enabled)
    {
        MusicEnabled = enabled;
        SaveSetting(MusicEnabledKey, enabled);
    }

    public void SetSfxEnabled(bool enabled)
    {
        SfxEnabled = enabled;
        SaveSetting(SfxEnabledKey, enabled);
    }

    public void SetVibrationEnabled(bool enabled)
    {
        VibrationEnabled = enabled;
        SaveSetting(VibrationEnabledKey, enabled);
    }

    public void ResetData()
    {
        progressDirty = false;
        try
        {
            PlayerPrefs.DeleteKey(BestScoreKey);
            PlayerPrefs.DeleteKey(HighestUnlockedLevelKey);
            PlayerPrefs.DeleteKey(MusicEnabledKey);
            PlayerPrefs.DeleteKey(SfxEnabledKey);
            PlayerPrefs.DeleteKey(VibrationEnabledKey);
            PlayerPrefs.Save();
        }
        catch (Exception exception) { Debug.LogException(exception, this); }
        LoadData();
    }

    private static bool ReadSetting(string key)
    {
        int value = PlayerPrefs.GetInt(key, 1);
        // Missing, wrong-type and invalid flags use the safe enabled default.
        if (value != 0 && value != 1)
        {
            Debug.LogWarning("Invalid preference flag; using enabled default: " + key);
            return true;
        }
        return value == 1;
    }

    private void SaveSetting(string key, bool enabled)
    {
        try
        {
            PlayerPrefs.SetInt(key, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }
}
