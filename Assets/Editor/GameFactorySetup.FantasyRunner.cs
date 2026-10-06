using System;
using UnityEditor;
using UnityEngine;

public static partial class GameFactorySetup
{
    private static void FantasyRunnerPass(Level1Build build)
    {
        GameObject player = UniqueNamedObject(build.Scene, "Player");
        if (player == null) throw new InvalidOperationException("A unique Player is required for the wolf presentation.");
        foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>(true))
            if (renderer.GetComponentInParent<Level1GeneratedObject>() == null &&
                (renderer is SkinnedMeshRenderer || renderer.transform != player.transform || !PrimitiveRenderer(renderer)))
            { build.Skipped++; Debug.Log("Fantasy: authored player model preserved; wolf generation skipped."); return; }
        FantasyMaterial(build, "WolfFur", new Color(0.9f, 0.96f, 1f));
        FantasyMaterial(build, "WolfJacket", new Color(0.045f, 0.28f, 0.8f));
        FantasyMaterial(build, "WolfOutfit", new Color(0.035f, 0.045f, 0.07f));
        FantasyMaterial(build, "WolfLime", new Color(0.55f, 1f, 0.05f), 0f, 0.7f);
        FantasyMaterial(build, "WolfEarInner", new Color(0.58f, 0.7f, 0.8f));
        GameObject visual = OwnedChild(build, player.transform, "PlayerVisual", "level1/fantasy/player", out bool created);
        Collider collider = player.GetComponent<Collider>();
        if (collider == null) throw new InvalidOperationException("Player collider required; wolf visuals never invent gameplay dimensions.");
        Bounds bounds = collider.bounds;
        if (created)
        {
            RecordPresentationObject(visual.transform, "Center wolf visual on existing collider");
            visual.transform.localPosition = player.transform.InverseTransformPoint(bounds.center);
            visual.transform.localScale = InverseScale(player.transform);
        }
        GameObject wolf = OwnedChild(build, visual.transform, "GeneratedWolf", "level1/fantasy/wolf", out bool wolfCreated);
        Transform root = wolf.transform;
        float h = Mathf.Clamp(bounds.size.y, 1f, 4f);
        float w = Mathf.Clamp(bounds.size.x, 0.5f, 2f);
        FantasyBox(build, root, "JacketBody", new Vector3(0, h * 0.03f, 0), new Vector3(w * 0.64f, h * 0.3f, w * 0.46f), "WolfJacket");
        FantasyBox(build, root, "JacketHem", new Vector3(0, -h * 0.12f, 0), new Vector3(w * 0.66f, h * 0.035f, w * 0.48f), "WolfLime");
        FantasyBox(build, root, "BackStripe", new Vector3(0, h * 0.055f, -w * 0.242f), new Vector3(w * 0.16f, h * 0.22f, 0.025f), "WolfLime");
        FantasyBox(build, root, "JacketZip", new Vector3(0, h * 0.02f, w * 0.242f), new Vector3(0.025f, h * 0.25f, 0.025f), "FantasyGold");
        Primitive(build, root, "Neck", PrimitiveType.Cylinder, new Vector3(0, h * 0.22f, 0), new Vector3(w * 0.3f, h * 0.04f, w * 0.3f), PresentationMaterial(build, "WolfLime"));
        Primitive(build, root, "WolfHead", PrimitiveType.Sphere, new Vector3(0, h * 0.32f, 0), new Vector3(w * 0.65f, h * 0.25f, w * 0.59f), PresentationMaterial(build, "WolfFur"));
        Primitive(build, root, "Muzzle", PrimitiveType.Sphere, new Vector3(0, h * 0.3f, w * 0.28f), new Vector3(w * 0.4f, h * 0.13f, w * 0.38f), PresentationMaterial(build, "WolfFur"));
        Primitive(build, root, "Nose", PrimitiveType.Sphere, new Vector3(0, h * 0.32f, w * 0.46f), new Vector3(w * 0.16f, h * 0.055f, w * 0.07f), PresentationMaterial(build, "WolfOutfit"));
        for (int sign = -1; sign <= 1; sign += 2)
        {
            string side = sign < 0 ? "Left" : "Right";
            WolfEar(build, root, side + "Ear", new Vector3(sign * w * 0.23f, h * 0.47f, -w * 0.045f), new Vector3(w * 0.25f, h * 0.18f, w * 0.24f), sign, "WolfFur");
            WolfEar(build, root, side + "InnerEar", new Vector3(sign * w * 0.23f, h * 0.47f, w * 0.035f), new Vector3(w * 0.12f, h * 0.11f, 0.014f), sign, "WolfEarInner");
            Primitive(build, root, side + "Eye", PrimitiveType.Sphere, new Vector3(sign * w * 0.18f, h * 0.35f, w * 0.27f), new Vector3(w * 0.1f, h * 0.043f, 0.035f), PresentationMaterial(build, "FantasyCyan"));
        }
        Transform leftArm = WolfLimb(build, root, "WolfLeftArm", new Vector3(-w * 0.4f, h * 0.15f, 0), h * 0.35f, w * 0.2f, true);
        Transform rightArm = WolfLimb(build, root, "WolfRightArm", new Vector3(w * 0.4f, h * 0.15f, 0), h * 0.35f, w * 0.2f, true);
        Transform leftLeg = WolfLimb(build, root, "WolfLeftLeg", new Vector3(-w * 0.19f, -h * 0.13f, 0), h * 0.34f, w * 0.25f, false);
        Transform rightLeg = WolfLimb(build, root, "WolfRightLeg", new Vector3(w * 0.19f, -h * 0.13f, 0), h * 0.34f, w * 0.25f, false);
        Primitive(build, root, "Tail", PrimitiveType.Capsule, new Vector3(0, -h * 0.16f, -w * 0.4f), new Vector3(w * 0.14f, h * 0.16f, w * 0.14f), PresentationMaterial(build, "WolfFur"), new Vector3(-55, 0, 0));
        SimpleRunnerVisual animator = EnsurePresentationComponent<SimpleRunnerVisual>(visual);
        SetLevel1Reference(animator, "playerController", player.GetComponent<PlayerController>());
        SetLevel1Reference(animator, "leftArm", leftArm); SetLevel1Reference(animator, "rightArm", rightArm);
        SetLevel1Reference(animator, "leftLeg", leftLeg); SetLevel1Reference(animator, "rightLeg", rightLeg);
        SetLevel1Reference(animator, "poseRoot", root);
        RunnerPresentationController controller = EnsurePresentationComponent<RunnerPresentationController>(visual);
        SetLevel1Reference(controller, "generatedVisual", wolf);
        string[] slots = { "head", "body", "outfit", "accent" };
        string[] palette = { "WolfFur", "WolfJacket", "WolfOutfit", "WolfLime" };
        for (int i = 0; i < slots.Length; i++)
        {
            if (ReadReference(controller, slots[i] + "Material") == null)
                SetLevel1Reference(controller, slots[i] + "Material", PresentationMaterial(build, palette[i]));
            var serialized = new SerializedObject(controller);
            SerializedProperty renderers = serialized.FindProperty(slots[i] + "Renderers");
            if (renderers == null || !renderers.isArray) throw new InvalidOperationException("Runner renderer-slot API missing: " + slots[i]);
            if (renderers.arraySize != 0) continue; // Explicit user slot edits are preserved.
            var matches = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer renderer in wolf.GetComponentsInChildren<Renderer>(true))
                if (renderer.sharedMaterial == PresentationMaterial(build, palette[i])) matches.Add(renderer);
            RecordPresentationObject(controller, "Wire runner appearance slots");
            renderers.arraySize = matches.Count;
            for (int j = 0; j < matches.Count; j++) renderers.GetArrayElementAtIndex(j).objectReferenceValue = matches[j];
            serialized.ApplyModifiedProperties();
        }
        Transform old = player.transform.Find("RunnerProxy");
        if (old != null) RetireOwnedVisual(old.gameObject, "level1/player-proxy");
        HidePrimitive(player.GetComponent<Renderer>());
        Debug.Log("Fantasy: white wolf proxy with blue jacket, dark sportswear and lime accents. Player root/physics unchanged; custom FBX swap seam ready.");
    }

    private static void WolfEar(Level1Build build, Transform parent, string name, Vector3 position, Vector3 scale, int side, string material)
    {
        RunPresentationStep(build, "wolf ear " + name, () => BuildWolfEar(build, parent, name, position, scale, side, material));
    }

    private static void BuildWolfEar(Level1Build build, Transform parent, string name, Vector3 position, Vector3 scale, int side, string material)
    {
        GameObject ear = OwnedChild(build, parent, name, "level1/fantasy/ear/" + name, out bool created);
        if (created)
        {
            RecordPresentationObject(ear.transform, "Align triangular wolf ear");
            ear.transform.localPosition = position; ear.transform.localScale = scale;
            ear.transform.localRotation = Quaternion.Euler(0, 0, side * -15f);
        }
        Material earMaterial = PresentationMaterial(build, material);
        GameObject meshHost = ear;
        // New ears use native primitive creation, which installs the render pair together.
        // Primitive removes its Collider; only its Mesh is replaced with the modeled ear.
        // Keep older complete ears on their original object to preserve serialized references.
        if (created || ear.transform.Find("EarGeometry") != null)
        {
            GameObject geometry = Primitive(build, ear.transform, "EarGeometry", PrimitiveType.Cube,
                Vector3.zero, Vector3.one, earMaterial);
            Transform meshChild = RequirePresentationObject(geometry.transform.Find("Mesh"), "Wolf ear mesh child " + name);
            meshHost = meshChild.gameObject;
        }
        bool newRenderer = meshHost.GetComponent<MeshRenderer>() == null;
        EnsurePresentationComponent<MeshFilter>(meshHost);
        EnsurePresentationComponent<MeshRenderer>(meshHost);
        MeshFilter filter = RequirePresentationObject(meshHost.GetComponent<MeshFilter>(), "Wolf ear " + name + " MeshFilter");
        MeshRenderer renderer = RequirePresentationObject(meshHost.GetComponent<MeshRenderer>(), "Wolf ear " + name + " MeshRenderer");
        bool needsMesh = filter.sharedMesh == null || filter.sharedMesh.vertexCount == 0 || (meshHost != ear && PrimitiveRenderer(renderer));
        if (!needsMesh && !newRenderer && renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null) return;
        string path = FantasyFolder + "/WolfEar.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("Unexpected wolf ear asset.");
            mesh = new Mesh { name = "WolfEar" };
            Vector3[] points = { new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0, 0.5f, 0), new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f) };
            int[] order = { 0, 2, 1, 1, 2, 4, 4, 2, 3, 3, 2, 0, 0, 1, 4, 0, 4, 3 };
            var vertices = new Vector3[order.Length]; var triangles = new int[order.Length];
            for (int i = 0; i < order.Length; i++) { vertices[i] = points[order[i]]; triangles[i] = i; }
            mesh.vertices = vertices; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
        }
        RequirePresentationObject(mesh, "Generated WolfEar mesh");
        if (needsMesh)
        {
            RecordPresentationObject(filter, "Assign procedural wolf ear mesh: " + name);
            filter.sharedMesh = mesh;
        }
        if (newRenderer || renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null)
        {
            RecordPresentationObject(renderer, "Assign wolf ear material: " + name);
            renderer.sharedMaterial = earMaterial;
        }
        RecordPrefabChange(filter); RecordPrefabChange(renderer);
    }

    private static Transform WolfLimb(Level1Build build, Transform parent, string name, Vector3 pivot, float length, float width, bool arm)
    {
        GameObject holder = OwnedChild(build, parent, name, "level1/fantasy/" + name, out bool created);
        if (created) { RecordPresentationObject(holder.transform, "Position wolf limb pivot"); holder.transform.localPosition = pivot; }
        Primitive(build, holder.transform, "Limb", PrimitiveType.Capsule, new Vector3(0, -length * 0.5f, 0), new Vector3(width, length * 0.5f, width), PresentationMaterial(build, arm ? "WolfJacket" : "WolfOutfit"));
        FantasyBox(build, holder.transform, "NeonCuff", new Vector3(0, -length * 0.84f, 0), new Vector3(width * 1.06f, length * 0.055f, width * 1.06f), "WolfLime");
        Primitive(build, holder.transform, arm ? "Paw" : "Shoe", PrimitiveType.Cube, new Vector3(0, -length, width * 0.2f), new Vector3(width * 1.1f, length * 0.17f, width * 1.5f), PresentationMaterial(build, arm ? "WolfFur" : "WolfOutfit"));
        return holder.transform;
    }
}
