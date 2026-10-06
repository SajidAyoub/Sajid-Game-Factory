using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Same Editor-only tool, split to keep generated presentation separate from setup.
public static partial class GameFactorySetup
{
    private const string GeneratedFolder = "Assets/GameFactory/Generated/Level1";
    private static readonly string[] Level1RootNames = { "Level1_Environment", "Level1_Visuals", "Level1_VFX", "Level1_UI" };

    [MenuItem(MenuRoot + "Build / Repair Level 1 Master")]
    public static void BuildOrRepairLevel1Master()
    {
        if (!TryGetSetupScene(out Scene scene)) return;
        // Retain the existing dependency/physics/build workflow, not a second implementation.
        SetupOrRepairEntireGame();
        var preflight = new ValidationReport { Level1Mode = true };
        ValidatePlayerAndCamera(scene, preflight);
        ValidateContacts(scene, "Coin_", typeof(Coin), "scoreManager", typeof(ScoreManager), preflight);
        ValidateContacts(scene, "Obstacle_", typeof(Obstacle), "gameManager", typeof(GameManager), preflight);
        ValidateContacts(scene, "FinishLine", typeof(FinishLine), "levelManager", typeof(LevelManager), preflight);
        foreach (Type type in new[] { typeof(GameManager), typeof(LevelManager), typeof(PauseManager), typeof(ScoreManager), typeof(SaveManager),
            typeof(SettingsManager), typeof(RewardManager), typeof(AudioManager), typeof(UIManager), typeof(HUDController) })
        {
            Component owner = CheckManager(scene, type, preflight, true);
            if (owner is Behaviour behaviour && !behaviour.isActiveAndEnabled) preflight.Error(type.Name + " must be active for the Level 1 bridge.");
        }
        if (UniqueNamedObject(scene, "FinishLine") == null) preflight.Error("A unique FinishLine is required for Level 1.");
        if (preflight.Errors > 0) { preflight.Log(); return; }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build / Repair Level 1 Master presentation");
        var build = new Level1Build { Scene = scene };
        try
        {
            GeneratedMaterials(build);
            foreach (string name in Level1RootNames) build.Roots[name] = Level1Root(build, name);
            PlayerVisual(build);
            CoinVisuals(build);
            EnvironmentVisuals(build);
            ObstacleAndFinishVisuals(build);
            LightingPresentation(build);
            Level1PresentationController bridge = PresentationBridge(build);
            Level1VFX(build, bridge);
            Level1UI(build, bridge);
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Level 1 presentation pass: created " + build.Created + ", reused " + build.Reused + ", skipped " + build.Skipped +
                " hierarchy objects. See material/skip logs. Save the reviewed scene/profile and generated assets manually. Asset creation and earlier core/build passes are outside this scene Undo group.");
        }
        catch (Exception exception)
        {
            Debug.LogError("Level 1 partial failure: attempting scene Undo rollback. Generated assets remain for reuse; earlier core/build passes remain. Do not assume the vertical slice is complete.");
            Debug.LogException(exception);
            try { Undo.FlushUndoRecordObjects(); Undo.RevertAllDownToGroup(group); }
            catch (Exception undoException) { Debug.LogException(undoException); }
        }
        ValidateCurrentGameSetup();
    }

    private sealed class Level1Build
    {
        public Scene Scene;
        public readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        public readonly Dictionary<string, GameObject> Roots = new Dictionary<string, GameObject>();
        public int Created;
        public int Reused;
        public int Skipped;
    }

    private static GameObject Level1Root(Level1Build build, string name)
    {
        var matches = SceneObjects(build.Scene).FindAll(item => item.name == name);
        if (matches.Count > 1) throw new InvalidOperationException("Ambiguous Level 1 container: " + name);
        if (matches.Count == 1) { build.Reused++; Debug.Log("Level 1: reused container " + name + ".", matches[0]); return matches[0]; }
        var root = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(root, "Create " + name);
        SceneManager.MoveGameObjectToScene(root, build.Scene);
        build.Created++;
        Debug.Log("Level 1: created container " + name + ".", root);
        return root;
    }

    private static GameObject OwnedChild(Level1Build build, Transform parent, string name, string id, out bool created, bool rect = false)
    {
        GameObject match = null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name != name) continue;
            if (match != null) throw new InvalidOperationException("Duplicate generated child name: " + name);
            match = child.gameObject;
        }
        if (match != null)
        {
            Level1GeneratedObject tag = match.GetComponent<Level1GeneratedObject>();
            if (tag == null || tag.GenerationId != id)
                throw new InvalidOperationException("Unowned object occupies generated name " + name + "; rename it or configure manually. Nothing will be replaced.");
            build.Reused++;
            Debug.Log("Level 1: reused " + parent.name + "/" + name + "; existing edits preserved.", match);
            created = false;
            return match;
        }
        match = rect ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
        Undo.RegisterCreatedObjectUndo(match, "Create " + name);
        Undo.SetTransformParent(match.transform, parent, "Parent " + name);
        Undo.RecordObject(match.transform, "Initialize generated transform");
        match.transform.localPosition = Vector3.zero;
        match.transform.localRotation = Quaternion.identity;
        match.transform.localScale = Vector3.one;
        Level1GeneratedObject marker = Undo.AddComponent<Level1GeneratedObject>(match);
        var serialized = new SerializedObject(marker);
        serialized.FindProperty("generationId").stringValue = id;
        serialized.ApplyModifiedProperties();
        build.Created++;
        created = true;
        Debug.Log("Level 1: created " + name + ".", match);
        return match;
    }

    private static GameObject Primitive(Level1Build build, Transform parent, string name, PrimitiveType type,
        Vector3 position, Vector3 scale, Material material, Vector3 euler = default(Vector3))
    {
        string id = "level1/" + parent.name + "/" + name;
        // First create an owned transform; keep primitive components on a child.
        GameObject holder = OwnedChild(build, parent, name, id, out bool created);
        if (!created && holder.transform.Find("Mesh") != null) return holder; // Preserve edited generated geometry/materials.
        if (created)
        {
            Undo.RecordObject(holder.transform, "Align decorative primitive");
            holder.transform.localPosition = position;
            holder.transform.localScale = scale;
            holder.transform.localRotation = Quaternion.Euler(euler);
        }
        GameObject mesh = GameObject.CreatePrimitive(type);
        mesh.name = "Mesh";
        Undo.RegisterCreatedObjectUndo(mesh, "Create decorative mesh");
        Undo.SetTransformParent(mesh.transform, holder.transform, "Parent decorative mesh");
        Undo.RecordObject(mesh.transform, "Initialize decorative mesh transform");
        mesh.transform.localPosition = Vector3.zero;
        mesh.transform.localRotation = Quaternion.identity;
        mesh.transform.localScale = Vector3.one;
        foreach (Collider collider in mesh.GetComponents<Collider>()) Undo.DestroyObjectImmediate(collider);
        Renderer renderer = mesh.GetComponent<Renderer>();
        Undo.RecordObject(renderer, "Assign decorative material");
        renderer.sharedMaterial = material;
        return holder;
    }

    private static void GeneratedMaterials(Level1Build build)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) throw new InvalidOperationException("URP/Lit shader unavailable; import the existing URP package before building visuals.");
        EnsureAssetFolder(GeneratedFolder);
        AddMaterial(build, "TrackMaterial", lit, new Color(0.055f, 0.09f, 0.15f), 0f, 0.3f);
        AddMaterial(build, "TrackAccentMaterial", lit, new Color(0.13f, 0.8f, 0.85f), 0.15f, 0.45f);
        AddMaterial(build, "EnvironmentMaterial", lit, new Color(0.18f, 0.27f, 0.35f), 0f, 0.25f);
        AddMaterial(build, "EnvironmentAccentMaterial", lit, new Color(0.21f, 0.55f, 0.36f), 0f, 0.2f);
        AddMaterial(build, "GoldCoinMaterial", lit, new Color(1f, 0.72f, 0.12f), 0.75f, 0.7f);
        AddMaterial(build, "ObstacleMaterial", lit, new Color(0.95f, 0.19f, 0.22f), 0.1f, 0.4f);
        AddMaterial(build, "PlayerProxyMaterial", lit, new Color(0.15f, 0.75f, 0.95f), 0.15f, 0.5f);
        AddMaterial(build, "PlayerSkinMaterial", lit, new Color(1f, 0.72f, 0.5f), 0f, 0.4f);
    }

    private static void EnsureAssetFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                if (AssetDatabase.LoadMainAssetAtPath(next) != null) throw new InvalidOperationException("Asset blocks generated folder " + next);
                if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(current, parts[i]))) throw new InvalidOperationException("Cannot create folder " + next);
            }
            current = next;
        }
    }

    private static void AddMaterial(Level1Build build, string name, Shader shader, Color color, float metallic, float smoothness)
    {
        string path = GeneratedFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("Unexpected asset at " + path);
            material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (name == "GoldCoinMaterial" && material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", color * 0.12f);
                material.EnableKeyword("_EMISSION");
            }
            AssetDatabase.CreateAsset(material, path);
            Debug.Log("Level 1: created material " + path + ". Asset creation is not part of scene Undo.");
        }
        else Debug.Log("Level 1: reused material " + path + "; user edits preserved.");
        build.Materials[name] = material;
    }

    private static Vector3 InverseScale(Transform transform)
    {
        Vector3 scale = transform.lossyScale;
        if (Mathf.Abs(scale.x) < 0.0001f || Mathf.Abs(scale.y) < 0.0001f || Mathf.Abs(scale.z) < 0.0001f)
            throw new InvalidOperationException("Cannot safely align visuals under zero-scale object " + transform.name);
        return new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
    }

    private static bool PrimitiveRenderer(Renderer renderer)
    {
        MeshFilter mesh = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
        if (mesh == null || mesh.sharedMesh == null) return false;
        string path = AssetDatabase.GetAssetPath(mesh.sharedMesh);
        return path == "Library/unity default resources" || path == "Resources/unity_builtin_extra";
    }

    private static void HidePrimitive(Renderer renderer)
    {
        if (renderer == null || !renderer.enabled || !PrimitiveRenderer(renderer)) return;
        Undo.RecordObject(renderer, "Hide replaced prototype renderer");
        renderer.enabled = false;
        RecordPrefabChange(renderer);
    }

    private static void DefaultMaterial(Renderer renderer, Material material)
    {
        if (renderer == null || renderer.sharedMaterial == material) return;
        Material existing = renderer.sharedMaterial;
        string path = existing != null ? AssetDatabase.GetAssetPath(existing) : string.Empty;
        if (existing != null && !path.StartsWith("Resources/", StringComparison.Ordinal) &&
            !(path.StartsWith("Packages/com.unity.render-pipelines.", StringComparison.Ordinal) && path.EndsWith("/Lit.mat", StringComparison.Ordinal)))
        {
            Debug.Log("Level 1: preserved custom material on " + renderer.name + ".", renderer);
            return;
        }
        Undo.RecordObject(renderer, "Assign Level 1 material");
        renderer.sharedMaterial = material;
        RecordPrefabChange(renderer);
    }

    private static void PlayerVisual(Level1Build build)
    {
        GameObject player = UniqueNamedObject(build.Scene, "Player");
        if (player == null) throw new InvalidOperationException("Missing/ambiguous Player.");
        foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.GetComponentInParent<Level1GeneratedObject>() != null) continue;
            if (renderer is SkinnedMeshRenderer || renderer.transform != player.transform || !PrimitiveRenderer(renderer))
            { build.Skipped++; Debug.Log("Level 1: existing character model preserved; proxy skipped.", player); return; }
        }
        GameObject proxy = OwnedChild(build, player.transform, "RunnerProxy", "level1/player-proxy", out bool created);
        Collider collider = player.GetComponent<Collider>();
        if (collider == null) throw new InvalidOperationException("Player root collider required to align the temporary proxy.");
        Bounds bounds = collider.bounds;
        if (created)
        {
            Undo.RecordObject(proxy.transform, "Align runner proxy");
            proxy.transform.localPosition = player.transform.InverseTransformPoint(bounds.center);
            proxy.transform.localScale = InverseScale(player.transform);
        }
        float h = Mathf.Max(1f, bounds.size.y);
        float w = Mathf.Max(0.5f, bounds.size.x);
        Material suit = build.Materials["PlayerProxyMaterial"];
        Material skin = build.Materials["PlayerSkinMaterial"];
        Primitive(build, proxy.transform, "Torso", PrimitiveType.Capsule, new Vector3(0, h * 0.05f, 0), new Vector3(w * 0.65f, h * 0.23f, w * 0.42f), suit);
        Primitive(build, proxy.transform, "Head", PrimitiveType.Sphere, new Vector3(0, h * 0.36f, 0), Vector3.one * h * 0.24f, skin);
        Transform leftArm = Limb(build, proxy.transform, "LeftArm", new Vector3(-w * 0.43f, h * 0.22f, 0), h * 0.38f, w * 0.18f, suit);
        Transform rightArm = Limb(build, proxy.transform, "RightArm", new Vector3(w * 0.43f, h * 0.22f, 0), h * 0.38f, w * 0.18f, suit);
        Transform leftLeg = Limb(build, proxy.transform, "LeftLeg", new Vector3(-w * 0.18f, -h * 0.16f, 0), h * 0.32f, w * 0.24f, suit);
        Transform rightLeg = Limb(build, proxy.transform, "RightLeg", new Vector3(w * 0.18f, -h * 0.16f, 0), h * 0.32f, w * 0.24f, suit);
        SimpleRunnerVisual animator = proxy.GetComponent<SimpleRunnerVisual>() ?? Undo.AddComponent<SimpleRunnerVisual>(proxy);
        SetLevel1Reference(animator, "playerController", player.GetComponent<PlayerController>());
        SetLevel1Reference(animator, "leftArm", leftArm); SetLevel1Reference(animator, "rightArm", rightArm);
        SetLevel1Reference(animator, "leftLeg", leftLeg); SetLevel1Reference(animator, "rightLeg", rightLeg);
        HidePrimitive(player.GetComponent<Renderer>());
    }

    private static Transform Limb(Level1Build build, Transform parent, string name, Vector3 position, float length, float width, Material material)
    {
        GameObject pivot = OwnedChild(build, parent, name, "level1/limb/" + name, out bool created);
        if (created) { Undo.RecordObject(pivot.transform, "Align limb pivot"); pivot.transform.localPosition = position; }
        Primitive(build, pivot.transform, "Sleeve", PrimitiveType.Capsule, Vector3.down * length * 0.5f, new Vector3(width, length * 0.5f, width), material);
        return pivot.transform;
    }

    private static void CoinVisuals(Level1Build build)
    {
        foreach (GameObject coin in SceneObjects(build.Scene).FindAll(item => item.name.StartsWith("Coin_", StringComparison.Ordinal)))
        {
            GameObject visual = OwnedChild(build, coin.transform, "CoinVisual", "level1/coin-visual", out bool created);
            if (created)
            {
                Undo.RecordObject(visual.transform, "Align coin visual");
                visual.transform.localScale = InverseScale(coin.transform);
            }
            Collider trigger = coin.GetComponent<Collider>();
            float diameter = trigger != null ? Mathf.Max(trigger.bounds.size.x, trigger.bounds.size.z) : 0.4f;
            Primitive(build, visual.transform, "GoldDisk", PrimitiveType.Cylinder, Vector3.zero, new Vector3(diameter, 0.04f, diameter), build.Materials["GoldCoinMaterial"], new Vector3(90, 0, 0));
            CoinVisualAnimator animator = coin.GetComponent<CoinVisualAnimator>();
            if (animator == null)
            {
                animator = Undo.AddComponent<CoinVisualAnimator>(coin);
                SetLevel1Reference(animator, "visual", visual.transform);
                var data = new SerializedObject(animator);
                data.FindProperty("bobHeight").floatValue = 0.12f / Mathf.Abs(coin.transform.lossyScale.y);
                data.ApplyModifiedProperties();
            }
            else SetLevel1Reference(animator, "visual", visual.transform);
            HidePrimitive(coin.GetComponent<Renderer>());
        }
    }

    private static void EnvironmentVisuals(Level1Build build)
    {
        GameObject ground = UniqueNamedObject(build.Scene, "Ground");
        Renderer surface = ground != null ? ground.GetComponent<Renderer>() : null;
        if (surface == null) throw new InvalidOperationException("Ground renderer required; track dimensions are never invented.");
        DefaultMaterial(surface, build.Materials["TrackMaterial"]);
        Bounds bounds = surface.bounds;
        Transform root = build.Roots["Level1_Environment"].transform;
        // Generated containers must have identity transforms for world-space track alignment.
        if (root.position != Vector3.zero || root.rotation != Quaternion.identity || root.lossyScale != Vector3.one)
            throw new InvalidOperationException("Level1_Environment has a customized transform; refusing to guess world alignment.");
        float side = Mathf.Max(5.6f, bounds.extents.x - 0.6f);
        for (int sign = -1; sign <= 1; sign += 2)
        {
            string sideName = sign < 0 ? "Left" : "Right";
            Primitive(build, root, sideName + "TrackAccent", PrimitiveType.Cube, new Vector3(bounds.center.x + sign * side, bounds.max.y + 0.025f, bounds.center.z), new Vector3(0.18f, 0.05f, bounds.size.z), build.Materials["TrackAccentMaterial"]);
            Primitive(build, root, sideName + "Rail", PrimitiveType.Cube, new Vector3(bounds.center.x + sign * (side + 0.35f), bounds.max.y + 0.2f, bounds.center.z), new Vector3(0.25f, 0.4f, bounds.size.z), build.Materials["EnvironmentMaterial"]);
            for (int i = 0; i < 6; i++)
            {
                float z = Mathf.Lerp(bounds.min.z, bounds.max.z, (i + 0.5f) / 6f);
                Primitive(build, root, sideName + "Pillar" + i, PrimitiveType.Cube, new Vector3(bounds.center.x + sign * (bounds.extents.x + 2f), bounds.max.y + 1.25f, z), new Vector3(0.9f, 2.5f, 0.9f), build.Materials["EnvironmentMaterial"]);
                Primitive(build, root, sideName + "Greenery" + i, PrimitiveType.Sphere, new Vector3(bounds.center.x + sign * (bounds.extents.x + 2f), bounds.max.y + 2.8f, z), Vector3.one * 1.5f, build.Materials["EnvironmentAccentMaterial"]);
            }
        }
        for (int i = 0; i < 10; i++)
            Primitive(build, root, "CenterDash" + i, PrimitiveType.Cube, new Vector3(bounds.center.x, bounds.max.y + 0.015f, Mathf.Lerp(bounds.min.z, bounds.max.z, (i + 0.5f) / 10f)), new Vector3(0.08f, 0.025f, 0.8f), build.Materials["TrackAccentMaterial"]);
        Primitive(build, root, "Horizon", PrimitiveType.Cube, new Vector3(bounds.center.x, bounds.max.y + 3f, bounds.max.z + 30f), new Vector3(45f, 6f, 2f), build.Materials["EnvironmentMaterial"]);
    }

    private static void ObstacleAndFinishVisuals(Level1Build build)
    {
        foreach (GameObject obstacle in SceneObjects(build.Scene).FindAll(item => item.name.StartsWith("Obstacle_", StringComparison.Ordinal)))
        {
            DefaultMaterial(obstacle.GetComponent<Renderer>(), build.Materials["ObstacleMaterial"]);
            GameObject visual = OwnedChild(build, obstacle.transform, "ObstacleVisual", "level1/obstacle-visual", out bool created);
            Primitive(build, visual.transform, "HazardBand", PrimitiveType.Cube, new Vector3(0f, 0.2f, -0.51f), new Vector3(0.92f, 0.16f, 0.025f), build.Materials["GoldCoinMaterial"]);
        }
        GameObject finish = UniqueNamedObject(build.Scene, "FinishLine");
        if (finish == null) throw new InvalidOperationException("Missing/ambiguous FinishLine; no gameplay finish object is invented.");
        GameObject gate = OwnedChild(build, finish.transform, "FinishVisual", "level1/finish-visual", out bool gateCreated);
        if (gateCreated) { Undo.RecordObject(gate.transform, "Align finish gate"); gate.transform.localScale = InverseScale(finish.transform); }
        Primitive(build, gate.transform, "LeftPost", PrimitiveType.Cube, new Vector3(-5.4f, 1.6f, 0), new Vector3(0.3f, 3.2f, 0.3f), build.Materials["TrackAccentMaterial"]);
        Primitive(build, gate.transform, "RightPost", PrimitiveType.Cube, new Vector3(5.4f, 1.6f, 0), new Vector3(0.3f, 3.2f, 0.3f), build.Materials["TrackAccentMaterial"]);
        Primitive(build, gate.transform, "Banner", PrimitiveType.Cube, new Vector3(0, 3.3f, 0), new Vector3(11f, 0.5f, 0.3f), build.Materials["GoldCoinMaterial"]);
        for (int i = 0; i < 12; i++)
            Primitive(build, gate.transform, "FinishTile" + i, PrimitiveType.Cube, new Vector3(-4.6f + i * 0.83f, 0.025f, 0), new Vector3(0.8f, 0.025f, 0.45f), build.Materials[i % 2 == 0 ? "TrackMaterial" : "GoldCoinMaterial"]);
    }

    private static void LightingPresentation(Level1Build build)
    {
        foreach (Component component in SceneComponents(build.Scene, typeof(Light)))
            if (((Light)component).type == LightType.Directional)
            { Debug.Log("Level 1: existing Directional Light and CameraFollow composition preserved."); return; }
        GameObject lightObject = OwnedChild(build, build.Roots["Level1_Visuals"].transform, "Level1DirectionalLight", "level1/light", out bool created);
        if (created)
        {
            Light light = Undo.AddComponent<Light>(lightObject);
            Undo.RecordObject(light, "Configure new directional light");
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.shadows = LightShadows.Soft;
            Undo.RecordObject(light.transform, "Aim new directional light");
            light.transform.localRotation = Quaternion.Euler(50, -35, 0);
        }
    }

    private static void SetLevel1Reference(Component owner, string field, UnityEngine.Object value)
    {
        bool changed = false;
        UnityEngine.Object existing = ReadReference(owner, field);
        SerializedProperty property = new SerializedObject(owner).FindProperty(field);
        if (property != null && property.propertyType == SerializedPropertyType.ObjectReference &&
            property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
            throw new InvalidOperationException("Broken Level 1 reference requires manual repair: " + owner.GetType().Name + "." + field);
        if (existing != null && existing != value) throw new InvalidOperationException("Configured Level 1 reference differs: " + owner.GetType().Name + "." + field);
        SetReference(owner, field, value, ref changed);
    }
}
