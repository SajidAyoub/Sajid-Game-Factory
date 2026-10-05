using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;

public interface IAnalyticsProvider
{
    void TrackEvent(string eventName, IReadOnlyDictionary<string, object> parameters);
}

// No SDK or network provider is installed. Future adapters must use Unity's main thread.
public class AnalyticsManager : MonoBehaviour
{
    [SerializeField] private bool debugLogging;
    private IAnalyticsProvider provider;
    private int reportingDepth;
    public event Action<string, IReadOnlyDictionary<string, object>> EventTracked;

    public void SetProvider(IAnalyticsProvider analyticsProvider) => provider = analyticsProvider;

    public void TrackGameStarted() => Report("game_started");
    public void TrackGameOver(int score) => Report("game_over", "score", Math.Max(0, score));
    public void TrackLevelStarted(int level) => Report("level_started", "level_index", Math.Max(0, level));
    public void TrackLevelCompleted(int level) => Report("level_completed", "level_index", Math.Max(0, level));
    public void TrackCoinCollected(int value = 1) => Report("coin_collected", "value", Math.Max(0, value));
    public void TrackScoreReached(int score) => Report("score_reached", "score", Math.Max(0, score));
    public void TrackRewardClaimed(string source, int amount) => Report("reward_claimed", "source", source, "amount", Math.Max(0, amount));
    public void TrackPurchase(string itemId, int price) => Report("purchase", "item_id", itemId, "price", Math.Max(0, price));
    public void TrackSkinSelected(string skinId) => Report("skin_selected", "skin_id", skinId);
    public void TrackMissionCompleted(string missionId) => Report("mission_completed", "mission_id", missionId);
    public void TrackAdRequested(string type, string placement) => Report("ad_requested", "ad_type", type, "placement", placement);
    public void TrackAdShown(string type, string placement) => Report("ad_shown", "ad_type", type, "placement", placement);
    public void TrackAdRewardGranted(string placement, string requestId) => Report("ad_reward_granted", "placement", placement, "request_id", requestId);
    public void TrackAdFailed(string type, string placement, string reason) => Report("ad_failed", "ad_type", type, "placement", placement, "reason", reason);

    public void TrackEvent(string eventName, IReadOnlyDictionary<string, object> parameters = null)
    {
        if (reportingDepth >= 8) return;
        reportingDepth++;
        // Isolate validation, logging and adapters so analytics cannot break gameplay.
        try
        {
            if (!IsValidName(eventName)) return;
            var clean = new Dictionary<string, object>();
            if (parameters != null)
            {
                foreach (var pair in parameters)
                {
                    if (clean.Count >= 32) break;
                    if (IsValidName(pair.Key) && TrySanitize(pair.Value, out object value)) clean[pair.Key] = value;
                }
            }
            var snapshot = new ReadOnlyDictionary<string, object>(clean);
            if (debugLogging)
            {
                var message = new StringBuilder("[Analytics] " + eventName);
                foreach (var pair in snapshot) message.Append(" ").Append(pair.Key).Append("=").Append(pair.Value);
                Debug.Log(message.ToString(), this);
            }
            try { provider?.TrackEvent(eventName, snapshot); }
            catch (Exception exception) { Debug.LogException(exception, this); }

            var listeners = EventTracked;
            if (listeners == null) return;
            foreach (Action<string, IReadOnlyDictionary<string, object>> listener in listeners.GetInvocationList())
            {
                try { listener(eventName, snapshot); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }
        }
        catch (Exception exception) { Debug.LogException(exception, this); }
        finally { reportingDepth--; }
    }

    private void Report(string eventName, params object[] fields)
    {
        var parameters = new Dictionary<string, object>();
        for (int index = 0; index + 1 < fields.Length; index += 2)
            parameters[(string)fields[index]] = fields[index + 1];
        TrackEvent(eventName, parameters);
    }

    private static bool IsValidName(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length > 64 || name[0] < 'a' || name[0] > 'z') return false;
        foreach (char character in name)
            if (!((character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') || character == '_')) return false;
        return true;
    }

    private static bool TrySanitize(object input, out object value)
    {
        value = null;
        if (input is string text)
        {
            text = text.Trim();
            var clean = new StringBuilder();
            foreach (char character in text)
            {
                if (!char.IsControl(character)) clean.Append(character);
                if (clean.Length == 256) break;
            }
            value = clean.ToString();
            return clean.Length > 0;
        }
        if (input is float number && (float.IsNaN(number) || float.IsInfinity(number))) return false;
        if (input is double real && (double.IsNaN(real) || double.IsInfinity(real))) return false;
        if (input is bool || input is int || input is long || input is float || input is double)
        {
            value = input;
            return true;
        }
        return false;
    }
}
