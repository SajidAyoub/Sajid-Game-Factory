# Game Factory architecture and audit

## Fantasy/wolf extension (current pass)

The first action was fetch/rebase onto `1b043ad` (Generate and verify Level 1 Master), with no conflicts. That commit includes the user's generated scene, materials/input assets and metadata. This code pass does not rewrite those files. It extends the same master and adds **Upgrade / Repair Level 1 Fantasy Presentation** as an alias; there is no import-time scene generation.

| File | Change / responsibility |
| --- | --- |
| GameFactorySetup.Fantasy.cs (new Editor partial) | Separate Fantasy palette, bridge/supports/rails/crystals/runes, geometric cliffs/static waterfall ribbons, ownership-checked legacy retirement and known-generated material reference migration. |
| GameFactorySetup.FantasyRunner.cs (new Editor partial) | White-wolf primitive hierarchy, tiny shared six-triangle ear Mesh asset, jacket/outfit/accent slots, explicit animator/controller references. Preserves authored models. |
| GameFactorySetup.FantasyPolish.cs (new Editor partial) | Coin emblems/rim details, hazard frames, finish crown and generated UI cards/rules; guarded default color/counter/proportional-layout/transition migration and HUD safe-area component. Existing listeners/references remain unchanged. |
| GameFactorySetup.FantasyValidation.cs (new Editor partial) | Read-only fantasy existence/activation/selection/physics/animation/UI checks and honest visual-review warning. |
| GameFactorySetup.Level1.cs (existing) | Four calls after original UI preparation: fantasy environment → wolf → feedback/gate → UI. Uses the existing presentation Undo group and final validation. |
| GameFactorySetup.Level1UI.cs (existing) | Adds the read-only fantasy validator to the existing validation path. |
| RunnerPresentationController.cs (new runtime) | Generated/custom visual selection under PlayerVisual; rejects root/physics/overlapping roots, leaves Player gameplay intact, exposes safe VisualChanged and explicit Head/Body/Outfit/Accent material application. No skin ownership logic or PlayerPrefs. |
| Level1HUDSafeArea.cs (new runtime) | Insets only the HUD anchors to Screen.safeArea when area/resolution changes, with no frame allocations; restores baseline anchors on disable. No Edit Mode execution. |
| SimpleRunnerVisual.cs (existing runtime visual helper) | Optional validated child poseRoot, bounded finite bob/lateral lean, position reset on enable and rest-pose restoration. Existing old proxies have no poseRoot, retaining their prior behavior. No Player movement or controller changes. |
| README.md / GAME_FACTORY_SETUP.md / this guide | Fantasy approximation, migration, swap instructions, Undo/asset limitations and exact pending Unity checks. |

No original gameplay, UI/state/economy/service manager script is changed. No scene, prefab, existing asset/meta, package or build list is changed in Codex. References/events/persistence/time-scale authority remain those documented below. AudioManager's five optional bridge clip hooks and the three existing bounded ParticleSystems are reused without audio downloads or additional runtime effects.

The tool generates thirteen Fantasy materials and one flat-shaded WolfEar Mesh asset only inside Unity. Emission is shader-based brightness, not bloom. Geometric waterfalls are deliberately opaque/static to avoid transparent sorting, overdraw and fluid simulations. Bridge supports imply elevation below the verified road rather than moving Ground/Player/contacts. Environment geometry is approximately 140 simple shapes; other details add renderers. Existing SRP batching is not a performance guarantee: target-device profiling and possible future mesh combining/LOD are required.

Migration is explicit and ownership-based: old owned RunnerProxy and environment decorations are retained inactive; only known generated material references are redirected. Custom/edited old geometry remains on those objects, so review before saving. Unexpected custom components under legacy decoration stop retirement; author-created player models skip the wolf. Generated-child/asset identity collisions abort rather than overwrite. Repeated calls reuse paths, identities, components and listeners; authored slot arrays, custom material references/styles and camera/lighting are preserved. Whole-pass asset/scene atomicity remains unavailable; generated assets and earlier core steps survive presentation rollback.

RunnerPresentationController has no singleton or scene search. SelectVisual validates child roots before toggling, emits via GameFactoryEvents' bounded exception-isolated notification, and reports failure without moving Player. ApplyMaterials touches only configured descendant Renderers and uses shared materials. Slot arrays are generated only when empty. SkinManager remains a separate data authority; a future equipment adapter can listen to its selection event and call presentation APIs. Supply a real rig/Animator and remove/disable procedural proxy animation deliberately; no FBX/skeleton is generated.

New static checks examine **37 C# files, twelve menu registrations and 29 read-only validation helpers**. Protected diff checks compare against `1b043ad`: only SimpleRunnerVisual is extended among existing runtime scripts, all gameplay/scene/assets/meta/packages/build settings unchanged. Exact new serialized fields and optional material slots, event isolation, no runtime Editor APIs, child-only animation and safe-area arithmetic were source-reviewed. Corrected a source-review compile risk in the safe-area Vector2 arithmetic before committing; these checks are not C# compilation or Unity execution.

**Remaining uncertainty:** no reference images are accessible in this task, so likeness follows the written description. Native mesh/rendering/shader behavior, Unity serialization/Undo, import/compilation, UI color/input/safe-area behavior, pause/reload, physics and mobile/player builds are pending Unity verification. No claimed reproduction of a professional character/environment or verified visual quality. Local PlayerPrefs security/transaction limitations are unchanged, and no ads/analytics/remote-config/network/skin-shop coupling was added.

## Earlier Level 1 Master extension (retained)

**Workflow:** Codex → GitHub → Unity Pull → Tools > Sajid Game Factory > Build / Repair Level 1 Master → Validate Current Game Setup → Play Test → Visual review → Lock Level 1 → Duplicate architecture for future levels.

No existing runtime gameplay script, scene, prefab, visual asset, metadata, package or build configuration was changed in Codex. The only existing code change is GameFactorySetup.cs becoming partial and adding Level 1 validation reporting. README and both guides are updated. No generated Unity assets or metadata were hand-written.

| New file | Responsibility / dependency contract |
| --- | --- |
| Assets/Editor/GameFactorySetup.Level1.cs | Master menu, owned hierarchy, URP materials, runner/coin/environment/obstacle/finish geometry and conservative lighting. Calls existing core automation first; does not replace it. |
| Assets/Editor/GameFactorySetup.Level1UI.cs | Generated particles, portrait Canvas/Text/buttons, persistent Input System assets/references, bridge Inspector wiring and read-only Level 1 validation. |
| Assets/Scripts/Level1GeneratedObject.cs | Serialized ownership ID for generated scene children; no gameplay or event behavior. |
| Assets/Scripts/SimpleRunnerVisual.cs | Actual Player displacement and enabled PlayerController drive bounded local limb swings; restores rest pose when stopped. Does not move Player or require Animator. |
| Assets/Scripts/CoinVisualAnimator.cs | Bounded local child-only rotation/bob using unscaled time; never animates root/collider. No per-frame managed allocations. |
| Assets/Scripts/Level1PresentationController.cs | Scene-owned presentation adapter with Inspector dependencies. Initializes HUD after Awake, subscribes once to exact publishers, unsubscribes on disable, routes UI/state/settings/audio/VFX without runtime scene searches. |

### Presentation data and state flow

The always-active bridge lives outside every panel. It observes GameManager.StateChanged/GameOverTriggered/LevelCompleted, ScoreManager.ScoreChanged, PauseManager.PauseStateChanged, RewardManager.RewardBalanceChanged, the three SettingsManager preference events and each unique Coin.Collected. Coin callbacks capture the coin position source; existing Coin already snapshots its event before disabling itself. Exact publisher snapshots and cloned coin subscriptions make disable/re-enable unsubscribe reliably even if Inspector fields change. Do not add a second bridge or separate listeners performing the same presentation action.

HUD is initialized in Start and displays current score, max(saved best/current score), explicit displayLevelNumber=1, per-run collected coin count and independent RewardManager balance. Coin score value is not coin count or reward currency. GameOver/LevelComplete call existing SaveManager.UpdateBestScore; no other save schema/key/reset is added. Run count resets by scene recreation. Settings changes continue through SettingsManager/SaveManager; no direct PlayerPrefs call exists in the helpers.

Terminal state has priority over Settings/Pause/HUD. Pause and Settings use the existing PauseManager time-scale authority; Back restores the prior pause state, repeated OpenSettings does not overwrite it. Restart/next delegate to existing loaders, which own pause restoration. Next is disabled until completion and a subsequent build index exists; no Level 2 is fabricated. A real Main Menu start gate remains future work: its generated panel is hidden, and existing automatic Playing behavior is preserved.

Three preallocated world-space ParticleSystems use manual bounded emission (10 coin, 16 hit, 24 finish; maximum 64 each). The bridge exposes public play methods and listens to existing events. Optional backgroundMusic/coinSFX/hitSFX/finishSFX/uiClickSFX use AudioManager and saved preferences; missing clips are safe and reported as optional warnings. No audio is supplied. Particle visibility/native simulation still needs Unity testing.

### Generation, preservation and Undo

Assets are created only when the user runs Master under Assets/GameFactory/Generated/Level1: eight URP Lit materials, a procedural radial texture, URP particle material, a persistent default Input System action asset and eight UI action-reference assets. This avoids transient action references disappearing on scene reload. New modules bind the persistent asset; existing compatible modules and user assignments are preserved. Confirm the pinned Input System package importer/API and saved bindings in Unity.

Scene containers group environment, lighting/visuals, VFX and UI; player, coin, obstacle and finish children stay under their original gameplay owners. Primitive decoration colliders are removed immediately using Undo, leaving original physics untouched. Parent-scale compensation is applied only on first creation. Built-in prototype materials/renderers can be upgraded/hidden; authored materials/models and camera/custom directional lighting are preserved. Existing generated transforms/materials/styles are not overwritten. Unexpected asset types, unowned same-name children, duplicate owners, custom Canvas ambiguity and conflicting/broken references fail safely rather than merge blindly.

Scene changes use Undo creation/addition/recording, persistent button listeners and explicit dirty marking. Missing owned CanvasScaler/Raycaster can be repaired without duplicate components. Presentation failure attempts group rollback; previously completed core/build passes and newly created assets remain. Normal scene Undo does not undo asset creation or shared build-list edits. Review Console, save manually and commit Unity-generated assets/metadata only after testing. Custom generated UI can be edited in place; replacing it wholesale requires deliberate reference/binding migration.

### Current audit and unresolved items

Static checks cover all **31 C# files**, legal partial declarations, eleven menus, exact serialized manager/bridge fields, void button methods, subscription/unsubscription pairs, assembly boundaries and 28 read-only validation methods. Protected diff checks confirm original gameplay scripts, scene, assets, metadata, packages and build configuration are unchanged. Source-reviewed Unity 6000 UnityEvent APIs and Input System source for persistent action handling; this is not semantic compilation of the pinned package.

Fixed during source review: persistent input assets replace transient defaults; duplicate coin entries cannot multiply callbacks; repeated Settings opening preserves prior pause state; disabled legacy input modules do not falsely conflict; existing custom models are preserved; coin dimensions follow original collider bounds; missing owned UI helper components repair safely; broken reference IDs are preserved/reported; disabled required managers are errors. Generated particles remain bounded and no currency/provider/reset calls were added.

The public validator ends with CRITICAL ERRORS FOUND for errors, LEVEL 1 SETUP INCOMPLETE for missing required visuals/UI/VFX, otherwise READY FOR LEVEL 1 PLAY TEST. Optional audio/catalog/next-level warnings remain visible without blocking readiness. It inspects component/reference/button structure, not runtime delivery, shader quality, physics cooking, asset serialization, native Undo or platform performance.

**All pending Unity verification:** import/compilation, Play Mode, physics, material rendering, VFX, UI layout/input/visual quality, save/reopen and reload behavior, repeated execution/Undo/Redo and player builds. Existing custom UI/modules may need manual migration; Input System import may fail; legacy fonts and touch/safe-area layout need review. Primitive geometry/material counts need mobile profiling. Transform-based movement still risks tunneling through thin triggers. There is no whole-command asset/scene transaction, no pooling reset, no multi-reason pause stack, no professional audio or art, and no trusted persistence/security improvement. Main-thread event/provider dispatch remains required. Mission, ads, analytics and remote-config gameplay integration remains deliberately absent.

Temporary/replace later: primitive character/procedural animation, generated environment, UI styling and basic VFX. Preserve gameplay, state architecture, automation, persistence ownership and service abstractions. PlayerPrefs remains editable/nontransactional prototype storage, not secure production currency or entitlement storage. The retained audit below documents remaining architecture/security risks in detail.

## Earlier architecture automation (retained)

The user reports that the original runner movement/camera, coin/score, obstacle/game-over, finish completion and MainGame build inclusion were verified in Unity. The committed scene now includes those components and references, Player's kinematic/gravity-free Rigidbody, contact triggers and script metadata; MainGame follows SampleScene in the shared enabled build list. There is no next enabled level. Neither the earlier architecture pass nor the Level 1 code task changes that scene directly, existing runtime scripts, Build Settings, existing metadata, packages or verified gameplay values.

The earlier architecture pass modified GameFactorySetup.cs and these guides without creating files. The current Level 1 extension adds the six files documented above. Editor automation runs only when the user invokes its menu commands; there is no import-time setup, ExecuteAlways component or runtime Editor dependency. The tool is now one static partial class across three Editor files; existing commands retain their setup/discovery contracts.

**Workflow:** Codex → GitHub → GitHub Desktop Pull → Unity → Tools > Sajid Game Factory > Setup / Repair Entire Game → Validate Current Game Setup → Play Test.

### Editor responsibilities and contracts

| Layer / command | Responsibility and limits |
| --- | --- |
| Setup Core Managers | Reuse/create Managers; add/reuse Game/Level/Pause/Save/Audio/Settings/Reward/Daily/Shop/Skin/Mission/Analytics/Ads/RemoteConfig. Unique managers elsewhere are reused, not moved. Duplicate owners are left alone and reported. |
| Serialized wiring | Exact field/type checks, unique target discovery, empty-slot assignment only. Non-null/broken references, disabled components, catalogs and gameplay values are preserved; ambiguities require manual repair. Reflection checks metadata only; it does not invoke gameplay or access internal runtime persistence helpers. |
| Setup Audio & Settings | Prepare Save/Audio/Settings dependencies and two distinct source roles. New sources use 2D audio, Play On Awake off, Music loop on / SFX loop off. Assigned sources and one unambiguous reusable source remain unchanged. Unknown roles are never inferred from component order. |
| Setup Economy Systems | Reward dependencies for Daily/Shop/Mission; existing SkinManager. No IDs, prices, assets, claims, selected skin, balances or local-save mutation. Empty catalogs are explicit warnings. |
| Setup Services Placeholders | Optional Analytics/Ads/RemoteConfig components and Ads' optional analytics link. Preserve existing flags/config. New ads retain disabled default. No provider injection, fetch, SDK, network, analytics report or reward adapter. |
| Prepare UI Architecture | UIManager/HUDController and unique existing compatible named panel/Text references. Reject panel ancestry that can disable gameplay owners and duplicate/nested panel roots. No Canvas/design/layout/text-content/activation changes. TMP uses manual string UnityEvent binding. |
| Setup / Repair Entire Game | Verify Player/camera/physics/score/coins before edits; refuse critical prerequisites, duplicate service owners and configured gameplay targets that legacy commands would replace. Execute the requested setup order and finish with read-only validation. Separate Undo groups mean partial success is possible. |
| Validate Current Game Setup | Read component/serialized snapshots, scene ownership/activation, Player/camera/contact refs, triggers/shapes/layers, manager deps, build status, source roles, catalog/mission definitions, services and UI refs. Never call preference-repairing runtime getters, claim/save methods, providers or UnityEvents. |
| Existing obstacle/finish/build commands | Existing behavior retained. Finish warning now reads a build-list snapshot without invoking the profile's repair-capable getter. Build-list edits remain a separately requested menu operation. |

The public validator now emits the Level 1 readiness states described above. Internal architecture preflight still distinguishes its own warnings from errors. "Ready" is permission to proceed to a manual test, not proof of successful C# compilation, physics, gameplay, complete event integration or a player build. Missing optional configuration can leave the report incomplete while the existing runner still works.

Core dependency edges inspected against current serialized fields:

- GameManager.playerController → PlayerController; GameManager.pauseManager → PauseManager.
- LevelManager.gameManager → GameManager; LevelManager.pauseManager → PauseManager.
- AudioManager.saveManager → SaveManager; AudioManager.musicSource / sfxSource → distinct AudioSources.
- SettingsManager.saveManager → SaveManager; SettingsManager.audioManager → AudioManager.
- DailyRewardManager.rewardManager, ShopManager.rewardManager, MissionManager.rewardManager → RewardManager.
- AdsManager.analyticsManager → AnalyticsManager (optional).
- FinishLine.levelManager → LevelManager; Obstacle.gameManager → GameManager; Coin.scoreManager → ScoreManager; CameraFollow.target → Player Transform remain compatible.

Earlier architecture-only automation added no subscriptions. LevelManager retains its GameManager subscription, and the new Level1PresentationController owns the bounded presentation subscriptions documented above. Mission/economy/service gameplay adapters remain future work. Coin scores remain distinct from coin counts and reward currency. RemoteConfig does not automatically apply defaults to movement, rewards, preferences or ad policy. No duplicate ad-currency subscriber is installed.

### Read-only build inspection and Undo

Unity 6's `EditorBuildSettings.scenes` honors the active Build Profile, but the profile scene getter can repair/remove invalid records. Validation and finish warnings therefore inspect profile `m_Scenes` via SerializedObject (`m_path` / `m_enabled`) without applying changes; the shared list uses `EditorBuildSettings.globalScenes`. Unknown serialized layouts cause a clear inspection failure instead of guessing. These field layouts match the reviewed Unity 6000.0 reference source but require pinned-editor verification.

Scene setup uses Undo component/creation/object records, prefab-instance override recording, flushed groups, dirty marking only after actual edits, and best-effort rollback on exceptions. The build tool has object Undo for an active profile override asset; shared EditorBuildSettings edits lack a normal object Undo target and log that limitation. Scene Undo cannot undo shared build-list changes or runtime PlayerPrefs. Profile changes remain dirty for saving. No whole-master atomicity is claimed.

### Earlier architecture audit evidence (historical; still relevant)

- Re-read all 24 runtime/helper C# scripts and the expanded Editor tool. Static checks cover 25 files, unique declarations and balanced source structure, exact dependency/reference contracts, Editor/runtime assembly boundaries, all ten menus and the read-only validator's 25 reachable local static methods. No detected mutation, provider, currency or preference call exists in that validation path. These checks are not C# semantic compilation.
- Corrected a source-review compile risk in the new code: a short-circuit condition could leave its `out` reason unassigned; it now initializes the reason before the condition. A GameObject is checked via `panel.scene`, not a Component-only accessor. Neither issue was claimed as compiler-tested.
- Avoided cross-assembly compile risks by not calling internal RunnerPersistence/GameFactoryEvents from Editor code. Catalog ID checks mirror the 1–64 ASCII contract; public MissionDefinition.IsValid/GetPreviousMissionIds are read-only metadata APIs. No external UI/TMP/SDK assembly assumption was introduced.
- Corrected build-index inspection so missing scene assets do not silently shift the inspected enabled ordering. A missing/unusable next scene is a warning, not a thrown load or fabricated level. Active profile scene reading avoids repair side effects.
- Preserved existing configured references and sources. Broken refs, duplicate owners, conflicting gameplay ownership, ambiguous source roles and unsafe UI ancestry are reported instead of overwritten. Runtime scripts were audited but no required API/safety fix was found for this automation integration, so none were changed.
- Added an additive-scene safety check: new setup passes refuse to add owners when another loaded scene already contains managers. Validation reports those external owners as errors. Existing legacy commands remain separately invokable with their original active-scene ownership rules.
- Pending: compile/import the new Editor tool; native SerializedObject/property layouts; profile/global-list save and Undo/Redo; prefab overrides; repeat-command idempotency; rollback under exceptions; persistent UnityEvent method resolution; scene activation/lifecycle and no-domain-reload sessions.
- Pending physics: mesh convex cooking is native and may fail for complex/degenerate geometry; existing shape checks cannot prove a usable cooked hull. Layer matrix checks do not fully model Unity 6 per-collider include/exclude overrides or explicit IgnoreCollision calls. Transform movement can miss thin triggers; no geometry/speed/global physics setting was changed to hide that risk.
- Validator limits: it does not enumerate runtime C# event subscribers, prove HUD/button delivery, verify sound/SFX assets supplied by callers, evaluate saved eligibility/corruption, authenticate ad callbacks or test provider disposal/thread dispatch. Catalog/mission metadata validation does not grant economic authorization. A valid next scene asset may still be a menu/utility scene; progression intentionally uses consecutive build indices.
- Existing PlayerPrefs schema/keys/reset ownership are unchanged: balances saturate/reject overflow as before, claims are staged before currency notifications, mission schema/aliases retain consumed claims, and resets remain scoped. Storage is still local, editable and nontransactional. A badly written future event listener can explicitly grant repeatedly or recurse; automatic wiring installs no such callbacks, and adapters must be bounded and idempotent.
- Audio ambiguity is intentionally not repaired by guessing. Existing settings/flags may need manual changes; the master does not silently turn off an explicitly configured mock mode or rewrite audio routing. UI initial state is preserved, so default MainMenu visibility does not pause automatic Playing behavior. Manual coordination/event adapters are required.

Still manual: visual design, models, animation, final UI/buttons/event bridges, music/SFX assets, mission assets/balancing, shop pricing and skin catalogs, next-level scene content, real ads/analytics SDKs, trusted time/entitlements/cloud save, and final Android/WebGL/iOS modules, build/signing/store configuration. **Unity compilation, Play Mode, physics and build success for this expansion are all pending Unity verification.**

The remainder of this document records the earlier architecture audit and unchanged runtime system contracts. Its modified-file table describes that historical audit, not additional runtime changes in this automation task.

## Status and boundaries

The architecture is a Unity 6 endless-runner prototype. All **23 original C# scripts under Assets/Scripts** were read/reviewed; GameFactoryEvents is a new shared notification helper. Existing tutorial/editor scripts and project/package/scene metadata were also inspected for repository-level risks. Scenes, prefabs, visuals, existing `.meta` files, packages and lockfiles were not changed. No scripts or folders were renamed/moved.

This audit is **source-level**, not a successful Unity compilation, package import, Play Mode session or player build. There is no available Unity editor/C# compiler in this environment. There were no concrete missing cross-script methods, inaccessible referenced members, mismatched event argument types, or duplicate declared project type names found. Unity native behavior and package assemblies still require verification.

Managers use explicit Inspector references, public methods, C# events and provider interfaces. No general-purpose singleton, automatic scene search, dependency injection framework, automatic UI bridge or currency-granting ad adapter is introduced. `[DisallowMultipleComponent]` now guards every MonoBehaviour here against duplicate types on the same GameObject; it cannot enforce one instance across different objects/scenes.

## System contracts

| Script | Responsibility / dependencies | Events / integration points | Lifetime / persistence |
| --- | --- | --- | --- |
| PlayerController | World +Z Transform motion and X steering/bounds; Unity Input System Keyboard | No events; GameManager toggles `enabled` | Player/run; no persistence |
| CameraFollow | LateUpdate Lerp of world offset; optional target look-at | Inspector target/configuration | Camera/scene; no persistence |
| Coin | One-shot score pickup; ScoreManager; identifies active PlayerController on collider ancestry | `Collected(int scoreValue)`; bridge reports one coin regardless of score value | Scene object; disables after collection; no currency grant |
| ScoreManager | Current run score; positive additions saturate at int.MaxValue | `ScoreChanged(int)`; AddScore/ResetScore/GetScore | Scene/run; no automatic best-score save |
| Obstacle | Trigger or collision contact; GameManager; active PlayerController ancestry | Calls TriggerGameOver | Scene object; no persistence |
| GameManager | Playing/GameOver/LevelComplete; PlayerController; optional PauseManager for reload | `StateChanged(GameState)`, `GameOverTriggered()`, `LevelCompleted()` | Gameplay scene; StartGame is not a full reset; RestartGame reloads own scene |
| FinishLine | One successful finish per object; LevelManager | Calls CompleteCurrentLevel | Scene latch; no pooling reset API |
| LevelManager | Own scene's zero-based build index; GameManager; optional PauseManager | `CurrentLevelCompleted(int)`; next/restart public methods | Gameplay scene; subscribes/unsubscribes exact GameManager publisher |
| PauseManager | Owns global Time.timeScale while paused; restores previous scale unless externally overridden | `PauseStateChanged(bool)`; Pause/Resume/Toggle/IsPaused | Usually scene; static owner token only for the global resource, not a manager singleton |
| SaveManager | Best score, highest unlocked index, preference flags; PlayerPrefs | LoadData/SaveData/ResetData and value-specific setters | Profile data; cached public getters can require LoadData after external writes |
| AudioManager | Music/SFX AudioSources; SaveManager initial preferences | Play/StopMusic, PlaySFX, runtime enable methods | Scene/session; preference persistence is delegated to SettingsManager |
| SettingsManager | Loads SaveManager; applies AudioManager preferences | Three `...EnabledChanged(bool)` events; setters/toggles/ReloadSettings | Scene/session cache; vibration preference only |
| UIManager | Exclusive panel visibility; no gameplay dependency | `UIStateChanged(UIState)`; seven show/hide methods | Scene; manager must stay outside panel ancestry |
| HUDController | Numeric text updates; legacy Unity UI Text or string UnityEvents for TMP | UpdateScore/BestScore/Level/CoinCount | Scene HUD; no state subscriptions/format policy by itself |
| RewardManager | Single currency authority; rejects negative/overflow mutations | `RewardBalanceChanged(int)`; bool AddRewards/SpendRewards; reset/read | Profile balance; internal staging callback for daily/mission claim records |
| DailyRewardManager | UTC calendar eligibility; RewardManager; replaceable UTC delegate | `DailyRewardClaimed(int)`, `AvailabilityChanged(bool)` | Claim date and highest observed UTC; local rollback protection only |
| ShopManager | ID/price catalog; RewardManager; shared ownership data helper | `PurchaseSucceeded(string)`, `PurchaseFailed(string, PurchaseFailure)` | Profile ownership; stages ownership before wallet save, best-effort compensation on failure |
| SkinManager | Available/default skin IDs; shared entitlement records; selected ID | `SkinChanged(string)`, `SkinUnlocked(string)` | Profile selection; no model instantiation/swapping |
| MissionDefinition | ScriptableObject ID/type/target/reward/version/rename aliases | Create → Endless Runner → Mission; readonly accessors and IsValid | Asset configuration; not an entitlement/reset mechanism |
| MissionManager | Validates active definitions; counts/max score; RewardManager; versioned records | `MissionProgressChanged(string,int)`, `MissionCompleted(string)`, `MissionRewardClaimed(string,int)` | Profile progress and permanent claim markers; no rotation |
| AnalyticsManager | Sanitized scalar event snapshots; optional IAnalyticsProvider | All Track... methods; `EventTracked(string,IReadOnlyDictionary<string,object>)`; debug logging | No SDK/network/storage/automatic gameplay subscription |
| AdsManager | Optional IAdProvider; optional AnalyticsManager; mock/disabled lifecycle | Opened/completed/failed; `RewardedAdRewardGranted(requestId,placement)` | One active request; unscaled cooldown and cap per component lifetime; no currency writes |
| RemoteConfigManager | Local typed defaults; optional IRemoteConfigProvider; atomic validated override batches | ConfigUpdated(); typed getters/snapshot/SetProvider/FetchConfiguration | In-memory values; no service/network or automatic gameplay writes |
| GameFactoryEvents (new) | Shared void/one-argument notification isolation and bounded nesting | Internal Raise methods; not a component | No persistence/provider/UI dependency |

RunnerPersistence remains an internal helper in ShopManager.cs. It shares ownership keys between shop/skins, validates IDs/flags and isolates their and mission events. It is not a MonoBehaviour and has no Inspector instance. GameFactoryEvents avoids making core gameplay depend on a persistence helper located in the shop module. Existing multi-argument service events retain their local safe dispatch.

## Important semantics

- **Score, coin count and currency are different.** Coin adds its configurable score and emits a collection notification. There is no automatic wallet grant. HUD's coin slot can display coins or currency only if a bridge chooses one.
- **UI visibility is not gameplay state.** Main Menu/Settings panels do not pause or stop the Player. GameManager starts Playing in Awake; no Menu/Waiting state is implemented. Terminal states disable the movement component, not the Player object. StartGame re-enables the existing run; only scene reload recreates coins/position/run score.
- **Levels are build indices.** Next means index + 1, not next unlocked ID or catalog entry. HighestUnlockedLevel is stored separately and not automatically advanced or enforced. The last level refuses progression. Do not place utility scenes between sequential gameplay levels.
- **Event ordering matters.** Data/state is committed before notifications. New core notifications isolate each listener and suppress excessive nested notification chains. The helper does not undo side effects of a callback that explicitly changes currency/state; do not wire reward-granting loops. GameManager rejects reentrant state reversals during its notification sequence. Query initial state after Awake because startup notifications may already be past.
- **Subscription ownership is explicit.** LevelManager records its publisher and unbinds on disable. Other managers expose hooks rather than auto-subscribing to game/UI events. A future bridge must bind once and unbind from the same instance; callbacks should not be multiplied by panel enable/disable.
- **Pause is a shared resource.** Only the owner resumes its saved scale. Later external nonzero scale overrides are preserved; the manager releases ownership on the next Update or Resume. Another PauseManager cannot acquire ownership while one holds it. This is not a token-based multi-reason pause stack. Menus, SDK presentation and future focus handling must coordinate with this one authority.
- **Providers must dispatch on Unity's main thread.** No synchronization/dispatcher is supplied. Callbacks after provider replacement, timeout, disable or request completion are ignored where request identity applies. Provider interfaces are runtime injection contracts, not Inspector-serialized interfaces.

## Persistence ownership and reset scope

All keys below are deliberately disjoint except shop/skin sharing and mission rename aliases. Valid IDs exclude whitespace/control characters and are case-sensitive ASCII letters/digits plus `.`, `_`, `-`, length 1–64.

| Owner | Keys / record encoding | Reset / recovery |
| --- | --- | --- |
| SaveManager | `Runner.BestScore`, `Runner.HighestUnlockedLevel` nonnegative ints; `Runner.MusicEnabled`, `Runner.SfxEnabled`, `Runner.VibrationEnabled` flags 0/1 | ResetData deletes **only these five keys**. Preference setters write only their own key. Progress saves merge monotonically; clean flushes reload to avoid stale-reset resurrection. Invalid flags use enabled defaults. Call SettingsManager.ReloadSettings after a reset. |
| RewardManager | `Runner.RewardBalance` nonnegative int | ResetRewards writes only this key as zero. It does not restore consumed daily/mission rewards or clear shop ownership. Invalid/negative/wrong-type balance reads zero; plausible positive tampering is undetectable. |
| DailyRewardManager | `Runner.DailyReward.LastClaimUtcDate` invariant `yyyy-MM-dd`; `Runner.DailyReward.LastSeenUtc` round-trip UTC `O` string | Missing keys allow a new profile; malformed/future/rolled-back time blocks claims. No public reset that would re-grant rewards. Clock saves checkpoint every 60 unscaled seconds and normal disable/background transitions. `TimeSpan.MaxValue` means blocked/unknown. |
| Shop + Skin shared ownership | `Runner.Items.Owned.<id>`, `Runner.Items.OwnedBackup.<id>` flags 0/1 | A valid owned copy repairs a damaged peer; otherwise recover unowned for that ID only. Default skin is always available. Purchase compensation clears both copies and restores a detected debit when possible. |
| Skin selection | `Runner.Skins.Selected` string ID | Removed/invalid/unowned selection falls back to configured default. Invalid default configuration uses `default`. Repeated catalog IDs collapse to one skin. No visual state persisted. |
| Mission current records | `Runner.Missions.Record.<id>` JSON schema **1** with schema/definition versions, one-based type encoding, target, reward, progress, claimState (1 unconsumed, 2 consumed) | Original two-key saves migrate on first read; missing/corrupt critical fields do not silently create reward eligibility. Higher unknown schema remains untouched/unavailable. |
| Mission legacy progress and claim ledger | `Runner.Missions.Progress.<id>` old nonnegative int; `Runner.Missions.Claimed.<id>` 0/1 consumption marker | Retained for migration and anti-duplication. Claimed markers persist for canonical IDs and aliases even after removal. Corrupt claim history is conservatively consumed, with warning. No blanket DeleteAll or automatic ID recycling. |
| Analytics / Ads / Config | No PlayerPrefs keys | Analytics has no storage; ads pending IDs/caps are instance memory; config overrides disappear on recreation. |

Mission migration preserves compatible progress, clamps to the new target, and retains consumed claims. An incompatible type/definition-version change resets progress but **does not reset claim eligibility**. Reward-only changes retain progress. Alias progress imports before a canonical record exists; attaching an alias after initialization does not retroactively import progress, though claim history is merged. Configure aliases before first loading a renamed ID. Removed definitions leave their records untouched. A new independently rewardable mission needs a genuinely new identity.

A quarantined/consumed mission flag is **not proof currency was paid**. It may reflect uncertain/corrupt history or an ambiguous local-write failure. This favors avoiding duplicate rewards over always recovering an earned reward. There is no production compensation ledger or reconciliation UI yet.

## Audit: fixed issues and exact modified-file rationale

Every MonoBehaviour below gained a same-GameObject duplicate-component guard. This is a setup safeguard, not cross-scene singleton enforcement. Additional changes are listed individually; valid configured gameplay behavior is retained.

| Existing file modified | Finding and fix |
| --- | --- |
| PlayerController.cs | `[Min]` is not runtime validation; negative/NaN/infinite speeds could corrupt position. Invalid speeds now use zero/nonnegative values before Transform motion. |
| CameraFollow.cs | Invalid smoothing could yield NaN position; smoothing speed is now finite/nonnegative. |
| Coin.cs | Score event exceptions could strand a collected active coin; ScoreManager's safe events fix that path. Null/disabled Player contacts are rejected. Added one-shot Collected(scoreValue) so counts/missions need not infer coins from score changes; listener snapshot survives coin deactivation. |
| ScoreManager.cs | A throwing score listener stopped later listeners and callers. Add/reset now use isolated core notifications; saturation remains unchanged. |
| Obstacle.cs | Null contacts and a movement-disabled Player could still reach collision handling; guarded both callback paths. Trigger setup remains required for kinematic-vs-static obstacles. |
| GameManager.cs | StateChanged exceptions could suppress terminal events; reentrant listeners could reverse a partially published transition. State publication is guarded and events isolated. Reload validates own build index, rejects repeated loads, catches load failures, and optionally resumes assigned PauseManager. |
| FinishLine.cs | Null/disabled Player contact could latch completion at an invalid time. Now rejects those contacts; existing one-shot latch remains. |
| LevelManager.cs | Unsubscribing from a changed Inspector reference could leak the original event subscription. Tracks actual publisher. Completion event isolated; next index bounded before addition; load attempts guarded/caught; optional PauseManager resumed before load. |
| PauseManager.cs | Multiple owners/external overrides could restore the wrong global time scale; lifecycle event exceptions could interrupt calls. Added one owner token, inactive/invalid-scale guards, preserved external overrides, and isolated notifications; normal disable releases pause. |
| SaveManager.cs | A stale instance's progress save wrote every cached preference and could undo a newer setting. Writes are now scoped: progress merges separately, each preference setter writes only its key. Clean flushes reload, avoiding stale progress resurrection after ResetData. Nonbinary flags are detected/defaulted; writes/reset catch storage exceptions without erasing unrelated keys. |
| AudioManager.cs | Non-finite SFX volume was forwarded to native audio. It is rejected; finite values still clamp 0–1. |
| SettingsManager.cs | Setting listener exceptions could interrupt later listeners; ResetData had no way to refresh cached settings/audio. Safe notifications plus ReloadSettings added. |
| UIManager.cs | New panel could activate before the old one disabled, briefly enabling both; invalid enum hid everything. Deactivates other panels first, then selects; invalid state falls back to MainMenu; notifications isolated. |
| HUDController.cs | Throwing string UnityEvent escaped into score/currency notification paths. Catches invocation failure; later UnityEvent listeners may still be skipped because UnityEvent cannot expose all runtime listeners for isolation. |
| RewardManager.cs | Duplicate-component guard only; existing overflow, nonnegative balance, event isolation and staged persistence contracts remain. Native persistence errors remain distinguishable to shop/daily/mission callers. |
| DailyRewardManager.cs | Lifecycle clock-save, observation writes and claim-save exceptions could propagate into gameplay. Added logged failure paths while retaining conservative staged claim data. |
| ShopManager.cs | Duplicate-component guard only. Existing per-ID recovery, backup flags, duplicate-ID refusal, isolated events and best-effort compensating refund were re-reviewed; no new transactional guarantee is claimed. |
| SkinManager.cs | Duplicate-component guard only. Existing default recovery, catalog deduplication, selection fallback and shared entitlement behavior retained. |
| MissionManager.cs | Duplicate-component guard only. Existing schema/alias migration and consumed claim ledger reviewed; no reset that reopens rewards was added. |
| AnalyticsManager.cs | Same-object guard and bounded inspection of parameters/strings; previously all-invalid/large inputs could require unbounded scanning. Provider/listener exception isolation and notification-depth guard retained. |
| AdsManager.cs | An already-open mock ad could complete after mock mode was switched off. Completion and Update now refuse/cancel that request. Provider readiness could disable the component or replace the provider before Show; state/identity rechecked. Unity Range attributes qualified as a compile precaution, not a confirmed prior compiler error. |
| RemoteConfigManager.cs | Duplicate-component guard and qualified Unity Range attributes only; prior known-schema validation, local fallback and callback identity behavior retained. |

MissionDefinition.cs was audited and left unchanged in this pass: explicit enum values, positive target/reward/version checks, alias validation, ScriptableObject menu and readonly API match MissionManager. No unrelated gameplay behavior was restyled or replaced.

New GameFactoryEvents.cs supplies safe core notifications without tying score/game/UI/pause/settings to the shop persistence helper. README.md now states project purpose and unverified status; GAME_FACTORY_SETUP.md supplies later editor steps; this file records contracts, findings and limitations. No scene, prefab, Canvas, visual, original GUID, manifest or lockfile modifications are part of the audit.

## Audit: remaining Unity-runtime and setup verification items

Each finding below is unresolved runtime/setup work, not a source audit pass:

1. **Unity compilation/import/build has not run.** No compiler/editor is available. Review found no concrete project API mismatch, but package assemblies, native API signatures and serialization must compile in pinned Unity and a player build.
2. Input System and Unity UI are declared; direct manifest versions match the lockfile. Registry access, Unity/platform compatibility and existing AI/GDK/legacy analytics/IAP package behavior remain unverified. The wrappers do not use those SDKs, but installed packages can affect import/build.
3. MainGame is now enabled after SampleScene in the shared list; no next level follows. A profile override may differ and must be inspected. Build-index progression may enter an unintended scene if a menu/utility scene is interleaved.
4. MainGame now contains trigger/kinematic Rigidbody setup from prior user verification. Transform movement and autoSyncTransforms=false can still miss thin triggers at high speed; test contacts across frame/physics rates and collider layer overrides.
5. Runtime and Editor scripts now have committed `.meta` files. Preserve GUIDs and check Missing Script components/import behavior for the new tool. The architecture still requires additional managers/configuration/UI event connections; metadata alone is not complete integration.
6. Same-object component guards do not prevent duplicate managers on different GameObjects, additive scenes, or repeated bootstrap creation. Shared PlayerPrefs does not synchronize each object's cached events/properties. Configure one intended service owner/lifetime; duplicate adapters can count runs/levels or grant ad currency multiple times.
7. No general singleton/automatic persistent bootstrap is implemented. Game/Level/Score/UI are scene-owned. Making them persistent keeps stale Player/scene references or index -1; do not do this casually. Ads caps/cooldown reset when AdsManager is recreated and can be bypassed by multiple instances.
8. Persistent pause owners require the explicit PauseManager references in loaders, or manual Resume before loading. Scene-local owners normally release on disable. An unrelated script/SDK writing timeScale must coordinate with the owner. There is no multi-reason pause stack; verify no-domain-reload editor sessions.
9. GameManager automatically starts Playing, and initial Playing need not emit StateChanged. A Main Menu panel alone does not stop motion. Startup UI/HUD bridges must query initial state after Awake; settings events also are not an initialization replay.
10. C# events are not Inspector UnityEvents. A bootstrap/scene bridge is still needed for UI, HUD, best-score save, unlocked-level save, missions, coin counts, audio, and analytics. No automatic best-score/level-unlock persistence or currency pickup is implemented. Avoid subscribing twice to both GameManager.LevelCompleted and LevelManager.CurrentLevelCompleted for the same mission count.
11. UI panels must be separate sibling roots; self/ancestor/nested panel assignments can disable managers or prevent the chosen panel from being active in hierarchy. Missing refs are null-safe but produce invisible UI. Pause resume must select terminal panels based on GameManager state.
12. StartGame is not RestartGame. It does not reset position/score/coin/finish latches. Re-enabling pooled Coin/FinishLine objects also does not reset them. Scene recreation is the current supported reset; pooling is future work.
13. Player input is desktop Keyboard-only; absent Keyboard returns zero steering. Touch/gamepad input, jumping and dynamic physics are not implemented. Invalid speed inputs are safe, but transform positions/camera offsets must still be finite and valid. Look-at geometry needs Play Mode verification.
14. HUD uses declared Unity UI Text and UnityEvent<string>; no TMP compile-time dependency. Verify generic UnityEvent serialization/persistent listeners in Unity 6 and import TMP resources if using that path. A throwing UnityEvent listener can skip later UnityEvent listeners even though the outer error is caught.
15. New core events suppress notification chains deeper than 16 to avoid stack overflow. This is not transactional state rollback and does not stop a badly written listener from issuing explicit repeated currency/state mutations. Keep callbacks observational or deliberately bounded. GameManager ignores immediate state reversals from its transition listeners; defer deliberate restart/start navigation appropriately.
16. Two distinct AudioSources and an AudioListener are required; same-source configuration is rejected for SFX and logged. Play On Awake must be off to avoid audio before preference loading. Music mute retains playback, and timeScale=0 does not pause audio. Reinitialization/scene changes and supported audio devices need testing.
17. SaveManager exposes cached properties. Explicit external/reset writes need LoadData / SettingsManager.ReloadSettings to refresh observers. Storage write errors are logged, not communicated as a success/failure return from void preference setters. Failed pending progress and duplicate owners require manual reconciliation after disk recovery.
18. Default skin IDs/catalogs must be consistent across the profile and excluded from paid shop entries; an early purchase before the default SkinManager initialized cannot infer arbitrary configured default IDs. Shop purchase events do not emit SkinUnlocked; shared entitlement reads still agree. Removed/renamed shop/skin IDs have no migration alias system.
19. Mission schema/JsonUtility behavior must be tested against real old records and malformed types/fields. DefinitionVersion changes reset progress but retain claims. Configure aliases before initializing a renamed canonical ID; adding aliases later does not import their old progress. Never recycle IDs. Removed records/tombstones accumulate; no garbage collector or rotation is implemented.
20. Malformed/unknown mission claim history is consumed, not verified paid, and warns. Unsupported higher schemas make that mission unavailable without overwriting it. Legacy saves lacked type/version metadata, so their migration can only assume the current definition identity is truthful. Progress events may repeat after actual content/definition resets, but consumption history prevents a second currency claim.
21. Daily reward malformed clock/date records block claims intentionally. Clock rollback can lock eligibility until the stored time is reached; corrected trusted time may also be earlier than a previously fast local clock. UTC Kind is required; MaxValue countdown means blocked. Clock precision, UTC midnight, background/lifecycle saving and multiple managers need device tests.
22. Analytics has no real provider and no automatic subscriptions. Names/keys reject invalid forms, scalar parameters are bounded, debug text is not anonymization. Avoid logging personal/secret data. A disposed/destroyed provider and application lifecycle still need adapter-specific handling.
23. Ads has no SDK. Mock mode is explicitly manual/testing and must remain off in release. Ad completion means provider-confirmed successful completion/reward eligibility, not merely closure. The wrapper trusts the adapter; no server receipt verification exists. Timeout/provider-change/disable may conservatively drop a late legitimate completion.
24. RewardedAdRewardGranted emits eligibility once per request; it does not grant/persist currency. Multiple subscribers can each grant currency, and cross-restart idempotency is not implemented. `ad_reward_granted` analytics records eligibility issuance, not guaranteed wallet-save success. A future currency adapter must define the durable transaction and request-ID ledger.
25. Interstitial caps are per component instance, not durable profile/session policy. Cooldown uses unscaled real time; no run-count/frequency adapter is connected to the remote-config frequency value. Rate-limit behavior across scene recreation, focus changes and long SDK presentations needs verification.
26. Real SDK provider cancellation, main-thread marshalling, consent, SDK initialization, lifecycle disposal, receipts and retry policy are future integrations. Replacing providers ignores old callbacks but cannot cancel an actual external ad presentation yet. Interface references are not Inspector serialized.
27. Remote config is local/in-memory and whitelist-only. Wrong-type/unknown keys use caller fallback; only known default keys are range-validated. No automatic gameplay application, saved-preference override, caching, request timeout, network retry or persistence is implemented. Float overrides normalize to float precision. Config callbacks/events can change configuration reentrantly; consumers should use snapshots when consistency across several reads matters.
28. The template editor ReadmeEditor uses reflection into UnityEditor.WindowLayout. That internal API and template layout loading require editor import verification; the editor-only folder protects player builds. Its Remove Readme Assets button deliberately deletes tutorial assets; no such operation was performed.
29. Platform PlayerPrefs storage and native disk failure behavior have not been fault-tested. Methods catching exceptions cannot guarantee a native Save failure is reported or atomic. Direct RewardManager mutation APIs can still propagate native persistence errors so claim/purchase callers can react; future direct currency adapters must handle them.
30. Frame-rate/physics-rate behavior, native SceneManager exceptions, event ordering during disable/destroy/reload, first-frame input, and Unity's domain reload settings require the ordered checklist in GAME_FACTORY_SETUP.md. No current-run automated Unity test suite exists for these systems.

## Production-security limitations

- PlayerPrefs is editable/deletable local prototype storage. Positive balances, prices/config, ownership backups, mission versions/claims and clock records are not authenticated. Backups detect accidental inconsistency, not malicious editing.
- No PlayerPrefs transaction crosses wallet, ownership, claim date, mission record or backup keys. One Save call reduces partial writes but is not an atomic entitlement transaction. A crash, disk failure or failed refund can leave currency/ownership/claim history uncertain.
- Shop compensation is best effort for a detected debit; there is no durable journal or reconciliation ledger. A corrupt entitlement with both copies lost may recover unowned even if previously purchased.
- Conservative daily/mission markers favor preventing duplicate awards over guaranteeing recovery of a missed award. They do not prove currency was delivered. Clearing keys or tampering with flags can reopen eligibility outside these protections.
- Local UTC rollback detection cannot stop clock-forward cheating, deleting records or altering stored timestamps. The periodic checkpoint can lose recent observations on a crash. An authoritative time/entitlement service is future work.
- Ad confirmations are provider-trusted, in-memory, per request. Mock mode is not production authorization. Persistent/server-backed deduplication, receipt validation, fraud controls and app-wide monetary limits are absent.
- No analytics consent/privacy pipeline, PII filtering beyond scalar sanitization, or production log policy exists. Debug logging must remain development-only.
- Remote-config overrides are not authenticated/signed, persisted or fetched. They cannot be relied on as production policy; adapters must preserve saved user settings and validate data before mutation.
- SaveManager.ResetData is intentionally not a complete profile/factory reset. A future full-reset tool needs an explicit multi-system policy and must not be used to replay economic rewards accidentally.

## Verification evidence and next action

The source audit checked all script names and call contracts, duplicate declared types, matching structural delimiters, supported lifecycle method shapes, package/lock pins, persistence prefixes, reset scope, and final diff restrictions. These are static checks, not C# semantic compilation or Unity tests. No package versions, scene membership, assets, existing GUIDs or visuals were silently changed.

Next: open the pinned Unity editor, generate/import metadata, resolve every package/compile error, perform GAME_FACTORY_SETUP.md in order, then run Play Mode and a player build. Record actual passed/failed checks; do not infer readiness from this documentation or a clean Git push.
