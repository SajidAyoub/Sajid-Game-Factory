using System;
using UnityEngine;

// Visual-only swap seam. Skin ownership/equipment decisions remain outside this component.
[DisallowMultipleComponent]
public sealed class RunnerPresentationController : MonoBehaviour
{
    [SerializeField] private GameObject generatedVisual;
    [SerializeField] private GameObject customVisual;
    [SerializeField] private bool useCustomVisual;
    [Header("Optional appearance slots; applied only by ApplyMaterials")]
    [SerializeField] private Material headMaterial;
    [SerializeField] private Material bodyMaterial;
    [SerializeField] private Material outfitMaterial;
    [SerializeField] private Material accentMaterial;
    [SerializeField] private Renderer[] headRenderers = new Renderer[0];
    [SerializeField] private Renderer[] bodyRenderers = new Renderer[0];
    [SerializeField] private Renderer[] outfitRenderers = new Renderer[0];
    [SerializeField] private Renderer[] accentRenderers = new Renderer[0];
    public event Action<GameObject> VisualChanged;

    private void Awake()
    {
        if (!SelectVisual(useCustomVisual) && useCustomVisual) SelectVisual(false);
    }

    public bool SelectVisual(bool custom)
    {
        GameObject selected = custom ? customVisual : generatedVisual;
        if (!SafeVisual(selected)) { Debug.LogWarning("Runner visual must be a physics-free child of PlayerVisual.", this); return false; }
        GameObject other = custom ? generatedVisual : customVisual;
        if (other != null && (!SafeVisual(other) || other == selected || other.transform.IsChildOf(selected.transform) || selected.transform.IsChildOf(other.transform)))
        { Debug.LogWarning("Runner visual roots must be independent safe siblings.", this); return false; }
        bool changed = useCustomVisual != custom || !selected.activeSelf;
        if (other != null) other.SetActive(false);
        selected.SetActive(true);
        useCustomVisual = custom;
        if (changed) GameFactoryEvents.Raise(VisualChanged, selected, this);
        return true;
    }

    public GameObject GetCurrentVisual() => useCustomVisual ? customVisual : generatedVisual;

    private bool SafeVisual(GameObject visual)
    {
        return visual != null && visual != gameObject && visual.transform.IsChildOf(transform) &&
            visual.GetComponentsInChildren<Collider>(true).Length == 0 &&
            visual.GetComponentsInChildren<Rigidbody>(true).Length == 0 &&
            visual.GetComponentsInChildren<PlayerController>(true).Length == 0;
    }

    public void ApplyMaterials()
    {
        Apply(headRenderers, headMaterial); Apply(bodyRenderers, bodyMaterial);
        Apply(outfitRenderers, outfitMaterial); Apply(accentRenderers, accentMaterial);
    }

    private void Apply(Renderer[] targets, Material material)
    {
        if (targets == null || material == null) return;
        foreach (Renderer target in targets)
            if (target != null && target.transform.IsChildOf(transform)) target.sharedMaterial = material;
    }
}
