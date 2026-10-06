using UnityEngine;

[DisallowMultipleComponent]
public sealed class CoinVisualAnimator : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField, Min(0f)] private float bobHeight = 0.15f;
    [SerializeField, Min(0f)] private float bobFrequency = 2.5f;
    private Vector3 restPosition;
    private Quaternion restRotation;
    private float phase;
    private float angle;

    private void OnEnable()
    {
        if (visual == null || visual == transform || !visual.IsChildOf(transform)) return;
        restPosition = visual.localPosition;
        restRotation = visual.localRotation;
    }

    private void Update()
    {
        // Only animate the child mesh; the Coin root/trigger never moves.
        if (visual == null || visual == transform || !visual.IsChildOf(transform)) return;
        float speed = Finite(rotationSpeed, 100f);
        phase = (phase + Time.unscaledDeltaTime * Mathf.Clamp(Finite(bobFrequency, 2.5f), 0f, 20f)) % (Mathf.PI * 2f);
        angle = (angle + Time.unscaledDeltaTime * Mathf.Clamp(speed, -720f, 720f)) % 360f;
        visual.localRotation = restRotation * Quaternion.Euler(0f, angle, 0f);
        visual.localPosition = restPosition + Vector3.up * (Mathf.Sin(phase) * Mathf.Clamp(Finite(bobHeight, 0.15f), 0f, 5f));
    }

    private void OnDisable()
    {
        if (visual == null || visual == transform || !visual.IsChildOf(transform)) return;
        visual.localPosition = restPosition;
        visual.localRotation = restRotation;
    }

    private static float Finite(float value, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
}
