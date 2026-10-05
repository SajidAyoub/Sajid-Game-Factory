using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    [SerializeField] private Text bestScoreText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text coinCountText;

    // Optional Inspector hooks support TMP text without referencing its assembly.
    [SerializeField] private UnityEvent<string> scoreTextUpdated = new UnityEvent<string>();
    [SerializeField] private UnityEvent<string> bestScoreTextUpdated = new UnityEvent<string>();
    [SerializeField] private UnityEvent<string> levelTextUpdated = new UnityEvent<string>();
    [SerializeField] private UnityEvent<string> coinCountTextUpdated = new UnityEvent<string>();

    public void UpdateScore(int score)
    {
        UpdateText(scoreText, scoreTextUpdated, score);
    }

    public void UpdateBestScore(int bestScore)
    {
        UpdateText(bestScoreText, bestScoreTextUpdated, bestScore);
    }

    public void UpdateLevel(int level)
    {
        UpdateText(levelText, levelTextUpdated, level);
    }

    public void UpdateCoinCount(int count)
    {
        UpdateText(coinCountText, coinCountTextUpdated, count);
    }

    private static void UpdateText(Text text, UnityEvent<string> updated, int value)
    {
        string displayValue = value.ToString();
        if (text != null)
        {
            text.text = displayValue;
        }

        updated?.Invoke(displayValue);
    }
}
