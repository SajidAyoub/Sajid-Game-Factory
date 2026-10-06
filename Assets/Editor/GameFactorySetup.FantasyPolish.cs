using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class GameFactorySetup
{
    private static void FantasyFeedbackPass(Level1Build build)
    {
        FantasyMaterial(build, "FantasyHazard", new Color(0.72f, 0.12f, 0.12f), 0.2f);
        foreach (GameObject coin in SceneObjects(build.Scene).FindAll(item => item.name.StartsWith("Coin_", StringComparison.Ordinal)))
        {
            Transform visual = coin.transform.Find("CoinVisual");
            if (visual == null) throw new InvalidOperationException("Coin visual missing after core pass: " + coin.name);
            Transform disk = visual.Find("GoldDisk/Mesh");
            if (disk != null) ReplaceKnownMaterial(disk.GetComponent<Renderer>(), "GoldCoinMaterial", PresentationMaterial(build, "FantasyGold"));
            Collider collider = coin.GetComponent<Collider>();
            float diameter = collider != null ? Mathf.Clamp(Mathf.Max(collider.bounds.size.x, collider.bounds.size.z), 0.1f, 2f) : 0.4f;
            GameObject detail = OwnedChild(build, visual, "FantasyCoinDetail", "level1/fantasy/coin", out bool created);
            for (int i = 0; i < 8; i++)
            {
                float radians = i * Mathf.PI / 4f;
                Primitive(build, detail.transform, "RimSpark" + i, PrimitiveType.Cube,
                    new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0) * diameter * 0.43f,
                    Vector3.one * diameter * 0.07f, PresentationMaterial(build, "FantasyFoam"), new Vector3(0, 0, i * 45f));
            }
            for (int sign = -1; sign <= 1; sign += 2)
                FantasyBox(build, detail.transform, "Emblem" + sign, new Vector3(0, 0, sign * 0.03f), new Vector3(diameter * 0.18f, diameter * 0.18f, 0.012f), "FantasyCyan", new Vector3(0, 0, 45));
        }
        foreach (GameObject obstacle in SceneObjects(build.Scene).FindAll(item => item.name.StartsWith("Obstacle_", StringComparison.Ordinal)))
        {
            ReplaceKnownMaterial(obstacle.GetComponent<Renderer>(), "ObstacleMaterial", PresentationMaterial(build, "FantasyHazard"));
            Transform visual = obstacle.transform.Find("ObstacleVisual");
            if (visual == null) continue;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                FantasyBox(build, visual, "FantasyFramePost" + sign, new Vector3(sign * 0.43f, 0, -0.53f), new Vector3(0.09f, 0.95f, 0.04f), "FantasyGold");
                FantasyBox(build, visual, "FantasyWarningBar" + sign, new Vector3(0, sign * 0.4f, -0.53f), new Vector3(0.95f, 0.09f, 0.04f), "FantasyGold");
            }
        }
        GameObject finish = UniqueNamedObject(build.Scene, "FinishLine");
        Transform gate = finish != null ? finish.transform.Find("FinishVisual") : null;
        if (gate == null) throw new InvalidOperationException("FinishVisual missing after core pass.");
        GameObject crown = OwnedChild(build, gate, "FantasyGate", "level1/fantasy/gate", out bool crownCreated);
        for (int sign = -1; sign <= 1; sign += 2)
        {
            FantasyBox(build, crown.transform, "GatePylon" + sign, new Vector3(sign * 5.4f, 1.7f, 0), new Vector3(0.9f, 3.4f, 0.85f), "FantasyStone");
            FantasyBox(build, crown.transform, "GateInlay" + sign, new Vector3(sign * 5.4f, 1.7f, -0.46f), new Vector3(0.17f, 3.05f, 0.07f), "FantasyCyan");
            Primitive(build, crown.transform, "GateCrystal" + sign, PrimitiveType.Cube, new Vector3(sign * 5.4f, 3.8f, 0), Vector3.one * 0.7f, PresentationMaterial(build, "FantasyCyan"), new Vector3(0, 45, 45));
        }
        FantasyBox(build, crown.transform, "ArchLeft", new Vector3(-2.7f, 3.7f, 0), new Vector3(5.5f, 0.28f, 0.75f), "FantasyGold", new Vector3(0, 0, 9));
        FantasyBox(build, crown.transform, "ArchRight", new Vector3(2.7f, 3.7f, 0), new Vector3(5.5f, 0.28f, 0.75f), "FantasyGold", new Vector3(0, 0, -9));
        Primitive(build, crown.transform, "PortalKeystone", PrimitiveType.Cube, new Vector3(0, 4.2f, 0), Vector3.one * 0.6f, PresentationMaterial(build, "FantasyCyan"), new Vector3(0, 45, 45));
        Debug.Log("Fantasy: pickup emblems, hazard frames and finish crown reused/created. Existing bounded collection/hit/finish VFX and optional audio hooks retained.");
    }

    private static void FantasyUIPass(Level1Build build)
    {
        Canvas canvas = UniqueComponent(build.Scene, typeof(Canvas)) as Canvas;
        if (canvas == null || canvas.GetComponent<Level1GeneratedObject>() == null)
            throw new InvalidOperationException("Generated Canvas required; custom UI is not restyled.");
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        foreach (string panelName in PanelNames)
        {
            Transform panel = canvas.transform.Find(panelName);
            if (panel == null) throw new InvalidOperationException("Generated panel missing: " + panelName);
            bool hud = panelName == "GameplayHUDPanel";
            Image background = panel.GetComponent<Image>();
            if (!hud && background != null && Approximately(background.color, new Color(0.035f, 0.05f, 0.09f, 0.92f)))
            {
                RecordPresentationObject(background, "Refine default panel backdrop");
                background.color = new Color(0.02f, 0.055f, 0.1f, 0.94f);
            }
            GameObject frame = OwnedChild(build, panel, "FantasyFrame", "level1/fantasy/ui/" + panelName, out bool created, true);
            if (created)
            {
                Undo.RegisterFullObjectHierarchyUndo(panel.gameObject, "Place presentation behind controls");
                frame.transform.SetAsFirstSibling();
                Place(frame.GetComponent<RectTransform>(), hud ? new Vector2(540, -172) : Vector2.zero, hud ? new Vector2(1035, 310) : new Vector2(940, 1160), hud);
                if (hud)
                {
                    RectTransform cardRect = frame.GetComponent<RectTransform>();
                    cardRect.anchorMin = new Vector2(0.02f, 1); cardRect.anchorMax = new Vector2(0.98f, 1);
                    cardRect.anchoredPosition = new Vector2(0, -172); cardRect.sizeDelta = new Vector2(0, 310);
                }
                Image card = EnsurePresentationComponent<Image>(frame);
                RecordPresentationObject(card, "Style fantasy UI card");
                card.color = hud ? new Color(0.025f, 0.09f, 0.15f, 0.84f) : new Color(0.06f, 0.14f, 0.22f, 0.97f);
                card.raycastTarget = false;
            }
            FantasyUIRule(build, frame.transform, "CyanTop", new Vector2(0, hud ? 150 : 575), new Vector2(hud ? 1025 : 930, 5), new Color(0.1f, 0.85f, 1f));
            FantasyUIRule(build, frame.transform, "GoldBottom", new Vector2(0, hud ? -150 : -575), new Vector2(hud ? 1025 : 930, 3), new Color(1f, 0.7f, 0.12f));
            if (!hud && panelName != "MainMenuPanel")
                MakeText(build, panel, "FantasySubtitle", "SAJID  /  SKYBRIDGE RUN", new Vector2(0, 535), new Vector2(800, 65), 24, font);
            foreach (Text text in panel.GetComponentsInChildren<Text>(true))
            {
                Level1GeneratedObject marker = text.GetComponent<Level1GeneratedObject>();
                if (marker == null) continue;
                if ((text.name.EndsWith("Label", StringComparison.Ordinal) && text.name != "Label") || text.name == "FantasySubtitle")
                {
                    if (Approximately(text.color, Color.white)) { RecordPresentationObject(text, "Accent default labels"); text.color = new Color(0.65f, 0.88f, 1f); }
                }
                if (hud && (text.name == "ScoreText" || text.name == "BestScoreText" || text.name == "CoinCountText"))
                {
                    if (!text.resizeTextForBestFit && text.fontSize == 42)
                    {
                        RecordPresentationObject(text, "Keep large counters readable");
                        text.resizeTextForBestFit = true; text.resizeTextMinSize = 20; text.resizeTextMaxSize = 42;
                    }
                }
            }
            if (hud)
            {
                FitDefaultHUDLayout(panel);
                if (panel.GetComponent<Level1HUDSafeArea>() == null) EnsurePresentationComponent<Level1HUDSafeArea>(panel.gameObject);
            }
            foreach (Button button in panel.GetComponentsInChildren<Button>(true))
            {
                Image image = button.targetGraphic as Image;
                if (button.GetComponent<Level1GeneratedObject>() == null || image == null ||
                    !Approximately(image.color, new Color(0.85f, 0.58f, 0.12f, 0.97f))) continue;
                RecordPresentationObject(image, "Refine default button palette");
                image.color = new Color(0.95f, 0.69f, 0.17f, 1f);
                ColorBlock colors = button.colors;
                if (!colors.Equals(ColorBlock.defaultColorBlock)) continue; // Preserve custom transition colors.
                RecordPresentationObject(button, "Improve default button feedback");
                colors.highlightedColor = new Color(0.65f, 0.94f, 1f);
                colors.pressedColor = new Color(0.35f, 0.75f, 0.87f);
                colors.disabledColor = new Color(0.4f, 0.47f, 0.52f, 0.65f);
                button.colors = colors;
            }
        }
        Debug.Log("Fantasy: refined generated UI panels/cards/accents and numeric sizing; existing text/button references and listeners preserved. HUD safe-area inset applies only in Play Mode.");
    }

    private static void FitDefaultHUDLayout(Transform panel)
    {
        // Migrate only untouched first-generation coordinates to proportional widths.
        // This lets safe-area insets reduce HUD width without pushing counters offscreen.
        for (int i = 0; i < TextNames.Length; i++)
            foreach (string name in new[] { TextNames[i], TextNames[i] + "Label" })
            {
                RectTransform rect = panel.Find(name) as RectTransform;
                if (rect == null || rect.anchorMin != new Vector2(0, 1) || rect.anchorMax != rect.anchorMin ||
                    Mathf.Abs(rect.anchoredPosition.x - (150 + i * 235)) > 0.1f || Mathf.Abs(rect.sizeDelta.x - 220) > 0.1f) continue;
                RecordPresentationObject(rect, "Fit generated HUD counters to safe width");
                rect.anchorMin = new Vector2((40 + i * 235) / 1080f, 1);
                rect.anchorMax = new Vector2((260 + i * 235) / 1080f, 1);
                rect.anchoredPosition = new Vector2(0, rect.anchoredPosition.y);
                rect.sizeDelta = new Vector2(0, rect.sizeDelta.y);
            }
        RectTransform pause = panel.Find("PauseButton") as RectTransform;
        if (pause != null && pause.anchorMin == new Vector2(0, 1) && pause.anchorMax == pause.anchorMin &&
            pause.anchoredPosition == new Vector2(870, -240) && pause.sizeDelta == new Vector2(270, 110))
        {
            RecordPresentationObject(pause, "Fit generated pause control to safe width");
            pause.anchorMin = new Vector2(0.68f, 1); pause.anchorMax = new Vector2(0.93f, 1);
            pause.anchoredPosition = new Vector2(0, -240); pause.sizeDelta = new Vector2(0, 110);
            RectTransform label = pause.Find("Label") as RectTransform;
            if (label != null && label.sizeDelta == new Vector2(260, 100) && label.anchorMin == new Vector2(0.5f, 0.5f))
            {
                RecordPresentationObject(label, "Fit default pause label");
                label.anchorMin = new Vector2(0.02f, 0.5f); label.anchorMax = new Vector2(0.98f, 0.5f);
                label.sizeDelta = new Vector2(0, 100);
            }
        }
    }

    private static bool Approximately(Color a, Color b) => Mathf.Abs(a.r - b.r) < 0.005f && Mathf.Abs(a.g - b.g) < 0.005f && Mathf.Abs(a.b - b.b) < 0.005f && Mathf.Abs(a.a - b.a) < 0.005f;

    private static void FantasyUIRule(Level1Build build, Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject item = OwnedChild(build, parent, name, "level1/fantasy/rule/" + parent.parent.name + "/" + name, out bool created, true);
        if (!created) return;
        Place(item.GetComponent<RectTransform>(), position, size);
        if (parent.parent.name == "GameplayHUDPanel")
        {
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.006f, 0.5f); rect.anchorMax = new Vector2(0.994f, 0.5f);
            rect.sizeDelta = new Vector2(0, size.y);
        }
        Image image = EnsurePresentationComponent<Image>(item);
        RecordPresentationObject(image, "Style fantasy UI rule");
        image.color = color; image.raycastTarget = false;
    }
}
