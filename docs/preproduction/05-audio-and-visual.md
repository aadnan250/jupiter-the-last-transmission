# 05 — Sound Design and Visual Direction

## Part A — Sound design

### A1. Principles
1. **Physics decides what can be heard.** In vacuum, there is no airborne sound. Outside, the player hears only: their suit (breathing, fans, servo clicks), contact sound conducted through the boots and gloves, and the radio. **[SCIENCE]**
2. **Silence is an instrument.** Every chapter has at least one long silence that the player notices.
3. **The Feature 7 pulse is the game's leitmotif.** It is not music; it is a sound the world makes. It appears first as data, then in the hull, then in the radio, then in the creatures' throats, then (variant B) in a lab on Earth.
4. **Radio distortion is rationed.** Clean radio (R1) is the default. Degradation (R2/R3) only happens for a reason: interference, damage or distance.
5. **Every voice disappearing is an event.** When a crew channel dies, the radio wheel shows it greyed out permanently.

### A2. Feature 7 (the signature sound)
- **Specification:** a fundamental near 0.4 Hz rhythm (felt, not heard), carried by sub-bass bursts (25–45 Hz) with a slow, irregular, *breathing* cadence: four pulses, a gap, three pulses, a long gap. Over the top: a faint, wavering upper harmonic like a stressed metal plate.
- **Creation:** layered recordings of ice-sheet stress (contact mics on frozen lakes are a known field technique), slowed whale song (licensed or CC0 only), and synthesised sine bursts. Final mix must not resemble any known film creature.
- **Indoors before attacks:** the Listener's hum is Feature 7 pitched up two octaves, which lets players *recognise* the threat by ear.

### A3. Sound categories

| Category | Sounds | Notes |
|---|---|---|
| **Spacecraft** | Ring rotation drone (very low, constant), electrical buzz per room, ventilation, pump cycles, distant structural creaks (thermal), doors (pneumatic + latch), airlock cycle (pressure equalisation hiss, valve clunk), alarms (soft chime → klaxon only in emergencies), UI beeps | Every room gets a unique **room tone** so players feel location by ear |
| **Space / suit** | Close-mic breathing (5 states: calm, exertion, fear, injured, exhausted), suit fans, radio squelch on/off, helmet impacts, glove contact on surfaces | Breathing is driven by gameplay state, not scripts |
| **Europa** | Boot-on-ice contact (crunch through the suit, muffled), ice cracking felt through soles, deep geological rumble, distant impacts (felt), the Keel's grinding (sub-bass) | **No wind.** Avoid any atmospheric "whoosh" outdoors; it would be false. |
| **Creatures** | Listener: crest clicks, pressure breath, Feature 7 hum, palm slaps, hide creak, venting hiss. Needles: rapid ticking in vents, skitter, squeal (indoors only). Keel: sub-bass grind (contact only). Rime: a faint crystalline chime when it flashes | Outdoors, creatures are heard **only by contact** |
| **Characters** | Dialogue, breathing, effort grunts, panic, crying (restrained), laughter (nervous and real) | Recorded per character with consistent mic setup |
| **Earth** | Wind, gulls, ocean, PA, families, birds | Used only in Ch. 1 and Ch. 10, deliberately mirrored |

### A4. Mixing and implementation
- **Audio mixer groups:** Master → Music, Ambience, SFX, Voice (Dialogue, Radio), UI. Snapshots: `Pressurised`, `Vacuum`, `Radio-Only`, `LowHealth`, `Cinematic`.
- **Vacuum snapshot:** low-pass at ~400 Hz on SFX, everything except contact/suit/radio routed to a muted bus.
- **Radio processing:** real-time Unity audio effects (high-pass + low-pass + distortion + a noise layer) driven by a `RadioQuality` value (0–1), so degradation is continuous, not baked.
- **Ducking:** dialogue ducks ambience and music by −8 dB.
- **Subtitles:** always available, with speaker names and `[RADIO]` tags.

### A5. Music direction
| Moment | Music |
|---|---|
| Ch. 1 | A sparse solo piano motif (the "home" theme), very quiet. |
| Ch. 2 | Diegetic only (Luca's music, Mara's player). |
| Ch. 3 Jupiter | Restrained strings and low brass: wonder, not triumph. Cuts dead when Feature 7 starts. |
| Ch. 4–7 | Almost none. Textural drones at most. Fear comes from ambience. |
| Ch. 8 death | **No music during the attack.** One low sustained note only after she dies. |
| Ch. 9 | Silence until launch; then a single, slow, broken version of the home theme. |
| Ch. 10 | The home theme returns, full but quiet. |

### A6. Voice production plan
- **Preferred:** real actors. Sources: university drama societies, voice-acting communities (casting-call sites), friends. Record with a consistent USB condenser mic in a treated space; written release forms for every actor (public repo).
- **Fallback:** a **commercially licensed synthetic-voice service** whose terms permit use in a published game, with original voices only. **No cloning of real people's voices without written permission.**
- **Line count estimate:** ~650 lines total (Sam ~180, Mara ~140, Ashford ~70, Hana ~80, Luca ~80, Grace ~40, family ~30, other ~30).
- **Temp VO:** the developer records scratch lines for timing during development.

---

## Part B — Visual direction

### B1. Style
**Grounded industrial realism.** Equipment looks designed by engineers on a budget, with labels, warning stripes, cable runs, scuffs, tape, handwritten notes and personal photos. Nothing chrome. Nothing glowing without a reason.

### B2. Palette by chapter
| Chapter | Key colour | Mood |
|---|---|---|
| 1 Earth | Warm gold, ocean blue | Life |
| 2 Journey | Warm white practicals, wood-tone personal items | Home away from home |
| 3 Jupiter | Ochre and cream from Jupiter-light | Awe |
| 4 Signal loss | Emergency red, monitor cyan | Wrongness |
| 5 Europa | Hard white sunlight, black shadows, Jupiter-orange fill | Alien beauty |
| 6–8 Base | Cold blue-white work lights; Rime's cyan is the *only* saturated colour | Dread |
| 9 Escape | Jupiter low and huge, long orange shadows | Solitude |
| 10 Home | Golden hour | Relief and grief |

### B3. Space and planets [SCIENCE]
- **Jupiter from Europa:** ~12° across, about **24× the apparent size of the full Moon** from Earth. At Conamara Chaos (~274° W longitude), Jupiter sits **low on the horizon and never moves** (Europa is tidally locked). This is the defining image of the game.
- **Jupiter assets:** banded clouds, the Great Red Spot, slow cloud motion via a scrolling/flow-map shader (cheap). Texture source: public NASA/JPL mission imagery (generally public domain; verify each image's credit line) or CC BY planetary texture packs, credited.
- **Sun at Jupiter's distance (5.2 AU):** appears about one-fifth its size seen from Earth and ~27× dimmer, still blinding against black space. One strong directional light; deep, hard shadows; very little ambient light except Jupiter's reflected glow.
- **Europa's surface:** water ice, reddish-brown streaks along cracks (salts/sulfur compounds, as seen in real imagery), chaos-terrain blocks, ridged plains. **No snow drifts, no wind erosion.**
- **Stars:** visible only when not looking near the sun or Jupiter (exposure-aware skybox).

### B4. Interiors
- **Meridian:** modular ring segments, soft-white practical lights, handrails everywhere, cable trays, storage nets, a galley with personal clutter. Observation deck with a single huge window.
- **Conamara Base:** inflatable/rigid hybrid habitat, insulated walls with quilted panels, frost on windows, cold blue LEDs, emergency amber. The drill chamber is the visual climax: a circular well, gantry, one swinging work lamp.

### B5. Creatures (visual)
See [01-story-bible.md §8](01-story-bible.md#8-creature-biology-and-designs). **Readability rule:** the Listener's crest silhouette must read at 50 m against Jupiter's disc, and at 3 m in darkness when lit only by its own nodes.

### B6. Cinematography rules
- Foreground framing: window frames, helmet edges, door frames.
- Scale shots: always put a human figure against Jupiter or the ice for scale.
- Only **three** third-person shots in the whole game (launch exterior; Mara's death; the final image).
- Camera shake is controlled and short; never during dialogue.
- Close-ups during emotional beats are achieved by **characters stepping close to the first-person camera**, not by cutting away.
