# TODO: vertical-slice build plan

Small phases, each ending with something playable and tested. `[x]` = done and in the repo, `[ ]` = to do.

## Slice 1: Player movement ✅
- [x] First-person controller: walk, run, crouch (headroom check), jump, look
- [x] Input actions for keyboard/mouse and gamepad (PS5 and Xbox)
- [x] Gravity profiles: Earth, ship spin ring, Europa
- [x] Grey-box test room and movement unit tests

## Slice 2: Interaction ✅
- [x] Look-at interaction with device-aware button prompts
- [x] Doors, light switch, readable documents with a reading panel
- [x] Split auto-doors with status lights, and locked doors

## Slice 3: The ship ✅
- [x] Meridian layout: corridor and 10 furnished rooms, observation deck, bridge windows, starfield
- [x] Procedural ship textures, ship kit, signs, blinking equipment
- [x] Room tones, soft footsteps, performance overlay

## Slice 4: Talking and choices ✅
- [x] Dialogue scripts, parser and runner (up to 4 replies)
- [x] Every reply has a consequence (enforced by tests); consequence ledger
- [x] Eerie sting, screen pulse, rumble and PS5 light bar on significant choices
- [x] Objectives, radio calls, save/load and autosave

## Slice 5: Living crew ✅
- [x] Realistic crew models (MakeHuman pipeline), lip sync, blinking, head tracking
- [x] Voiced lines for every character (Piper), radio filter

## Slice 6: Creature and survival core ✅
- [x] Vibration hearing, suspicion, torpor and node kills (unit tested)
- [x] Health, inventory, bolt driver, combat HUD
- [x] Conamara Base grey-box with the Listener and a NavMesh

## Slice 7: Chapters 6–7 story in the base (in progress)
- [x] Ch. 6 "The Discovery": base exploration, Tomas's message choice, Tiamat log, Ashford's sealed orders, palm prints
- [x] Ch. 6.3 EVA to the *Tiamat* site itself (on the Europa surface: the lander, the manifest, Directive 7)
- [x] Ch. 7 "Contact": confrontation, drill chamber eruption, first Listener encounter, Ashford's door, retreat, safe room
- [x] Luca's radio death and Ashford's confession over a failing channel (on the move; Ashford gives the ascent code)
- [x] The Listener rebuilt on the Blender creature (crest, six back nodes); a pistol for the base; a downed Listener can be finished off
- [x] Hiding spots (lockers), thrown distractions, flare pistol
- [x] Needles (vent crawlers): they pour out of vents in the ice during the siege at Halcyon
- [ ] Playtest pass: is it frightening?

## Slice 8: Europa surface (in progress)
- [x] Surface level (Conamara Chaos), Jupiter fixed low on the horizon, moons, Milky Way
- [x] Landing in the lander Wren: ramp onto the ice, suited landing party (Ashford, Mara, Hana) walking and fighting with Sam
- [x] Quake and ambush: ten Stalkers burst from the ice; arc beam rifles with first-person arms, charge and cells HUD
- [x] Story moved onto the surface: Tiamat's lander and Directive 7, the drill and the Listener's eruption, Luca and Ashford's last calls, the run to the ascent vehicle Halcyon
- [x] Conamara Base interiors taken out of the story flow (scene kept in the project; tests still cover it)
- [x] Vacuum audio (suit, boots and radio only; the world muffled through the suit), suit integrity (breach leaks, patches seal), radiation dose timer
- [x] Distant silhouette reveal beyond the ridge on the way to Tiamat, Rime fields on the ice

## Slice 9: Remaining chapters ✅ (the story plays start to finish)
- [x] Ch. 1 Cabo Lumen at dawn: the family goodbye (Ada, Zara), the gantry, the launch; Ch. 3 the first sound; Ch. 4 the blackout and relay puzzle
- [x] Ch. 8 the siege at Halcyon and Mara's last words; Ch. 9 the sample (vent or keep) and the last transmission; Ch. 10 home with Grace, Ada and Zara
- [x] Ending variants from the sample choice, the five names, credits (voice licence credited)

## Slice 10: Art and polish
- [ ] Final Listener model and animation; facial expressions for the crew
- [x] Music cues: a synthesised score (family theme, ship, Europa dread, fights, ending) that follows the story
- [ ] Baked lighting, real Jupiter imagery
- [x] Accessibility (subtitles size, invert Y, head-bob toggle, look sensitivity, volume), settings menu, pause menu (Esc / Options)
- [x] Performance: graphics presets (Low / Medium / High: render scale, shadow distance, MSAA), 60 fps cap with vsync; Medium by default
- [ ] Measure 60 fps at 720p on Iris Xe (needs the laptop)

## Slice 11: Release
- [ ] Windows build as a GitHub Release, trailer video, final README and screenshots
