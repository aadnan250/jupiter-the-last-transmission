# JUPITER: THE LAST TRANSMISSION

*The mission was supposed to take humanity closer to the stars. It was never supposed to bring something back.*

A first-person science-fiction survival-horror game set in 2183, built in **Unity 6 (URP)** and **C#** for Windows.

[![Repository checks](https://github.com/aadnan250/jupiter-the-last-transmission/actions/workflows/repo-checks.yml/badge.svg)](https://github.com/aadnan250/jupiter-the-last-transmission/actions/workflows/repo-checks.yml)

> **Status: Phase 2 — Visual prototype complete.** The *Meridian*'s habitat ring is explorable in grey-box form, with lighting, procedural ship audio, readable documents and a placeholder Jupiter outside the observation window. 33 automated tests.

| | |
|---|---|
| ![Corridor](docs/images/phase2-corridor.png) | ![Observation deck](docs/images/phase2-observation-deck.png) |
| *Habitat ring corridor* | *Observation deck: Jupiter fills the window* |
| ![Command](docs/images/phase2-command.png) | ![Galley](docs/images/phase2-galley.png) |
| *Command deck* | *Galley* |

## About

The first crewed expedition to Europa, one of Jupiter's moons, goes wrong. The crew went to study the ice. Something beneath it was already listening.

- ~60-minute narrative horror experience
- Scientifically grounded: vacuum silences sound, Jupiter hangs fixed in Europa's sky, and messages from Earth arrive 43 minutes late
- Original creatures driven by a vibration-based detection AI
- Radio-driven storytelling with full voice acting
- Designed to run on integrated graphics (Intel Iris Xe)

## Technical highlights (so far)

- **Environment profiles.** Gravity and movement come from ScriptableObjects: Earth (1 g), the *Meridian*'s spin ring (0.6 g) and Europa (0.134 g, the real value). The same controller works everywhere; walking through a trigger volume swaps the profile.
- **Testable movement maths.** All movement physics (jump velocity from `v² = 2gh`, gravity integration, air control) lives in a pure static class with **EditMode unit tests**.
- **Event-driven interaction.** `IInteractable` + raycast; the HUD listens to a `TargetChanged` event instead of polling.
- **Procedural audio, 100% original.** Room tones (ring drone, ventilation, equipment hum, medical monitor), doors, switches and footsteps are synthesised in C# and written to WAV by an editor tool: deterministic, seamless loops (tested), no licensing risk.
- **Footsteps from distance travelled**, not timers, so the step rate always matches speed; crouching is near-silent. This feeds the creature hearing system in Phase 5.
- **Generated levels.** The *Meridian* level, the test room, the player prefab and the Jupiter placeholder texture are built by editor scripts, so the whole grey-box can be regenerated and reviewed as code (**Jupiter → Rebuild All Generated Content**).
- **Environmental storytelling.** `Readable` documents raise a static event that the reading UI listens to, so documents can be placed anywhere without wiring.
- **Performance budget for Intel Iris Xe.** URP Forward+, no additional-light shadows, 30 m / 2-cascade main shadows, MSAA off; press **F3** in-game for a frame-time overlay.
- **Input System** action asset with keyboard/mouse and gamepad schemes.
- **Assembly definitions** separate runtime, editor tooling and tests.
- **CI:** GitHub Actions checks repository hygiene (no generated folders, every asset has its `.meta`, no oversized files outside LFS).

## Getting started

**Requirements:** Unity **6000.6.10f1** (install via Unity Hub) and [Git LFS](https://git-lfs.com).

```powershell
git lfs install
git clone https://github.com/aadnan250/jupiter-the-last-transmission.git
```

1. In Unity Hub: **Add → Add project from disk** and select the cloned folder.
2. The *Meridian* level (`Assets/_Project/Scenes/L02_Meridian.unity`) opens automatically. Press **Play**. The movement test room is in `Scenes/Sandbox/`.
3. To regenerate all generated levels, audio and prefabs: **Jupiter → Rebuild All Generated Content**.

### Controls

| Action | Keyboard / Mouse | Gamepad |
|---|---|---|
| Move | WASD / Arrows | Left stick |
| Look | Mouse | Right stick |
| Run | Left Shift | Left stick press |
| Crouch (toggle) | Left Ctrl / C | B / Circle |
| Jump | Space | A / Cross |
| Interact | E | X / Square |
| Performance overlay | F3 | — |
| Release cursor | Esc | — |

### Running tests

In Unity: **Window → General → Test Runner**, then run **EditMode** and **PlayMode**. From the command line:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.10f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml
```

## Project structure

```
Assets/_Project/
  Scripts/Player/    Controller, camera, input, interaction
  Scripts/Audio/     Procedural synthesis, WAV encoder, room tones, footsteps
  Scripts/World/     Environment profiles and zones, doors, switches, readables
  Scripts/UI/        HUD, reading panel, performance overlay
  Scripts/Editor/    Level, prefab, audio and texture generators
  Tests/EditMode/    Unit tests
  Tests/PlayMode/    Scene smoke tests
  Audio/Generated/   Procedurally generated sounds
  Prefabs/Player/    PlayerRig prefab
  Data/              ScriptableObject assets
  Settings/Input/    Input actions
docs/                Design and technical documentation
```

## Documentation

- [Pre-production design document](docs/preproduction/README.md)
- [Asset licence register](docs/ASSET_LICENSES.md)

## Roadmap

| Phase | Status |
|---|---|
| 0. Pre-production | Done |
| 1. Project foundation | Done |
| 2. Visual prototype (*Meridian* grey-box, lighting, interactions, ambience) | **Done** |
| 3. Story foundation (dialogue, radio, mission, save) | Next |
| 4. Vertical slice (Chapters 6–7) | Planned |
| 5–12. Full production and release | Planned |

## Known limitations

- Grey-box art only; sounds and the Jupiter texture are procedural placeholders.
- The habitat ring is modelled as locally flat (its real curvature over a 50 m section is small).
- Lighting is real-time during grey-boxing; baked lighting comes with the art pass.
- Unity test runs are local for now. Running them in CI needs a Unity licence stored as repository secrets (planned).

## Author

**adnan** ([@aadnan250](https://github.com/aadnan250))
