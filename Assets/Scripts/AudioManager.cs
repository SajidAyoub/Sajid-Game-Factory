using UnityEngine;

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    private bool initialized;
    private bool musicEnabled = true;
    private bool sfxEnabled = true;

    private void Awake()
    {
        Initialize();
        if (musicSource != null && musicSource == sfxSource)
        {
            Debug.LogError("Assign separate Music and SFX AudioSources.", this);
        }
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
            // Load explicitly so initialization does not depend on Awake order.
            saveManager.LoadData();
            musicEnabled = saveManager.MusicEnabled;
            sfxEnabled = saveManager.SfxEnabled;
        }
        else
        {
            Debug.LogWarning("Assign SaveManager to load saved audio preferences.", this);
        }

        ApplyMuteSettings();
    }

    public void PlayMusic(AudioClip clip)
    {
        Initialize();
        if (musicSource == null || clip == null)
        {
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        Initialize();
        if (!sfxEnabled || sfxSource == null || sfxSource == musicSource || clip == null ||
            float.IsNaN(volumeScale) || float.IsInfinity(volumeScale))
        {
            return;
        }

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void SetMusicEnabled(bool enabled)
    {
        Initialize();
        musicEnabled = enabled;
        ApplyMuteSettings();
    }

    public void SetSFXEnabled(bool enabled)
    {
        Initialize();
        sfxEnabled = enabled;
        if (!enabled && sfxSource != null && sfxSource != musicSource)
        {
            sfxSource.Stop();
        }

        ApplyMuteSettings();
    }

    private void ApplyMuteSettings()
    {
        if (musicSource != null)
        {
            // Muting preserves playback position instead of restarting music.
            musicSource.mute = !musicEnabled;
        }

        if (sfxSource != null && sfxSource != musicSource)
        {
            sfxSource.mute = !sfxEnabled;
        }
    }
}
