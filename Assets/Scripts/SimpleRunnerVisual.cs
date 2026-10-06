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
    [Header("Optional visual-only body motion")]
    [SerializeField] private Transform poseRoot;
    [SerializeField, Range(0f, 0.15f)] private float bodyBob = 0.045f;
    [SerializeField, Range(0f, 15f)] private float lateralLean = 6f;
    private Vector3 restPosition;
    private Quaternion restBodyRotation;
    private Transform[] limbs;
    private Quaternion[] rest;
    private Vector3 previousPosition;
    private float phase;

    private void Awake()
    {
        limbs = new[] { leftArm, rightArm, leftLeg, rightLeg };
        rest = new Quaternion[limbs.Length];
        for (int i = 0; i < limbs.Length; i++) rest[i] = limbs[i] != null ? limbs[i].localRotation : Quaternion.identity;
        if (poseRoot != null && poseRoot != transform && poseRoot.IsChildOf(transform))
        { restPosition = poseRoot.localPosition; restBodyRotation = poseRoot.localRotation; }
        else poseRoot = null;
        previousPosition = playerController != null ? playerController.transform.position : Vector3.zero;
    }

    private void OnEnable()
    {
        previousPosition = playerController != null ? playerController.transform.position : Vector3.zero;
    }

    private void LateUpdate()
    {
        if (playerController == null || !playerController.isActiveAndEnabled) { RestorePose(); return; }
        Vector3 current = playerController.transform.position;
        Vector3 delta = current - previousPosition;
        bool moving = delta.sqrMagnitude > 0.000001f;
        previousPosition = current;
        if (Time.deltaTime <= 0f) return; // Freeze the pose during pause.
        if (!moving) { RestorePose(); return; }
        float frequency = float.IsNaN(strideFrequency) || float.IsInfinity(strideFrequency) ? 9f : Mathf.Clamp(strideFrequency, 0.1f, 20f);
        float amplitude = float.IsNaN(swingDegrees) || float.IsInfinity(swingDegrees) ? 26f : Mathf.Clamp(swingDegrees, 0f, 60f);
        phase = (phase + Time.deltaTime * frequency) % (Mathf.PI * 2f);
        float angle = Mathf.Sin(phase) * amplitude;
        if (poseRoot != null)
        {
            float bob = float.IsNaN(bodyBob) || float.IsInfinity(bodyBob) ? 0f : Mathf.Clamp(bodyBob, 0f, 0.15f);
            float lean = float.IsNaN(lateralLean) || float.IsInfinity(lateralLean) ? 0f : Mathf.Clamp(lateralLean, 0f, 15f);
            poseRoot.localPosition = restPosition + Vector3.up * (Mathf.Abs(Mathf.Sin(phase)) * bob);
            poseRoot.localRotation = restBodyRotation * Quaternion.Euler(2f, 0f, -Mathf.Clamp(delta.x / Time.deltaTime, -1f, 1f) * lean);
        }
        for (int i = 0; i < limbs.Length; i++)
            if (limbs[i] != null) limbs[i].localRotation = rest[i] * Quaternion.Euler((i == 0 || i == 3) ? angle : -angle, 0f, 0f);
    }

    private void OnDisable() => RestorePose();

    private void RestorePose()
    {
        if (poseRoot != null) { poseRoot.localPosition = restPosition; poseRoot.localRotation = restBodyRotation; }
        if (limbs == null) return;
        for (int i = 0; i < limbs.Length; i++) if (limbs[i] != null) limbs[i].localRotation = rest[i];
    }
}
