using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only, user-invoked setup. Never loads or saves a scene automatically.
public static class GameFactorySetup
{
    private const string MenuPath = "Tools/Sajid Game Factory/Setup Obstacle & Game Over";
    private const string FinishMenuPath = "Tools/Sajid Game Factory/Setup Finish Line & Level Progression";
    private const string BuildMenuPath = "Tools/Sajid Game Factory/Ensure MainGame In Build Scenes";
    private const string MainGameScenePath = "Assets/Scenes/MainGame.unity";

    [MenuItem(BuildMenuPath)]
    public static void EnsureMainGameInBuildScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogError("Game Factory: unsafe editor state; run build-scene setup in Edit Mode after compilation/import finishes.");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainGameScenePath) == null)
        {
            Debug.LogError("Game Factory: scene asset missing at " + MainGameScenePath + "; build scenes were not changed.");
            return;
        }

        BuildProfile profile = null;
        bool profileOverride = false;
        EditorBuildSettingsScene[] original = null;
        EditorBuildSettingsScene[] updated = null;
        bool attemptedWrite = false;
        try
        {
            profile = BuildProfile.GetActiveBuildProfile();
            profileOverride = profile != null && profile.overrideGlobalScenes;
            if (profileOverride && (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(profile)) ||
                !AssetDatabase.IsOpenForEdit(AssetDatabase.GetAssetPath(profile))))
                throw new InvalidOperationException("Unsupported/unsafe Build Profile state: active override asset is unsaved or not editable.");
            if (profileOverride)
            {
                // Unity's profile getter automatically removes missing scene assets.
                // Inspect the serialized list first so malformed profiles are left alone.
                SerializedObject serializedProfile = new SerializedObject(profile);
                SerializedProperty rawScenes = serializedProfile.FindProperty("m_Scenes");
                if (rawScenes == null || !rawScenes.isArray)
                    throw new InvalidOperationException("Unsupported Build Profile scene serialization; configure it manually.");
                for (int i = 0; i < rawScenes.arraySize; i++)
                {
                    SerializedProperty path = rawScenes.GetArrayElementAtIndex(i).FindPropertyRelative("m_path");
                    if (path == null || path.propertyType != SerializedPropertyType.String ||
                        AssetDatabase.LoadAssetAtPath<SceneAsset>(path.stringValue) == null)
                        throw new InvalidOperationException("Unsafe Build Profile contains an invalid scene asset; repair it manually before running setup.");
                }
            }

            // Unity 6 routes this API to the active override, or to the shared list.
            original = CopyBuildScenes(EditorBuildSettings.scenes);
            string sceneGuid = AssetDatabase.AssetPathToGUID(MainGameScenePath);
            int match = -1;
            for (int i = 0; i < original.Length; i++)
            {
                bool samePath = original[i].path == MainGameScenePath;
                bool sameGuid = original[i].guid.ToString() == sceneGuid;
                if (!samePath && !sameGuid) continue;
                if (match >= 0 || !samePath)
                    throw new InvalidOperationException("Unsupported/unsafe scene list: duplicate or stale MainGame entries. Resolve them manually; no changes made.");
                match = i;
            }
            if (match >= 0 && original[match].enabled)
            {
                Debug.Log("Game Factory: MainGame is already configured and enabled in the active build scene list; no changes needed.");
                return;
            }

            foreach (EditorBuildSettingsScene entry in original)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.path) == null ||
                    (!entry.guid.Equals(default(GUID)) && AssetDatabase.GUIDToAssetPath(entry.guid.ToString()) != entry.path))
                    throw new InvalidOperationException("Unsafe build scene list contains a missing asset or stale GUID/path. Repair it manually; existing entries will not be overwritten.");
            }

            updated = CopyBuildScenes(original);
            if (match >= 0) updated[match].enabled = true;
            else
            {
                Array.Resize(ref updated, updated.Length + 1);
                updated[updated.Length - 1] = new EditorBuildSettingsScene(MainGameScenePath, true);
            }
            if (profileOverride) Undo.RegisterCompleteObjectUndo(profile, "Ensure MainGame In Build Scenes");
            else Debug.LogWarning("Game Factory: shared EditorBuildSettings scene-list changes do not have a normal object Undo target. Undo is unavailable for this operation; other entries/order will be preserved.");

            attemptedWrite = true;
            EditorBuildSettings.scenes = updated;
            if (BuildProfile.GetActiveBuildProfile() != profile ||
                (profile != null && profile.overrideGlobalScenes) != profileOverride ||
                !SameBuildScenes(EditorBuildSettings.scenes, updated))
                throw new InvalidOperationException("Active build scene list did not retain the requested update.");
            if (profileOverride) EditorUtility.SetDirty(profile);
            Debug.Log("Game Factory: MainGame " + (match >= 0 ? "re-enabled" : "added successfully at the end") +
                " in the active build scene list. Existing entries and order preserved. " +
                (profileOverride ? "Build Profile Undo is available; the profile asset is marked dirty for saving." : "Shared build scene list updated."));
        }
        catch (Exception exception)
        {
            Debug.LogError("Game Factory: build-scene setup failed" + (attemptedWrite ? " after a write attempt (possible partial failure)." : " before writing; no build changes made."));
            Debug.LogException(exception);
            if (!attemptedWrite || original == null) return;
            try
            {
                // Do not overwrite a different profile or another tool's intervening edits.
                BuildProfile current = BuildProfile.GetActiveBuildProfile();
                if (current != profile || (current != null && current.overrideGlobalScenes) != profileOverride ||
                    !SameBuildScenes(EditorBuildSettings.scenes, updated))
                {
                    Debug.LogError("Game Factory: safe rollback unavailable because the active profile/list changed; inspect Build Profiles manually.");
                    return;
                }
                EditorBuildSettings.scenes = original;
                if (!SameBuildScenes(EditorBuildSettings.scenes, original))
                    throw new InvalidOperationException("Original build scene list could not be restored; inspect Build Profiles manually.");
                if (profileOverride) EditorUtility.SetDirty(profile);
                Debug.LogWarning("Game Factory: restored the original build scene list after the partial failure; verify Build Profiles.");
            }
            catch (Exception rollbackException) { Debug.LogException(rollbackException); }
        }
    }

    private static EditorBuildSettingsScene[] CopyBuildScenes(EditorBuildSettingsScene[] scenes)
    {
        if (scenes == null) throw new InvalidOperationException("Unsupported/unsafe null build scene list.");
        EditorBuildSettingsScene[] copy = new EditorBuildSettingsScene[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i] == null || scenes[i].path == null)
                throw new InvalidOperationException("Unsupported/unsafe malformed build scene entry; fix it manually.");
            copy[i] = new EditorBuildSettingsScene { path = scenes[i].path, enabled = scenes[i].enabled, guid = scenes[i].guid };
        }
        return copy;
    }

    private static bool SameBuildScenes(EditorBuildSettingsScene[] left, EditorBuildSettingsScene[] right)
    {
        if (left == null || right == null || left.Length != right.Length) return false;
        for (int i = 0; i < left.Length; i++)
            if (left[i] == null || right[i] == null || left[i].path != right[i].path ||
                left[i].enabled != right[i].enabled || !left[i].guid.Equals(right[i].guid)) return false;
        return true;
    }

    [MenuItem(FinishMenuPath)]
    public static void SetupFinishLineAndLevelProgression()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            PrefabStageUtility.GetCurrentPrefabStage() != null ||
            !scene.IsValid() || !scene.isLoaded || scene.name != "MainGame")
        {
            Debug.LogError("Game Factory: open MainGame as the active scene in Edit Mode (outside Prefab Mode) before finish-line setup.");
            return;
        }

        GameObject managers = FindUniqueSceneObject(scene, "Managers");
        GameObject finishObject = FindUniqueSceneObject(scene, "FinishLine");
        GameObject playerObject = FindUniqueSceneObject(scene, "Player");
        if (managers == null || finishObject == null || playerObject == null) return;

        GameManager gameManager = managers.GetComponent<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("Game Factory: missing GameManager on Managers. Complete obstacle/game-over setup first; no changes made.", managers);
            return;
        }
        PlayerController player = playerObject.GetComponent<PlayerController>();
        Rigidbody playerBody = playerObject.GetComponent<Rigidbody>();
        if (player == null || playerBody == null)
        {
            Debug.LogError("Game Factory: Player requires PlayerController and Rigidbody. Complete Player setup first; no changes made.", playerObject);
            return;
        }
        bool validPlayerCollider = false;
        foreach (Collider collider in playerObject.GetComponentsInChildren<Collider>(true))
        {
            if (!collider.enabled || !collider.gameObject.activeInHierarchy || collider.attachedRigidbody != playerBody) continue;
            if (collider is MeshCollider mesh && (mesh.sharedMesh == null || !mesh.convex))
            {
                Debug.LogError("Game Factory: invalid Player MeshCollider for the runner's kinematic Rigidbody; fix the collider manually. No changes made.", collider);
                return;
            }
            validPlayerCollider = true;
        }
        if (!validPlayerCollider || !playerBody.detectCollisions)
        {
            Debug.LogError("Game Factory: missing/invalid Player 3D Collider or Rigidbody collision detection is disabled. No changes made.", playerObject);
            return;
        }
        if (!HasCompatibleReference(typeof(LevelManager), "gameManager", typeof(GameManager)) ||
            !HasCompatibleReference(typeof(FinishLine), "levelManager", typeof(LevelManager)))
        {
            Debug.LogError("Game Factory: incompatible LevelManager.gameManager or FinishLine.levelManager Inspector API; no changes made.");
            return;
        }
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (LevelManager existing in root.GetComponentsInChildren<LevelManager>(true))
            {
                if (existing.gameObject != managers)
                {
                    Debug.LogError("Game Factory: LevelManager already exists outside Managers; resolve ownership first. No changes made.", existing);
                    return;
                }
            }
        }
        Collider[] finishColliders = finishObject.GetComponents<Collider>();
        foreach (Collider collider in finishColliders)
        {
            if (collider is MeshCollider mesh && mesh.sharedMesh == null)
            {
                Debug.LogError("Game Factory: invalid FinishLine MeshCollider with no mesh; fix it manually. No changes made.", collider);
                return;
            }
        }

        WarnAboutBuildProgression(scene);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup Finish Line & Level Progression");
        bool changed = false;
        try
        {
            LevelManager levelManager = GetOrAdd<LevelManager>(managers, ref changed);
            SetReference(levelManager, "gameManager", gameManager, ref changed);
            FinishLine finishLine = GetOrAdd<FinishLine>(finishObject, ref changed);
            SetReference(finishLine, "levelManager", levelManager, ref changed);
            if (finishColliders.Length == 0)
            {
                BoxCollider box = GetOrAdd<BoxCollider>(finishObject, ref changed);
                MeshFilter mesh = finishObject.GetComponent<MeshFilter>();
                if (mesh != null && mesh.sharedMesh != null)
                {
                    Undo.RecordObject(box, "Fit finish-line BoxCollider");
                    box.center = mesh.sharedMesh.bounds.center;
                    // Flat meshes still need a nonzero trigger volume.
                    box.size = Vector3.Max(mesh.sharedMesh.bounds.size, Vector3.one * 0.01f);
                }
                else Debug.LogWarning("Game Factory: missing FinishLine Collider; added a BoxCollider without a local mesh. Verify its bounds manually.", finishObject);
                RecordPrefabChange(box);
                finishColliders = new Collider[] { box };
            }
            foreach (Collider collider in finishColliders)
            {
                if (collider is MeshCollider mesh && !mesh.convex)
                {
                    Undo.RecordObject(mesh, "Make finish-line trigger mesh convex");
                    mesh.convex = true;
                    RecordPrefabChange(mesh);
                    changed = true;
                    Debug.LogWarning("Game Factory: verify convex FinishLine mesh cooking and trigger shape in Unity.", mesh);
                }
                if (!collider.enabled || !collider.isTrigger)
                {
                    Undo.RecordObject(collider, "Configure finish-line trigger");
                    collider.enabled = true;
                    collider.isTrigger = true;
                    RecordPrefabChange(collider);
                    changed = true;
                }
            }
            if (!player.isActiveAndEnabled || !gameManager.isActiveAndEnabled ||
                !levelManager.isActiveAndEnabled || !finishLine.isActiveAndEnabled)
                Debug.LogWarning("Game Factory: a required component/object is inactive or disabled; its activation was preserved. Verify before Play Mode.");
            if (!playerBody.isKinematic || playerBody.useGravity)
                Debug.LogWarning("Game Factory: Player Rigidbody differs from the runner's kinematic, gravity-free setup; settings preserved. Verify physics manually.", playerBody);
            if (changed) EditorSceneManager.MarkSceneDirty(scene);
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("Game Factory: finish-line/level-progression setup completed. " +
                (changed ? "Review and save MainGame manually." : "References and settings were already correct; no changes needed.") +
                " Build Settings and all existing Player/GameManager settings were preserved.");
        }
        catch (Exception exception)
        {
            Debug.LogError("Game Factory: partial finish-line setup failure. Attempting Undo rollback; inspect the scene before saving.");
            Debug.LogException(exception);
            try
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(undoGroup);
            }
            catch (Exception undoException) { Debug.LogException(undoException); }
        }
    }

    private static GameObject FindUniqueSceneObject(Scene scene, string name)
    {
        GameObject match = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                if (item.name != name) continue;
                if (match != null)
                {
                    Debug.LogError("Game Factory: multiple objects named " + name + "; setup cancelled without changes.");
                    return null;
                }
                match = item.gameObject;
            }
        }
        if (match == null) Debug.LogError("Game Factory: missing " + name + "; setup cancelled without changes.");
        return match;
    }

    private static void WarnAboutBuildProgression(Scene scene)
    {
        List<EditorBuildSettingsScene> enabledScenes = new List<EditorBuildSettingsScene>();
        foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
            if (entry.enabled) enabledScenes.Add(entry);
        int index = enabledScenes.FindIndex(entry => entry.path == scene.path);
        if (string.IsNullOrEmpty(scene.path) || index < 0)
            Debug.LogWarning("Game Factory: MainGame is missing/disabled in the active build scene list. Run Tools > Sajid Game Factory > Ensure MainGame In Build Scenes. Finish-line setup does not modify build configuration.");
        else if (index + 1 >= enabledScenes.Count ||
            AssetDatabase.LoadAssetAtPath<SceneAsset>(enabledScenes[index + 1].path) == null)
            Debug.LogWarning("Game Factory: no valid next enabled build scene. Finish completion can work, but LoadNextLevel needs another scene; build configuration was not changed.");
        // EditorBuildSettings.scenes includes the active Unity 6 profile override.
    }

    [MenuItem(MenuPath)]
    public static void SetupObstacleAndGameOver()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            PrefabStageUtility.GetCurrentPrefabStage() != null ||
            !scene.IsValid() || !scene.isLoaded || scene.name != "MainGame")
        {
            Debug.LogError("Game Factory: open MainGame as the active scene in Edit Mode (outside Prefab Mode) before setup.");
            return;
        }

        List<GameObject> players = new List<GameObject>();
        List<GameObject> managers = new List<GameObject>();
        List<GameObject> obstacles = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                if (item.name == "Player") players.Add(item.gameObject);
                if (item.name == "Managers") managers.Add(item.gameObject);
                if (item.name.StartsWith("Obstacle_", StringComparison.Ordinal))
                    obstacles.Add(item.gameObject);
            }
        }

        if (players.Count != 1 || managers.Count > 1)
        {
            Debug.LogError(players.Count == 0
                ? "Game Factory: missing Player; setup cancelled without changes."
                : "Game Factory: ambiguous Player or Managers names; setup cancelled without changes.");
            return;
        }

        PlayerController player = players[0].GetComponent<PlayerController>();
        Collider[] playerColliders = players[0].GetComponentsInChildren<Collider>(true);
        bool hasUsableCollider = false;
        foreach (Collider collider in playerColliders)
        {
            // A nested Rigidbody owns its own contacts, so it is not the Player body's collider.
            Rigidbody body = collider.attachedRigidbody;
            if (collider.enabled && (body == null || body.gameObject == players[0]) &&
                collider is MeshCollider playerMesh &&
                (playerMesh.sharedMesh == null || !playerMesh.convex))
            {
                Debug.LogError("Game Factory: Player MeshCollider needs a valid mesh and Convex enabled for a kinematic Rigidbody. Fix its shape manually; setup cancelled without changes.", collider);
                return;
            }
            if (collider.enabled && collider.gameObject.activeInHierarchy &&
                (body == null || body.gameObject == players[0])) hasUsableCollider = true;
        }
        if (player == null || !hasUsableCollider)
        {
            Debug.LogError("Game Factory: Player requires PlayerController and an enabled, active 3D Collider belonging to its Rigidbody. Setup cancelled without changes.", players[0]);
            return;
        }
        if (!HasCompatibleReference(typeof(GameManager), "playerController", typeof(PlayerController)) ||
            !HasCompatibleReference(typeof(Obstacle), "gameManager", typeof(GameManager)))
        {
            Debug.LogError("Game Factory: incompatible serialized script references; expected GameManager.playerController and Obstacle.gameManager. Setup cancelled without changes.");
            return;
        }

        foreach (GameObject obstacleObject in obstacles)
        {
            foreach (MeshCollider meshCollider in obstacleObject.GetComponents<MeshCollider>())
            {
                if (meshCollider.sharedMesh == null)
                {
                    Debug.LogError("Game Factory: obstacle MeshCollider has no mesh; assign a valid mesh or replace it with a primitive collider. Setup cancelled without changes.", meshCollider);
                    return;
                }
            }
        }

        // Other GameManagers would make the intended game-state owner ambiguous.
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (GameManager existing in root.GetComponentsInChildren<GameManager>(true))
            {
                if (managers.Count == 0 || existing.gameObject != managers[0])
                {
                    Debug.LogError("Game Factory: GameManager already exists outside Managers. Resolve ownership before setup; no changes made.", existing);
                    return;
                }
            }
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup Obstacle & Game Over");
        bool changed = false;
        try
        {
            GameObject managerObject;
            if (managers.Count == 0)
            {
                managerObject = new GameObject("Managers");
                Undo.RegisterCreatedObjectUndo(managerObject, "Create Managers");
                SceneManager.MoveGameObjectToScene(managerObject, scene);
                // A new root has identity position, rotation and scale.
                changed = true;
                Debug.LogWarning("Game Factory: missing Managers; created it at the scene origin.", managerObject);
            }
            else managerObject = managers[0];

            GameManager gameManager = GetOrAdd<GameManager>(managerObject, ref changed);
            SetReference(gameManager, "playerController", player, ref changed);
            Rigidbody rigidbody = GetOrAdd<Rigidbody>(players[0], ref changed);
            if (rigidbody.useGravity || !rigidbody.isKinematic || !rigidbody.detectCollisions)
            {
                Undo.RecordObject(rigidbody, "Configure Player Rigidbody");
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = true;
                RecordPrefabChange(rigidbody);
                changed = true;
            }

            foreach (GameObject obstacleObject in obstacles)
            {
                Obstacle obstacle = GetOrAdd<Obstacle>(obstacleObject, ref changed);
                SetReference(obstacle, "gameManager", gameManager, ref changed);
                Collider[] colliders = obstacleObject.GetComponents<Collider>();
                if (colliders.Length == 0)
                {
                    BoxCollider box = GetOrAdd<BoxCollider>(obstacleObject, ref changed);
                    MeshFilter mesh = obstacleObject.GetComponent<MeshFilter>();
                    if (mesh != null && mesh.sharedMesh != null)
                    {
                        Undo.RecordObject(box, "Fit obstacle BoxCollider");
                        box.center = mesh.sharedMesh.bounds.center;
                        box.size = mesh.sharedMesh.bounds.size;
                    }
                    else Debug.LogWarning("Game Factory: added a BoxCollider without a local mesh; verify its bounds manually.", obstacleObject);
                    RecordPrefabChange(box);
                    colliders = new Collider[] { box };
                    changed = true;
                }
                foreach (Collider collider in colliders)
                {
                    if (collider is MeshCollider meshCollider && !meshCollider.convex)
                    {
                        Undo.RecordObject(meshCollider, "Make obstacle trigger mesh convex");
                        meshCollider.convex = true;
                        RecordPrefabChange(meshCollider);
                        changed = true;
                        Debug.LogWarning("Game Factory: convex MeshCollider cooking and collision shape require Unity verification.", meshCollider);
                    }
                    if (!collider.enabled || !collider.isTrigger)
                    {
                        Undo.RecordObject(collider, "Configure obstacle trigger");
                        collider.enabled = true;
                        collider.isTrigger = true;
                        RecordPrefabChange(collider);
                        changed = true;
                    }
                }
                if (!obstacleObject.activeInHierarchy || !obstacle.enabled)
                    Debug.LogWarning("Game Factory: obstacle is inactive or its Obstacle component is disabled; left that state unchanged.", obstacleObject);
            }

            if (obstacles.Count == 0)
                Debug.LogWarning("Game Factory: no scene objects named Obstacle_* found; Player and Managers setup completed only.");
            if (!player.isActiveAndEnabled || !gameManager.isActiveAndEnabled)
                Debug.LogWarning("Game Factory: PlayerController or GameManager is inactive/disabled; verify activation before Play Mode.");
            if (changed) EditorSceneManager.MarkSceneDirty(scene);
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("Game Factory: obstacle/game-over setup completed for " + obstacles.Count +
                " obstacle(s). " + (changed ? "Review the scene and save it manually." : "Settings were already correct; no changes needed."));
        }
        catch (Exception exception)
        {
            Debug.LogError("Game Factory: partial setup failure. Attempting to undo this operation; inspect the scene and Console before saving.");
            Debug.LogException(exception);
            try
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(undoGroup);
            }
            catch (Exception undoException) { Debug.LogException(undoException); }
        }
    }

    private static bool HasCompatibleReference(Type owner, string fieldName, Type valueType)
    {
        FieldInfo field = owner.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return field != null && field.FieldType.IsAssignableFrom(valueType) &&
            (field.IsPublic || Attribute.IsDefined(field, typeof(SerializeField)));
    }

    private static T GetOrAdd<T>(GameObject target, ref bool changed) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component != null) return component;
        component = Undo.AddComponent<T>(target);
        if (component == null) throw new InvalidOperationException("Could not add " + typeof(T).Name + " to " + target.name);
        changed = true;
        return component;
    }

    private static void SetReference(Component owner, string name, UnityEngine.Object value, ref bool changed)
    {
        SerializedObject serialized = new SerializedObject(owner);
        serialized.Update();
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            throw new InvalidOperationException("Incompatible Inspector reference: " + owner.GetType().Name + "." + name);
        if (property.objectReferenceValue == value) return;
        Undo.RecordObject(owner, "Assign " + name);
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        RecordPrefabChange(owner);
        changed = true;
    }

    private static void RecordPrefabChange(UnityEngine.Object target)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(target))
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
}
