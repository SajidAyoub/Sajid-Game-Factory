using System;
using System.Globalization;
using UnityEngine;

[DisallowMultipleComponent]
public class DailyRewardManager : MonoBehaviour
{
    private const string LastClaimDateKey = "Runner.DailyReward.LastClaimUtcDate";
    private const string LastSeenUtcKey = "Runner.DailyReward.LastSeenUtc";
    private const string DateFormat = "yyyy-MM-dd";
    private const float ClockSaveInterval = 60f;

    [SerializeField] private RewardManager rewardManager;
    [SerializeField, Min(1)] private int dailyRewardAmount = 10;

    private Func<DateTime> utcTimeProvider = () => DateTime.UtcNow;
    private bool claiming;
    private bool? lastAvailability;
    private float nextAvailabilityCheck;
    private float nextClockSave;
    private bool warnedAboutInvalidData;

    public event Action<int> DailyRewardClaimed;
    public event Action<bool> AvailabilityChanged;

    // Local protection only: PlayerPrefs and the device clock can be tampered with.
    // A future trusted time service can inject UTC without changing claim logic.
    public void SetUtcTimeProvider(Func<DateTime> provider)
    {
        if (provider == null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        utcTimeProvider = provider;
        RefreshAvailability();
    }

    private void OnEnable()
    {
        lastAvailability = null;
        RefreshAvailability();
    }

    private void OnDisable()
    {
        // Flush the latest observed time across normal scene changes and shutdown.
        FlushClockSafely();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            FlushClockSafely();
        }
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextAvailabilityCheck)
        {
            RefreshAvailability();
        }
    }

    public bool CanClaimDailyReward()
    {
        return TryPrepareClaim(out _);
    }

    private bool TryPrepareClaim(out DateTime now)
    {
        now = default(DateTime);
        if (claiming || !TryObserveUtcNow(out now) ||
            !TryGetLastClaimDate(out DateTime? lastClaimDate))
        {
            return false;
        }

        return rewardManager != null && dailyRewardAmount > 0 &&
            dailyRewardAmount <= int.MaxValue - rewardManager.GetRewardBalance() &&
            (!lastClaimDate.HasValue || now.Date > lastClaimDate.Value);
    }

    public bool ClaimDailyReward()
    {
        // Capture one sample, including when a claim crosses UTC midnight.
        if (!TryPrepareClaim(out DateTime now))
        {
            return false;
        }

        claiming = true;
        try
        {
            string claimDate = now.ToString(DateFormat, CultureInfo.InvariantCulture);
            bool added = rewardManager.AddRewardsWithPersistence(dailyRewardAmount,
                () => PlayerPrefs.SetString(LastClaimDateKey, claimDate));
            if (!added)
            {
                return false;
            }

            InvokeSafely(DailyRewardClaimed, dailyRewardAmount);
            return true;
        }
        catch (Exception exception)
        {
            // A failed local write is not permission to clear a staged claim date.
            Debug.LogException(exception, this);
            return false;
        }
        finally
        {
            claiming = false;
            RefreshAvailability();
        }
    }

    public TimeSpan GetTimeUntilNextClaim()
    {
        if (claiming || !TryObserveUtcNow(out DateTime now) ||
            !TryGetLastClaimDate(out DateTime? lastClaimDate) ||
            rewardManager == null || dailyRewardAmount <= 0 ||
            dailyRewardAmount > int.MaxValue - rewardManager.GetRewardBalance())
        {
            // Unknown/blocked eligibility must not be displayed as ready.
            return TimeSpan.MaxValue;
        }

        if (!lastClaimDate.HasValue || now.Date > lastClaimDate.Value)
        {
            return TimeSpan.Zero;
        }

        if (lastClaimDate.Value == DateTime.MaxValue.Date)
        {
            return TimeSpan.MaxValue;
        }

        return lastClaimDate.Value.AddDays(1) - now;
    }

    private bool TryObserveUtcNow(out DateTime now)
    {
        now = default(DateTime);
        try { return ObserveUtcNow(out now); }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return false;
        }
    }

    private bool ObserveUtcNow(out DateTime now)
    {
        now = default(DateTime);
        try
        {
            now = utcTimeProvider();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return false;
        }

        if (now.Kind != DateTimeKind.Utc)
        {
            WarnInvalidData();
            return false;
        }

        if (PlayerPrefs.HasKey(LastSeenUtcKey))
        {
            string savedTime = PlayerPrefs.GetString(LastSeenUtcKey, string.Empty);
            if (!DateTime.TryParseExact(savedTime, "O", CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out DateTime lastSeen) ||
                lastSeen.Kind != DateTimeKind.Utc)
            {
                WarnInvalidData();
                return false;
            }

            if (now < lastSeen)
            {
                return false;
            }
        }

        // Never replace the high-water mark with an earlier clock reading.
        PlayerPrefs.SetString(LastSeenUtcKey, now.ToString("O", CultureInfo.InvariantCulture));
        if (Time.unscaledTime >= nextClockSave)
        {
            PlayerPrefs.Save();
            nextClockSave = Time.unscaledTime + ClockSaveInterval;
        }

        return true;
    }

    private bool TryGetLastClaimDate(out DateTime? lastClaimDate)
    {
        lastClaimDate = null;
        if (!PlayerPrefs.HasKey(LastClaimDateKey))
        {
            return true;
        }

        string savedDate = PlayerPrefs.GetString(LastClaimDateKey, string.Empty);
        if (!DateTime.TryParseExact(savedDate, DateFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime parsedDate))
        {
            WarnInvalidData();
            return false;
        }

        lastClaimDate = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);
        return true;
    }

    private void WarnInvalidData()
    {
        if (!warnedAboutInvalidData)
        {
            warnedAboutInvalidData = true;
            Debug.LogWarning("Invalid daily reward date/time data; claims are blocked.", this);
        }
    }

    private void FlushClockSafely()
    {
        try { PlayerPrefs.Save(); }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }

    private void RefreshAvailability()
    {
        nextAvailabilityCheck = Time.unscaledTime + 1f;
        bool available = CanClaimDailyReward();
        if (lastAvailability != available)
        {
            lastAvailability = available;
            InvokeSafely(AvailabilityChanged, available);
        }
    }

    private void InvokeSafely<T>(Action<T> listeners, T value)
    {
        if (listeners == null)
        {
            return;
        }

        foreach (Action<T> listener in listeners.GetInvocationList())
        {
            try
            {
                listener(value);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}
