# JUPITER: THE LAST TRANSMISSION

*The mission was supposed to take humanity closer to the stars. It was never supposed to bring something back.*

A first-person science-fiction survival-horror game set in 2183, built in **Unity 6 (URP)** and **C#** for Windows. Playable with keyboard and mouse or a **PS5 DualSense** / Xbox controller.

[![Repository checks](https://github.com/aadnan250/jupiter-the-last-transmission/actions/workflows/repo-checks.yml/badge.svg)](https://github.com/aadnan250/jupiter-the-last-transmission/actions/workflows/repo-checks.yml)

> **[Download v1.0.0 for Windows](https://github.com/aadnan250/jupiter-the-last-transmission/releases/tag/v1.0.0)**: unzip, then double-click `JupiterTheLastTransmission.exe`. **[Watch the trailer](https://github.com/aadnan250/jupiter-the-last-transmission/releases/download/v1.0.0/JupiterTheLastTransmission-Trailer.mp4)** (65 s).
>
> **Status: the whole game plays start to finish.** Ten chapters: the launch from Cabo Lumen, the voyage on the *Meridian*, the landing and the fight on Europa, the siege at the ascent vehicle, the long way home, and an ending that depends on what you did. Fully voiced, with a score, a pause and settings menu, and a Windows build. More than 210 automated tests.

| | | |
|---|---|---|
| ![Goodbye](docs/images/story-cabo-goodbye.png) | ![Rocket](docs/images/story-cabo-rocket.png) | ![Jupiter](docs/images/story-jupiter.png) |
| *Ch. 1: the goodbye at Cabo Lumen* | *The stack on the pad* | *Jupiter from the Meridian* |
| ![Ambush](docs/images/europa-ambush.png) | ![Silhouette](docs/images/story-silhouette.png) | ![Halcyon](docs/images/europa-halcyon.png) |
| *Europa: the ambush* | *Something beyond the ridge* | *Halcyon, the way off the ice* |
| ![Canister](docs/images/story-canister.png) | ![Home](docs/images/story-home.png) | ![Stalker](docs/images/europa-stalker.png) |
| *Ch. 9: Ashford's canister* | *Ch. 10: home* | *A Stalker* |

| | | | |
|---|---|---|---|
| ![Mara](docs/images/crew-mara.png) | ![Ashford](docs/images/crew-ashford.png) | ![Luca](docs/images/crew-luca.png) | ![Hana](docs/images/crew-hana.png) |
| *Mara, flight engineer* | *Commander Ashford* | *Luca, pilot* | *Dr. Hana Mori* |

| | |
|---|---|
| ![Corridor](docs/images/phase2-corridor.png) | ![Observation deck](docs/images/phase2-observation-deck.png) |
| *Habitat ring corridor* | *Observation deck: Jupiter fills the window* |
| ![Command](docs/images/phase2-command.png) | ![Galley](docs/images/phase2-galley.png) |
| *Command deck* | *Galley* |

## About

The first crewed expedition to Europa, one of Jupiter's moons, goes wrong. The crew went to study the ice. Something beneath it was already listening.

- ~60-minute narrative horror experience
- **Every decision matters:** up to four replies in every conversation, each one changing a crew member's trust or the story's memory
- Scientifically grounded: vacuum silences sound, Jupiter hangs fixed in Europa's sky, and messages from Earth arrive with real light-delay
- Original creatures driven by a vibration-based detection AI: the Listener, the Stalkers that burst from the ice, and the Needles that pour out of vents
- Survival on the surface: an EVA suit that cracks and leaks, a radiation dose that keeps climbing, and sound that only reaches you through your suit
- Two endings, decided by what you do with the sample, then the names of the crew and the credits
- Designed to run on integrated graphics (Intel Iris Xe), with Low, Medium and High presets

### The story, chapter by chapter

| Level | Chapters | What happens |
|---|---|---|
| `L01_CaboLumen` | 1 | Dawn at the launch site. Say goodbye to Ada and Zara, walk to the tower, launch. |
| `L02_Meridian` | 2–4 | The voyage: the crew, Jupiter, the first sound from Europa, the blackout and the relay. |
| `L03_EuropaSurface` | 5–8 | The landing, the ambush, Tiamat's lander and Directive 7, the drill, the Listener, the siege at Halcyon. |
| `L05_Return` | 9 | Alone on the way home: Ashford's sample, kept or vented, and the last transmission. |
| `L06_Home` | 10 | Ten months later, the recovery ship, Zara's question, and the ending. |

## Technical highlights

### Narrative systems
- **Custom dialogue language + parser.** Conversations are plain-text scripts with nodes, conditions, effects and up to four replies ([format](docs/DIALOGUE_FORMAT.md)). The parser reports errors with file and line numbers.
- **Pure-C# dialogue runner** (no Unity dependency) walks the graph against a `StoryState` of trust values (−5..+5) and flags. Fully unit tested, including infinite-loop detection.
- **Consequences are enforced by tests:** a content test fails the build if any reply in any conversation has no effect, or any conversation lacks a significant decision. See the [consequence ledger](docs/CONSEQUENCES.md).
- **Significant choices are felt, not explained:** an original synthesised sting (reversed breath, sub impact, a detuning dissonant cluster over a whispering formant bed), a dark screen pulse, a heartbeat rumble, and the **DualSense light bar bleeding red**.
- **Reactive content:** a document read in the science bay unlocks a hidden reply for the Commander; who radios you on the observation deck depends on what you told whom.
- **Radio calls** with squelch and a `[RADIO]` tag, objective tracking driven by story events, and **save/load** (JSON, versioned, atomic writes, autosave after every conversation).

### Characters and voices
- **Realistic crew from CC0 MakeHuman assets**, generated by a headless Blender + MPFB script from per-character data sheets (age, build, ethnicity, hair, outfit), rigged with a 163-bone skeleton including jaw and eyelids ([pipeline](docs/CHARACTERS.md)).
- **Every line is voiced** with Piper, a local neural TTS. Nine characters were cast from 904 speakers by **measuring each voice's pitch** with an autocorrelation tracker ([voices](docs/VOICES.md)).
- **Lip sync from audio loudness**, plus procedural breathing, blinking, weight shift and head-tracking. Bone axes are derived from the skeleton, not hard-coded.
- **Facial expressions:** each line is read for its stage direction and words (smile, sadness, fear, sternness), and the brows and mouth corners move to match while it is spoken.
- **Creatures from a headless Blender pipeline** (metaballs, remesh, custom weights, baked textures), animated procedurally: the Listener kneels to listen, stalks upright, runs on all fours, recoils from light and slumps in torpor.
- Voices play **from the speaking character's position** in 3D; radio lines go through a band-pass and distortion filter.

### Engine and tools
- **Controller support:** Input System action maps for Player and Dialogue; on-screen prompts switch live between keyboard, PlayStation (▲ ● X ■) and Xbox glyphs based on the last device used.
- **Environment profiles.** Gravity and movement come from ScriptableObjects: Earth (1 g), the *Meridian*'s spin ring (0.6 g) and Europa (0.134 g, the real value).
- **Procedural audio and music, 100% original.** Room tones, doors, footsteps, weapons, creatures, the consequence sting and the whole score (a family theme, the ship, Europa's dread, the fights, the ending) are synthesised in C# and written to WAV by an editor tool: deterministic, seamless loops (tested), no licensing risk. The music follows story flags and crossfades.
- **Generated levels.** Every level, the crew placement, the player prefab, the creatures and the textures are built by editor scripts (**Jupiter → Rebuild Full Story**).
- **Performance budget for Intel Iris Xe.** URP Forward+, Low / Medium / High presets (render scale, shadow distance, MSAA), a 60 fps cap with vsync, box-projected reflection probes captured once per level; press **F3** for a frame-time overlay.
- **Pause and settings menu** (Esc / Options): look sensitivity, invert Y, head bob, subtitle size, volume and graphics quality, saved between sessions.
- **Assembly definitions** separate runtime, editor tooling and tests. **CI** checks repository hygiene on every push.

## Getting started

**Requirements:** Unity **6000.6.10f1** (install via Unity Hub) and [Git LFS](https://git-lfs.com).

```powershell
git lfs install
git clone https://github.com/aadnan250/jupiter-the-last-transmission.git
```

1. In Unity Hub: **Add → Add project from disk** and select the cloned folder.
2. The first level, `Assets/_Project/Scenes/L01_CaboLumen.unity` (Chapter 1, the launch from Earth), opens automatically; press **Play**.
   The Game view plays maximized over the whole editor (Shift+Space toggles it); a built game starts in borderless full screen.
   The whole story plays start to finish, each level leading into the next with a time card:
   - `L01_CaboLumen`: say goodbye to Ada and Zara, climb the tower, launch.
   - `L02_Meridian`: the ship, Jupiter, the first sound from Europa, the blackout and the relay.
   - `L03_EuropaSurface`: the landing, the ambush, Tiamat, the drill and the Listener, the siege at Halcyon.
   - `L05_Return`: alone on the way home: Ashford's sample, and the last transmission.
   - `L06_Home`: the recovery ship, Zara's question, and the ending (it changes with what you did with the sample).
   Your choices carry all the way through. To jump to a chapter, open its scene and press Play.
3. On Earth: talk to Ada, then walk to the launch tower. On the Meridian: talk to the crew (Luca and Hana in the galley, Mara in the workshop, Commander Ashford in command), go to the observation deck, then the seismometer console; in the blackout, reset the right relay.
   On Europa: survive the ambush, follow the green beacon to Tiamat's lander, read its cargo manifest, cover Hana at the drill, kill what comes out of the bore (knock it down, then keep firing), then hold the pad at Halcyon until the hatch opens and board.
   Back aboard: look at the canister in the cargo hold, then record the transmission at the comms console. At home, Zara is waiting.
   On Europa, watch the **SUIT** and **RAD** readouts: a breached suit leaks until you use a med-patch (**H**), which also seals it.
4. To regenerate all generated levels, audio and prefabs: **Jupiter → Rebuild Full Story** (or **Rebuild All Generated Content** for every scene, including the test rooms).

### Building the Windows game

**Jupiter → Build Windows Player** builds the five story levels into `Builds/Windows/JupiterTheLastTransmission.exe`. From the command line:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.10f1\Editor\Unity.exe" -batchmode -quit -projectPath . -executeMethod Jupiter.EditorTools.WindowsBuild.Build -logFile build.log
```

Copy the whole `Builds/Windows` folder to share the game; double-click the `.exe` to play.

### Making the trailer

The trailer is recorded from the running game and cut in Blender, with the game's own music and voices. First record the shots as frames into `Builds/Trailer`:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.10f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Jupiter.Tests.TrailerCapture
```

Then add the title cards, music and voice lines and render `Builds/Trailer/trailer.mp4`:

```powershell
& "..\_tools\blender\blender.exe" -b --factory-startup --python Tools/TrailerPipeline/edit_trailer.py -- .
```

### Controls

| Action | Keyboard / Mouse | PlayStation (DualSense) | Xbox |
|---|---|---|---|
| Move / Look | WASD / Mouse | Left / right stick | Left / right stick |
| Run | Left Shift | L3 | LS |
| Crouch (toggle) | Left Ctrl / C | ● Circle | B |
| Jump | Space | X Cross | A |
| Fire (hold for the beam rifle) | Left mouse | R2 | RT |
| Reload / swap power cell | R | ▲ Triangle | Y |
| Use med-patch | H | D-pad up | D-pad up |
| Throw held object | G | R1 | RB |
| Fire flare pistol | T | D-pad down | D-pad down |
| Interact / Talk / Hide in locker | E | ■ Square | X |
| Continue dialogue | E / Space / Click | X Cross | A |
| Replies 1–4 | 1 / 2 / 3 / 4 | ▲ / ● / X / ■ | Y / B / A / X |
| Quick save / load | F5 / F9 | (autosave after conversations) | |
| Performance overlay | F3 | | |
| Pause menu and settings (sensitivity, invert Y, head bob, subtitle size, volume) | Esc | Options | Menu |

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
  Scripts/Combat/      Health, suit, weapons (beam rifle, pistol, bolt driver, flares), squad, inventory
  Scripts/Creatures/   Listener, Stalkers and Needles: hearing, suspicion, torpor, procedural rigs
  Scripts/Audio/       Procedural synthesis and score, music cues, vacuum audio, WAV encoder, footsteps
  Scripts/World/       Environment profiles and zones, doors, switches, readables
  Scripts/UI/          HUD, dialogue panel, reading panel, pause and settings menu, performance overlay
  Scripts/Editor/      Level, prefab, audio and texture generators
  Narrative/Dialogue/  Conversation scripts (.txt)
  Art/Characters/      Generated crew models, textures and materials
  Resources/Voice/     Generated voice lines
  Tests/EditMode/      Unit and content-validation tests
  Tests/PlayMode/      Scene and end-to-end dialogue tests
Tools/CharacterPipeline/  Blender + MPFB character build script
Tools/CreaturePipeline/   Blender creature build script (the Stalker and Listener body)
Tools/VoicePipeline/      Piper voice generation
Tools/TrailerPipeline/    Blender script that cuts the trailer
docs/                  Design and technical documentation
```

## Documentation

- [Pre-production design document](docs/preproduction/README.md)
- [Dialogue script format](docs/DIALOGUE_FORMAT.md)
- [Creature AI: hearing, suspicion and torpor](docs/AI.md)
- [Consequence ledger](docs/CONSEQUENCES.md)
- [Characters pipeline](docs/CHARACTERS.md)
- [Voices and credits](docs/VOICES.md)
- [Asset licence register](docs/ASSET_LICENSES.md)

## Roadmap

| Phase | Status |
|---|---|
| 0. Pre-production | Done |
| 1. Project foundation | Done |
| 2. Visual prototype (*Meridian* grey-box, lighting, interactions, ambience) | Done |
| 3. Story foundation (dialogue, consequences, radio, objectives, save, controller support) | **Done** |
| 4. Vertical slice (Chapters 6–7 on Europa) | Done |
| 5. Full story, Chapters 1–10 with the ending | Done |
| 6. Polish (pause and settings, suit and radiation, music, Needles, faces, Listener animation, lighting, Jupiter, graphics presets) | Done |
| 7. Release (Windows build, README, screenshots, GitHub Release v1.0.0) | Done |

## Known limitations

- Environments are built from primitives by editor scripts, and every sound and the music are synthesised; there are no hand-made art or recorded audio assets.
- Voices are synthesised (Piper TTS), not acted; any line can be replaced by a recorded file with the same name.
- Faces move brows, mouth corners, jaw and eyelids; body motion is procedural (no motion-captured animation clips).
- DualSense support uses Unity's Input System (rumble and light bar); adaptive triggers are not used.
- The habitat ring is modelled as locally flat. Lighting is real-time by design (the blackout and alarms change it while you play), with reflection probes for the interiors.
- 60 fps at 720p on Iris Xe is the target; it has not yet been measured on that hardware.
- Unity test runs are local for now. Running them in CI needs a Unity licence stored as repository secrets (planned).

## Author

**adnan** ([@aadnan250](https://github.com/aadnan250))
