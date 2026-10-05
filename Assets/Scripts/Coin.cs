using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField, Min(0)] private int scoreValue = 1;
    [SerializeField] private ScoreManager scoreManager;

    private bool collected;

    private void OnTriggerEnter(Collider other)
    {
        if (collected || other.GetComponentInParent<PlayerController>() == null)
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
        scoreManager.AddScore(Mathf.Max(0, scoreValue));
        gameObject.SetActive(false);
    }
}
