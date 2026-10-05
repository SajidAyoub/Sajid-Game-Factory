using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public interface IRemoteConfigProvider
{
    // Future adapters must return typed values and dispatch callbacks on Unity's main thread.
    void Fetch(Action<IReadOnlyDictionary<string, object>> completed, Action<string> failed);
}

// Local defaults only; no remote service, SDK or network implementation is installed.
// Overrides are limited to this known schema and never applied to gameplay automatically.
public class RemoteConfigManager : MonoBehaviour
{
    public const string ForwardSpeedKey = "player_forward_speed";
    public const string HorizontalSpeedKey = "horizontal_speed";
    public const string DailyRewardKey = "daily_reward_amount";
    public const string InterstitialFrequencyKey = "interstitial_frequency";
    public const string InterstitialCooldownKey = "interstitial_cooldown";
    public const string RewardedAdRewardKey = "rewarded_ad_reward_amount";
    public const string MusicEnabledKey = "default_music_enabled";
    public const string SfxEnabledKey = "default_sfx_enabled";
    public const string DifficultyKey = "difficulty_multiplier";
    public const string ConfigLabelKey = "config_label";

    [SerializeField, Range(0.1f, 100f)] private float forwardSpeed = 8f;
    [SerializeField, Range(0.1f, 100f)] private float horizontalSpeed = 5f;
    [SerializeField, Range(1, 1000000)] private int dailyRewardAmount = 10;
    [SerializeField, Range(1, 100)] private int interstitialFrequency = 3;
    [SerializeField, Range(0f, 3600f)] private float interstitialCooldown = 60f;
    [SerializeField, Range(1, 1000000)] private int rewardedAdRewardAmount = 10;
    [SerializeField] private bool defaultMusicEnabled = true;
    [SerializeField] private bool defaultSfxEnabled = true;
    [SerializeField, Range(0.1f, 10f)] private float difficultyMultiplier = 1f;
    [SerializeField] private string configLabel = "local";

    private Dictionary<string, object> values;
    private IRemoteConfigProvider provider;
    private string latestRequest;
    private bool notifyingUpdate;
    private bool startingFetch;
    public event Action ConfigUpdated;

    private void Awake() => EnsureInitialized();
    private void OnDisable() => latestRequest = null;

    public void SetProvider(IRemoteConfigProvider configProvider)
    {
        provider = configProvider;
        latestRequest = null; // Discard callbacks from a replaced provider.
    }

    public bool FetchConfiguration()
    {
        EnsureInitialized();
        if (provider == null || startingFetch) return false; // Defaults remain usable.
        string request = Guid.NewGuid().ToString("N");
        latestRequest = request;
        bool finished = false;
        startingFetch = true;
        try
        {
            provider.Fetch(overrides =>
            {
                if (finished || latestRequest != request) return;
                finished = true;
                ApplyOverrides(overrides);
            }, reason =>
            {
                if (finished || latestRequest != request) return;
                finished = true;
                // Failure keeps the last validated configuration; no gameplay dependency.
                Debug.LogWarning("Config provider failed; keeping validated local values.", this);
            });
            return true;
        }
        catch (Exception exception)
        {
            finished = true;
            Debug.LogException(exception, this);
            return false;
        }
        finally { startingFetch = false; }
    }

    public int GetInt(string key, int fallback = 0)
    {
        EnsureInitialized();
        return key != null && values.TryGetValue(key, out object value) && value is int number ? number : fallback;
    }

    public float GetFloat(string key, float fallback = 0f)
    {
        EnsureInitialized();
        return key != null && values.TryGetValue(key, out object value) && value is float number ? number : fallback;
    }

    public bool GetBool(string key, bool fallback = false)
    {
        EnsureInitialized();
        return key != null && values.TryGetValue(key, out object value) && value is bool flag ? flag : fallback;
    }

    public string GetString(string key, string fallback = "")
    {
        EnsureInitialized();
        return key != null && values.TryGetValue(key, out object value) && value is string text ? text : fallback ?? string.Empty;
    }

    public IReadOnlyDictionary<string, object> GetSnapshot()
    {
        EnsureInitialized();
        return new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(values));
    }

    public bool ApplyOverrides(IReadOnlyDictionary<string, object> overrides)
    {
        EnsureInitialized();
        if (overrides == null) return false;
        try
        {
            var updated = new Dictionary<string, object>(values);
            bool changed = false;
            int count = 0;
            foreach (var pair in overrides)
            {
                if (++count > 10 || !TryValidate(pair.Key, pair.Value, out object valid))
                {
                    Debug.LogWarning("Rejected invalid config batch; keeping validated values.", this);
                    return false; // Reject the whole batch, avoiding partial configuration.
                }
                if (!Equals(updated[pair.Key], valid)) changed = true;
                updated[pair.Key] = valid;
            }
            if (changed)
            {
                values = updated;
                NotifyUpdated();
            }
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return false;
        }
    }

    public void ResetToDefaults()
    {
        values = BuildDefaults();
        NotifyUpdated();
    }

    private void EnsureInitialized()
    {
        if (values == null) values = BuildDefaults();
    }

    private Dictionary<string, object> BuildDefaults()
    {
        return new Dictionary<string, object>
        {
            { ForwardSpeedKey, SafeFloat(forwardSpeed, 8f, 0.1f, 100f) },
            { HorizontalSpeedKey, SafeFloat(horizontalSpeed, 5f, 0.1f, 100f) },
            { DailyRewardKey, Mathf.Clamp(dailyRewardAmount, 1, 1000000) },
            { InterstitialFrequencyKey, Mathf.Clamp(interstitialFrequency, 1, 100) },
            { InterstitialCooldownKey, SafeFloat(interstitialCooldown, 60f, 0f, 3600f) },
            { RewardedAdRewardKey, Mathf.Clamp(rewardedAdRewardAmount, 1, 1000000) },
            { MusicEnabledKey, defaultMusicEnabled },
            { SfxEnabledKey, defaultSfxEnabled },
            { DifficultyKey, SafeFloat(difficultyMultiplier, 1f, 0.1f, 10f) },
            { ConfigLabelKey, ValidString(configLabel) ? configLabel : "local" }
        };
    }

    private static bool TryValidate(string key, object input, out object value)
    {
        value = null;
        switch (key)
        {
            case ForwardSpeedKey:
            case HorizontalSpeedKey: return ValidateFloat(input, 0.1f, 100f, out value);
            case DifficultyKey: return ValidateFloat(input, 0.1f, 10f, out value);
            case InterstitialCooldownKey: return ValidateFloat(input, 0f, 3600f, out value);
            case DailyRewardKey:
            case RewardedAdRewardKey: return ValidateInt(input, 1, 1000000, out value);
            case InterstitialFrequencyKey: return ValidateInt(input, 1, 100, out value);
            case MusicEnabledKey:
            case SfxEnabledKey:
                if (!(input is bool)) return false;
                value = input;
                return true;
            case ConfigLabelKey:
                if (!(input is string text) || !ValidString(text)) return false;
                value = text;
                return true;
            default: return false;
        }
    }

    private static bool ValidateInt(object input, int min, int max, out object value)
    {
        value = null;
        if (!(input is int) && !(input is long)) return false;
        long number = input is int integer ? integer : (long)input;
        if (number < min || number > max) return false;
        value = (int)number;
        return true;
    }

    private static bool ValidateFloat(object input, float min, float max, out object value)
    {
        value = null;
        double number;
        if (input is float single) number = single;
        else if (input is double real) number = real;
        else if (input is int integer) number = integer;
        else return false;
        if (double.IsNaN(number) || double.IsInfinity(number)) return false;
        float normalized = (float)number;
        if (float.IsInfinity(normalized) || normalized < min || normalized > max) return false;
        value = normalized;
        return true;
    }

    private static float SafeFloat(float value, float fallback, float min, float max)
        => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);

    private static bool ValidString(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128) return false;
        foreach (char character in value) if (char.IsControl(character)) return false;
        return true;
    }

    private void NotifyUpdated()
    {
        if (notifyingUpdate) return;
        notifyingUpdate = true;
        try
        {
            var listeners = ConfigUpdated;
            if (listeners == null) return;
            foreach (Action listener in listeners.GetInvocationList())
            {
                try { listener(); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }
        }
        finally { notifyingUpdate = false; }
    }
}
