using System;
using UnityEditor;
using UnityEngine;

// Presentation-only extension. Generated assets and scene edits happen on menu invocation.
public static partial class GameFactorySetup
{
    private const string FantasyFolder = GeneratedFolder + "/Fantasy";

    [MenuItem(MenuRoot + "Upgrade / Repair Level 1 Fantasy Presentation")]
    public static void UpgradeLevel1FantasyPresentation() => BuildOrRepairLevel1Master();

    private static Material FantasyMaterial(Level1Build build, string name, Color color, float metallic = 0f, float emission = 0f)
    {
        string path = FantasyFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("Unexpected fantasy material asset: " + path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit required for the fantasy presentation.");
            material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", metallic > 0f ? 0.65f : 0.32f);
            if (emission > 0f && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }
            AssetDatabase.CreateAsset(material, path);
            Debug.Log("Fantasy: created " + path + "; asset creation is outside scene Undo.");
        }
        else Debug.Log("Fantasy: reused material " + path + "; custom edits preserved.");
        build.Materials[name] = material;
        return material;
    }

    private static void FantasyPalette(Level1Build build)
    {
        EnsureAssetFolder(FantasyFolder);
        FantasyMaterial(build, "FantasyStone", new Color(0.28f, 0.39f, 0.48f));
        FantasyMaterial(build, "FantasyRoad", new Color(0.065f, 0.095f, 0.16f));
        FantasyMaterial(build, "FantasyCyan", new Color(0.12f, 0.86f, 1f), 0.15f, 1.4f);
        FantasyMaterial(build, "FantasyGold", new Color(1f, 0.68f, 0.1f), 0.72f, 0.25f);
        FantasyMaterial(build, "FantasyCliff", new Color(0.15f, 0.24f, 0.32f));
        FantasyMaterial(build, "FantasyWater", new Color(0.17f, 0.58f, 0.82f), 0.1f, 0.18f);
        FantasyMaterial(build, "FantasyFoam", new Color(0.55f, 0.92f, 1f), 0f, 0.6f);
    }

    private static GameObject FantasyBox(Level1Build build, Transform parent, string name, Vector3 position, Vector3 scale, string material, Vector3 euler = default(Vector3))
        => Primitive(build, parent, name, PrimitiveType.Cube, position, scale, build.Materials[material], euler);

    private static void RetireOwnedVisual(GameObject item, string expectedId)
    {
        if (item == null || !item.activeSelf) return;
        Level1GeneratedObject marker = item.GetComponent<Level1GeneratedObject>();
        if (marker == null || marker.GenerationId != expectedId)
            throw new InvalidOperationException("Cannot retire unowned presentation: " + item.name);
        // Never deactivate an object containing gameplay or custom behaviours/physics.
        foreach (Component component in item.GetComponentsInChildren<Component>(true))
            if (!(component is Transform || component is MeshFilter || component is MeshRenderer ||
                  component is Level1GeneratedObject || component is SimpleRunnerVisual))
                throw new InvalidOperationException("Custom component in old presentation; preserve and migrate manually: " + item.name);
        Undo.RecordObject(item, "Retire legacy generated presentation");
        item.SetActive(false);
        RecordPrefabChange(item);
        Debug.Log("Fantasy: retired owned " + item.name + " without deleting it; Undo restores it.", item);
    }

    private static void ReplaceKnownMaterial(Renderer renderer, string previousName, Material replacement)
    {
        if (renderer == null || renderer.sharedMaterial == replacement) return;
        string path = renderer.sharedMaterial != null ? AssetDatabase.GetAssetPath(renderer.sharedMaterial) : string.Empty;
        if (path != GeneratedFolder + "/" + previousName + ".mat")
        { Debug.Log("Fantasy: custom material preserved on " + renderer.name + ".", renderer); return; }
        Undo.RecordObject(renderer, "Upgrade generated presentation material");
        renderer.sharedMaterial = replacement;
        RecordPrefabChange(renderer);
    }

    private static void FantasyEnvironmentPass(Level1Build build)
    {
        FantasyPalette(build);
        Transform environment = build.Roots["Level1_Environment"].transform;
        GameObject group = OwnedChild(build, environment, "FantasyBridge", "level1/fantasy/bridge", out bool created);
        if (environment.position != Vector3.zero || environment.rotation != Quaternion.identity || environment.lossyScale != Vector3.one)
            throw new InvalidOperationException("Fantasy bridge requires the existing identity environment container.");
        GameObject ground = UniqueNamedObject(build.Scene, "Ground");
        Renderer renderer = ground != null ? ground.GetComponent<Renderer>() : null;
        if (renderer == null) throw new InvalidOperationException("Ground bounds unavailable; refusing to invent track geometry.");
        Bounds b = renderer.bounds;
        if (b.size.z < 10f || b.size.x < 10f || b.size.z > 500f || b.size.x > 100f)
            throw new InvalidOperationException("Ground dimensions outside the safe Level 1 presentation envelope; review manually.");
        ReplaceKnownMaterial(renderer, "TrackMaterial", build.Materials["FantasyRoad"]);
        Transform root = group.transform;
        float top = b.max.y;
        float edge = b.extents.x - 0.35f;
        FantasyBox(build, root, "BridgeDeck", new Vector3(b.center.x, top - 0.65f, b.center.z), new Vector3(b.size.x + 0.3f, 1.25f, b.size.z), "FantasyStone");
        for (int sign = -1; sign <= 1; sign += 2)
        {
            string side = sign < 0 ? "West" : "East";
            float x = b.center.x + sign * edge;
            FantasyBox(build, root, side + "EdgeLight", new Vector3(x, top + 0.07f, b.center.z), new Vector3(0.17f, 0.09f, b.size.z), "FantasyCyan");
            FantasyBox(build, root, side + "RailBase", new Vector3(x + sign * 0.15f, top + 0.22f, b.center.z), new Vector3(0.3f, 0.4f, b.size.z), "FantasyStone");
            FantasyBox(build, root, side + "RailTop", new Vector3(x + sign * 0.15f, top + 0.82f, b.center.z), new Vector3(0.19f, 0.14f, b.size.z), "FantasyGold");
            for (int i = 0; i < 7; i++)
            {
                float z = Mathf.Lerp(b.min.z, b.max.z, (i + 0.25f) / 7f);
                FantasyBox(build, root, side + "Post" + i, new Vector3(x, top + 0.7f, z), new Vector3(0.65f, 1.4f, 0.65f), "FantasyStone");
                Primitive(build, root, side + "Crystal" + i, PrimitiveType.Cube, new Vector3(x, top + 1.5f, z), Vector3.one * 0.3f, build.Materials["FantasyCyan"], new Vector3(0, 45, 45));
                FantasyBox(build, root, side + "Support" + i, new Vector3(x, top - 4.5f, z), new Vector3(1.1f, 8f, 1.3f), "FantasyCliff");
            }
            for (int i = 0; i < 3; i++)
            {
                float z = b.min.z + b.size.z * (0.35f + i * 0.27f);
                float cliffX = b.center.x + sign * (b.extents.x + 9f + i * 1.7f);
                FantasyBox(build, root, side + "Cliff" + i, new Vector3(cliffX, top - 4f, z), new Vector3(8f, 19f + i * 2f, 9f), "FantasyCliff", new Vector3(0, sign * 14, sign * 5));
                FantasyBox(build, root, side + "CliffCrown" + i, new Vector3(cliffX, top + 5.7f + i, z), new Vector3(8.5f, 1f, 9.5f), "FantasyStone");
                FantasyBox(build, root, side + "Waterfall" + i, new Vector3(cliffX - sign * 4.1f, top - 3.5f, z), new Vector3(0.12f, 18f + i * 2f, 3.8f), "FantasyWater");
                for (int ribbon = 0; ribbon < 3; ribbon++)
                    FantasyBox(build, root, side + "FallRibbon" + i + "_" + ribbon, new Vector3(cliffX - sign * 4.2f, top - 3.5f, z - 1.3f + ribbon * 1.3f), new Vector3(0.08f, 18f + i * 2f, 0.2f), "FantasyFoam");
            }
        }
        for (int sign = -1; sign <= 1; sign += 2)
            for (int i = 0; i < 12; i++)
                FantasyBox(build, root, "LaneRune" + sign + "_" + i, new Vector3(b.center.x + sign * 1.55f, top + 0.018f, Mathf.Lerp(b.min.z, b.max.z, (i + 0.5f) / 12f)), new Vector3(0.055f, 0.025f, 1.25f), "FantasyCyan");
        // Retain, but hide, only our original procedural decoration. No gameplay owner is moved.
        for (int i = 0; i < environment.childCount; i++)
        {
            GameObject old = environment.GetChild(i).gameObject;
            if (old == group) continue;
            Level1GeneratedObject marker = old.GetComponent<Level1GeneratedObject>();
            if (marker != null && marker.GenerationId == "level1/Level1_Environment/" + old.name)
                RetireOwnedVisual(old, marker.GenerationId);
        }
        Debug.Log("Fantasy bridge built/reused: opaque geometric waterfalls, elevated visual supports and emissive lane trims. Ground physics/camera unchanged; emission does not require bloom.");
    }
}
