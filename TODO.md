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
- [ ] Ch. 6.3 EVA to the *Tiamat* site itself (needs the Slice 8 surface level; the log is read in the lab for now)
- [ ] Ch. 7 "Contact": drill chamber eruption, first Listener encounter, retreat, safe room
- [ ] Luca's radio death and Ashford's confession over a failing channel
- [x] Hiding spots (lockers), thrown distractions, flare pistol
- [ ] Needles (vent crawlers)
- [ ] Playtest pass: is it frightening?

## Slice 8: Europa surface
- [ ] Surface level (Conamara Chaos), Jupiter fixed low on the horizon
- [ ] Vacuum audio (suit, boots and radio only), suit integrity, radiation timer
- [ ] Distant silhouette reveal, Rime fields

## Slice 9: Remaining chapters
- [ ] Ch. 1 Earth launch, Ch. 2 journey scenes, Ch. 3 Jupiter arrival and first sound, Ch. 4 signal loss puzzle
- [ ] Ch. 8 Mara's death sequence, Ch. 9 escape (the Keel) and last transmission, Ch. 10 home
- [ ] Ending variants from the sample choice

## Slice 10: Art and polish
- [ ] Final Listener model and animation; facial expressions for the crew
- [ ] Baked lighting, real Jupiter imagery, music cues
- [ ] Accessibility (subtitles size, invert Y, head-bob toggle), settings menu, pause menu
- [ ] Performance pass on Iris Xe (60 fps at 720p)

## Slice 11: Release
- [ ] Windows build as a GitHub Release, trailer video, final README and screenshots
