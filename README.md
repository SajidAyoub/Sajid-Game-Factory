# Sajid Game Factory

A Unity 6 prototype for a 3D endless runner, with modular movement, score, game state, levels, pause, settings/audio, UI controllers, rewards, shop/skin data, missions, and provider-neutral service hooks.

Use **Unity 6000.0.84f1** (revision `78ab6fc243d5`). The project declares Input System 1.20.0 and Unity UI 2.0.0. Desktop movement uses A/D and arrow keys. No real ad SDK, analytics provider, remote-config service, cloud save, IAP, or visual skin swapping is implemented by these scripts. The existing package manifest also contains unrelated analytics/IAP/AI/GDK packages; the wrapper scripts do not integrate them.

- [Unity setup checklist](GAME_FACTORY_SETUP.md): objects, references, physics, UI, event wiring, and test order.
- [Architecture and audit](GAME_FACTORY_ARCHITECTURE.md): responsibilities, events, persistence, per-file changes, findings, and remaining risks.

## Current validation status

All 23 original scripts under `Assets/Scripts` were reviewed, along with the new shared event helper and project/package settings. Source structure, public call contracts, package pins, persistence key namespaces, and diff scope were checked without launching Unity. **No Unity/C# compiler or editor is available here: compilation, package import, Play Mode, and player builds are unverified.**

`MainGame.unity` exists but is not currently an enabled build scene; only `SampleScene.unity` is listed. MainGame's coin, obstacle, and finish colliders are non-trigger colliders, and its Player has no serialized Rigidbody. Most new scripts still need Unity-generated `.meta` files and scene references. The audit does not alter scenes, prefabs, assets, package manifests, or existing GUIDs.

The code is an architecture prototype, not a fully wired game. C# events require subscription code; public panel methods do not automatically control gameplay. PlayerPrefs is local, editable, nontransactional storage and must not be trusted for production currency or reward entitlement.
