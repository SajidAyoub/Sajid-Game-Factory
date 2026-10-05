using UnityEngine;

[DisallowMultipleComponent]
public class Obstacle : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    private void OnTriggerEnter(Collider other)
    {
        HandleContact(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null) HandleContact(collision.collider);
    }

    private void HandleContact(Collider other)
    {
        PlayerController player = other != null ? other.GetComponentInParent<PlayerController>() : null;
        if (player == null || !player.isActiveAndEnabled)
        {
            return;
        }

        if (gameManager == null)
        {
            Debug.LogWarning("Assign a GameManager before contacting this obstacle.", this);
            return;
        }

        gameManager.TriggerGameOver();
    }
}
