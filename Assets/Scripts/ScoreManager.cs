using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private int currentScore;

    public event Action<int> ScoreChanged;

    public void AddScore(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        // Saturate rather than overflow during long runs.
        currentScore += Math.Min(amount, int.MaxValue - currentScore);
        ScoreChanged?.Invoke(currentScore);
    }

    public void ResetScore()
    {
        currentScore = 0;
        ScoreChanged?.Invoke(currentScore);
    }

    public int GetScore()
    {
        return currentScore;
    }
}
