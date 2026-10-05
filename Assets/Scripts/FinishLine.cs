using UnityEngine;

[DisallowMultipleComponent]
public class FinishLine : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;

    private bool completed;

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other != null ? other.GetComponentInParent<PlayerController>() : null;
        if (completed || player == null || !player.isActiveAndEnabled)
        {
            return;
        }

        if (levelManager == null)
        {
            Debug.LogWarning("Assign a LevelManager to this finish line.", this);
            return;
        }

        // Guard before notifying listeners, including players with multiple colliders.
        completed = true;
        if (!levelManager.CompleteCurrentLevel())
        {
            completed = false;
        }
    }
}
