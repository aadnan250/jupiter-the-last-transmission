# JUPITER: THE LAST TRANSMISSION

*The mission was supposed to take humanity closer to the stars. It was never supposed to bring something back.*

A first-person science-fiction survival-horror game set in 2183, built in **Unity 6 (URP)** and **C#** for Windows. Playable with keyboard and mouse or a **PS5 DualSense** / Xbox controller.

[![Repository checks](https://github.com/aadnan250/jupiter-the-last-transmission/actions/workflows/repo-checks.yml/badge.svg)](https://github.com/aadnan250/jupiter-the-last-transmission/actions/workflows/repo-checks.yml)

> **Status: Phase 3 — Story foundation complete.** The crew of the *Meridian* can be talked to. Every reply changes trust or the story, and the big decisions are *felt*. Branching dialogue, radio calls, objectives and save/load are in, covered by 60 automated tests.

| | |
|---|---|
| ![Corridor](docs/images/phase2-corridor.png) | ![Observation deck](docs/images/phase2-observation-deck.png) |
| *Habitat ring corridor* | *Observation deck: Jupiter fills the window* |
| ![Command](docs/images/phase2-command.png) | ![Galley](docs/images/phase2-galley.png) |
| *Command deck* | *Galley: Luca and Hana (placeholder crew)* |

## About

The first crewed expedition to Europa, one of Jupiter's moons, goes wrong. The crew went to study the ice. Something beneath it was already listening.

- ~60-minute narrative horror experience
- **Every decision matters:** up to four replies in every conversation, each one changing a crew member's trust or the story's memory
- Scientifically grounded: vacuum silences sound, Jupiter hangs fixed in Europa's sky, and messages from Earth arrive with real light-delay
- Original creatures driven by a vibration-based detection AI
- Designed to run on integrated graphics (Intel Iris Xe)

## Technical highlights

### Narrative systems
- **Custom dialogue language + parser.** Conversations are plain-text scripts with nodes, conditions, effects and up to four replies ([format](docs/DIALOGUE_FORMAT.md)). The parser reports errors with file and line numbers.
- **Pure-C# dialogue runner** (no Unity dependency) walks the graph against a `StoryState` of trust values (−5..+5) and flags. Fully unit tested, including infinite-loop detection.
- **Consequences are enforced by tests:** a content test fails the build if any reply in any conversation has no effect, or any conversation lacks a significant decision. See the [consequence ledger](docs/CONSEQUENCES.md).
- **Significant choices are felt, not explained:** an original synthesised sting (reversed breath, sub impact, a detuning dissonant cluster over a whispering formant bed), a dark screen pulse, a heartbeat rumble, and the **DualSense light bar bleeding red**.
- **Reactive content:** a document read in the science bay unlocks a hidden reply for the Commander; who radios you on the observation deck depends on what you told whom.
- **Radio calls** with squelch and a `[RADIO]` tag, objective tracking driven by story events, and **save/load** (JSON, versioned, atomic writes, autosave after every conversation).

### Engine and tools
- **Controller support:** Input System action maps for Player and Dialogue; on-screen prompts switch live between keyboard, PlayStation (▲ ● X ■) and Xbox glyphs based on the last device used.
- **Environment profiles.** Gravity and movement come from ScriptableObjects: Earth (1 g), the *Meridian*'s spin ring (0.6 g) and Europa (0.134 g, the real value).
- **Procedural audio, 100% original.** Room tones, doors, soft footsteps, radio squelch and the consequence sting are synthesised in C# (including an RBJ biquad filter) and written to WAV by an editor tool: deterministic, seamless loops (tested), no licensing risk.
- **Generated levels.** The *Meridian* level, crew placement, the player prefab and placeholder textures are built by editor scripts (**Jupiter → Rebuild All Generated Content**).
- **Performance budget for Intel Iris Xe.** URP Forward+, no additional-light shadows, 30 m / 2-cascade main shadows, MSAA off; press **F3** for a frame-time overlay.
- **Assembly definitions** separate runtime, editor tooling and tests. **CI** checks repository hygiene on every push.

## Getting started

**Requirements:** Unity **6000.6.10f1** (install via Unity Hub) and [Git LFS](https://git-lfs.com).

```powershell
git lfs install
git clone https://github.com/aadnan250/jupiter-the-last-transmission.git
```

1. In Unity Hub: **Add → Add project from disk** and select the cloned folder.
2. Open `Assets/_Project/Scenes/L02_Meridian.unity` (it opens automatically on a fresh clone) and press **Play**.
3. Talk to the crew: Luca and Hana in the galley, Mara in the workshop, Commander Ashford in command. Then go to the observation deck.
4. To regenerate all generated levels, audio and prefabs: **Jupiter → Rebuild All Generated Content**.

### Controls

| Action | Keyboard / Mouse | PlayStation (DualSense) | Xbox |
|---|---|---|---|
| Move / Look | WASD / Mouse | Left / right stick | Left / right stick |
| Run | Left Shift | L3 | LS |
| Crouch (toggle) | Left Ctrl / C | ● Circle | B |
| Jump | Space | X Cross | A |
| Interact / Talk | E | ■ Square | X |
| Continue dialogue | E / Space / Click | X Cross | A |
| Replies 1–4 | 1 / 2 / 3 / 4 | ▲ / ● / X / ■ | Y / B / A / X |
| Quick save / load | F5 / F9 | (autosave after conversations) | |
| Performance overlay | F3 | | |

### Running tests

In Unity: **Window → General → Test Runner**, then run **EditMode** and **PlayMode**. From the command line:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.10f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml
```

## Project structure

```
Assets/_Project/
  Scripts/Core/        Game session, save system
  Scripts/Narrative/   Dialogue parser, runner, manager, crew, triggers, objectives, consequences
  Scripts/Player/      Controller, camera, input, interaction, device-aware button prompts
  Scripts/Audio/       Procedural synthesis, WAV encoder, room tones, footsteps
  Scripts/World/       Environment profiles and zones, doors, switches, readables
  Scripts/UI/          HUD, dialogue panel, reading panel, performance overlay
  Scripts/Editor/      Level, prefab, audio and texture generators
  Narrative/Dialogue/  Conversation scripts (.txt)
  Tests/EditMode/      Unit and content-validation tests
  Tests/PlayMode/      Scene and end-to-end dialogue tests
docs/                  Design and technical documentation
```

## Documentation

- [Pre-production design document](docs/preproduction/README.md)
- [Dialogue script format](docs/DIALOGUE_FORMAT.md)
- [Consequence ledger](docs/CONSEQUENCES.md)
- [Asset licence register](docs/ASSET_LICENSES.md)

## Roadmap

| Phase | Status |
|---|---|
| 0. Pre-production | Done |
| 1. Project foundation | Done |
| 2. Visual prototype (*Meridian* grey-box, lighting, interactions, ambience) | Done |
| 3. Story foundation (dialogue, consequences, radio, objectives, save, controller support) | **Done** |
| 4. Vertical slice (Chapters 6–7 on Europa) | Next |
| 5–12. Full production and release | Planned |

## Known limitations

- Grey-box art only; crew are placeholder capsules. Sounds and the Jupiter texture are procedural placeholders.
- No voice acting yet: dialogue is subtitled. Radio voice processing arrives with recorded lines.
- DualSense support uses Unity's Input System (rumble and light bar); adaptive triggers are not used.
- The habitat ring is modelled as locally flat. Lighting is real-time during grey-boxing; baked lighting comes with the art pass.
- Unity test runs are local for now. Running them in CI needs a Unity licence stored as repository secrets (planned).

## Author

**adnan** ([@aadnan250](https://github.com/aadnan250))
