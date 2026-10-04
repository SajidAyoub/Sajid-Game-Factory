using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -8f);
    [SerializeField, Min(0f)] private float smoothSpeed = 5f;
    [SerializeField] private bool lookAtTarget = true;
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        float blend = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);

        if (lookAtTarget)
        {
            Vector3 lookPosition = target.position + lookAtOffset;
            if ((lookPosition - transform.position).sqrMagnitude > 0.0001f)
            {
                transform.LookAt(lookPosition);
            }
        }
    }
}
