using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static partial class GameFactorySetup
{
    private static Texture2D ParticleDot()
    {
        string path = GeneratedFolder + "/ParticleDot.asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture != null) return texture;
        if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("Unexpected asset at " + path);
        texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = "ParticleDot", wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[32 * 32];
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float radius = new Vector2((x - 15.5f) / 15.5f, (y - 15.5f) / 15.5f).magnitude;
                pixels[y * 32 + x] = new Color(1, 1, 1, Mathf.Clamp01((1f - radius) * 3f));
            }
        texture.SetPixels(pixels); texture.Apply();
        AssetDatabase.CreateAsset(texture, path);
        Debug.Log("Level 1: generated radial particle texture; asset creation is outside scene Undo.");
        return texture;
    }

    private static void Level1VFX(Level1Build build, Level1PresentationController bridge)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) throw new InvalidOperationException("URP particle shader missing; resolve URP before VFX setup.");
        string path = GeneratedFolder + "/VFXMaterial.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("Unexpected VFX asset at " + path);
            material = new Material(shader) { name = "VFXMaterial" };
            material.SetTexture("_BaseMap", ParticleDot());
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(material, path);
        }
        SetLevel1Reference(bridge, "coinCollectVFX", MakeEffect(build, "CoinCollectVFX", new Color(1f, 0.75f, 0.1f), material));
        SetLevel1Reference(bridge, "playerHitVFX", MakeEffect(build, "PlayerHitVFX", new Color(1f, 0.25f, 0.15f), material));
        SetLevel1Reference(bridge, "finishVFX", MakeEffect(build, "FinishVFX", new Color(0.2f, 1f, 0.85f), material));
    }

    private static ParticleSystem MakeEffect(Level1Build build, string name, Color color, Material material)
    {
        GameObject item = OwnedChild(build, build.Roots["Level1_VFX"].transform, name, "level1/vfx/" + name, out bool created);
        ParticleSystem effect = item.GetComponent<ParticleSystem>();
        if (effect != null) return effect;
        effect = Undo.AddComponent<ParticleSystem>(item);
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Undo.RecordObject(effect, "Configure Level 1 VFX");
        var main = effect.main;
        main.loop = false; main.playOnAwake = false; main.duration = 1f;
        main.startLifetime = 0.65f; main.startSpeed = 0f; main.startSize = 0.16f;
        main.startColor = color; main.maxParticles = 64;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = effect.emission; emission.enabled = false;
        var shape = effect.shape; shape.enabled = false;
        var collision = effect.collision; collision.enabled = false;
        var lights = effect.lights; lights.enabled = false;
        ParticleSystemRenderer renderer = item.GetComponent<ParticleSystemRenderer>();
        Undo.RecordObject(renderer, "Configure VFX renderer");
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return effect;
    }

    private static void Level1UI(Level1Build build, Level1PresentationController bridge)
    {
        var canvases = SceneComponents(build.Scene, typeof(Canvas));
        GameObject canvasObject;
        Transform root = build.Roots["Level1_UI"].transform;
        if (canvases.Count > 1) throw new InvalidOperationException("Multiple Canvases; Level 1 will not create another designed UI or guess an owner.");
        if (canvases.Count == 1)
        {
            canvasObject = canvases[0].gameObject;
            Level1GeneratedObject tag = canvasObject.GetComponent<Level1GeneratedObject>();
            if (tag == null || tag.GenerationId != "level1/canvas" || canvasObject.transform.parent != root)
                throw new InvalidOperationException("Custom Canvas exists. Preserve it; configure presentation manually or remove the ambiguity before generating temporary UI.");
        }
        else canvasObject = OwnedChild(build, root, "Level1Canvas", "level1/canvas", out bool canvasCreated, true);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = Undo.AddComponent<Canvas>(canvasObject);
            Undo.RecordObject(canvas, "Configure Level 1 canvas");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        if (canvasObject.GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = Undo.AddComponent<CanvasScaler>(canvasObject);
            Undo.RecordObject(scaler, "Configure scalable UI");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
        if (canvasObject.GetComponent<GraphicRaycaster>() == null) Undo.AddComponent<GraphicRaycaster>(canvasObject);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) throw new InvalidOperationException("Unity built-in LegacyRuntime font unavailable; no external font will be substituted.");
        UIManager ui = UniqueComponent(build.Scene, typeof(UIManager)) as UIManager;
        HUDController hud = UniqueComponent(build.Scene, typeof(HUDController)) as HUDController;
        if (ui == null || hud == null) throw new InvalidOperationException("Missing/duplicate UIManager or HUDController; run core preparation first.");
        GameObject[] panels = new GameObject[PanelNames.Length];
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i] = OwnedChild(build, canvasObject.transform, PanelNames[i], "level1/panel/" + PanelNames[i], out bool created, true);
            if (created)
            {
                Stretch(panels[i].GetComponent<RectTransform>());
                Image background = Undo.AddComponent<Image>(panels[i]);
                Undo.RecordObject(background, "Style generated panel");
                background.color = i == 1 ? new Color(0, 0, 0, 0) : new Color(0.035f, 0.05f, 0.09f, 0.92f);
                background.raycastTarget = i != 1;
                Undo.RecordObject(panels[i], "Set initial panel visibility");
                panels[i].SetActive(i == 1);
            }
            SetLevel1Reference(ui, PanelFields[i], panels[i]);
        }
        // Hidden MainMenu exists only to satisfy the existing six-state UI contract.
        Title(build, panels[0].transform, "LEVEL 1", font);
        Transform hudRoot = panels[1].transform;
        for (int i = 0; i < TextFields.Length; i++)
        {
            string[] labels = { "SCORE", "BEST", "LEVEL", "COINS" };
            float x = 150 + i * 235;
            MakeText(build, hudRoot, TextNames[i] + "Label", labels[i], new Vector2(x, -80), new Vector2(220, 55), 25, font, true);
            Text value = MakeText(build, hudRoot, TextNames[i], i == 2 ? "1" : "0", new Vector2(x, -145), new Vector2(220, 65), 42, font, true);
            SetLevel1Reference(hud, TextFields[i], value);
        }
        SetLevel1Reference(bridge, "rewardText", MakeText(build, hudRoot, "RewardText", "Rewards  0", new Vector2(240, -225), new Vector2(420, 60), 30, font, true));
        MakeButton(build, hudRoot, "PauseButton", "PAUSE", new Vector2(870, -240), font, bridge.Pause);
        Title(build, panels[2].transform, "PAUSED", font);
        MakeButton(build, panels[2].transform, "ResumeButton", "RESUME", new Vector2(0, 180), font, bridge.Resume);
        MakeButton(build, panels[2].transform, "RestartButton", "RESTART", new Vector2(0, 20), font, bridge.Restart);
        MakeButton(build, panels[2].transform, "SettingsButton", "SETTINGS", new Vector2(0, -140), font, bridge.OpenSettings);
        Title(build, panels[3].transform, "GAME OVER", font);
        SetLevel1Reference(bridge, "gameOverScoreText", MakeText(build, panels[3].transform, "ResultScoreText", "Score  0", new Vector2(0, 170), new Vector2(700, 100), 44, font));
        SetLevel1Reference(bridge, "gameOverBestText", MakeText(build, panels[3].transform, "ResultBestText", "Best  0", new Vector2(0, 50), new Vector2(700, 100), 38, font));
        MakeButton(build, panels[3].transform, "RestartButton", "TRY AGAIN", new Vector2(0, -160), font, bridge.Restart);
        Title(build, panels[4].transform, "LEVEL COMPLETE", font);
        SetLevel1Reference(bridge, "completeScoreText", MakeText(build, panels[4].transform, "ResultScoreText", "Score  0", new Vector2(0, 160), new Vector2(700, 100), 44, font));
        Button next = MakeButton(build, panels[4].transform, "NextLevelButton", "NEXT LEVEL — NOT CONFIGURED", new Vector2(0, -10), font, bridge.NextLevel);
        SetLevel1Reference(bridge, "nextLevelButton", next);
        if (next.GetComponent<Level1GeneratedObject>() != null)
        {
            Undo.RecordObject(next, "Disable unconfigured next level");
            var enabled = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var entry in ReadActiveBuildScenesReadOnly()) if (entry != null && entry.enabled) enabled.Add(entry);
            int index = enabled.FindIndex(entry => entry.path == build.Scene.path);
            next.interactable = index >= 0 && index + 1 < enabled.Count && AssetDatabase.LoadAssetAtPath<SceneAsset>(enabled[index + 1].path) != null;
        }
        MakeButton(build, panels[4].transform, "RestartButton", "PLAY AGAIN", new Vector2(0, -170), font, bridge.Restart);
        Title(build, panels[5].transform, "SETTINGS", font);
        SetLevel1Reference(bridge, "musicText", MakeButton(build, panels[5].transform, "MusicButton", "Music  ON", new Vector2(0, 220), font, bridge.ToggleMusic).GetComponentInChildren<Text>(true));
        SetLevel1Reference(bridge, "sfxText", MakeButton(build, panels[5].transform, "SFXButton", "SFX  ON", new Vector2(0, 60), font, bridge.ToggleSFX).GetComponentInChildren<Text>(true));
        SetLevel1Reference(bridge, "vibrationText", MakeButton(build, panels[5].transform, "VibrationButton", "Vibration  ON", new Vector2(0, -100), font, bridge.ToggleVibration).GetComponentInChildren<Text>(true));
        MakeButton(build, panels[5].transform, "BackButton", "BACK", new Vector2(0, -300), font, bridge.CloseSettings);
        Level1EventSystem(build);
    }

    private static void Level1EventSystem(Level1Build build)
    {
        var systems = SceneComponents(build.Scene, typeof(EventSystem));
        if (systems.Count > 1) throw new InvalidOperationException("Multiple EventSystems; refusing duplicate input ownership.");
        GameObject item;
        if (systems.Count == 1) item = systems[0].gameObject;
        else
        {
            item = OwnedChild(build, build.Roots["Level1_UI"].transform, "Level1EventSystem", "level1/event-system", out bool created);
            Undo.AddComponent<EventSystem>(item);
        }
        BaseInputModule[] modules = item.GetComponents<BaseInputModule>();
        int activeModules = 0;
        foreach (BaseInputModule module in modules) if (module.enabled) activeModules++;
        if (activeModules > 1) throw new InvalidOperationException("Competing enabled UI input modules; preserve them and resolve ownership manually.");
        if (modules.Length == 0)
        {
            InputSystemUIInputModule input = Undo.AddComponent<InputSystemUIInputModule>(item);
            Undo.RecordObject(input, "Assign built-in UI Input System actions");
            ConfigurePersistentUIInput(input); // Existing project already requires Input System.
        }
        else if (item.GetComponent<InputSystemUIInputModule>() == null)
                throw new InvalidOperationException("Existing EventSystem does not use InputSystemUIInputModule. Preserve custom input; configure it manually.");
    }

    private static void ConfigurePersistentUIInput(InputSystemUIInputModule input)
    {
        string path = GeneratedFolder + "/UIInputActions.inputactions";
        string absolute = System.IO.Path.Combine(Application.dataPath, path.Substring("Assets/".Length));
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        if (asset == null)
        {
            if (System.IO.File.Exists(absolute)) throw new InvalidOperationException("Existing UI actions failed import; file preserved for manual repair.");
            var defaults = new DefaultInputActions();
            string json = defaults.asset.ToJson();
            UnityEngine.Object.DestroyImmediate(defaults.asset);
            // Export through the supported Input System JSON format, not Unity YAML.
            System.IO.File.WriteAllText(absolute, json);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            if (asset == null) throw new InvalidOperationException("Generated UI input actions did not import.");
        }
        input.actionsAsset = asset;
        input.point = UIActionReference(asset, "Point");
        input.leftClick = UIActionReference(asset, "Click");
        input.rightClick = UIActionReference(asset, "RightClick");
        input.middleClick = UIActionReference(asset, "MiddleClick");
        input.scrollWheel = UIActionReference(asset, "ScrollWheel");
        input.move = UIActionReference(asset, "Navigate");
        input.submit = UIActionReference(asset, "Submit");
        input.cancel = UIActionReference(asset, "Cancel");
        // Persist references as separate assets: temporary Create() references would
        // disappear on scene reload, and importer-owned subassets must not be edited.
    }

    private static InputActionReference UIActionReference(InputActionAsset asset, string name)
    {
        string path = GeneratedFolder + "/UI_" + name + ".asset";
        InputActionReference reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(path);
        InputAction action = asset.FindAction("UI/" + name, true);
        if (reference != null)
        {
            if (reference.action != action) throw new InvalidOperationException("Custom/missing generated UI action reference at " + path + "; preserved.");
            return reference;
        }
        if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("Unexpected asset at " + path);
        reference = InputActionReference.Create(action);
        reference.name = "UI_" + name;
        AssetDatabase.CreateAsset(reference, path);
        return reference;
    }

    private static void Stretch(RectTransform rect)
    {
        Undo.RecordObject(rect, "Stretch generated panel");
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size, bool top = false)
    {
        Undo.RecordObject(rect, "Lay out generated UI");
        rect.anchorMin = rect.anchorMax = top ? new Vector2(0, 1) : new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
    }

    private static Text MakeText(Level1Build build, Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Font font, bool top = false)
    {
        GameObject item = OwnedChild(build, parent, name, "level1/ui/" + parent.name + "/" + name, out bool created, true);
        Text text = item.GetComponent<Text>();
        if (text == null)
        {
            text = Undo.AddComponent<Text>(item);
            Undo.RecordObject(text, "Style generated text");
            text.font = font; text.fontSize = fontSize;
            text.color = Color.white; text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false; text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
        }
        if (created) Place(item.GetComponent<RectTransform>(), position, size, top);
        return text;
    }

    private static void Title(Level1Build build, Transform panel, string title, Font font)
        => MakeText(build, panel, "Title", title, new Vector2(0, 440), new Vector2(950, 120), 60, font);

    private static Button MakeButton(Level1Build build, Transform parent, string name, string label, Vector2 position, Font font, UnityAction action)
    {
        GameObject item = OwnedChild(build, parent, name, "level1/button/" + parent.name + "/" + name, out bool created, true);
        Image image = item.GetComponent<Image>();
        if (image == null) { image = Undo.AddComponent<Image>(item); Undo.RecordObject(image, "Style generated button"); image.color = new Color(0.85f, 0.58f, 0.12f, 0.97f); }
        Button button = item.GetComponent<Button>();
        if (button == null)
        {
            button = Undo.AddComponent<Button>(item);
            Undo.RecordObject(button, "Assign button graphic");
            button.targetGraphic = image;
        }
        bool hud = parent.name == "GameplayHUDPanel";
        if (created) Place(item.GetComponent<RectTransform>(), position, hud ? new Vector2(270, 110) : new Vector2(700, 120), hud);
        MakeText(build, item.transform, "Label", label, Vector2.zero, hud ? new Vector2(260, 100) : new Vector2(680, 110), 33, font);
        int count = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
            if (button.onClick.GetPersistentTarget(i) == action.Target as UnityEngine.Object && button.onClick.GetPersistentMethodName(i) == action.Method.Name)
                return button;
        if (count > 0) throw new InvalidOperationException("Custom button binding preserved on " + name + "; refusing to install competing navigation.");
        Undo.RecordObject(button, "Bind Level 1 UI action");
        UnityEventTools.AddPersistentListener(button.onClick, action);
        RecordPrefabChange(button);
        return button;
    }

    private static Level1PresentationController PresentationBridge(Level1Build build)
    {
        var existing = SceneComponents(build.Scene, typeof(Level1PresentationController));
        if (existing.Count > 1) throw new InvalidOperationException("Multiple Level1PresentationController owners; no duplicate subscriptions will be installed.");
        GameObject host = OwnedChild(build, build.Roots["Level1_UI"].transform, "Level1Presentation", "level1/presentation", out bool created);
        if (existing.Count == 1 && existing[0].gameObject != host) throw new InvalidOperationException("Custom presentation owner exists; configure it manually instead of adding another.");
        Level1PresentationController bridge = host.GetComponent<Level1PresentationController>() ?? Undo.AddComponent<Level1PresentationController>(host);
        foreach (Type type in new[] { typeof(GameManager), typeof(LevelManager), typeof(PauseManager), typeof(ScoreManager), typeof(SaveManager),
            typeof(SettingsManager), typeof(RewardManager), typeof(AudioManager), typeof(UIManager), typeof(HUDController) })
        {
            string field = char.ToLowerInvariant(type.Name[0]) + type.Name.Substring(1);
            if (type == typeof(HUDController)) field = "hudController";
            if (type == typeof(UIManager)) field = "uiManager";
            Component target = UniqueComponent(build.Scene, type);
            if (target == null) throw new InvalidOperationException("Missing/duplicate bridge dependency " + type.Name);
            SetLevel1Reference(bridge, field, target);
        }
        SetLevel1Reference(bridge, "player", UniqueNamedObject(build.Scene, "Player").transform);
        SetLevel1Reference(bridge, "finish", UniqueNamedObject(build.Scene, "FinishLine").transform);
        var coins = SceneComponents(build.Scene, typeof(Coin));
        var data = new SerializedObject(bridge);
        SerializedProperty array = data.FindProperty("coins");
        Undo.RecordObject(bridge, "Assign scene coin hooks");
        array.arraySize = coins.Count;
        for (int i = 0; i < coins.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = coins[i];
        data.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("Level 1: audio clip placeholders are on Level1Presentation. Missing clips remain silent; no saved preferences/balances reset.", bridge);
        return bridge;
    }

    private static void ValidateLevel1(UnityEngine.SceneManagement.Scene scene, ValidationReport report)
    {
        foreach (string name in Level1RootNames)
        {
            GameObject root = UniqueNamedObject(scene, name);
            if (root == null || !root.activeInHierarchy) report.Level1Warning("Missing/ambiguous/inactive " + name + "; run Build / Repair Level 1 Master.");
            else report.Pass(name + " exists.");
        }
        GameObject environment = UniqueNamedObject(scene, "Level1_Environment");
        if (environment != null)
        {
            if (environment.GetComponentsInChildren<Renderer>(true).Length == 0) report.Level1Warning("Level 1 environment geometry is missing.");
            if (environment.transform.position != Vector3.zero || environment.transform.rotation != Quaternion.identity || environment.transform.lossyScale != Vector3.one)
                report.Level1Warning("Customized environment root transform requires manual alignment review.");
        }
        GameObject player = UniqueNamedObject(scene, "Player");
        if (player != null)
        {
            Transform proxy = player.transform.Find("RunnerProxy");
            bool realModel = false;
            foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>(true))
                if (renderer.enabled && renderer.GetComponentInParent<Level1GeneratedObject>() == null &&
                    (renderer is SkinnedMeshRenderer || renderer.transform != player.transform || !PrimitiveRenderer(renderer))) realModel = true;
            if (proxy == null && !realModel) report.Level1Warning("No runner proxy/real model detected.");
            if (proxy != null)
            {
                if (proxy.GetComponent<SimpleRunnerVisual>() == null) report.Level1Warning("Runner proxy animation component missing.");
                if (proxy.GetComponentsInChildren<Renderer>(true).Length < 6) report.Level1Warning("Runner proxy geometry incomplete.");
                foreach (string part in new[] { "Torso", "Head", "LeftArm", "RightArm", "LeftLeg", "RightLeg" })
                    if (proxy.Find(part) == null) report.Level1Warning("Runner proxy missing " + part + ".");
            }
        }
        foreach (GameObject item in SceneObjects(scene))
        {
            if (item.name.StartsWith("Coin_", StringComparison.Ordinal))
            {
                Transform visual = item.transform.Find("CoinVisual");
                CoinVisualAnimator animator = item.GetComponent<CoinVisualAnimator>();
                if (visual == null || animator == null || ReadReference(animator, "visual") != visual)
                    report.Level1Warning(item.name + " gold visual/child animation missing or mismatched.");
                else if (visual.GetComponentsInChildren<Renderer>(true).Length == 0) report.Level1Warning(item.name + " gold mesh missing.");
            }
            if (item.name.StartsWith("Obstacle_", StringComparison.Ordinal) && item.transform.Find("ObstacleVisual") == null)
                report.Level1Warning(item.name + " visual hazard band missing.");
        }
        GameObject finish = UniqueNamedObject(scene, "FinishLine");
        if (finish == null || finish.transform.Find("FinishVisual") == null) report.Level1Warning("Finish visual gate missing.");
        foreach (Component component in SceneComponents(scene, typeof(Level1GeneratedObject)))
        {
            if (component.GetComponentsInChildren<Collider>(true).Length > 0)
                report.Error("Generated decoration contains a Collider: " + component.name + "; do not let presentation alter verified physics.");
            foreach (Renderer renderer in component.GetComponentsInChildren<Renderer>(true))
                if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null) report.Level1Warning("Generated renderer missing material/shader: " + renderer.name);
        }
        Level1PresentationController bridge = UniqueComponent(scene, typeof(Level1PresentationController)) as Level1PresentationController;
        if (SceneComponents(scene, typeof(Level1PresentationController)).Count > 1) report.Error("Duplicate Level 1 presentation adapters: duplicate event/VFX/UI delivery risk.");
        if (bridge == null) { report.Level1Warning("Level1PresentationController missing; functional UI/VFX not connected."); return; }
        if (!bridge.isActiveAndEnabled) report.Error("Level 1 bridge must live on an always-active object outside panels.");
        foreach (Type type in new[] { typeof(GameManager), typeof(LevelManager), typeof(PauseManager), typeof(ScoreManager), typeof(SaveManager),
            typeof(SettingsManager), typeof(RewardManager), typeof(AudioManager), typeof(UIManager), typeof(HUDController) })
        {
            string field = char.ToLowerInvariant(type.Name[0]) + type.Name.Substring(1);
            if (type == typeof(HUDController)) field = "hudController";
            if (type == typeof(UIManager)) field = "uiManager";
            Component manager = UniqueComponent(scene, type);
            CheckReference(bridge, field, manager, report, true);
            if (manager is Behaviour behaviour && !behaviour.isActiveAndEnabled)
                report.Error("Required Level 1 manager inactive: " + type.Name);
        }
        CheckReference(bridge, "player", player != null ? player.transform : null, report, true);
        CheckReference(bridge, "finish", finish != null ? finish.transform : null, report, true);
        SerializedProperty coinRefs = new SerializedObject(bridge).FindProperty("coins");
        var actualCoins = SceneComponents(scene, typeof(Coin));
        var wiredCoins = new System.Collections.Generic.HashSet<UnityEngine.Object>();
        if (coinRefs == null || !coinRefs.isArray) report.Error("Bridge coin hook API incompatible.");
        else
        {
            for (int i = 0; i < coinRefs.arraySize; i++)
                if (!wiredCoins.Add(coinRefs.GetArrayElementAtIndex(i).objectReferenceValue)) report.Error("Duplicate bridge coin subscription entry.");
            foreach (Component coin in actualCoins) if (!wiredCoins.Contains(coin)) report.Level1Warning("Coin feedback hook missing: " + coin.name);
        }
        foreach (string field in new[] { "rewardText", "gameOverScoreText", "gameOverBestText", "completeScoreText", "musicText", "sfxText", "vibrationText", "nextLevelButton" })
        {
            UnityEngine.Object target = ReadReference(bridge, field);
            if (target == null) report.Level1Warning("Level 1 UI reference missing: " + field);
            else if (target is Component component && component.gameObject.scene != scene) report.Error("Level 1 UI reference points outside MainGame: " + field);
        }
        UIManager ui = UniqueComponent(scene, typeof(UIManager)) as UIManager;
        if (ui != null)
            foreach (string field in PanelFields)
                if (ReadReference(ui, field) == null) report.Level1Warning("Level 1 panel not assigned: " + field);
        HUDController hud = UniqueComponent(scene, typeof(HUDController)) as HUDController;
        if (hud != null)
            foreach (string field in TextFields)
            {
                Text text = ReadReference(hud, field) as Text;
                if (text == null || text.font == null || !text.enabled) report.Level1Warning("Level 1 HUD Text/font missing or disabled: " + field);
            }
        GameObject uiRoot = UniqueNamedObject(scene, "Level1_UI");
        Canvas[] canvases = uiRoot != null ? uiRoot.GetComponentsInChildren<Canvas>(true) : new Canvas[0];
        if (canvases.Length != 1 || SceneComponents(scene, typeof(Canvas)).Count != 1) report.Level1Warning("Level 1 needs exactly one Canvas under Level1_UI; custom Canvas ambiguity must be resolved manually.");
        else
        {
            CanvasScaler scaler = canvases[0].GetComponent<CanvasScaler>();
            if (!canvases[0].isActiveAndEnabled || canvases[0].renderMode != RenderMode.ScreenSpaceOverlay || scaler == null ||
                scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize || canvases[0].GetComponent<GraphicRaycaster>() == null)
                report.Level1Warning("Level 1 Canvas/scaler/raycaster configuration incomplete.");
            foreach (Transform panel in canvases[0].transform)
                if (panel.GetComponent<Level1GeneratedObject>() != null && bridge.transform.IsChildOf(panel)) report.Error("Presentation bridge cannot be disabled by a UI panel.");
            ValidateLevel1Buttons(canvases[0].transform, bridge, report);
        }
        var systems = SceneComponents(scene, typeof(EventSystem));
        if (systems.Count != 1) report.Error("Level 1 requires exactly one EventSystem.");
        else
        {
            InputSystemUIInputModule input = systems[0].GetComponent<InputSystemUIInputModule>();
            if (input == null || !input.isActiveAndEnabled || input.point == null || input.leftClick == null || input.actionsAsset == null)
                report.Error("UI Input System module/action references missing or disabled.");
            StandaloneInputModule legacy = systems[0].GetComponent<StandaloneInputModule>();
            if (legacy != null && legacy.enabled) report.Error("Enabled legacy UI input module conflicts with New Input System-only settings; disable it manually.");
        }
        foreach (string field in new[] { "coinCollectVFX", "playerHitVFX", "finishVFX" })
        {
            ParticleSystem effect = ReadReference(bridge, field) as ParticleSystem;
            if (effect == null) report.Level1Warning("Missing Level 1 effect " + field);
            else if (effect.main.playOnAwake || effect.main.loop || effect.main.simulationSpace != ParticleSystemSimulationSpace.World || effect.main.maxParticles > 128)
                report.Level1Warning(field + " differs from the bounded manual/world-space burst contract.");
        }
        foreach (string field in new[] { "backgroundMusic", "coinSFX", "hitSFX", "finishSFX", "uiClickSFX" })
            if (ReadReference(bridge, field) == null) report.Warning("Optional audio clip missing: " + field + "; silence is safe.");
        ValidateFantasyPresentation(scene, report);
        report.Pass("Level 1 structural inspection performed without playing VFX, invoking buttons or editing generated assets.");
        report.Pass("Optional missing audio, next level and shop/mission catalogs are warnings; they do not alone block Level 1 play-test readiness. Blocking Level 1 presentation gaps are tracked separately.");
    }

    private static void ValidateLevel1Buttons(Transform canvas, Level1PresentationController bridge, ValidationReport report)
    {
        string[] paths = { "GameplayHUDPanel/PauseButton", "PauseMenuPanel/ResumeButton", "PauseMenuPanel/RestartButton", "PauseMenuPanel/SettingsButton",
            "GameOverPanel/RestartButton", "LevelCompletePanel/NextLevelButton", "LevelCompletePanel/RestartButton",
            "SettingsPanel/MusicButton", "SettingsPanel/SFXButton", "SettingsPanel/VibrationButton", "SettingsPanel/BackButton" };
        string[] methods = { "Pause", "Resume", "Restart", "OpenSettings", "Restart", "NextLevel", "Restart", "ToggleMusic", "ToggleSFX", "ToggleVibration", "CloseSettings" };
        for (int i = 0; i < paths.Length; i++)
        {
            Transform item = canvas.Find(paths[i]);
            Button button = item != null ? item.GetComponent<Button>() : null;
            int matches = 0;
            if (button != null)
                for (int j = 0; j < button.onClick.GetPersistentEventCount(); j++)
                    if (button.onClick.GetPersistentTarget(j) == bridge && button.onClick.GetPersistentMethodName(j) == methods[i] &&
                        button.onClick.GetPersistentListenerState(j) != UnityEventCallState.Off) matches++;
            if (matches != 1) report.Level1Warning("Missing/duplicate enabled button binding: " + paths[i] + " -> " + methods[i]);
        }
    }
}
