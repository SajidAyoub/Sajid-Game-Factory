using UnityEngine;

// Ownership tag for Editor-created presentation only. No gameplay or persistence.
[DisallowMultipleComponent, AddComponentMenu("")]
public sealed class Level1GeneratedObject : MonoBehaviour
{
    [SerializeField] private string generationId;
    public string GenerationId => generationId;
}
