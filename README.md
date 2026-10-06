# Sajid Game Factory

A Unity 6 endless-runner prototype with modular gameplay, persistence, economy and provider-neutral service hooks. Use **Unity 6000.0.84f1**, URP 17.0.4, Input System 1.20.0 and Unity UI 2.0.0. Desktop movement uses A/D and arrow keys.

The **Tools > Sajid Game Factory > Build / Repair Level 1 Master** command (also available as **Upgrade / Repair Level 1 Fantasy Presentation**) prepares a temporary Level 1 vertical slice in the existing MainGame scene: a stylized white-wolf runner in blue/dark/lime sportswear, coin animation, an elevated fantasy bridge with cyan trims, geometric cliffs/waterfalls and obstacle/finish decorations, three particle effects, portrait UI and live game-state/HUD/settings wiring. Scene and asset generation happen only when you run the menu in Unity; Codex has not changed MainGame or generated Unity assets.

**Codex → GitHub → Unity Pull → Build / Repair Level 1 Master → Validate Current Game Setup → Play Test → Visual review → Lock Level 1 → Duplicate architecture for future levels.**

- [Unity setup and exact test sequence](GAME_FACTORY_SETUP.md)
- [Architecture, ownership and limitations](GAME_FACTORY_ARCHITECTURE.md)

The original **Setup / Repair Entire Game** remains available for architecture preparation. The new master adds presentation afterward. It reuses owned objects/assets, preserves custom assignments and gameplay values, and reports unsafe ambiguity instead of overwriting user content. Review Console messages and save the scene/profile manually. Generated assets are outside normal scene Undo.

Validation ends with `READY FOR LEVEL 1 PLAY TEST`, `LEVEL 1 SETUP INCOMPLETE`, or `CRITICAL ERRORS FOUND`. Missing optional audio clips, catalogs and a next level remain warnings; readiness is a structural result, not proof of working gameplay.

**Pending Unity verification:** compilation/import, Play Mode, physics, UI input/layout, generated materials, VFX, Undo/Redo, scene reload and player builds. No Unity editor or C# compiler is available in this cloud environment. The original gameplay scripts, scene, generated assets, metadata, packages and build configuration are unchanged by the fantasy code task. SimpleRunnerVisual gains optional child-only bob/lean; RunnerPresentationController and Level1HUDSafeArea are new runtime helpers. Four new Editor partial files extend the existing tool. The latest pulled scene already contains the earlier Level 1 generation.

The written fantasy/wolf direction is approximated with primitives and a tiny procedural ear mesh; the reference images are unavailable in this task. Waterfalls are static layered geometry and emission supplies bright trims without bloom or post-processing. Temporary: generated character, procedural animation, environment, UI styling and basic VFX. Professional audio, real models/animation, future levels, balancing and platform/store setup remain manual. No real ad/analytics/remote-config SDK, cloud save, IAP or visual skin swapping is integrated by the wrapper scripts. Unrelated analytics/IAP/AI/GDK packages already in the manifest may have independent import/platform requirements.

PlayerPrefs remains local, editable, nontransactional prototype storage. Preserve gameplay, game-state architecture, persistence ownership, automation and service abstractions when replacing presentation.
