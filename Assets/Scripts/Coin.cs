using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Coin : MonoBehaviour
{
    [SerializeField, Min(0)] private int scoreValue = 1;
    [SerializeField] private ScoreManager scoreManager;

    private bool collected;
    // Score value, not currency or coin count. A bridge reports one collected coin.
    public event Action<int> Collected;

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other != null ? other.GetComponentInParent<PlayerController>() : null;
        if (collected || player == null || !player.isActiveAndEnabled)
        {
            return;
        }

        if (scoreManager == null)
        {
            Debug.LogWarning("Assign a ScoreManager before collecting this coin.", this);
            return;
        }

        // Multiple player colliders must not award the same coin twice.
        collected = true;
        int value = Mathf.Max(0, scoreValue);
        Action<int> listeners = Collected;
        scoreManager.AddScore(value);
        gameObject.SetActive(false);
        GameFactoryEvents.Raise(listeners, value, this);
    }
}
