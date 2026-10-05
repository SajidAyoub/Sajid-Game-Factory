using System;
using UnityEngine;

public class RewardManager : MonoBehaviour
{
    private const string RewardBalanceKey = "Runner.RewardBalance";

    public event Action<int> RewardBalanceChanged;

    public int GetRewardBalance()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(RewardBalanceKey, 0));
    }

    public bool AddRewards(int amount)
    {
        int balance = GetRewardBalance();
        if (amount <= 0 || amount > int.MaxValue - balance)
        {
            return false;
        }

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
        RewardBalanceChanged?.Invoke(balance);
    }
}
