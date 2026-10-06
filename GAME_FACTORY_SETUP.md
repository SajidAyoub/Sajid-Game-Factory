# Game Factory Unity setup checklist

## Fantasy presentation upgrade: run this next

The repository was fetched/rebased to the user-generated Level 1 commit `1b043ad` before this pass. Existing MainGame and generated assets are retained byte-for-byte by Codex. The following changes are created **only when you run the menu in Unity**.

1. Pull the code, open Unity 6000.0.84f1 and resolve all import/compiler errors. Open MainGame in Edit Mode. Save a Git checkpoint of the current verified scene; do not upgrade packages.
2. Run **Tools > Sajid Game Factory > Upgrade / Repair Level 1 Fantasy Presentation**. This is an alias for **Build / Repair Level 1 Master**, which now performs core setup and the original presentation repair, then the fantasy environment, wolf, pickup/gate and UI passes, followed by validation. Use either command, not a second independent pipeline.
3. Inspect `Level1_Environment/FantasyBridge`: the deck/supports sit below the unchanged Ground, with rails/crystal posts outside the playable lane, cyan edge/rune strips, cliffs and opaque waterfall ribbons. Inspect camera occlusion and the start/end of the road. Neither Player/Ground position nor CameraFollow/lighting is altered. Emissive materials look bright but do not produce a post-process bloom halo.
4. Inspect `Player/PlayerVisual/GeneratedWolf`: white fur, triangular procedural ears, muzzle/eyes/nose, blue jacket, black sportswear, lime cuffs/back stripe and tail. Root Player Rigidbody/Collider/controller/Transform remain unchanged. Only visual limbs and GeneratedWolf bob/lean animate; GameOver stops and pause freezes them. Author-created models are preserved and wolf creation is skipped when detected.
5. Inspect CoinVisual/FantasyCoinDetail, gold/blue pickup emblems, red/gold obstacle frames and FinishVisual/FantasyGate. No new Collider may exist on decoration. Existing score/coin events, three bounded VFX and audio placeholders are reused; no reward currency is added by coins or this upgrade.
6. Inspect generated UI cards/rules, large-number fitting, proportional default counter/button widths, button feedback and HUD safe-area inset. Existing panel references, eleven button bindings, pause/settings flow and Next guard remain intact. Default colors are migrated; custom colors/transition settings are preserved. New cards are inserted behind controls. The safe-area helper runs only in Play Mode and restores original anchors when hidden/disabled.
7. Save the scene, generated Fantasy materials/mesh, new script metadata and any reviewed profile change. Run the tool twice more: object/component/listener/asset counts must remain stable. Undo/Redo the presentation pass; original owned proxy/environment decorations are retained but deactivated, not deleted. Their manual edits remain on the inactive objects. Remove neither old nor new content blindly; review changes before committing Unity outputs.
8. Run **Tools > Sajid Game Factory > Validate Current Game Setup** and read the final report after intermediate core reports. New checks cover fantasy geometry, selected runner subtree, animation reference, double proxies, pickup/gate details and UI cards. Missing required generated features produce LEVEL 1 SETUP INCOMPLETE; unsafe dependencies/physics produce CRITICAL ERRORS FOUND. Optional missing clips/next scene and the visual-review reminder are warnings. READY FOR LEVEL 1 PLAY TEST remains a structural check, not runtime certification.
9. Play and test forward movement/A-D/arrows/camera → coin collection and exact score/run count/VFX → pause/resume → settings/back and time scale → separate obstacle GameOver and finish LevelComplete runs → restart at every state → safe disabled Next. Test saved best/preferences after scene reload; no saved data should be reset.
10. Review the wolf front/side/back and running pose, ground/collider alignment, fantasy scenery placement, cyan/gold contrast, coin readability, particles, UI fonts/counters/buttons, portrait/landscape/notch simulation and target-device touch. Profile draw calls/triangles/shadows on target hardware; approximately 140 environment shapes plus runner/coin/gate detail are not a proven mobile budget. Then test a development build. **Unity compilation/import, gameplay/physics, UI/input/visual quality, VFX/materials, Undo/Redo and builds remain pending.**

### Replacing the generated wolf later

RunnerPresentationController lives on PlayerVisual with separate generatedVisual/customVisual references and four optional Head/Body/Outfit/Accent material/renderer slots. Existing user slot assignments are preserved. Add an authored FBX/Animator as a separate physics-free child under PlayerVisual, assign customVisual and useCustomVisual, or call SelectVisual(true) from a dedicated equipment adapter. It validates descendant roots/physics and returns false on invalid configuration; VisualChanged is exception-isolated. Material application is explicit via ApplyMaterials, so heterogeneous/custom materials are not rewritten on Awake. Do not put PlayerController, Rigidbody, Colliders or managers under a swappable model. Configure the FBX scale/offset/Animator deliberately and keep CameraFollow targeting Player. The generated limb animation only drives its own hidden proxy; it is not a replacement for a real rig/Animator. SkinManager ownership/selection is deliberately separate and no skin/economy adapter was added.

Generated additional assets are under `Assets/GameFactory/Generated/Level1/Fantasy`: thirteen URP Lit materials and one six-triangle WolfEar mesh. No paid art, external downloads, textures, packages or post-processing are needed. Materials/mesh already at those paths are reused; user edits are not rewritten. Known earlier generated scene material references may switch to the new palette, but old assets remain intact. Custom material asset references are preserved. Normal scene Undo does not undo asset creation or earlier core/build steps; partial failure attempts presentation rollback and reports what remains.

The closest procedural approximation is based on the written direction only: reference images were unavailable here. Waterfalls are static opaque ribbons, not simulated water; the wolf is a recognizable geometric proxy, not a production character matching a reference sheet. Professional rig/mesh/animation, audio, art direction and device profiling remain manual. Preserve verified gameplay, persistence/state architecture and service abstractions when replacing them.

## Level 1 Master: preferred workflow

**Codex → GitHub → Unity Pull → Tools > Sajid Game Factory > Build / Repair Level 1 Master → Validate Current Game Setup → Play Test → Visual review → Lock Level 1 → Duplicate architecture for future levels.**

1. Pull the commits, open pinned Unity 6000.0.84f1 and allow package/script import. Resolve every compile/import error before proceeding. New script `.meta` files are generated by Unity: preserve and commit their GUIDs with the reviewed generated assets and scene. Do not upgrade packages as part of this first check.
2. Open `Assets/Scenes/MainGame.unity` in Edit Mode, outside Prefab Mode. Close other manager-bearing scenes. Keep a clean Git checkpoint; run **Validate Current Game Setup** for a read-only baseline.
3. Run **Build / Repair Level 1 Master**. It first calls the existing entire-game setup, then creates/reuses generated assets and the four containers `Level1_Environment`, `Level1_Visuals`, `Level1_VFX`, `Level1_UI`; builds player/coin/environment/obstacle/finish visuals; preserves lighting/camera; creates the bridge, VFX and UI; validates. Earlier core/build passes are separate operations, not one atomic transaction.
4. Review the detailed created/reused/skipped messages. Custom Canvas/module ownership, duplicate names/components, conflicting/broken references or unexpected assets require manual resolution; presentation fails safely rather than overwriting them. Existing custom materials, models, generated-object transforms/styles and lighting are preserved. Missing owned helper components/references can be filled, but repair does not erase custom content or normalize altered styles.
5. Inspect Managers and the always-active `Level1_UI/Level1Presentation` object. Its Inspector references should point to GameManager, LevelManager, PauseManager, ScoreManager, SaveManager, SettingsManager, RewardManager, AudioManager, UIManager, HUDController, Player, FinishLine, all Coins, result/settings text, Next button and three ParticleSystems. Leave optional clips empty or assign your licensed background music, coin/hit/finish/UI-click SFX. No clips is supported.
6. Inspect one Overlay Canvas with a 1080×1920 scaler, six sibling panels (Main Menu is a hidden placeholder; HUD/Pause/Game Over/Level Complete/Settings are wired), white built-in Text and eleven persistent button bindings. The generated EventSystem uses InputSystemUIInputModule and persistent generated input-action references. Preserve Active Input Handling = Input System Package (New).
7. Save MainGame and any changed active Build Profile manually; save/commit generated assets and new metadata from Unity. The shared build list may already be updated by core setup. No Level 2 is created. Next Level remains disabled with “NOT CONFIGURED” when no next enabled scene exists.
8. Run Master again twice: counts, assets, components and listeners must stay stable. Test scene Undo/Redo, then save, close/reopen MainGame and revalidate. Scene Undo cannot remove created materials/textures/input assets or undo shared build-list edits. On failure the presentation group attempts rollback; earlier setup and generated assets may remain for reuse. Check Console before retrying.
9. Play: verify automatic forward travel and A/D/arrows, camera, animated limbs, gold coin rotation/bob, exactly one score/count/VFX update per coin and unchanged colliders. HUD level is explicitly 1, not build index + 1; COINS is per-run count, Rewards is independent persisted currency. Best is displayed live and persisted at GameOver/LevelComplete. Setup never resets user data.
10. Pause → Resume; Pause → Settings → toggle all preferences → Back must return to Pause; Resume restores gameplay time. Restart must reload MainGame and restore time scale, score/run coin count, trigger latches, HUD and subscriptions. In separate runs hit an obstacle and reach the finish: movement stops, only the appropriate result panel appears, VFX fires once and Restart works. Confirm Next is harmless/disabled with no next scene.
11. Check every button with mouse and target-device touch, portrait and landscape/small viewports, fonts, clipping, safe-area needs, transparent particles and gold/track contrast. Reopen/reload to test persistent UI bindings/input actions, saved best/settings and no duplicate callbacks. Also test disabled/re-enabled bridge and no-domain-reload Editor sessions.
12. Assign optional licensed clips and retest settings/audio routing; no clip must remain error-free. Run a development player build on the intended platform. **Compilation, Play Mode, physics, materials, VFX, UI visual quality and builds are all pending Unity verification.** Lock Level 1 only after these checks pass and commit the reviewed Unity outputs. Duplicate the architecture for future levels afterward; set each bridge's display level explicitly and review consecutive enabled build indices.

### Assets, replacement and validation

Generated only by Unity under `Assets/GameFactory/Generated/Level1`: eight URP Lit materials (track/accent/environment/accent/gold/obstacle/player/skin), ParticleDot texture asset, particle material, `UIInputActions.inputactions` and eight persistent `InputActionReference` assets. Existing assets at those paths are reused without rewriting; invalid types/references fail safely. A missing shader/importer is a setup error, not a fallback to broken pink assets. Created assets cannot be reverted by normal scene Undo; remove them only after checking references.

Visual mesh children have no Colliders. Player/Coin root primitive renderers may be hidden; original Transform, Rigidbody, Collider and gameplay settings remain intact. To replace the runner, use the PlayerVisual/RunnerPresentationController swap seam described above (the older RunnerProxy is retained inactive), add your FBX/Animator under PlayerVisual and retain root physics/controller; restore the root MeshRenderer only if returning to the primitive. Replace CoinVisual without moving the collider and update/remove its visual animator reference. Replace decoration, UI and VFX deliberately; ownership markers let repair distinguish generated from manual children. Remove/update bindings and bridge references when adopting a custom Canvas; do not layer duplicate adapters.

`Validate Current Game Setup` is read-only: **CRITICAL ERRORS FOUND** for unsafe ownership/references/physics/dependencies; **LEVEL 1 SETUP INCOMPLETE** for missing required presentation/UI/VFX; **READY FOR LEVEL 1 PLAY TEST** when those structural requirements pass. Optional missing clips/catalogs/next scene are still Console warnings but do not block Level 1 readiness. It cannot prove rendering, physics contacts, event delivery or native serialization. A valid next build scene may be a utility scene: review progression manually.

Temporary/replace later: primitive runner, sinusoidal animation, generated environment, UI styling and basic VFX; professional audio is absent. Preserve gameplay systems, state architecture, automation, persistence and service abstractions. No economy, missions, ads or remote-config gameplay adapters are installed by this presentation pass.

The remaining sections document the existing architecture-only commands and manual alternatives. Their narrower preservation rules still apply; use the Level 1 master for generated presentation.

## 0. Existing architecture automation and safe boundaries

**Codex → GitHub → GitHub Desktop Pull → Unity → Tools > Sajid Game Factory > Setup / Repair Entire Game → Validate Current Game Setup → Play Test**

1. Pull the pushed commits in GitHub Desktop. Open the existing project in pinned Unity 6000.0.84f1, resolve import/compile errors, and open `Assets/Scenes/MainGame.unity` as the active scene in Edit Mode, outside Prefab Mode. This cloud task cannot execute Unity.
2. Keep a clean/reviewable scene before running automation. Run **Tools > Sajid Game Factory > Validate Current Game Setup** first for a read-only baseline.
3. Run **Setup / Repair Entire Game**. Its preflight verifies Player, CameraFollow, Rigidbody/colliders, ScoreManager and Coin_* contacts. Critical missing prerequisites, duplicate owners and conflicting gameplay references stop the master pass before edits. It does not repair missing Player/Camera/Score/Coin systems by inventing them.
4. The master then runs obstacle setup, MainGame build inclusion, finish/level setup, core managers, audio/settings, economy, optional service placeholders, UI preparation, and final validation in that order. Individual steps can fail/roll back independently. Earlier completed steps are not a whole-game transaction; inspect warnings and the final report rather than assuming every step succeeded.
5. Save MainGame manually after reviewing changes. A profile override is marked dirty and must be saved; the shared build list is updated through Unity's native EditorBuildSettings API. No automatic scene save, Build Profile switch, scene reordering or creation of a next level occurs.
6. Run validation again. The public validator now uses the Level 1 states explained above; architecture-only setup can leave Level 1 presentation incomplete. Internal architecture preflight retains its separate warning/error assessment. These are structural results, not compilation/physics/build certification.
7. Run the same setup twice, compare component/reference counts and gameplay values, then test Undo/Redo and save/reopen persistence. Do this before relying on the tool for repeated repairs.

All menu commands are under **Tools > Sajid Game Factory**:

| Command | Behavior |
| --- | --- |
| Setup Obstacle & Game Over | Existing command: reuse/create Managers, wire GameManager, configure Player body and Obstacle_* triggers. |
| Ensure MainGame In Build Scenes | Existing command: append MainGame or re-enable its entry in the active list, preserve all other entries/order, reject unsafe states. |
| Setup Finish Line & Level Progression | Existing command: requires Managers/GameManager, Player physics and FinishLine; add/wire LevelManager/FinishLine, configure triggers, warn about scene progression. |
| Setup Core Managers | Add/reuse the 14 core managers listed below and fill only empty exact unique dependencies, including PauseManager references in both loaders. |
| Setup Audio & Settings | Add/reuse Save/Audio/Settings managers, fill dependencies, create two missing sources only when roles are unambiguous. New Music: Loop on; new SFX: Loop off; both Spatial Blend 0 and Play On Awake off. |
| Setup Economy Systems | Add/reuse Reward/Daily/Shop/Skin/Mission managers, assign RewardManager, warn about empty catalogs. No IDs/prices/balancing/assets or PlayerPrefs writes. |
| Setup Services Placeholders | Add/reuse Analytics/Ads/RemoteConfig, optionally wire Analytics. New ads default disabled; preserve and warn about existing mock mode. No SDK, provider request, network call or currency grant. |
| Prepare UI Architecture | Add/reuse UIManager/HUDController, wire only compatible unique existing panels/text using the names below, log missing-element checklist. No visual creation or event bridge. |
| Setup / Repair Entire Game | Run verification and setup passes in dependency order, then authoritative final validation. |
| Validate Current Game Setup | Read-only scene/components/references/build/audio/catalog/services/UI inspection. No Undo, SetDirty, scene save, gameplay calls, preference repair or provider invocation. |

New setup passes reuse unique existing components even outside Managers and warn about location. They do not create another owner when duplicates already exist. Non-null references and broken serialized references are preserved with warnings. Disabled objects/components remain disabled. Resolve these cases manually; separate legacy obstacle/finish commands retain their existing ownership rules, and master preflight refuses incompatible ownership.

Close other loaded scenes containing managers before running the new setup/master commands; they refuse additive-scene manager duplication. Validation reports these external owners as errors. Additional loaded visual scenes without manager owners do not prevent setup.

**Undo:** scene passes use Undo.AddComponent, creation/object recording, grouped operations, prefab-instance override recording, explicit scene dirty marking and best-effort rollback on exceptions. Existing Build Profile assets have object Undo; shared build-list changes have no normal object Undo target, logged by the command. Scene Undo does not undo a shared build change or PlayerPrefs created later in Play Mode. Save reviewed profile changes separately.

**Audio preservation:** assigned sources keep all settings/clips/volume/mixer routing. One unassigned source for one empty role can be reused unchanged. Multiple unassigned sources or one source for two unknown roles are ambiguous: the tool adds nothing and asks for explicit Music/SFX assignment. New sources never receive a clip. Review existing sources if warnings recommend different loop/spatial/awake settings.

**UI discovery convention:** independent RectTransform panels under a Canvas named `MainMenuPanel`, `GameplayHUDPanel`, `PauseMenuPanel`, `GameOverPanel`, `LevelCompletePanel`, `SettingsPanel`; legacy Unity UI Text objects named `ScoreText`, `BestScoreText`, `LevelText`, `CoinCountText`. A candidate must be unique across MainGame. Panels cannot contain managers/Player/Camera/Score or overlap other panel roots. Different names, TMP text and ambiguous layouts need explicit Inspector assignments. The tool never changes layout, fonts, colors, anchors, graphics or panel activation. Review initial UI state: it remains MainMenu by default and does not stop automatic Player movement.

**Intentionally manual:** visual design; models; animation; professional UI and further button/event adapters; clips/music; mission definitions/balancing; shop IDs/prices; skin IDs/model swapping; next-level scene creation; real ads and analytics SDKs; server/cloud save; Android/WebGL/iOS platform modules, signing, store settings and final builds.

**Exact next Unity test sequence:** pull → import/clear Console → open MainGame → read-only baseline validation → master setup → inspect warnings/references/counts → save scene/profile → run master twice and test Undo/Redo → revalidate → test movement/camera/coins → separate runs for obstacle GameOver and finish LevelComplete → test restart/next/final-level refusal → test pause/reload → audio/settings → disposable economy/mission saves → disabled service placeholders → manual UI event delivery → development player build. No Unity compilation, Play Mode, physics or build success was verified in Codex.

## 1. Open and import

1. Open the existing checkout using Unity **6000.0.84f1**, revision `78ab6fc243d5`. Do not upgrade or rename scripts as part of initial verification.
2. Resolve the existing packages and clear every Console compile/import error before configuring objects. Input System 1.20.0 and Unity UI 2.0.0 are declared and match the lockfile; that does not prove package compatibility or registry access. AI/GDK/analytics/IAP packages already in the manifest may have their own platform requirements.
3. Keep **Active Input Handling = Input System Package (New)**; the serialized value is currently `1`. Keyboard input requires the Input System package, not the legacy Input Manager.
4. Existing runtime and Editor scripts now have committed `.meta` files. Preserve their GUIDs and check for Missing Script components on import; do not move or rename files yet.
5. Open `Assets/Scenes/MainGame.unity`. This checklist describes changes to make later in Unity; the architecture audit did not modify this scene.

## 2. Create manager objects and assign references

Reuse the always-active **Managers** object outside every UI panel (automation creates it if absent). Add one of each scene-owned component below; multiple different component types can share this object. `[DisallowMultipleComponent]` prevents duplicate types on one GameObject, not duplicate objects elsewhere.

| Component | Inspector references/configuration |
| --- | --- |
| GameManager | Player's PlayerController; the PauseManager that owns this run |
| LevelManager | This scene's GameManager; the same PauseManager |
| ScoreManager | No references; run score starts at zero |
| PauseManager | No references; one authority for global `Time.timeScale` |
| SaveManager | No references; settings and best/unlocked progress |
| RewardManager | No references; shared persisted currency |
| DailyRewardManager | RewardManager; positive daily amount |
| AudioManager | SaveManager; separate Music and SFX AudioSources |
| SettingsManager | The same SaveManager; AudioManager |
| UIManager | Six panel roots; an initial state appropriate to this scene |
| HUDController | Score/best/level/count Text references or compatible string UnityEvents |
| ShopManager | RewardManager; unique item IDs and nonnegative integer prices |
| SkinManager | Valid default skin ID; additional skin IDs |
| MissionManager | RewardManager; active MissionDefinition assets |
| AnalyticsManager | Debug Logging optional; no provider required |
| AdsManager | AnalyticsManager optional; mock mode OFF; cooldown, session cap, timeout |
| RemoteConfigManager | Safe local defaults; no provider required |

Use one owner of each global/data service per intended lifetime. Keep GameManager, LevelManager, ScoreManager, UI references, and the player scene-owned: their references/build indices become invalid if moved to `DontDestroyOnLoad`. Services can be scene-owned initially. A future bootstrap may retain AdsManager for an entire app session, but must not create duplicates or retain obsolete scene references. Ads limits are currently per AdsManager instance and reset if recreated. No automatic singleton or lifetime bootstrap is provided.

Assign all references before enabling objects. If LevelManager's GameManager reference changes at runtime, disable/re-enable LevelManager to rebind its event subscription. Never put manager objects under a panel that UIManager will disable.

## 3. Player and camera

1. Keep PlayerController on the Player root. The Player's collider can be on that root or a child: pickups use `GetComponentInParent<PlayerController>()` and need no Player tag.
2. Add a **3D Collider** and **Rigidbody** to the Player root. Set **Is Kinematic = true**, **Use Gravity = false**. Avoid another Rigidbody on a child collider. Movement remains world-space Transform movement, not forces; no jumping or dynamic physics controller is implemented.
3. Set forwardSpeed/horizontalSpeed; defaults are 8/5. Confirm world +Z is forward and the track spans X = -4.5 to +4.5. A/D and Left/Right arrows steer; opposing keys cancel. Mobile/touch/gamepad support is not implemented.
4. Put CameraFollow on the camera and assign the Player Transform. Default offset is world-space `(0, 5, -8)`. Configure smoothing, optional target look-at, and look-at offset. Verify a nondegenerate view direction and finite offsets. The camera uses scaled time, so following freezes during pause; optional look-at still updates orientation.
5. Validate contacts at normal and maximum speed. Transform movement with `Physics.autoSyncTransforms` currently off needs Play Mode validation. Thin triggers can be missed between physics steps. Prefer wider trigger volumes; do not silently enable global sync or replace movement with Rigidbody physics during this setup.

## 4. Coins, obstacles and finish line

1. For each coin, add Coin and a **3D trigger Collider** (`Is Trigger = true`). Assign this scene's ScoreManager and a nonnegative score value.
2. For each obstacle, add Obstacle and a **3D trigger Collider**. Assign GameManager. Its non-trigger collision callback also exists, but a kinematic Player versus a static collider does not guarantee `OnCollisionEnter`; use triggers for this prototype.
3. For the finish object, add FinishLine and a **3D trigger Collider**. Assign LevelManager. Ensure the trigger spans the track and is wide enough for the speed.
4. At least one participant in each trigger pair must have a Rigidbody (the Player's kinematic Rigidbody suffices). Enable their layer interactions in the Physics layer collision matrix. Do not use 2D physics components.
5. Verify one award despite multiple Player colliders. Collected coins disable themselves; finish lines latch completion. These flags are reset by scene recreation, not by re-enabling pooled objects. Object pooling/reset APIs are future work.
6. Coin `Collected(int scoreValue)` fires once after collection; report **one** coin to missions/count UI, not scoreValue coins. Coins add score only; reward currency is separate.

Current MainGame contains Player's kinematic/gravity-free Rigidbody and coin/obstacle/finish trigger configuration committed after prior user testing. Preserve that setup. New automation behavior, contacts at different speeds/layers and lifecycle conditions still require Unity verification.

## 5. Build Profiles / scene list

1. In Unity 6 **Build Profiles → Scene List** (formerly Build Settings), keep gameplay levels in intended consecutive build-index order. The current shared list contains SampleScene followed by enabled MainGame; no following level is present. Ensure MainGame In Build Scenes respects the active profile override/shared list but never creates a next level or reorders entries.
2. Do not insert a menu/utility scene between gameplay levels: LoadNextLevel uses `currentIndex + 1`, not scene names or a separate level catalog. Indices are zero-based; displayed level numbers can use index + 1.
3. Keep GameManager/LevelManager in the gameplay scene whose index they manage. They use their own GameObject's scene, not an unrelated active scene.
4. Assign PauseManager to both loaders. Scene-local PauseManager also resumes on disable, but a future persistent pause owner requires explicit resume before loads.
5. Verify final-level LoadNextLevel refuses gracefully. RestartGame/RestartCurrentLevel reload the scene in Single mode and recreate run objects. StartGame only resumes the existing PlayerController/state; it does not reset position, score, coins, missions or persisted data.

## 6. UI and event wiring

1. Create a Canvas and six separate **sibling** panel roots later: Main Menu, Gameplay HUD, Pause Menu, Game Over, Level Complete, Settings. Assign them to UIManager. Do not reference UIManager's own object, any manager ancestor, or nested/overlapping panels.
2. For MainGame, choose **GameplayHUD** initially while GameManager automatically starts Playing in Awake. A Main Menu panel alone does not stop movement. A future menu/start coordinator must hold the player until a deliberate run start; there is no Waiting/Menu game state yet.
3. Put HUDController on the HUD. Assign legacy `UnityEngine.UI.Text` components, or leave those fields empty and connect its text-update UnityEvents to TMP components' dynamic string `text` property. TMP is not a compile-time dependency in HUDController. Import TMP essentials if required by the installed Unity UI package.
4. C# `event Action...` members are **not Inspector UnityEvents**. A small scene/bootstrap bridge must subscribe in OnEnable and unsubscribe from the exact publisher in OnDisable. The Level 1 master now creates Level1PresentationController for basic HUD/state/settings/audio/VFX only; do not add duplicate subscriptions. Mission/analytics/economy adapters remain manual.
5. Suggested wiring (avoid duplicate adapters):

| Publisher / call | Consumer |
| --- | --- |
| GameManager.GameOverTriggered | UIManager.ShowGameOver; SaveManager.UpdateBestScore(score.GetScore()) |
| GameManager.LevelCompleted | UIManager.ShowLevelComplete |
| LevelManager.CurrentLevelCompleted(index) | MissionManager.ReportLevelCompleted; explicitly unlock index + 1 only if it exists |
| PauseManager.PauseStateChanged(paused) | ShowPauseMenu when paused; otherwise select HUD or terminal panel using GameManager.CurrentState |
| ScoreManager.ScoreChanged(score) | HUDController.UpdateScore; MissionManager.ReportScore |
| Coin.Collected(scoreValue) | Increment separate run coin count; ReportCoinsCollected(1); TrackCoinCollected(scoreValue) |
| RewardManager.RewardBalanceChanged(balance) | A dedicated currency display, or HUD.UpdateCoinCount if intentionally using that slot for currency |
| SettingsManager's three change events | Future setting displays |
| Shop/Mission/Skin/Daily events | Future UI/audio/analytics adapters; never grant the same reward in two listeners |

6. Initialize HUD with current score, SaveManager.BestScore, your human-readable display level (MainGame is Level 1 despite its build index), and your chosen coin/currency count **after all Awake methods**. Query game/pause/settings state when subscribing: initial events may already have fired.
7. Call MissionManager.ReportRunPlayed once per deliberate run start, not every OnEnable or StartGame call. Methods with non-void returns and optional parameters may not be selectable in Inspector button UnityEvents; future buttons should use void adapter methods. Update methods taking one int can use dynamic int listeners where a UnityEvent exists.
8. UI panel methods only affect visibility. Resume, restart, next-level, and settings navigation must explicitly call gameplay APIs. The pause resume handler must not overwrite a GameOver/LevelComplete panel with HUD.
9. HUD shows numeric values without fixed labels. Use separate label objects or format through the text-update event. A throwing UnityEvent listener can skip later UnityEvent listeners; the HUD catches that failure but cannot isolate every runtime listener individually.

## 7. Audio and settings

1. Create two AudioSources and assign Music/SFX. Disable **Play On Awake** and use **Spatial Blend = 0** for global audio. Ensure an enabled AudioListener exists, normally on the camera.
2. Call PlayMusic(clip) to start the desired track. Repeated calls for the same currently playing track do not restart it. Disabled music is muted while playback continues, preserving position; StopMusic stops playback.
3. Call PlaySFX(clip, volumeScale) for one-shots. AudioManager rejects non-finite volume and clamps finite volume to 0–1. Distinct sources are required.
4. Change preferences through SettingsManager so SaveManager persistence and AudioManager updates stay synchronized. The casing difference `SaveManager.SetSfxEnabled` / `SettingsManager.SetSFXEnabled` is intentional and compatible.
5. After SaveManager.ResetData call SettingsManager.ReloadSettings. ResetData does not reset currency, shop entitlements, daily eligibility, or mission claims. Vibration is a preference only.
6. Time.timeScale does not pause Unity audio playback automatically; audio pause behavior is future work.

## 8. Rewards, daily rewards, shop and skins

1. Configure a positive DailyReward amount and assign RewardManager. Call CanClaimDailyReward/ClaimDailyReward through future adapters; check the bool result.
2. UTC calendar days are used, not rolling 24-hour periods. GetTimeUntilNextClaim returns `TimeSpan.MaxValue` for blocked/unknown eligibility; display an unavailable state rather than an overflowing countdown.
3. For deterministic tests, inject a `Func<DateTime>` via SetUtcTimeProvider, always returning `DateTimeKind.Utc`. Do not delete rollback/claim records to force normal production eligibility. Reset only test-profile data deliberately.
4. Use matching IDs in ShopManager and SkinManager. IDs must contain 1–64 ASCII letters/digits or `.`, `_`, `-`; they are case-sensitive. Prices are integer currency amounts; zero-priced grants are allowed and negative prices are rejected. Exclude the default skin from paid shop entries.
5. Configure exactly one SkinManager/default ID across the profile. Repeated skin entries collapse; duplicate shop entries are rejected. Ownership backup flags can preserve an undamaged entitlement; if both copies are unusable, recovery is unowned.
6. UnlockSkin grants ownership but does not swap a model or grant currency. SelectSkin only selects available owned skins. Shop purchases share ownership storage automatically but emit shop events, not SkinUnlocked events.
7. Test on a disposable PlayerPrefs profile: malformed flags, insufficient funds, repeated purchases, removed skins, selected-skin fallback, overflow limits, duplicate daily claims, midnight, and clock rollback.

## 9. Mission assets and migration

1. Use **Create → Endless Runner → Mission**. Set a unique mission ID, type, positive target, positive reward, definition version ≥ 1. Assign the assets to MissionManager.
2. Report CollectCoins/CompleteLevels/PlayRuns as counts; ReachScore uses the highest reported score, not the sum of score updates. Collecting a value-5 coin still counts as one coin.
3. Use CanClaimMissionReward/ClaimMissionReward and check the result. Claims persist consumption markers before currency events. Do not also add currency from MissionRewardClaimed.
4. Compatible target changes clamp existing progress; reward changes retain progress. Increase DefinitionVersion for incompatible semantics or type changes; progress resets but claim history remains consumed.
5. For renames, configure Previous Mission IDs **before loading the renamed mission's data**. Alias progress imports once; later attaching an alias to an already initialized canonical record does not re-import its progress. Claim history is still merged. Avoid overlapping aliases across active definitions.
6. Never reuse IDs/aliases for a different mission. Removed missions retain data and claim markers. A genuinely new repeatable mission requires a new ID; daily/weekly rotation is not implemented.
7. Verify migration from old two-key saves, schema-1 records, malformed JSON, negative/excess progress, claimed markers, renamed IDs, and incompatible definition changes. Corrupt/uncertain reward history is conservatively treated as consumed and logged; newer unknown schemas remain untouched/unavailable.

## 10. Ads, analytics and local config

1. Leave providers null for safe disabled/local behavior. No real SDK or fetched service is installed by these scripts. Future providers implement IAnalyticsProvider, IAdProvider, IRemoteConfigProvider and are injected with SetProvider; interfaces are not serialized Inspector references.
2. All future callbacks/adapters must execute on Unity's main thread. No dispatcher, network client, SDK initialization, consent flow, or cancellation of actual SDK presentations is implemented.
3. For local ad tests only, enable mock mode, call ShowRewardedAd, then CompleteMockAd(true). Test unsuccessful completion, duplicate/late callbacks, timeout, provider replacement and disabling mock mode mid-request. Disabled mock mode must not emit reward eligibility.
4. Subscribe **one** currency adapter to RewardedAdRewardGranted(requestId, placement). It receives eligibility only, not proof currency was saved. Deduplicate request IDs in the gameplay transaction layer for any future cross-restart/server-backed rewards.
5. Interstitial cooldown uses unscaled real time and the cap is per AdsManager instance. Keep one manager for the intended session or explicitly accept a cap reset at scene recreation. No progression-based frequency adapter exists yet.
6. Enable Analytics Debug Logging for development only; connect explicit reporting calls. Never include credentials or personal data in debug parameters. Event names/keys must be lowercase snake_case, ≤64 characters; accepted scalar parameters are bounded and immutable snapshots.
7. RemoteConfigManager's known-key typed getters return safe local values. Invalid override batches leave previous values intact. Use its key constants and the correct getter type. Unknown/wrong-type keys use caller fallbacks; do not feed arbitrary fallback values directly into gameplay.
8. ConfigUpdated does not automatically apply speed, reward, ad frequency or saved settings changes. A future adapter must use validated APIs and preserve users' explicit saved preferences. Overrides are in-memory only and reset on manager recreation.

## 11. Recommended verification order

1. Unity import/Console compilation, generated GUIDs, package availability, and a development player build.
2. Player input/world bounds and camera; null keyboard/target behavior; different frame rates.
3. Physics contact at speed; one coin score/event; multiple child colliders; terminal-state contacts rejected.
4. Score overflow, GameOver/LevelComplete one-shot transitions, throwing listeners, restart/next-level bounds, final-level refusal.
5. Pause at time scales 1 and 0.5, repeated calls, external overrides, disable, reload while paused, and persistent-owner references.
6. Audio/settings persistence and ResetData + ReloadSettings; verify default and corrupt flags.
7. HUD initialization and subscriptions across panel enable/disable and scene reload. Menus must not leave movement active accidentally.
8. Currency/daily/shop/skins and mission migration/claim idempotency using disposable local records.
9. Disabled ads, explicit mock completion, cooldown/cap/timeout; analytics throwing provider; config invalid/stale callbacks and safe defaults.
10. Test duplicate manager setup on different GameObjects, no-domain-reload Play Mode, device pause/resume, and supported build platforms. Confirm there is one intended service owner and no duplicate reward adapter.

These expansion/regression checks are pending until Unity is opened. Prior runner behavior was reported verified by the user; the cloud source audit does not claim execution of the new menus, Unity compilation, Play Mode, physics or builds.
