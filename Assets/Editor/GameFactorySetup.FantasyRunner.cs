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
            Undo.RecordObject(visual.transform, "Center wolf visual on existing collider");
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
        Primitive(build, root, "Neck", PrimitiveType.Cylinder, new Vector3(0, h * 0.22f, 0), new Vector3(w * 0.3f, h * 0.04f, w * 0.3f), build.Materials["WolfLime"]);
        Primitive(build, root, "WolfHead", PrimitiveType.Sphere, new Vector3(0, h * 0.32f, 0), new Vector3(w * 0.65f, h * 0.25f, w * 0.59f), build.Materials["WolfFur"]);
        Primitive(build, root, "Muzzle", PrimitiveType.Sphere, new Vector3(0, h * 0.3f, w * 0.28f), new Vector3(w * 0.4f, h * 0.13f, w * 0.38f), build.Materials["WolfFur"]);
        Primitive(build, root, "Nose", PrimitiveType.Sphere, new Vector3(0, h * 0.32f, w * 0.46f), new Vector3(w * 0.16f, h * 0.055f, w * 0.07f), build.Materials["WolfOutfit"]);
        for (int sign = -1; sign <= 1; sign += 2)
        {
            string side = sign < 0 ? "Left" : "Right";
            FantasyBox(build, root, side + "Ear", new Vector3(sign * w * 0.23f, h * 0.47f, -w * 0.045f), new Vector3(w * 0.19f, h * 0.17f, w * 0.17f), "WolfFur", new Vector3(0, 0, sign * -18));
            FantasyBox(build, root, side + "InnerEar", new Vector3(sign * w * 0.23f, h * 0.47f, w * 0.045f), new Vector3(w * 0.095f, h * 0.11f, 0.014f), "WolfEarInner", new Vector3(0, 0, sign * -18));
            Primitive(build, root, side + "Eye", PrimitiveType.Sphere, new Vector3(sign * w * 0.18f, h * 0.35f, w * 0.27f), new Vector3(w * 0.1f, h * 0.043f, 0.035f), build.Materials["FantasyCyan"]);
        }
        Transform leftArm = WolfLimb(build, root, "WolfLeftArm", new Vector3(-w * 0.4f, h * 0.15f, 0), h * 0.35f, w * 0.2f, true);
        Transform rightArm = WolfLimb(build, root, "WolfRightArm", new Vector3(w * 0.4f, h * 0.15f, 0), h * 0.35f, w * 0.2f, true);
        Transform leftLeg = WolfLimb(build, root, "WolfLeftLeg", new Vector3(-w * 0.19f, -h * 0.13f, 0), h * 0.34f, w * 0.25f, false);
        Transform rightLeg = WolfLimb(build, root, "WolfRightLeg", new Vector3(w * 0.19f, -h * 0.13f, 0), h * 0.34f, w * 0.25f, false);
        Primitive(build, root, "Tail", PrimitiveType.Capsule, new Vector3(0, -h * 0.16f, -w * 0.4f), new Vector3(w * 0.14f, h * 0.16f, w * 0.14f), build.Materials["WolfFur"], new Vector3(-55, 0, 0));
        SimpleRunnerVisual animator = visual.GetComponent<SimpleRunnerVisual>() ?? Undo.AddComponent<SimpleRunnerVisual>(visual);
        SetLevel1Reference(animator, "playerController", player.GetComponent<PlayerController>());
        SetLevel1Reference(animator, "leftArm", leftArm); SetLevel1Reference(animator, "rightArm", rightArm);
        SetLevel1Reference(animator, "leftLeg", leftLeg); SetLevel1Reference(animator, "rightLeg", rightLeg);
        SetLevel1Reference(animator, "poseRoot", root);
        RunnerPresentationController controller = visual.GetComponent<RunnerPresentationController>() ?? Undo.AddComponent<RunnerPresentationController>(visual);
        SetLevel1Reference(controller, "generatedVisual", wolf);
        string[] slots = { "head", "body", "outfit", "accent" };
        string[] palette = { "WolfFur", "WolfJacket", "WolfOutfit", "WolfLime" };
        for (int i = 0; i < slots.Length; i++)
        {
            SetLevel1Reference(controller, slots[i] + "Material", build.Materials[palette[i]]);
            var serialized = new SerializedObject(controller);
            SerializedProperty renderers = serialized.FindProperty(slots[i] + "Renderers");
            if (renderers.arraySize != 0) continue; // Explicit user slot edits are preserved.
            var matches = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer renderer in wolf.GetComponentsInChildren<Renderer>(true))
                if (renderer.sharedMaterial == build.Materials[palette[i]]) matches.Add(renderer);
            Undo.RecordObject(controller, "Wire runner appearance slots");
            renderers.arraySize = matches.Count;
            for (int j = 0; j < matches.Count; j++) renderers.GetArrayElementAtIndex(j).objectReferenceValue = matches[j];
            serialized.ApplyModifiedProperties();
        }
        Transform old = player.transform.Find("RunnerProxy");
        if (old != null) RetireOwnedVisual(old.gameObject, "level1/player-proxy");
        HidePrimitive(player.GetComponent<Renderer>());
        Debug.Log("Fantasy: white wolf proxy with blue jacket, dark sportswear and lime accents. Player root/physics unchanged; custom FBX swap seam ready.");
    }

    private static Transform WolfLimb(Level1Build build, Transform parent, string name, Vector3 pivot, float length, float width, bool arm)
    {
        GameObject holder = OwnedChild(build, parent, name, "level1/fantasy/" + name, out bool created);
        if (created) { Undo.RecordObject(holder.transform, "Position wolf limb pivot"); holder.transform.localPosition = pivot; }
        Primitive(build, holder.transform, "Limb", PrimitiveType.Capsule, new Vector3(0, -length * 0.5f, 0), new Vector3(width, length * 0.5f, width), build.Materials[arm ? "WolfJacket" : "WolfOutfit"]);
        FantasyBox(build, holder.transform, "NeonCuff", new Vector3(0, -length * 0.84f, 0), new Vector3(width * 1.06f, length * 0.055f, width * 1.06f), "WolfLime");
        Primitive(build, holder.transform, arm ? "Paw" : "Shoe", PrimitiveType.Cube, new Vector3(0, -length, width * 0.2f), new Vector3(width * 1.1f, length * 0.17f, width * 1.5f), build.Materials[arm ? "WolfFur" : "WolfOutfit"]);
        return holder.transform;
    }
}
