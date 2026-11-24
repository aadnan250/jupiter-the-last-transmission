# 06 — Technical Plan

## 1. Engine and packages

| Item | Choice | Reason |
|---|---|---|
| Engine | **Unity 6 (6000.6.10f1)**, already installed | Current LTS-line editor on the development machine |
| Render pipeline | **URP** (Forward+), created from the installed `urp-blank` template | Scales down to Intel Iris Xe |
| Input | **Input System** package (action maps: `Player`, `UI`, `Cinematic`) | Rebindable controls, gamepad support |
| Cutscenes | **Timeline** + **Cinemachine 3** | Authored sequences without custom tooling |
| AI navigation | **AI Navigation** package (NavMesh) | Creature pathing |
| Testing | **Unity Test Framework** (EditMode + PlayMode) | Unit tests for core logic |
| Text | **TextMesh Pro** (via uGUI) | Subtitles, UI |
| Version control | Git + **Git LFS** | Binary assets |

No paid plugins are required.

## 2. Folder structure

```
Assets/
  _Project/
    Art/            Materials, Models, Textures, Shaders, VFX
    Audio/          Ambience, Creatures, Dialogue/<Character>/, Music, SFX, Mixers
    Data/           ScriptableObjects (dialogue, radio, items, weapons, creatures, environments, missions)
    Prefabs/        Player, Creatures, Interactables, Environment kits, UI
    Scenes/         Boot, MainMenu, L01_CaboLumen … L05_Home, Cinematics/
    Scripts/
      Core/         Bootstrap, services, events, save
      Player/       Controller, interaction, camera, health, inventory
      Combat/       Weapons, damage
      Creatures/    AI, detection, health
      Narrative/    Dialogue, radio, mission, cutscenes
      Audio/        AudioManager, vibration propagation
      World/        Environment profiles, puzzles, doors, interactables
      UI/
    Tests/
      EditMode/     Pure-logic tests
      PlayMode/     Scene-level behaviour tests
  ThirdParty/       Redistributable third-party assets only (licences documented)
docs/               Design, architecture, licensing, devlog
```

## 3. System architecture

**Principles:** small single-purpose components; ScriptableObjects for data; C# events / a lightweight event channel for decoupling; interfaces where more than one implementation exists (`IInteractable`, `IDamageable`, `IVibrationListener`, `ISaveable`). No singleton god-objects. A minimal **service locator** in `Bootstrap` registers the few true globals (audio, save, scene transition).

| System | Responsibility | Key dependencies |
|---|---|---|
| `PlayerController` | Movement, crouch, run, jump, reads `EnvironmentProfile` for gravity/speed | Input System, `CharacterController` |
| `CameraController` | Look, head bob, sway, shake channels | `PlayerController` |
| `PlayerInteraction` | Raycast for `IInteractable`, prompt display, interaction execution | `UIManager` |
| `HealthSystem` | HP, damage, death, low-health state events | `IDamageable` |
| `SuitSystem` | Suit integrity, O₂, radiation dose (Europa only) | `EnvironmentProfile`, `HealthSystem` |
| `InventorySystem` | Slots, item data (`ItemDefinition` SO), consumables | `UIManager` |
| `WeaponSystem` | Fire, reload, ammo, recoil, emits vibration events | `InventorySystem`, `VibrationSystem` |
| `VibrationSystem` | Central **sound/vibration propagation**: emits events with source, loudness, medium (air/ground) | `EnvironmentProfile` (vacuum vs pressurised) |
| `CreatureDetection` | Receives vibration events and short-range sight; maintains a suspicion value | `VibrationSystem` |
| `CreatureAI` | State machine: Dormant → Listening → Investigating → Hunting → Attacking → Torpor → Recovering / Dead | `CreatureDetection`, NavMesh |
| `CreatureHealth` | Body damage, torpor threshold, **node** damage, permanent death | `IDamageable` |
| `DialogueManager` | Plays `DialogueSequence` SOs: lines, subtitles, voice clips, choices, timing | `AudioManager`, `UIManager` |
| `RadioCommunicationSystem` | Channels per crew member, quality (0–1), channel death, radio wheel | `DialogueManager`, `AudioManager` |
| `AudioManager` | Mixer snapshots, pooled sources, ducking | Unity Audio Mixer |
| `CutsceneManager` | Plays Timelines, switches input map to `Cinematic`, handles skip | Timeline, Cinemachine |
| `MissionManager` | Chapter/objective state machine, story flags, choice records | `SaveSystem` |
| `PuzzleSystem` | Reusable puzzle components (power routing, keypad, sequence switches) | `MissionManager` |
| `EnvironmentalInteraction` | Doors, airlocks, valves, lights, breakable objects | `PowerNetwork` |
| `SaveSystem` | Checkpoint saves to JSON (story flags, inventory, chapter, position) | `ISaveable` |
| `UIManager` | HUD, prompts, subtitles, records, pause/settings | — |
| `SceneTransitionManager` | Additive loading, fades, loading screens | `SaveSystem` |

### 3.1 Creature AI (technical explanation)
A finite state machine with **suspicion** as its input:
- Each `VibrationEvent` has `position`, `loudness`, `medium`. A listener computes perceived intensity = `loudness × falloff(distance) × mediumFactor × rimeAmplifier`. Outdoors, `mediumFactor` is 0 for air events and 1 for ground events; indoors both are 1.
- Suspicion rises with perceived intensity and decays over time. Thresholds drive transitions: **Listening** (stops and kneels, palms down) → **Investigating** (moves to the last estimated source with error proportional to distance) → **Hunting** (direct pursuit when intensity is high or the player is seen within 4 m).
- **Torpor:** when body damage crosses a threshold, the creature enters torpor for a random 20–40 s, then recovers with partial health, unless ≥3 nodes are ruptured or it takes explosive damage while in torpor.
- This is pure logic in a plain C# class (`SuspicionModel`, `CreatureHealthModel`), wrapped by MonoBehaviours, so it is **unit-testable in EditMode**.

### 3.2 Dialogue and radio architecture
- `DialogueLine` (data): speaker, text, `AudioClip`, delivery note (dev-only), mode (in-person/radio/recorded/PA), processing preset, delay, optional condition flag.
- `DialogueSequence` (ScriptableObject): ordered lines, choices, events fired at the end.
- `RadioCommunicationSystem` owns a `RadioChannel` per speaker with `quality` and `alive`. Radio lines are routed through a mixer group whose effects are driven by `quality`. Killing a channel (`SetAlive(false)`) greys it on the wheel permanently and is saved.

### 3.3 Save/load
Checkpoint-based. A `SaveData` POCO collects `ISaveable` states (story flags, choices, inventory, health, current scene and checkpoint ID) and serialises it with `JsonUtility` to `Application.persistentDataPath`. Versioned (`saveVersion`) so old saves can be migrated or rejected cleanly. Round-trip serialisation is unit-tested.

### 3.4 Resource management
Hand-placed pickups; a `ResourceBudget` ScriptableObject per chapter documents the intended totals (ammo, med-patches, sealant) so balancing is visible and testable (an EditMode test asserts that placed items match the budget).

## 4. Unit and automated tests

| Test | Type |
|---|---|
| Suspicion rises/decays correctly; vacuum blocks air events | EditMode |
| Creature health: torpor threshold, recovery, node kill, explosive kill in torpor | EditMode |
| Inventory add/remove/stack limits | EditMode |
| Weapon ammo/reload state machine | EditMode |
| Save data round-trip and version handling | EditMode |
| Radio channel quality → effect parameters mapping; channel death persists | EditMode |
| Mission flag progression for each chapter | EditMode |
| Player can traverse each level start→end (scripted walker) | PlayMode (later) |

**CI (GitHub Actions):**
1. **Repository checks** (no Unity licence needed): verify every asset has a `.meta` file, no file > 50 MB outside LFS, no forbidden folders committed (`Library/`, `Temp/`), and licence register entries exist for `ThirdParty/` folders.
2. **Unity tests** via the open-source GameCI test-runner action. This **requires storing a Unity licence as repository secrets**, which you'll set up once (instructions will be in the README).

## 5. Asset requirements

| Category | Count (approx.) | Source plan |
|---|---|---|
| Characters (5 crew + 3 Earth people) | 8 | Low-poly realistic humans: customise a free base mesh in Blender (MakeHuman CC0 output or similar) and **verify licences before committing**. Crew mostly seen in suits; faces needed only for close scenes. |
| Creatures (Listener, Needle, Keel back) | 3 | **Original models in Blender** (required: they must be unique). Listener is the priority: ~15k tris, simple rig. Needle: ~3k tris. Keel: a sculpted back section only. |
| Animations | ~40 clips | Humans: licence-checked animation sources plus hand-keyed. Creatures: hand-keyed in Blender (idle-listen, walk, bound, lunge, crest strike, torpor, recover, death). |
| Environment kits | 3 | `Meridian` modular kit, `Conamara` modular kit, Europa ice-block kit. Blender + CC0 textures (e.g., Poly Haven, ambientCG). |
| Props | ~80 | Blender + CC0 packs (e.g., Kenney for placeholders). |
| Jupiter / Earth / sky | 3 | Public-domain NASA imagery or CC BY texture sets (credited). |
| Audio SFX | ~250 | Self-recorded + Freesound (CC0 preferred; CC BY credited) + Sonniss GDC bundles (royalty-free licence). |
| Voice | ~650 lines | See audio doc §A6. |
| Music | ~6 cues | Original (self-composed or commissioned) or CC BY with credit. |

**Placeholder-first:** every system is built with grey boxes and capsules before any art exists.

### 5.1 Asset licensing policy (public repository)
- Only assets whose licence **permits redistribution in source form** may be committed: CC0, CC BY (with credit), public domain, or original work.
- **Unity Asset Store assets may not be redistributed in a public repository** under the Asset Store EULA. If any are used, they live in a git-ignored folder (`Assets/ThirdParty/Private/`) and the README lists them so anyone cloning knows what to import.
- Every third-party asset gets a row in `docs/ASSET_LICENSES.md`: name, author, source URL, licence, where it's used.

## 6. Hardware optimisation plan (Intel Iris Xe, 16 GB)

| Area | Plan |
|---|---|
| Resolution | Target **1280×720** at 60 fps on "Medium", with URP render-scale slider (0.7–1.0) and optional FSR/upscaling. 900p "High". |
| Lighting | **Baked lightmaps** for static interiors and the surface; few real-time lights (≤ 4 per view); one shadow-casting directional light; spot-light shadows only on the swinging drill lamp and the player's flashlight. |
| Shadows | Shadow distance 25 m indoors / 60 m outdoors, 2 cascades, soft shadows off on Low. |
| Post-processing | Tonemapping (ACES), bloom (low), vignette, film grain, colour grading LUT. **No** SSR or heavy volumetrics. **Fake volumetrics** via cheap fog cards and gradient fog. |
| Geometry | LOD groups on all props/kits; **occlusion culling** baked for interiors; static batching; GPU instancing on ice blocks and repeated props. |
| Textures | 1K–2K max, ASTC/BC compression, mipmaps, texture streaming. |
| Scenes | Additive loading per area; unload previous chapter. |
| Physics | Simple colliders; fixed timestep 0.02; no cloth. |
| Audio | Compressed Vorbis for ambience/music, ADPCM for short SFX, streaming for long dialogue. Pooled AudioSources. |
| Pooling | Projectiles, decals, VFX, Needles. |
| Animation | Culling mode "cull completely" off-screen; ≤ 3 active creatures at once. |
| Profiling | Unity Profiler + Frame Debugger at each milestone; **performance budget** documented: ≤ 16.6 ms frame, ≤ 300 draw calls (SRP batcher on), ≤ 1.5 M tris in view. |

## 7. Development schedule

Assumes **~15 hours/week** alongside studies. Estimates are honest planning numbers, not promises.

| Phase | Weeks | Milestone / deliverable |
|---|---|---|
| 0. Pre-production | 1 | This document approved |
| 1. Foundation | 2 | URP project, folders, input actions, FP controller, grey-box test room, CI repository checks |
| 2. Visual prototype | 3 | Grey-box *Meridian* segment, lighting test on Iris Xe, interaction system, environment audio |
| 3. Story foundation | 3 | Dialogue + radio systems, subtitles, mission manager, save system, temp VO |
| 4. **Vertical slice** | 6 | Ch. 6–7 in Conamara Base fully playable with placeholder art (see gameplay doc §9.4) |
| 5. Creature system | (inside 4) | Listener AI, detection, torpor/node health, Needles |
| 6. Combat and survival | (inside 4) | Bolt driver, health, suit, inventory |
| — | — | **Review point: is the slice frightening? Adjust before scaling up.** |
| 7. Europa environment | 4 | Surface level, Jupiter skybox, vacuum audio, Rime |
| 8. Remaining chapters | 8 | Ch. 1–5, 8–10 at grey-box → playable |
| 9. Cinematic story | 4 | Timelines, real VO, animations, Mara's death sequence |
| 10. Art pass | 6 | Final models, textures, lighting bake |
| 11. Polish and optimisation | 4 | Performance budget met, bugs, accessibility, playtests |
| 12. Release | 1 | Windows build, trailer, screenshots, README |
| **Total** | **~42 weeks** | About 10 months part-time |

A **portfolio-ready vertical slice** exists at about **week 15**, so recruiters can see real work long before the full game is finished.

## 8. GitHub release strategy

- **Repository:** `jupiter-the-last-transmission` under `aadnan250`, **public** from the start, so the commit history shows steady progress.
- **Commits:** authored only by the developer; small, descriptive messages ("Add suspicion model with vacuum attenuation").
- **Branches:** `main` (always builds), short-lived feature branches merged with pull requests (this shows a professional workflow).
- **Milestones and issues:** each phase is a GitHub Milestone; tasks are Issues; a Project board tracks them.
- **Releases:** tagged builds `v0.1-foundation`, `v0.4-vertical-slice`, `v1.0`; Windows build zipped as a **GitHub Release asset** (not committed to the repo).
- **README:** hero image/GIF of Jupiter from Europa, 30-second trailer, feature list, technical highlights (AI, vibration propagation, radio system), architecture diagram, build instructions, controls, known limitations, licence register link.
- **Docs:** `docs/ARCHITECTURE.md`, `docs/AI.md`, `docs/AUDIO.md`, `docs/PERFORMANCE.md` (with profiler captures), `docs/ASSET_LICENSES.md`, `docs/DEVLOG.md`.
- **Playable web demo:** not planned. A one-hour URP horror game is a poor fit for WebGL on recruiters' laptops. A short **video** plus a downloadable Windows build is more reliable.
