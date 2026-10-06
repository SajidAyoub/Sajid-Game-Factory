# Sajid Game Factory

A Unity 6 prototype for a 3D endless runner, with modular movement, score, game state, levels, pause, settings/audio, UI controllers, rewards, shop/skin data, missions, and provider-neutral service hooks.

Use **Unity 6000.0.84f1** (revision `78ab6fc243d5`). The project declares Input System 1.20.0 and Unity UI 2.0.0. Desktop movement uses A/D and arrow keys. No real ad SDK, analytics provider, remote-config service, cloud save, IAP, or visual skin swapping is implemented by these scripts. The existing package manifest also contains unrelated analytics/IAP/AI/GDK packages; the wrapper scripts do not integrate them.

- [Unity setup checklist](GAME_FACTORY_SETUP.md): objects, references, physics, UI, event wiring, and test order.
- [Architecture and audit](GAME_FACTORY_ARCHITECTURE.md): responsibilities, events, persistence, per-file changes, findings, and remaining risks.

## Development workflow

**Codex → GitHub → GitHub Desktop Pull → Unity → Tools > Sajid Game Factory > Setup / Repair Entire Game → Validate Current Game Setup → Play Test**

Open MainGame in Edit Mode after resolving import/compile errors. The Editor tool reuses managers, fills only empty unambiguous references, prepares audio/economy/service/UI architecture, and checks the result. Existing movement/camera values, scene geometry, clips, catalogs and balancing are preserved. Ambiguities need manual resolution; the tool does not create designed UI or event adapters. Save reviewed scene/profile changes from Unity.

Validation reports `READY FOR PLAY TEST`, `SETUP INCOMPLETE`, or `CRITICAL ERRORS FOUND`. Readiness is a structural inspection result, not proof of compilation, working gameplay or a successful build. See the setup checklist for all ten menu commands, Undo limitations, required object names and exact tests.

## Current validation status

All 24 existing runtime/helper scripts under `Assets/Scripts` and the expanded Editor tool were reviewed. Static checks covered source structure, dependency contracts, ten menu registrations, validation's read-only call path, assembly boundaries and protected diff scope. Persistence and lifecycle contracts were re-reviewed without changing runtime scripts. **No Unity/C# compiler or editor is available here: compilation/import of the expansion, Play Mode, physics and player builds remain pending Unity verification.**

The user reports prior Unity verification of movement, camera, coins/score, obstacles/game over, finish completion and MainGame build inclusion. The current repository contains MainGame as an enabled shared build scene, Player's kinematic/gravity-free Rigidbody, trigger contact components and script metadata. Those are existing committed changes; this automation expansion does not alter scenes, prefabs, visuals, runtime scripts, Build Settings, packages or existing GUIDs in Codex. The new menus and validation still require Unity testing.

The code is an architecture prototype, not a fully wired game. C# events require subscription code; public panel methods do not automatically control gameplay. PlayerPrefs is local, editable, nontransactional storage and must not be trusted for production currency or reward entitlement.

Intentionally manual: visual design, 3D models, animation, final UI/buttons/event bridges, sound/music assets, mission balancing, shop prices and skin IDs, real ads/analytics SDKs, server/cloud saves, and final Android/WebGL/iOS build/signing/store configuration.
