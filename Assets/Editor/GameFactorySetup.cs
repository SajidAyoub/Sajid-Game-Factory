using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
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
        foreach (EditorBuildSettingsScene entry in ReadActiveBuildScenesReadOnly())
            if (entry != null && entry.enabled) enabledScenes.Add(entry);
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

    private const string MenuRoot = "Tools/Sajid Game Factory/";
    private static readonly Type[] CoreManagerTypes =
    {
        typeof(GameManager), typeof(LevelManager), typeof(PauseManager), typeof(SaveManager),
        typeof(AudioManager), typeof(SettingsManager), typeof(RewardManager), typeof(DailyRewardManager),
        typeof(ShopManager), typeof(SkinManager), typeof(MissionManager), typeof(AnalyticsManager),
        typeof(AdsManager), typeof(RemoteConfigManager)
    };
    private static readonly string[] PanelFields = { "mainMenuPanel", "gameplayHUDPanel", "pauseMenuPanel", "gameOverPanel", "levelCompletePanel", "settingsPanel" };
    private static readonly string[] PanelNames = { "MainMenuPanel", "GameplayHUDPanel", "PauseMenuPanel", "GameOverPanel", "LevelCompletePanel", "SettingsPanel" };
    private static readonly string[] TextFields = { "scoreText", "bestScoreText", "levelText", "coinCountText" };
    private static readonly string[] TextNames = { "ScoreText", "BestScoreText", "LevelText", "CoinCountText" };

    private sealed class SetupContext
    {
        public Scene Scene;
        public GameObject Managers;
        public bool Changed;
        public readonly Dictionary<Type, Component> Components = new Dictionary<Type, Component>();
        public Component Get(Type type) => Components.TryGetValue(type, out Component value) ? value : null;
    }

    [MenuItem(MenuRoot + "Setup Core Managers")]
    public static void SetupCoreManagers()
    {
        RunManagerSetup("Core Managers", CoreManagerTypes, WireManagerDependencies);
    }

    [MenuItem(MenuRoot + "Setup Audio & Settings")]
    public static void SetupAudioAndSettings()
    {
        RunManagerSetup("Audio & Settings", new[] { typeof(SaveManager), typeof(AudioManager), typeof(SettingsManager) }, context =>
        {
            Wire(context, typeof(AudioManager), "saveManager", typeof(SaveManager));
            Wire(context, typeof(SettingsManager), "saveManager", typeof(SaveManager));
            Wire(context, typeof(SettingsManager), "audioManager", typeof(AudioManager));
            AudioManager audio = context.Get(typeof(AudioManager)) as AudioManager;
            if (audio == null) return;
            AudioSource music = ReadReference(audio, "musicSource") as AudioSource;
            AudioSource sfx = ReadReference(audio, "sfxSource") as AudioSource;
            if (music != null && music == sfx)
            {
                Debug.LogWarning("Game Factory: Music and SFX reference the same AudioSource. Assign distinct sources manually; configured references preserved.", audio);
                return;
            }
            var unused = new List<AudioSource>();
            foreach (AudioSource source in audio.GetComponents<AudioSource>())
                if (source != music && source != sfx) unused.Add(source);
            int missing = (music == null ? 1 : 0) + (sfx == null ? 1 : 0);
            // Names/order cannot identify the role of an unassigned user AudioSource.
            // One remaining source for one empty slot is the only unambiguous reuse.
            if (missing > 0 && unused.Count > 0 && !(unused.Count == 1 && missing == 1))
            {
                Debug.LogWarning("Game Factory: unassigned AudioSources have ambiguous roles. Assign AudioManager Music/SFX references manually; no sources added or changed.", audio);
                return;
            }
            if (music == null) PrepareAudioSource(context, audio, "musicSource", true, unused);
            if (sfx == null) PrepareAudioSource(context, audio, "sfxSource", false, unused);
            WarnConfiguredSource(music, true);
            WarnConfiguredSource(sfx, false);
            Debug.Log("Game Factory: audio clips, volume, mixer routing and existing source settings were preserved. Supply clips and test saved settings manually.");
        });
    }

    private static void PrepareAudioSource(SetupContext context, AudioManager owner, string field, bool music, List<AudioSource> unused)
    {
        SerializedProperty property = new SerializedObject(owner).FindProperty(field);
        if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            throw new InvalidOperationException("Incompatible AudioManager source field: " + field);
        if (property.objectReferenceInstanceIDValue != 0)
        {
            Debug.LogWarning("Game Factory: broken audio reference preserved; repair " + field + " manually.", owner);
            return;
        }
        AudioSource source;
        if (unused.Count == 1)
        {
            source = unused[0];
            unused.Clear();
            WarnConfiguredSource(source, music);
        }
        else
        {
            source = Undo.AddComponent<AudioSource>(owner.gameObject);
            if (source == null) throw new InvalidOperationException("Could not create an AudioSource.");
            Undo.RecordObject(source, "Configure new " + (music ? "Music" : "SFX") + " AudioSource");
            source.playOnAwake = false;
            source.loop = music;
            source.spatialBlend = 0f;
            RecordPrefabChange(source);
            context.Changed = true;
        }
        AssignMissingReference(context, owner, field, source);
    }

    private static void WarnConfiguredSource(AudioSource source, bool music)
    {
        if (source != null && (source.playOnAwake || source.spatialBlend != 0f || source.loop != music))
            Debug.LogWarning("Game Factory: existing " + (music ? "Music" : "SFX") +
                " source differs from recommended Play On Awake off, Spatial Blend 0, and Loop " + music +
                ". User configuration preserved; review manually.", source);
    }

    [MenuItem(MenuRoot + "Setup Economy Systems")]
    public static void SetupEconomySystems()
    {
        RunManagerSetup("Economy Systems", new[] { typeof(RewardManager), typeof(DailyRewardManager),
            typeof(ShopManager), typeof(SkinManager), typeof(MissionManager) }, context =>
        {
            Wire(context, typeof(DailyRewardManager), "rewardManager", typeof(RewardManager));
            Wire(context, typeof(ShopManager), "rewardManager", typeof(RewardManager));
            Wire(context, typeof(MissionManager), "rewardManager", typeof(RewardManager));
            WarnEmptyCatalog(context.Get(typeof(ShopManager)), "items", "Configure shop IDs/prices manually; no entries or prices invented.");
            WarnEmptyCatalog(context.Get(typeof(SkinManager)), "availableSkinIds", "Configure available skin IDs manually; the existing default is preserved.");
            WarnEmptyCatalog(context.Get(typeof(MissionManager)), "activeMissions", "Create/configure MissionDefinition assets and assign them manually; no missions generated.");
            Debug.Log("Game Factory: economy balancing and PlayerPrefs were not touched; validation will report malformed/duplicate catalog data.");
        });
    }

    private static void WarnEmptyCatalog(Component owner, string field, string guidance)
    {
        if (owner == null) return;
        SerializedProperty property = new SerializedObject(owner).FindProperty(field);
        if (property == null || !property.isArray)
            throw new InvalidOperationException("Incompatible catalog API: " + owner.GetType().Name + "." + field);
        if (property.arraySize == 0) Debug.LogWarning("Game Factory: " + owner.GetType().Name + " has no " + field + ". " + guidance, owner);
    }

    [MenuItem(MenuRoot + "Setup Services Placeholders")]
    public static void SetupServicesPlaceholders()
    {
        RunManagerSetup("Services Placeholders", new[] { typeof(AnalyticsManager), typeof(AdsManager), typeof(RemoteConfigManager) }, context =>
        {
            Wire(context, typeof(AdsManager), "analyticsManager", typeof(AnalyticsManager));
            Component ads = context.Get(typeof(AdsManager));
            if (ads != null && RequiredProperty(ads, "enableMockAds", SerializedPropertyType.Boolean).boolValue)
                Debug.LogWarning("Game Factory: existing mock ads are enabled. Setting preserved; turn it off before release. No currency adapter is installed.", ads);
            Debug.Log("Game Factory: optional local service placeholders only. New ads use the runtime disabled default; no provider, network fetch, tracking request or currency grant was invoked.");
        });
    }

    // Reads serialized data only: never calls gameplay getters that may repair PlayerPrefs.
    private static UnityEngine.Object ReadReference(Component owner, string field)
    {
        if (owner == null) return null;
        SerializedProperty property = new SerializedObject(owner).FindProperty(field);
        if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            throw new InvalidOperationException("Incompatible serialized reference: " + owner.GetType().Name + "." + field);
        return property.objectReferenceValue;
    }

    private static SerializedProperty RequiredProperty(Component owner, string field, SerializedPropertyType type)
    {
        SerializedProperty property = new SerializedObject(owner).FindProperty(field);
        if (property == null || property.propertyType != type)
            throw new InvalidOperationException("Incompatible serialized field: " + owner.GetType().Name + "." + field);
        return property;
    }

    [MenuItem(MenuRoot + "Prepare UI Architecture")]
    public static void PrepareUIArchitecture()
    {
        RunManagerSetup("UI Architecture", new[] { typeof(UIManager), typeof(HUDController) }, context =>
        {
            Component ui = context.Get(typeof(UIManager));
            Component hud = context.Get(typeof(HUDController));
            for (int i = 0; i < PanelFields.Length; i++)
            {
                if (ui == null || ReadReference(ui, PanelFields[i]) != null) continue;
                GameObject panel = UniqueNamedObject(context.Scene, PanelNames[i]);
                if (panel != null && SafePanel(panel, ui, out string reason))
                    AssignMissingReference(context, ui, PanelFields[i], panel);
                else Debug.LogWarning("Game Factory UI checklist: create/assign one independent " + PanelNames[i] +
                    " RectTransform panel under an existing Canvas. Ambiguous/unsafe objects are not wired; no visuals created.");
            }
            for (int i = 0; i < TextFields.Length; i++)
            {
                if (hud == null || ReadReference(hud, TextFields[i]) != null) continue;
                GameObject textObject = UniqueNamedObject(context.Scene, TextNames[i]);
                UnityEngine.UI.Text text = textObject != null ? textObject.GetComponent<UnityEngine.UI.Text>() : null;
                if (text != null && text.GetComponentInParent<Canvas>(true) != null)
                    AssignMissingReference(context, hud, TextFields[i], text);
                else Debug.LogWarning("Game Factory UI checklist: assign existing " + TextNames[i] +
                    " Unity UI Text, or manually bind the HUD string UnityEvent to a TMP text setter. No text/layout/fonts modified.");
            }
            Debug.Log("Game Factory UI checklist: configure buttons, initial state and gameplay event subscriptions manually. No C# event bridge, designed UI or TMP dependency was added.");
        });
    }

    private static bool SafePanel(GameObject panel, Component owner, out string reason)
    {
        reason = null;
        if (panel.GetComponent<RectTransform>() == null || panel.GetComponentInParent<Canvas>(true) == null)
            reason = "Panel requires RectTransform and parent Canvas.";
        else if (owner.transform.IsChildOf(panel.transform)) reason = "Panel would disable UIManager itself.";
        else
        {
            foreach (Type type in CoreManagerTypes)
                if (panel.GetComponentsInChildren(type, true).Length > 0) reason = "Panel would disable a gameplay/service manager.";
            if (panel.GetComponentInChildren<PlayerController>(true) != null ||
                panel.GetComponentInChildren<CameraFollow>(true) != null ||
                panel.GetComponentInChildren<ScoreManager>(true) != null)
                reason = "Panel would disable Player, CameraFollow or ScoreManager.";
            foreach (string field in PanelFields)
            {
                GameObject other = ReadReference(owner, field) as GameObject;
                if (other != null && (other == panel || other.transform.IsChildOf(panel.transform) || panel.transform.IsChildOf(other.transform)))
                    reason = "Panels must be distinct independent objects, not nested.";
            }
        }
        return reason == null;
    }

    [MenuItem(MenuRoot + "Setup / Repair Entire Game")]
    public static void SetupOrRepairEntireGame()
    {
        if (!TryGetSetupScene(out Scene scene)) return;
        var preflight = new ValidationReport();
        try
        {
        ValidatePlayerAndCamera(scene, preflight);
        CheckManager(scene, typeof(ScoreManager), preflight, true);
        ValidateContacts(scene, "Coin_", typeof(Coin), "scoreManager", typeof(ScoreManager), preflight);
        // Legacy commands intentionally own their specific references. Refuse the
        // master pass when that would overwrite another configured working owner.
        Component game = UniqueComponent(scene, typeof(GameManager));
        Component level = UniqueComponent(scene, typeof(LevelManager));
        GameObject player = UniqueNamedObject(scene, "Player");
        CheckExistingTarget(game, "playerController", player != null ? player.GetComponent<PlayerController>() : null, preflight);
        CheckExistingTarget(level, "gameManager", game, preflight);
        foreach (GameObject item in SceneObjects(scene))
        {
            if (item.name.StartsWith("Obstacle_", StringComparison.Ordinal))
                CheckExistingTarget(item.GetComponent<Obstacle>(), "gameManager", game, preflight);
            if (item.name == "FinishLine") CheckExistingTarget(item.GetComponent<FinishLine>(), "levelManager", level, preflight);
        }
        foreach (Type type in CoreManagerTypes)
            if (SceneComponents(scene, type).Count > 1) preflight.Error("Duplicate " + type.Name + " owners; master pass will not guess.");
        if (SceneObjects(scene).FindAll(item => item.name == "Managers").Count > 1)
            preflight.Error("Multiple Managers objects; master pass will not guess.");
        foreach (Component owner in new[] { game, level })
            if (owner != null && owner.gameObject.name != "Managers")
                preflight.Error(owner.GetType().Name + " lives outside Managers; legacy setup would not safely reuse it. Resolve ownership manually.");
        }
        catch (Exception exception) { preflight.Error("Master preflight could not finish: " + exception.Message); }
        if (preflight.Errors > 0)
        {
            preflight.Warning("Master setup stopped at verification before any changes. Resolve critical prerequisites manually.");
            preflight.Log();
            return;
        }
        Debug.Log("Game Factory: master setup preserves gameplay values and visuals. Scene steps have separate Undo groups; the build-list step has its own Undo limitations. Individual failed steps may leave later systems incomplete.");
        SetupObstacleAndGameOver();
        EnsureMainGameInBuildScenes();
        SetupFinishLineAndLevelProgression();
        SetupCoreManagers();
        SetupAudioAndSettings();
        SetupEconomySystems();
        SetupServicesPlaceholders();
        PrepareUIArchitecture();
        ValidateCurrentGameSetup();
    }

    private static void CheckExistingTarget(Component owner, string field, UnityEngine.Object expected, ValidationReport report)
    {
        if (owner == null) return;
        UnityEngine.Object actual = ReadReference(owner, field);
        if (actual != null && actual != expected)
            report.Error(owner.GetType().Name + "." + field + " has another configured target; preserved, repair manually before running the master command.");
    }

    private sealed class ValidationReport
    {
        public int Errors;
        public int Warnings;
        private readonly StringBuilder lines = new StringBuilder("GAME FACTORY VALIDATION\n\n");
        public void Pass(string text) => lines.AppendLine("PASS: " + text);
        public void Warning(string text) { Warnings++; lines.AppendLine("WARNING: " + text); }
        public void Error(string text) { Errors++; lines.AppendLine("ERROR: " + text); }
        public void Log()
        {
            lines.AppendLine("\n" + (Errors > 0 ? "CRITICAL ERRORS FOUND" : Warnings > 0 ? "SETUP INCOMPLETE" : "READY FOR PLAY TEST"));
            if (Errors > 0) Debug.LogError(lines.ToString());
            else if (Warnings > 0) Debug.LogWarning(lines.ToString());
            else Debug.Log(lines.ToString());
        }
    }

    [MenuItem(MenuRoot + "Validate Current Game Setup")]
    public static void ValidateCurrentGameSetup()
    {
        var report = new ValidationReport();
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.path != MainGameScenePath ||
            EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            report.Error("Open Assets/Scenes/MainGame.unity in Edit Mode, outside Prefab Mode, for read-only validation.");
            report.Log();
            return;
        }
        try
        {
            ValidatePlayerAndCamera(scene, report);
            foreach (Type type in CoreManagerTypes)
                CheckManager(scene, type, report, type == typeof(GameManager) || type == typeof(LevelManager));
            CheckManager(scene, typeof(ScoreManager), report, true);
            CheckManager(scene, typeof(UIManager), report, false);
            CheckManager(scene, typeof(HUDController), report, false);
            var containers = SceneObjects(scene).FindAll(item => item.name == "Managers");
            if (containers.Count != 1) report.Error("Expected exactly one Managers object; found " + containers.Count + ".");
            ValidateContacts(scene, "Coin_", typeof(Coin), "scoreManager", typeof(ScoreManager), report);
            ValidateContacts(scene, "Obstacle_", typeof(Obstacle), "gameManager", typeof(GameManager), report);
            ValidateContacts(scene, "FinishLine", typeof(FinishLine), "levelManager", typeof(LevelManager), report);
            ValidateDependencies(scene, report);
            ValidateBuildScenes(scene, report);
            ValidateAudio(scene, report);
            ValidateEconomy(scene, report);
            ValidateServices(scene, report);
            ValidateUI(scene, report);
            foreach (Component external in ExternalManagerOwners(scene))
                report.Error("Another loaded scene owns " + external.GetType().Name + "; resolve additive-scene ownership before setup/play testing.");
            report.Pass("Read-only inspection completed: no scene, asset, PlayerPrefs, gameplay method, provider or configuration mutation requested.");
            report.Pass("Structural readiness only; Unity compilation, Play Mode, physics and player builds must be tested separately.");
        }
        catch (Exception exception) { report.Error("Validation could not finish: " + exception.Message); }
        report.Log();
    }

    private static Component CheckManager(Scene scene, Type type, ValidationReport report, bool critical)
    {
        List<Component> matches = SceneComponents(scene, type);
        if (matches.Count == 0)
        {
            if (critical) report.Error("Missing " + type.Name + ".");
            else report.Warning("Missing " + type.Name + "; run the relevant setup command.");
            return null;
        }
        if (matches.Count > 1) { report.Error("Duplicate " + type.Name + " owners (" + matches.Count + ")."); return null; }
        Component owner = matches[0];
        if (owner is Behaviour behaviour && !behaviour.isActiveAndEnabled)
            report.Warning(type.Name + " is inactive/disabled; its existing state is preserved.");
        else report.Pass(type.Name + " has one active owner.");
        return owner;
    }

    private static void ValidatePlayerAndCamera(Scene scene, ValidationReport report)
    {
        GameObject player = UniqueNamedObject(scene, "Player");
        if (player == null) report.Error("Missing or ambiguous Player.");
        else
        {
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller == null || !controller.isActiveAndEnabled) report.Error("Player needs an active PlayerController.");
            else
            {
                ValidateFloat(controller, "forwardSpeed", 0.001f, float.MaxValue, report);
                ValidateFloat(controller, "horizontalSpeed", 0.001f, float.MaxValue, report);
                report.Pass("PlayerController exists; movement settings inspected without changing them.");
            }
            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body == null) report.Error("Player is missing Rigidbody.");
            else if (!body.isKinematic || body.useGravity || !body.detectCollisions)
                report.Error("Player Rigidbody requires kinematic, gravity off, collision detection on.");
            else report.Pass("Player Rigidbody settings match Transform runner movement.");
            bool usable = false;
            foreach (Collider collider in player.GetComponentsInChildren<Collider>(true))
            {
                if (!collider.enabled || !collider.gameObject.activeInHierarchy || body == null || collider.attachedRigidbody != body) continue;
                usable = true;
                ValidateCollider(collider, false, report);
            }
            if (!usable) report.Error("Player has no usable active 3D Collider owned by its Rigidbody.");
            else report.Pass("Player has a usable 3D Collider.");
        }
        var cameras = SceneComponents(scene, typeof(Camera)).FindAll(component => component.gameObject.CompareTag("MainCamera"));
        if (cameras.Count != 1) report.Error("Expected one Main Camera in MainGame; found " + cameras.Count + ".");
        else
        {
            CameraFollow follow = cameras[0].GetComponent<CameraFollow>();
            if (follow == null || !follow.isActiveAndEnabled) report.Error("Main Camera requires active CameraFollow.");
            else
            {
                if (player == null || ReadReference(follow, "target") != player.transform) report.Error("CameraFollow target does not match Player Transform.");
                else report.Pass("CameraFollow targets Player; camera composition was preserved.");
                ValidateFloat(follow, "smoothSpeed", 0.001f, float.MaxValue, report);
            }
            if (!(cameras[0] as Camera).isActiveAndEnabled) report.Error("Main Camera is inactive/disabled.");
        }
    }

    private static void ValidateContacts(Scene scene, string name, Type script, string field, Type target, ValidationReport report)
    {
        var objects = SceneObjects(scene).FindAll(item => name == "FinishLine" ? item.name == name : item.name.StartsWith(name, StringComparison.Ordinal));
        if (objects.Count == 0) { report.Warning("No " + name + " objects found."); return; }
        if (name == "FinishLine" && objects.Count > 1) report.Error("Multiple FinishLine objects; resolve ambiguous names.");
        foreach (GameObject item in objects)
        {
            Component owner = item.GetComponent(script);
            if (owner == null) report.Error(item.name + " missing " + script.Name + ".");
            else
            {
                if (item.GetComponents(script).Length > 1) report.Error(item.name + " has duplicate " + script.Name + " components.");
                CheckReference(owner, field, UniqueComponent(scene, target), report, true);
                if (owner is Behaviour behaviour && !behaviour.isActiveAndEnabled) report.Warning(item.name + " is inactive/disabled.");
                if (owner is Coin) ValidateInteger(owner, "scoreValue", 1, int.MaxValue, report);
            }
            Collider[] colliders = item.GetComponents<Collider>();
            if (colliders.Length == 0) report.Error(item.name + " missing local 3D Collider; collider-less roots are not assumed to receive trigger messages.");
            foreach (Collider collider in colliders)
            {
                ValidateCollider(collider, true, report);
                GameObject player = UniqueNamedObject(scene, "Player");
                if (player != null)
                {
                    bool acceptsPlayerLayer = false;
                    foreach (Collider playerCollider in player.GetComponentsInChildren<Collider>(true))
                        if (playerCollider.enabled && !Physics.GetIgnoreLayerCollision(collider.gameObject.layer, playerCollider.gameObject.layer)) acceptsPlayerLayer = true;
                    if (!acceptsPlayerLayer) report.Error(item.name + " layer collision matrix blocks all enabled Player collider layers.");
                }
            }
        }
        report.Pass("Inspected " + objects.Count + " " + name + " object(s), without modifying their setup.");
    }

    private static void ValidateCollider(Collider collider, bool trigger, ValidationReport report)
    {
        string label = collider.gameObject.name + " " + collider.GetType().Name;
        if (!collider.enabled || !collider.gameObject.activeInHierarchy) report.Warning(label + " is inactive/disabled.");
        if (trigger && !collider.isTrigger) report.Error(label + " requires Is Trigger for this setup.");
        if (collider is MeshCollider mesh && (mesh.sharedMesh == null || ((trigger || collider.attachedRigidbody != null) && !mesh.convex)))
            report.Error(label + " requires a valid mesh and Convex for triggers/runner Rigidbody.");
        if (collider is BoxCollider box && (box.size.x <= 0f || box.size.y <= 0f || box.size.z <= 0f)) report.Error(label + " has zero/negative dimensions.");
        if (collider is SphereCollider sphere && sphere.radius <= 0f) report.Error(label + " has invalid radius.");
        if (collider is CapsuleCollider capsule && (capsule.radius <= 0f || capsule.height <= 0f)) report.Error(label + " has invalid dimensions.");
        if (collider.transform.lossyScale.x == 0f || collider.transform.lossyScale.y == 0f || collider.transform.lossyScale.z == 0f)
            report.Error(label + " has a zero-scale transform; geometry must be fixed manually.");
    }

    private static void ValidateDependencies(Scene scene, ValidationReport report)
    {
        GameObject player = UniqueNamedObject(scene, "Player");
        CheckReference(UniqueComponent(scene, typeof(GameManager)), "playerController", player != null ? player.GetComponent<PlayerController>() : null, report, true);
        ValidateDependency(scene, typeof(GameManager), "pauseManager", typeof(PauseManager), report);
        ValidateDependency(scene, typeof(LevelManager), "gameManager", typeof(GameManager), report);
        ValidateDependency(scene, typeof(LevelManager), "pauseManager", typeof(PauseManager), report);
        ValidateDependency(scene, typeof(AudioManager), "saveManager", typeof(SaveManager), report);
        ValidateDependency(scene, typeof(SettingsManager), "saveManager", typeof(SaveManager), report);
        ValidateDependency(scene, typeof(SettingsManager), "audioManager", typeof(AudioManager), report);
        ValidateDependency(scene, typeof(DailyRewardManager), "rewardManager", typeof(RewardManager), report);
        ValidateDependency(scene, typeof(ShopManager), "rewardManager", typeof(RewardManager), report);
        ValidateDependency(scene, typeof(MissionManager), "rewardManager", typeof(RewardManager), report);
        ValidateDependency(scene, typeof(AdsManager), "analyticsManager", typeof(AnalyticsManager), report);
    }

    private static void ValidateDependency(Scene scene, Type owner, string field, Type target, ValidationReport report)
    {
        CheckReference(UniqueComponent(scene, owner), field, UniqueComponent(scene, target), report, owner == typeof(LevelManager));
    }

    private static void CheckReference(Component owner, string field, UnityEngine.Object expected, ValidationReport report, bool critical)
    {
        if (owner == null) return;
        UnityEngine.Object actual = ReadReference(owner, field);
        if (actual == null || expected == null || actual != expected)
        {
            string message = owner.GetType().Name + "." + field + " missing or not targeting the unique expected scene owner.";
            if (critical) report.Error(message); else report.Warning(message);
        }
        else report.Pass(owner.GetType().Name + "." + field + " is wired.");
    }

    private static EditorBuildSettingsScene[] ReadActiveBuildScenesReadOnly()
    {
        BuildProfile profile = BuildProfile.GetActiveBuildProfile();
        if (profile == null || !profile.overrideGlobalScenes) return EditorBuildSettings.globalScenes;
        // Do not call profile.scenes / EditorBuildSettings.scenes here: Unity 6's
        // profile getter repairs/removes invalid records and can mark the asset dirty.
        SerializedProperty scenes = new SerializedObject(profile).FindProperty("m_Scenes");
        if (scenes == null || !scenes.isArray) throw new InvalidOperationException("Unsupported active Build Profile serialization.");
        var snapshot = new EditorBuildSettingsScene[scenes.arraySize];
        for (int i = 0; i < scenes.arraySize; i++)
        {
            SerializedProperty entry = scenes.GetArrayElementAtIndex(i);
            SerializedProperty path = entry.FindPropertyRelative("m_path");
            SerializedProperty enabled = entry.FindPropertyRelative("m_enabled");
            if (path == null || enabled == null) throw new InvalidOperationException("Malformed active Build Profile scene entry.");
            snapshot[i] = new EditorBuildSettingsScene(path.stringValue, enabled.boolValue);
        }
        return snapshot;
    }

    private static void ValidateBuildScenes(Scene scene, ValidationReport report)
    {
        EditorBuildSettingsScene[] snapshot = ReadActiveBuildScenesReadOnly();
        var enabled = new List<EditorBuildSettingsScene>();
        int mainMatches = 0;
        foreach (EditorBuildSettingsScene entry in snapshot)
        {
            if (entry == null) { report.Warning("Malformed build-scene entry; repair build configuration manually."); continue; }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.path) == null) report.Warning("Missing build-scene asset; repair build configuration manually.");
            if (entry.path == scene.path) mainMatches++;
            if (entry.enabled) enabled.Add(entry);
        }
        if (mainMatches > 1) report.Error("Duplicate MainGame build entries.");
        int index = enabled.FindIndex(entry => entry.path == scene.path);
        if (index < 0) report.Warning("MainGame missing/disabled in active build scenes; run Ensure MainGame In Build Scenes.");
        else
        {
            report.Pass("MainGame enabled at build index " + index + ".");
            if (index + 1 >= enabled.Count || AssetDatabase.LoadAssetAtPath<SceneAsset>(enabled[index + 1].path) == null)
                report.Warning("No valid next enabled scene; finish completion can work, but next-level loading cannot be tested yet. Add another level manually.");
            else report.Pass("Next enabled scene: " + enabled[index + 1].path + ". Verify it is a gameplay level.");
        }
    }

    private static void ValidateAudio(Scene scene, ValidationReport report)
    {
        Component audio = UniqueComponent(scene, typeof(AudioManager));
        if (audio == null) return;
        AudioSource music = ReadReference(audio, "musicSource") as AudioSource;
        AudioSource sfx = ReadReference(audio, "sfxSource") as AudioSource;
        if (music == null || sfx == null || music == sfx) { report.Warning("Assign distinct Music and SFX AudioSources; run Setup Audio & Settings or resolve ambiguous sources manually."); return; }
        foreach (AudioSource source in new[] { music, sfx })
        {
            if (source.gameObject.scene != scene || !source.isActiveAndEnabled) report.Warning("AudioSource is outside MainGame or inactive/disabled.");
            if (source.playOnAwake || source.spatialBlend != 0f) report.Warning("AudioSource should have Play On Awake off and Spatial Blend 0 for this prototype.");
        }
        if (!music.loop || sfx.loop) report.Warning("Recommended Music Loop on and SFX Loop off; existing values were preserved.");
        if (music.clip == null) report.Warning("Music asset not assigned; supply clips manually. SFX assets are supplied by future callers.");
        var listeners = SceneComponents(scene, typeof(AudioListener)).FindAll(item => (item as AudioListener).isActiveAndEnabled);
        if (listeners.Count != 1) report.Warning("Expected exactly one active AudioListener; found " + listeners.Count + ".");
        report.Pass("AudioSource references inspected; no clips/settings/playback modified.");
    }

    private static void ValidateEconomy(Scene scene, ValidationReport report)
    {
        Component daily = UniqueComponent(scene, typeof(DailyRewardManager));
        if (daily != null) ValidateInteger(daily, "dailyRewardAmount", 1, int.MaxValue, report);
        Component skin = UniqueComponent(scene, typeof(SkinManager));
        var skinIds = new HashSet<string>(StringComparer.Ordinal);
        string defaultSkin = null;
        if (skin != null)
        {
            defaultSkin = RequiredProperty(skin, "defaultSkinId", SerializedPropertyType.String).stringValue;
            if (!ValidEconomyId(defaultSkin)) report.Warning("SkinManager defaultSkinId is invalid; runtime fallback exists but configure it deliberately.");
            else skinIds.Add(defaultSkin);
            SerializedProperty ids = new SerializedObject(skin).FindProperty("availableSkinIds");
            if (ids == null || !ids.isArray) throw new InvalidOperationException("SkinManager.availableSkinIds API mismatch.");
            if (ids.arraySize == 0) report.Warning("No optional skin IDs configured; default skin only.");
            for (int i = 0; i < ids.arraySize; i++)
            {
                string id = ids.GetArrayElementAtIndex(i).stringValue;
                if (!ValidEconomyId(id) || !skinIds.Add(id)) report.Warning("Invalid/duplicate skin ID at index " + i + ". No ownership data was changed.");
            }
        }
        Component shop = UniqueComponent(scene, typeof(ShopManager));
        if (shop != null)
        {
            SerializedProperty items = new SerializedObject(shop).FindProperty("items");
            if (items == null || !items.isArray) throw new InvalidOperationException("ShopManager.items API mismatch.");
            if (items.arraySize == 0) report.Warning("Shop catalog empty; configure intended IDs/prices manually.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < items.arraySize; i++)
            {
                SerializedProperty item = items.GetArrayElementAtIndex(i);
                SerializedProperty idProperty = item.FindPropertyRelative("itemId");
                SerializedProperty price = item.FindPropertyRelative("price");
                if (idProperty == null || price == null) { report.Warning("Malformed shop item at index " + i + "."); continue; }
                string id = idProperty.stringValue;
                if (!ValidEconomyId(id) || !ids.Add(id) || price.intValue < 0) report.Warning("Invalid/duplicate shop item or negative price at index " + i + ".");
                if (id == defaultSkin) report.Warning("Default skin must not be a paid shop entry; its ownership is always available.");
                // Generic shop items need not be skins; no arbitrary ID linkage is inferred.
            }
        }
        Component missions = UniqueComponent(scene, typeof(MissionManager));
        if (missions != null)
        {
            SerializedProperty active = new SerializedObject(missions).FindProperty("activeMissions");
            if (active == null || !active.isArray) throw new InvalidOperationException("MissionManager.activeMissions API mismatch.");
            if (active.arraySize == 0) report.Warning("No active missions; create and assign balanced MissionDefinition assets manually.");
            var identities = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < active.arraySize; i++)
            {
                MissionDefinition definition = active.GetArrayElementAtIndex(i).objectReferenceValue as MissionDefinition;
                if (definition == null || !definition.IsValid()) { report.Warning("Missing/invalid mission definition at index " + i + "."); continue; }
                if (!identities.Add(definition.MissionId)) report.Warning("Duplicate mission ID: " + definition.MissionId + ".");
                foreach (string alias in definition.GetPreviousMissionIds())
                    if (!identities.Add(alias)) report.Warning("Overlapping mission alias: " + alias + ".");
            }
        }
        report.Pass("Economy inspected without reading/repairing PlayerPrefs, changing balances, claiming rewards or generating catalog data.");
    }

    private static bool ValidEconomyId(string id)
    {
        // Mirrors the current runtime ownership contract; does not access its
        // internal RunnerPersistence type across the Editor/runtime assembly boundary.
        if (string.IsNullOrEmpty(id) || id.Length > 64) return false;
        foreach (char c in id)
            if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-' || c == '.')) return false;
        return true;
    }

    private static void ValidateServices(Scene scene, ValidationReport report)
    {
        Component ads = UniqueComponent(scene, typeof(AdsManager));
        if (ads != null)
        {
            if (RequiredProperty(ads, "enableMockAds", SerializedPropertyType.Boolean).boolValue) report.Warning("Mock ads enabled: testing only, disable for release. Validator does not complete an ad or grant currency.");
            else report.Pass("Ads use disabled default without a runtime provider; no SDK installed by this tool.");
            ValidateFloat(ads, "interstitialCooldown", 0f, 3600f, report);
            ValidateFloat(ads, "requestTimeoutSeconds", 5f, 600f, report);
            ValidateInteger(ads, "maxInterstitialsPerSession", 0, 1000, report);
        }
        Component config = UniqueComponent(scene, typeof(RemoteConfigManager));
        if (config != null)
        {
            ValidateFloat(config, "forwardSpeed", 0.1f, 100f, report);
            ValidateFloat(config, "horizontalSpeed", 0.1f, 100f, report);
            ValidateFloat(config, "difficultyMultiplier", 0.1f, 10f, report);
            ValidateFloat(config, "interstitialCooldown", 0f, 3600f, report);
            ValidateInteger(config, "dailyRewardAmount", 1, 1000000, report);
            ValidateInteger(config, "rewardedAdRewardAmount", 1, 1000000, report);
            ValidateInteger(config, "interstitialFrequency", 1, 100, report);
            report.Pass("Remote config is an optional local-default wrapper; no fetch/apply invoked.");
        }
        if (UniqueComponent(scene, typeof(AnalyticsManager)) != null) report.Pass("Analytics wrapper present; no analytics event/provider method invoked by validation.");
    }

    private static void ValidateUI(Scene scene, ValidationReport report)
    {
        Component ui = UniqueComponent(scene, typeof(UIManager));
        if (ui != null)
        {
            for (int i = 0; i < PanelFields.Length; i++)
            {
                GameObject panel = ReadReference(ui, PanelFields[i]) as GameObject;
                if (panel == null) report.Warning("Missing UI panel: " + PanelNames[i] + "; create/configure visuals manually.");
                else
                {
                    // SafePanel compares other fields; exclude this panel's own slot.
                    string reason = null;
                    if (panel.scene != scene || !SafePanelForValidation(panel, ui, PanelFields[i], out reason))
                        report.Error("Unsafe UI panel " + PanelFields[i] + ": " + (reason ?? "outside MainGame"));
                }
            }
        }
        Component hud = UniqueComponent(scene, typeof(HUDController));
        if (hud != null)
        {
            for (int i = 0; i < TextFields.Length; i++)
            {
                UnityEngine.UI.Text text = ReadReference(hud, TextFields[i]) as UnityEngine.UI.Text;
                if (text != null)
                {
                    if (text.gameObject.scene != scene || text.GetComponentInParent<Canvas>(true) == null) report.Warning("HUD " + TextFields[i] + " is outside MainGame/Canvas.");
                    else report.Pass("HUD " + TextFields[i] + " is assigned.");
                    continue;
                }
                string eventName = TextFields[i] + "Updated";
                // Inspect persistent listeners without invoking them or installing adapters.
                SerializedProperty calls = new SerializedObject(hud).FindProperty(eventName + ".m_PersistentCalls.m_Calls");
                bool bound = false;
                if (calls != null && calls.isArray)
                    for (int j = 0; j < calls.arraySize; j++)
                    {
                        SerializedProperty call = calls.GetArrayElementAtIndex(j);
                        var target = call.FindPropertyRelative("m_Target");
                        var method = call.FindPropertyRelative("m_MethodName");
                        var state = call.FindPropertyRelative("m_CallState");
                        var mode = call.FindPropertyRelative("m_Mode");
                        if (target != null && target.objectReferenceValue != null && method != null &&
                            !string.IsNullOrEmpty(method.stringValue) && state != null && state.intValue != 0 &&
                            mode != null && (mode.intValue == 0 || mode.intValue == 5))
                        {
                            MethodInfo callback = UnityEngine.Events.UnityEventBase.GetValidMethodInfo(target.objectReferenceValue, method.stringValue, new[] { typeof(string) });
                            if (callback != null && callback.ReturnType == typeof(void)) bound = true;
                        }
                    }
                if (bound) report.Pass("HUD " + eventName + " has a compatible persistent string listener; verify delivery in Unity.");
                else report.Warning("HUD " + TextFields[i] + " empty and no persistent string listener; assign Text or wire TMP manually.");
            }
        }
    }

    private static bool SafePanelForValidation(GameObject panel, Component owner, string ownField, out string reason)
    {
        reason = null;
        if (panel.GetComponent<RectTransform>() == null || panel.GetComponentInParent<Canvas>(true) == null)
            reason = "requires RectTransform and Canvas";
        else if (owner.transform.IsChildOf(panel.transform)) reason = "would disable UIManager";
        foreach (Type type in CoreManagerTypes)
            if (panel.GetComponentsInChildren(type, true).Length > 0) reason = "would disable a core manager";
        if (panel.GetComponentInChildren<PlayerController>(true) != null || panel.GetComponentInChildren<CameraFollow>(true) != null || panel.GetComponentInChildren<ScoreManager>(true) != null)
            reason = "would disable Player, CameraFollow or ScoreManager";
        foreach (string field in PanelFields)
        {
            if (field == ownField) continue;
            GameObject other = ReadReference(owner, field) as GameObject;
            if (other != null && (other == panel || other.transform.IsChildOf(panel.transform) || panel.transform.IsChildOf(other.transform))) reason = "duplicate/nested panels";
        }
        return reason == null;
    }

    private static void ValidateFloat(Component owner, string field, float min, float max, ValidationReport report)
    {
        float value = RequiredProperty(owner, field, SerializedPropertyType.Float).floatValue;
        if (float.IsNaN(value) || float.IsInfinity(value) || value < min || value > max)
            report.Warning(owner.GetType().Name + "." + field + " outside expected range; runtime guards may apply, configured value preserved.");
    }

    private static void ValidateInteger(Component owner, string field, int min, int max, ValidationReport report)
    {
        int value = RequiredProperty(owner, field, SerializedPropertyType.Integer).intValue;
        if (value < min || value > max) report.Warning(owner.GetType().Name + "." + field + " outside expected range; value preserved.");
    }

    private static bool TryGetSetupScene(out Scene scene)
    {
        scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || PrefabStageUtility.GetCurrentPrefabStage() != null ||
            !scene.IsValid() || !scene.isLoaded || scene.path != MainGameScenePath)
        {
            Debug.LogError("Game Factory: open Assets/Scenes/MainGame.unity in Edit Mode outside Prefab Mode, and wait for imports/compilation.");
            return false;
        }
        if (ExternalManagerOwners(scene).Count > 0)
        {
            Debug.LogError("Game Factory: another loaded scene contains manager owners. Close it or resolve additive-scene ownership before setup; no changes made.");
            return false;
        }
        return true;
    }

    private static List<Component> ExternalManagerOwners(Scene scene)
    {
        var owners = new List<Component>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene other = SceneManager.GetSceneAt(i);
            if (!other.IsValid() || !other.isLoaded || other == scene) continue;
            foreach (Type type in CoreManagerTypes) owners.AddRange(SceneComponents(other, type));
            foreach (Type type in new[] { typeof(ScoreManager), typeof(UIManager), typeof(HUDController) })
                owners.AddRange(SceneComponents(other, type));
        }
        return owners;
    }

    private static List<GameObject> SceneObjects(Scene scene)
    {
        var objects = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)) objects.Add(item.gameObject);
        return objects;
    }

    private static List<Component> SceneComponents(Scene scene, Type type)
    {
        var components = new List<Component>();
        foreach (GameObject root in scene.GetRootGameObjects())
            components.AddRange(root.GetComponentsInChildren(type, true));
        return components;
    }

    private static Component UniqueComponent(Scene scene, Type type)
    {
        List<Component> matches = SceneComponents(scene, type);
        return matches.Count == 1 ? matches[0] : null;
    }

    private static GameObject UniqueNamedObject(Scene scene, string name)
    {
        GameObject match = null;
        foreach (GameObject item in SceneObjects(scene))
        {
            if (item.name != name) continue;
            if (match != null) return null;
            match = item;
        }
        return match;
    }

    private static void RunManagerSetup(string label, Type[] types, Action<SetupContext> configure)
    {
        if (!TryGetSetupScene(out Scene scene)) return;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Game Factory: " + label);
        try
        {
            var context = new SetupContext { Scene = scene };
            var namedManagers = SceneObjects(scene).FindAll(item => item.name == "Managers");
            if (namedManagers.Count > 1) throw new InvalidOperationException("Multiple Managers objects; refusing to guess an owner.");
            if (namedManagers.Count == 0)
            {
                context.Managers = new GameObject("Managers");
                Undo.RegisterCreatedObjectUndo(context.Managers, "Create Managers");
                SceneManager.MoveGameObjectToScene(context.Managers, scene);
                context.Changed = true;
                Debug.LogWarning("Game Factory: missing Managers; created an identity root without changing existing objects.");
            }
            else context.Managers = namedManagers[0];

            foreach (Type type in types)
            {
                List<Component> existing = SceneComponents(scene, type);
                Component component = null;
                if (existing.Count > 1)
                    Debug.LogWarning("Game Factory: multiple " + type.Name + " owners; no additional component or guessed wiring was created.");
                else if (existing.Count == 1)
                {
                    component = existing[0];
                    if (component.gameObject != context.Managers)
                        Debug.LogWarning("Game Factory: reusing existing " + type.Name + " outside Managers; no duplicate added.", component);
                }
                else
                {
                    component = Undo.AddComponent(context.Managers, type);
                    if (component == null) throw new InvalidOperationException("Failed to add " + type.Name);
                    context.Changed = true;
                }
                context.Components[type] = component;
            }
            configure(context);
            if (context.Changed) EditorSceneManager.MarkSceneDirty(scene);
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(group);
            Debug.Log("Game Factory: " + label + " pass finished. " +
                (context.Changed ? "Review and save MainGame manually. " : "No changes needed. ") +
                "Warnings may indicate incomplete setup; run Validate Current Game Setup after preparation.");
        }
        catch (Exception exception)
        {
            Debug.LogError("Game Factory: partial " + label + " failure; attempting to undo this pass. Inspect Console/scene before saving.");
            Debug.LogException(exception);
            try { Undo.FlushUndoRecordObjects(); Undo.RevertAllDownToGroup(group); }
            catch (Exception undoException) { Debug.LogException(undoException); }
        }
    }

    private static void WireManagerDependencies(SetupContext context)
    {
        GameObject playerObject = UniqueNamedObject(context.Scene, "Player");
        Component player = playerObject != null ? playerObject.GetComponent<PlayerController>() : null;
        AssignMissingReference(context, context.Get(typeof(GameManager)), "playerController", player);
        Wire(context, typeof(GameManager), "pauseManager", typeof(PauseManager));
        Wire(context, typeof(LevelManager), "gameManager", typeof(GameManager));
        Wire(context, typeof(LevelManager), "pauseManager", typeof(PauseManager));
        Wire(context, typeof(AudioManager), "saveManager", typeof(SaveManager));
        Wire(context, typeof(SettingsManager), "saveManager", typeof(SaveManager));
        Wire(context, typeof(SettingsManager), "audioManager", typeof(AudioManager));
        Wire(context, typeof(DailyRewardManager), "rewardManager", typeof(RewardManager));
        Wire(context, typeof(ShopManager), "rewardManager", typeof(RewardManager));
        Wire(context, typeof(MissionManager), "rewardManager", typeof(RewardManager));
        Wire(context, typeof(AdsManager), "analyticsManager", typeof(AnalyticsManager));
    }

    private static void Wire(SetupContext context, Type owner, string field, Type target)
    {
        Component component = context.Get(owner);
        if (component == null) return;
        Component dependency = context.Get(target) ?? UniqueComponent(context.Scene, target);
        AssignMissingReference(context, component, field, dependency);
    }

    private static void AssignMissingReference(SetupContext context, Component owner, string field, UnityEngine.Object target)
    {
        if (owner == null) return;
        SerializedProperty property = new SerializedObject(owner).FindProperty(field);
        if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            throw new InvalidOperationException("Incompatible Inspector API: " + owner.GetType().Name + "." + field);
        if (property.objectReferenceValue != null || property.objectReferenceInstanceIDValue != 0)
        {
            if (target == null || property.objectReferenceValue != target)
                Debug.LogWarning("Game Factory: preserved configured " + owner.GetType().Name + "." + field + "; verify its owner/target manually.", owner);
            return;
        }
        if (target == null)
        {
            Debug.LogWarning("Game Factory: no unique target for " + owner.GetType().Name + "." + field + "; reference left empty.", owner);
            return;
        }
        if (!HasCompatibleReference(owner.GetType(), field, target.GetType()))
            throw new InvalidOperationException("Incompatible assignment type: " + owner.GetType().Name + "." + field);
        SetReference(owner, field, target, ref context.Changed);
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
