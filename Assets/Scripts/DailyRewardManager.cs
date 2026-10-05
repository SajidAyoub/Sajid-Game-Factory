using System;
using System.Globalization;
using UnityEngine;

public class DailyRewardManager : MonoBehaviour
{
    private const string LastClaimDateKey = "Runner.DailyReward.LastClaimUtcDate";
    private const string DateFormat = "yyyy-MM-dd";

    [SerializeField] private RewardManager rewardManager;
    [SerializeField, Min(1)] private int dailyRewardAmount = 10;

    private bool claiming;
    private bool? lastAvailability;
    private float nextAvailabilityCheck;

    public event Action<int> DailyRewardClaimed;
    public event Action<bool> AvailabilityChanged;

    private void OnEnable()
    {
        lastAvailability = null;
        RefreshAvailability();
    }

    private void Update()
    {
        // Unscaled polling keeps availability current while gameplay is paused.
        if (Time.unscaledTime >= nextAvailabilityCheck)
        {
            RefreshAvailability();
        }
    }

    public bool CanClaimDailyReward()
    {
        if (claiming || !TryGetLastClaimDate(out DateTime? lastClaimDate))
        {
            return false;
        }

        // A future saved date also blocks claims if the device clock moves backward.
        return !lastClaimDate.HasValue || DateTime.UtcNow.Date > lastClaimDate.Value;
    }

    public bool ClaimDailyReward()
    {
        if (!CanClaimDailyReward() || rewardManager == null || dailyRewardAmount <= 0)
        {
            return false;
        }

        if (dailyRewardAmount > int.MaxValue - rewardManager.GetRewardBalance())
        {
            return false;
        }

        string previousDate = PlayerPrefs.GetString(LastClaimDateKey, string.Empty);
        claiming = true;
        try
        {
            // Persist before reward events so reentrant calls cannot claim twice.
            PlayerPrefs.SetString(LastClaimDateKey,
                DateTime.UtcNow.ToString(DateFormat, CultureInfo.InvariantCulture));
            PlayerPrefs.Save();

            if (!rewardManager.AddRewards(dailyRewardAmount))
            {
                RestoreLastClaimDate(previousDate);
                return false;
            }
        }
        finally
        {
            claiming = false;
            RefreshAvailability();
        }

        DailyRewardClaimed?.Invoke(dailyRewardAmount);
        return true;
    }

    public TimeSpan GetTimeUntilNextClaim()
    {
        if (!TryGetLastClaimDate(out DateTime? lastClaimDate))
        {
            // Invalid saved data must not grant another claim.
            return TimeSpan.MaxValue;
        }

        DateTime now = DateTime.UtcNow;
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

    private static bool TryGetLastClaimDate(out DateTime? lastClaimDate)
    {
        string savedDate = PlayerPrefs.GetString(LastClaimDateKey, string.Empty);
        lastClaimDate = null;
        if (string.IsNullOrEmpty(savedDate))
        {
            return true;
        }

        if (!DateTime.TryParseExact(savedDate, DateFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime parsedDate))
        {
            return false;
        }

        lastClaimDate = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);
        return true;
    }

    private static void RestoreLastClaimDate(string previousDate)
    {
        if (string.IsNullOrEmpty(previousDate))
        {
            PlayerPrefs.DeleteKey(LastClaimDateKey);
        }
        else
        {
            PlayerPrefs.SetString(LastClaimDateKey, previousDate);
        }

        PlayerPrefs.Save();
    }

    private void RefreshAvailability()
    {
        nextAvailabilityCheck = Time.unscaledTime + 1f;
        bool available = CanClaimDailyReward();
        if (lastAvailability != available)
        {
            lastAvailability = available;
            AvailabilityChanged?.Invoke(available);
        }
    }
}
