using System;
using UnityEngine;

public enum AdType { Rewarded, Interstitial }

public interface IAdProvider
{
    bool IsReady(AdType type);
    // Callbacks must run on Unity's main thread. completed(true) means confirmed
    // successful completion (including reward eligibility for rewarded ads), not closure.
    void Show(AdType type, string placement, Action opened, Action<bool> completed, Action<string> failed);
}

// No real ad SDK. Disabled by default; mock ads require an explicit test completion.
[DisallowMultipleComponent]
public class AdsManager : MonoBehaviour
{
    [SerializeField] private AnalyticsManager analyticsManager;
    [SerializeField] private bool enableMockAds;
    [SerializeField, UnityEngine.Range(0f, 3600f)] private float interstitialCooldown = 60f;
    [SerializeField, UnityEngine.Range(0, 1000)] private int maxInterstitialsPerSession = 10;
    [SerializeField, UnityEngine.Range(5f, 600f)] private float requestTimeoutSeconds = 120f;

    private class Request
    {
        public string id;
        public string placement;
        public AdType type;
        public bool opened;
        public bool finished;
        public bool failed;
        public bool mock;
        public double deadline;
    }

    private IAdProvider provider;
    private Request active;
    private double lastInterstitialOpened = double.NegativeInfinity;
    private int interstitialsShown;
    private bool reportingFailure;
    private bool startingRequest;
    private bool changingProvider;
    private bool checkingReadiness;

    public event Action<AdType, string> AdOpened;
    public event Action<AdType, string> AdCompleted;
    public event Action<AdType, string, string> AdFailed;
    // Request ID and placement; a future gameplay adapter decides the currency amount.
    public event Action<string, string> RewardedAdRewardGranted;

    public void SetProvider(IAdProvider adProvider)
    {
        if (changingProvider) return;
        changingProvider = true;
        try
        {
            if (active != null) Fail(active, "provider_changed");
            provider = adProvider;
        }
        finally { changingProvider = false; }
    }

    public bool IsRewardedAdReady() => IsReady(AdType.Rewarded);
    public bool IsInterstitialReady() => IsReady(AdType.Interstitial);
    public bool ShowRewardedAd(string placement = "default") => Show(AdType.Rewarded, placement);
    public bool ShowInterstitial(string placement = "default") => Show(AdType.Interstitial, placement);

    public bool CompleteMockAd(bool successful = true)
    {
        if (active == null || !active.mock) return false;
        if (!enableMockAds || !isActiveAndEnabled || provider != null)
        {
            Fail(active, "mock_disabled");
            return false;
        }
        Complete(active, successful);
        return true;
    }

    private void Update()
    {
        if (active != null && active.mock && !enableMockAds) Fail(active, "mock_disabled");
        if (active != null && Time.realtimeSinceStartupAsDouble >= active.deadline) Fail(active, "timeout");
    }

    private void OnDisable()
    {
        if (active != null) Fail(active, "manager_disabled");
    }

    private bool IsReady(AdType type)
    {
        if (!isActiveAndEnabled || active != null || changingProvider || checkingReadiness) return false;
        if (type == AdType.Interstitial &&
            (interstitialsShown >= Mathf.Clamp(maxInterstitialsPerSession, 0, 1000) ||
            Time.realtimeSinceStartupAsDouble - lastInterstitialOpened < SafeFloat(interstitialCooldown, 60f, 0f, 3600f))) return false;
        checkingReadiness = true;
        try { return provider != null ? provider.IsReady(type) : enableMockAds; }
        catch (Exception exception) { Debug.LogException(exception, this); return false; }
        finally { checkingReadiness = false; }
    }

    private bool Show(AdType type, string placement)
    {
        if (startingRequest || changingProvider)
        {
            ReportFailure(type, "busy", "request_in_progress");
            return false;
        }
        startingRequest = true;
        try { return StartRequest(type, placement); }
        finally { startingRequest = false; }
    }

    private bool StartRequest(AdType type, string placement)
    {
        if (!ValidPlacement(placement))
        {
            ReportFailure(type, "invalid", "invalid_placement");
            return false;
        }
        if (analyticsManager != null) analyticsManager.TrackAdRequested(type.ToString().ToLowerInvariant(), placement);
        IAdProvider selectedProvider = provider;
        if (!IsReady(type))
        {
            ReportFailure(type, placement, "not_ready_or_disabled");
            return false;
        }
        // Readiness is external code and may disable this manager or replace its provider.
        if (!isActiveAndEnabled || !ReferenceEquals(selectedProvider, provider))
        {
            ReportFailure(type, placement, "provider_or_manager_changed");
            return false;
        }

        var request = new Request
        {
            id = Guid.NewGuid().ToString("N"), placement = placement, type = type,
            mock = provider == null,
            deadline = Time.realtimeSinceStartupAsDouble + SafeFloat(requestTimeoutSeconds, 120f, 5f, 600f)
        };
        active = request;
        try
        {
            if (request.mock) Open(request);
            else provider.Show(type, placement, () => Open(request),
                successful => Complete(request, successful), reason => Fail(request, reason));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            Fail(request, "provider_exception");
        }
        return !request.failed;
    }

    private bool IsCurrent(Request request) => active == request && !request.finished;

    private void Open(Request request)
    {
        if (!IsCurrent(request) || request.opened) return;
        request.opened = true;
        if (request.type == AdType.Interstitial)
        {
            interstitialsShown++;
            lastInterstitialOpened = Time.realtimeSinceStartupAsDouble;
        }
        if (analyticsManager != null) analyticsManager.TrackAdShown(request.type.ToString().ToLowerInvariant(), request.placement);
        if (IsCurrent(request)) Raise(AdOpened, request.type, request.placement);
    }

    private void Complete(Request request, bool successful)
    {
        if (!IsCurrent(request)) return;
        if (!successful || !request.opened)
        {
            Fail(request, "not_completed");
            return;
        }
        // End the request before callbacks. Repeated/late provider callbacks are ignored.
        request.finished = true;
        active = null;
        Raise(AdCompleted, request.type, request.placement);
        if (request.type == AdType.Rewarded)
        {
            if (analyticsManager != null) analyticsManager.TrackAdRewardGranted(request.placement, request.id);
            Raise(RewardedAdRewardGranted, request.id, request.placement);
        }
    }

    private void Fail(Request request, string reason)
    {
        if (!IsCurrent(request)) return;
        request.failed = true;
        request.finished = true;
        active = null;
        ReportFailure(request.type, request.placement, reason);
    }

    private void ReportFailure(AdType type, string placement, string reason)
    {
        if (reportingFailure) return;
        reportingFailure = true;
        try
        {
            string safeReason = string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Replace('\n', ' ').Replace('\r', ' ');
            if (safeReason.Length > 128) safeReason = safeReason.Substring(0, 128);
            if (analyticsManager != null) analyticsManager.TrackAdFailed(type.ToString().ToLowerInvariant(), placement, safeReason);
            var listeners = AdFailed;
            if (listeners == null) return;
            foreach (Action<AdType, string, string> listener in listeners.GetInvocationList())
            {
                try { listener(type, placement, safeReason); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }
        }
        finally { reportingFailure = false; }
    }

    private void Raise<T1, T2>(Action<T1, T2> listeners, T1 first, T2 second)
    {
        if (listeners == null) return;
        foreach (Action<T1, T2> listener in listeners.GetInvocationList())
        {
            try { listener(first, second); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }
    }

    private static bool ValidPlacement(string placement)
    {
        if (string.IsNullOrWhiteSpace(placement) || placement.Length > 64) return false;
        foreach (char character in placement) if (char.IsControl(character)) return false;
        return true;
    }

    private static float SafeFloat(float value, float fallback, float min, float max)
        => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
}
