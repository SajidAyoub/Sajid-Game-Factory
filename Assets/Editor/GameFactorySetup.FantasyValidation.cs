using System;
using UnityEditor;
using UnityEngine;

public static partial class GameFactorySetup
{
    private static void ValidateFantasyPresentation(UnityEngine.SceneManagement.Scene scene, ValidationReport report)
    {
        GameObject environment = UniqueNamedObject(scene, "Level1_Environment");
        Transform bridge = environment != null ? environment.transform.Find("FantasyBridge") : null;
        if (bridge == null || !bridge.gameObject.activeInHierarchy)
            report.Level1Warning("Fantasy bridge missing/inactive; run Upgrade / Repair Level 1 Fantasy Presentation.");
        else
        {
            if (bridge.localPosition != Vector3.zero || bridge.localRotation != Quaternion.identity || bridge.localScale != Vector3.one)
                report.Level1Warning("Fantasy bridge root has custom alignment; review lane/cliff placement.");
            foreach (string part in new[] { "BridgeDeck", "WestEdgeLight", "EastEdgeLight", "WestWaterfall0", "EastWaterfall0" })
                if (bridge.Find(part) == null) report.Level1Warning("Fantasy environment part missing: " + part);
            report.Pass("Fantasy bridge/cliff/waterfall geometry inspected; not proof of rendering quality.");
        }
        GameObject player = UniqueNamedObject(scene, "Player");
        Transform visual = player != null ? player.transform.Find("PlayerVisual") : null;
        bool authored = false;
        if (player != null)
            foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>(true))
                if (renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.GetComponentInParent<Level1GeneratedObject>() == null &&
                    (renderer is SkinnedMeshRenderer || renderer.transform != player.transform || !PrimitiveRenderer(renderer))) authored = true;
        var controllers = SceneComponents(scene, typeof(RunnerPresentationController));
        if (controllers.Count > 1) report.Error("Duplicate runner presentation controllers; resolve visual ownership.");
        RunnerPresentationController controller = visual != null ? visual.GetComponent<RunnerPresentationController>() : null;
        if (controller == null)
        {
            if (!authored) report.Level1Warning("Wolf runner/visual swap controller missing; run fantasy upgrade.");
            else report.Warning("Authored runner retained; wolf proxy and swap controller intentionally skipped.");
        }
        else
        {
            SerializedObject state = new SerializedObject(controller);
            bool custom = state.FindProperty("useCustomVisual").boolValue;
            GameObject selected = ReadReference(controller, custom ? "customVisual" : "generatedVisual") as GameObject;
            if (selected == null || selected == visual.gameObject || !selected.transform.IsChildOf(visual))
                report.Error("Selected runner visual missing or outside the presentation subtree.");
            else
            {
                if (!selected.activeInHierarchy) report.Level1Warning("Selected runner visual inactive; check configured swap mode.");
                if (selected.GetComponentsInChildren<Collider>(true).Length > 0 || selected.GetComponentsInChildren<Rigidbody>(true).Length > 0)
                    report.Error("Runner visual has physics components; root Player physics must remain the sole gameplay owner.");
                if (selected.GetComponentsInChildren<Renderer>(true).Length == 0) report.Level1Warning("Selected runner has no renderer.");
                if (!custom)
                    foreach (string part in new[] { "JacketBody", "WolfHead", "Muzzle", "LeftEar", "RightEar", "WolfLeftArm", "WolfRightLeg" })
                        if (selected.transform.Find(part) == null) report.Level1Warning("Wolf proxy part missing: " + part);
            }
            SimpleRunnerVisual animator = visual.GetComponent<SimpleRunnerVisual>();
            if (animator == null || ReadReference(animator, "playerController") != (player != null ? player.GetComponent<PlayerController>() : null))
                report.Level1Warning("Wolf procedural animation reference missing/mismatched.");
            Transform old = player != null ? player.transform.Find("RunnerProxy") : null;
            if (old != null && old.gameObject.activeInHierarchy) report.Level1Warning("Legacy runner proxy also active; double character presentation.");
        }
        Canvas canvas = UniqueComponent(scene, typeof(Canvas)) as Canvas;
        if (canvas != null)
            foreach (string name in PanelNames)
            {
                Transform panel = canvas.transform.Find(name);
                if (panel == null || panel.Find("FantasyFrame") == null) report.Level1Warning("Fantasy UI styling missing for " + name);
                if (panel != null && name == "GameplayHUDPanel" && panel.GetComponent<Level1HUDSafeArea>() == null)
                    report.Warning("HUD safe-area helper missing; notch/viewport inset requires manual review.");
            }
        foreach (GameObject item in SceneObjects(scene))
            if (item.name.StartsWith("Coin_", StringComparison.Ordinal) && item.transform.Find("CoinVisual/FantasyCoinDetail") == null)
                report.Level1Warning(item.name + " fantasy pickup details missing.");
        GameObject finish = UniqueNamedObject(scene, "FinishLine");
        if (finish == null || finish.transform.Find("FinishVisual/FantasyGate") == null) report.Level1Warning("Fantasy finish crown missing.");
        report.Warning("Fantasy references approximated from written direction; exact images unavailable in this task. Rendering, UI input/safe areas, VFX, draw calls and physics require Unity testing.");
    }
}
