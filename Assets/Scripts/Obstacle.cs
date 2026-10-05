using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    private void OnTriggerEnter(Collider other)
    {
        HandleContact(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleContact(collision.collider);
    }

    private void HandleContact(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
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
