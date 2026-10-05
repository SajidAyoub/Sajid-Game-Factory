using System;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private AudioManager audioManager;

    private bool initialized;

    public bool MusicEnabled { get; private set; } = true;
    public bool SFXEnabled { get; private set; } = true;
    public bool VibrationEnabled { get; private set; } = true;

    public event Action<bool> MusicEnabledChanged;
    public event Action<bool> SFXEnabledChanged;
    public event Action<bool> VibrationEnabledChanged;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        if (saveManager != null)
        {
            saveManager.LoadData();
            MusicEnabled = saveManager.MusicEnabled;
            SFXEnabled = saveManager.SfxEnabled;
            VibrationEnabled = saveManager.VibrationEnabled;
        }
        else
        {
            Debug.LogWarning("Assign SaveManager to persist settings.", this);
        }

        if (audioManager != null)
        {
            audioManager.SetMusicEnabled(MusicEnabled);
            audioManager.SetSFXEnabled(SFXEnabled);
        }
    }

    public void SetMusicEnabled(bool enabled)
    {
        Initialize();
        MusicEnabled = enabled;
        if (saveManager != null)
        {
            saveManager.SetMusicEnabled(enabled);
        }

        if (audioManager != null)
        {
            audioManager.SetMusicEnabled(enabled);
        }

        MusicEnabledChanged?.Invoke(enabled);
    }

    public void SetSFXEnabled(bool enabled)
    {
        Initialize();
        SFXEnabled = enabled;
        if (saveManager != null)
        {
            saveManager.SetSfxEnabled(enabled);
        }

        if (audioManager != null)
        {
            audioManager.SetSFXEnabled(enabled);
        }

        SFXEnabledChanged?.Invoke(enabled);
    }

    public void SetVibrationEnabled(bool enabled)
    {
        Initialize();
        VibrationEnabled = enabled;
        if (saveManager != null)
        {
            saveManager.SetVibrationEnabled(enabled);
        }

        VibrationEnabledChanged?.Invoke(enabled);
    }

    public void ToggleMusic()
    {
        Initialize();
        SetMusicEnabled(!MusicEnabled);
    }

    public void ToggleSFX()
    {
        Initialize();
        SetSFXEnabled(!SFXEnabled);
    }

    public void ToggleVibration()
    {
        Initialize();
        SetVibrationEnabled(!VibrationEnabled);
    }
}
