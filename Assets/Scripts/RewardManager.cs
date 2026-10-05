using System;
using UnityEngine;

[DisallowMultipleComponent]
public class RewardManager : MonoBehaviour
{
    private const string RewardBalanceKey = "Runner.RewardBalance";
    private bool warnedAboutInvalidBalance;

    public event Action<int> RewardBalanceChanged;

    public int GetRewardBalance()
    {
        if (!PlayerPrefs.HasKey(RewardBalanceKey))
        {
            return 0;
        }

        int balance = PlayerPrefs.GetInt(RewardBalanceKey, -1);
        if (balance < 0)
        {
            if (!warnedAboutInvalidBalance)
            {
                warnedAboutInvalidBalance = true;
                Debug.LogWarning("Invalid saved reward balance; treating it as zero.", this);
            }

            return 0;
        }

        return balance;
    }

    public bool AddRewards(int amount)
    {
        return AddRewardsWithPersistence(amount, null);
    }

    // Stage related PlayerPrefs writes before the balance's single save and events.
    // This reduces partial writes but PlayerPrefs is not a transactional database.
    internal bool AddRewardsWithPersistence(int amount, Action stageAdditionalData)
    {
        int balance = GetRewardBalance();
        if (amount <= 0 || amount > int.MaxValue - balance)
        {
            return false;
        }

        stageAdditionalData?.Invoke();
        SetBalance(balance + amount);
        return true;
    }

    public bool SpendRewards(int amount)
    {
        int balance = GetRewardBalance();
        if (amount <= 0 || amount > balance)
        {
            return false;
        }

        SetBalance(balance - amount);
        return true;
    }

    public void ResetRewards()
    {
        SetBalance(0);
    }

    private void SetBalance(int balance)
    {
        PlayerPrefs.SetInt(RewardBalanceKey, balance);
        PlayerPrefs.Save();
        Action<int> listeners = RewardBalanceChanged;
        if (listeners == null)
        {
            return;
        }

        foreach (Action<int> listener in listeners.GetInvocationList())
        {
            try
            {
                listener(balance);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}
