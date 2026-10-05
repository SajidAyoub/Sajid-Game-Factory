using System;
using UnityEngine;

[DisallowMultipleComponent]
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
        GameFactoryEvents.Raise(ScoreChanged, currentScore, this);
    }

    public void ResetScore()
    {
        currentScore = 0;
        GameFactoryEvents.Raise(ScoreChanged, currentScore, this);
    }

    public int GetScore()
    {
        return currentScore;
    }
}
