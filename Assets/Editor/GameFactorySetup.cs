using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only, user-invoked setup. Never loads or saves a scene automatically.
public static class GameFactorySetup
{
    private const string MenuPath = "Tools/Sajid Game Factory/Setup Obstacle & Game Over";

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
