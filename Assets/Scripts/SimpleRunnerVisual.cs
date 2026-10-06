using UnityEngine;

// Replace this visual hierarchy with FBX/Animator later; never moves the Player.
[DisallowMultipleComponent]
public sealed class SimpleRunnerVisual : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform leftArm;
    [SerializeField] private Transform rightArm;
    [SerializeField] private Transform leftLeg;
    [SerializeField] private Transform rightLeg;
    [SerializeField, Range(0f, 60f)] private float swingDegrees = 26f;
    [SerializeField, Range(0.1f, 20f)] private float strideFrequency = 9f;
    private Transform[] limbs;
    private Quaternion[] rest;
    private Vector3 previousPosition;
    private float phase;

    private void Awake()
    {
        limbs = new[] { leftArm, rightArm, leftLeg, rightLeg };
        rest = new Quaternion[limbs.Length];
        for (int i = 0; i < limbs.Length; i++) rest[i] = limbs[i] != null ? limbs[i].localRotation : Quaternion.identity;
        previousPosition = playerController != null ? playerController.transform.position : Vector3.zero;
    }

    private void LateUpdate()
    {
        if (playerController == null || !playerController.isActiveAndEnabled) { RestorePose(); return; }
        Vector3 current = playerController.transform.position;
        bool moving = (current - previousPosition).sqrMagnitude > 0.000001f;
        previousPosition = current;
        if (Time.deltaTime <= 0f) return; // Freeze the pose during pause.
        if (!moving) { RestorePose(); return; }
        float frequency = float.IsNaN(strideFrequency) || float.IsInfinity(strideFrequency) ? 9f : Mathf.Clamp(strideFrequency, 0.1f, 20f);
        float amplitude = float.IsNaN(swingDegrees) || float.IsInfinity(swingDegrees) ? 26f : Mathf.Clamp(swingDegrees, 0f, 60f);
        phase = (phase + Time.deltaTime * frequency) % (Mathf.PI * 2f);
        float angle = Mathf.Sin(phase) * amplitude;
        for (int i = 0; i < limbs.Length; i++)
            if (limbs[i] != null) limbs[i].localRotation = rest[i] * Quaternion.Euler((i == 0 || i == 3) ? angle : -angle, 0f, 0f);
    }

    private void OnDisable() => RestorePose();

    private void RestorePose()
    {
        if (limbs == null) return;
        for (int i = 0; i < limbs.Length; i++) if (limbs[i] != null) limbs[i].localRotation = rest[i];
    }
}
