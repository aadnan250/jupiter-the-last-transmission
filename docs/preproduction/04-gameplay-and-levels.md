# 04 — Gameplay Mechanics and Level Design

## 1. Design principles

1. **Tension over shooting.** Most encounters are best solved by avoiding them. Combat is the expensive option.
2. **One rule set, learned through play.** The player learns the world's rules (vacuum silences sound; Rime is an alarm; the Listener reseals; nodes kill it) by experience and through Hana's notes, never through a tutorial pop-up.
3. **Sam is not a soldier.** Weapons feel heavy and imprecise. The game never hands the player a power fantasy.
4. **Never take control away without a reason.** Cutscenes are rare and short; most story beats happen while the player can still look and move.

---

## 2. First-person movement

| Action | Default (KB/M) | Gamepad | Notes |
|---|---|---|---|
| Walk | WASD | Left stick | 2.2 m/s (ship, Earth); suit on Europa 1.6 m/s |
| Run | Shift (hold) | L3 | 4.0 m/s; **loud** (vibration events); limited by stamina/suit O₂ consumption |
| Crouch | Ctrl (toggle) | B / Circle | 1.1 m/s; near-silent |
| Look | Mouse | Right stick | Adjustable sensitivity, invertible |
| Interact | E | X / Square | Contextual prompt only when looking at an interactable |
| Jump | Space | A / Cross | Small hop on ship; **low-gravity arc on Europa** (0.134 g, capped for playability) |
| Radio wheel | Q (hold) | LB | Shows crew channels; how players "call" crew (and later learn they're gone) |
| Helmet zoom | Right mouse (no weapon) | LT | 3× zoom on Europa; reveals distant details |
| Flashlight | F | D-pad up | Helmet light; **visible to forms indoors** |

**Gravity profiles [SCIENCE]:** Earth 1 g · *Meridian* ring 0.6 g (spin gravity, **[FICTION]** design choice to avoid zero-g gameplay) · Europa 0.134 g (real value). Movement code reads gravity from the current `EnvironmentProfile` ScriptableObject.

**Camera feel:** subtle head bob (toggleable for accessibility); breathing-driven idle sway in the suit; controlled shake only for big physical events.

## 3. Exploration and interaction

- **Interactables:** doors, terminals, lockers, documents, audio logs, valves, breaker panels, recorders.
- **Prompt rule:** a single minimalist reticle change and a 1–2 word prompt ("Read", "Open", "Reroute").
- **Logs and documents:** stored in a **Records** tab (text + replayable audio). About 24 collectibles in total, each written to deepen a character or the mystery. Optional but rewarding.
- **Examine mode:** pick up and rotate small objects (Zara's drawing, the music player, Tomas's message chip).

## 4. Survival

| System | Behaviour |
|---|---|
| **Health** | 100 HP; no regeneration. Restored with med-patches (+40). Low-health state: muffled audio, heartbeat, desaturation. |
| **Suit integrity** (Europa only) | Damage can puncture the suit; a puncture drains O₂ until patched with sealant (an item). Patching takes 3 s, which creates tension. |
| **Radiation dose** (Europa surface only) **[SCIENCE]** | Surface dose at Europa is extreme (on the order of several sieverts per day, unshielded). The suit dosimeter rises slowly while outside and acts as a **soft timer**: past the warning threshold, the screen grain increases and Sam's breathing changes. It **never kills by itself** within normal play; it pushes the player onward. |
| **Inventory** | Grid-free, slot-based: 1 primary weapon, 1 tool, 4 consumable slots (med-patch, sealant, flare, decoy). Ammo shown as count. Deliberately small. |
| **Resources** | Scarce by design. Placement is hand-authored, not random, so difficulty can be tuned precisely. |

## 5. Combat

**Weapons (limited, believable) [FICTION, grounded]**

| Weapon | Role | Behaviour |
|---|---|---|
| **Bolt driver** (modified construction tool) | Primary (from Ch. 7) | Single-shot pneumatic bolt gun used for anchoring equipment in ice. Slow reload (2.2 s). 6 bolts per magazine. High stopping power, low ammo. |
| **Mara's flare pistol** | Utility | 3 flares. Blinds/attracts forms indoors (heat + light). Doubles as distraction. |
| **Seismic charges** | Set-piece tool | Explosive sensor charges from the geophysics kit. Used scripted in Ch. 8; limited availability in Ch. 9. |
| **Decoy thumper** | Stealth tool | Throwable vibration emitter; draws the Listener away. |

There are **no assault rifles**. This is a science expedition.

**Creature damage model**
- **Body hits** → stagger + venting, damage to *torpor threshold*. At threshold → torpor (looks dead). In torpor, it recovers after 20–40 s **unless** nodes are ruptured or it is blown apart.
- **Node hits** → high damage; three nodes ruptured = permanent death.
- **Needles** die from one bolt or one melee swipe.
- **The Keel** is invulnerable.

## 6. Stealth

| Mechanic | Rule |
|---|---|
| **Vibration events** | Every noisy action emits an event with a radius (footstep walk 3 m, run 9 m, crouch 0.8 m, gunshot 25 m, dropped object 6 m, decoy 15 m). Outdoors, events propagate **only through the ground** (airborne sound doesn't exist in vacuum); indoors, through both. |
| **Rime** | Rime patches amplify vibration events that occur near them (×2 radius) and **visibly flash cyan** when triggered. Players learn to read and route around them. |
| **Hiding** | Lockers, under-floor service hatches, behind equipment racks. Standing still in darkness = undetectable. |
| **Line of sight** | Forms are nearly blind beyond 4 m; light and heat reveal you at close range (the flashlight matters). |
| **Distraction** | Throw objects (cans, tools) or decoys; flares indoors. |

## 7. Puzzles (system-based, short)

| Puzzle | Chapter | Mechanic |
|---|---|---|
| Launch checklist | 1 | Flip switches in callout order (teaches interaction). |
| Music player repair | 2 | Replace a part from the workshop drawer (teaches examine/inventory). Optional. |
| Comms restore | 4 | Read power readouts → identify the faulty relay (1 of 3) → reroute power → align antenna with Mara's radio guidance. |
| Seismic deployment | 5 | Place three sensors at marked crack points; confirm signal. |
| Ashford's terminal | 6 | Code found through an earlier Luca joke ("his birthday, backwards, like everything else he does"). Players who listened know it. |
| Pressure doors | 7 | Restore local power to a door while forms approach. |
| Ascent vehicle launch | 9 | Enter codes and run a solo pre-flight (callouts now read by Sam alone). |

## 8. Story choices (limited by design)

| Choice | Chapter | Effect |
|---|---|---|
| Music player: leave it or not | 2 | One line in Ch. 9 changes (she kept it / Sam finds it in the workshop). |
| Show Tomas's message | 6 | Dialogue variant (§2.4 in screenplay); Mara's trust line in Ch. 8 differs. |
| Vent or keep the sample | 9 | Final transmission line and the final image differ. |
| Closing line of the transmission | 9 | One of three spoken lines. |

**No branching structure.** Every playthrough has the same levels and the same deaths. Choices change *meaning*, not *plot*. This keeps the project finishable.

---

## 9. Level design

### 9.1 Level list and scene plan (additive loading)

| Scene | Chapter(s) | Size | Key spaces | Notes |
|---|---|---|---|---|
| `L01_CaboLumen` | 1 | Small interior + gantry | Crew quarters, family lounge, suit room, gantry, cockpit | Exterior launch is a **separate cinematic scene** with a low-poly distant pad |
| `L02_Meridian` | 2, 3, 4, 9 (end) | Medium interior, reused | Habitat ring (galley, quarters, med bay, workshop, command), observation deck, science bay, comms deck, spine corridor, cargo hold | **Reused across four chapters** with lighting/state variants. Largest cost saving in the project. |
| `L03_EuropaSurface` | 5, 6, 9 | Medium exterior (~600 × 600 m playable, skybox beyond) | Landing zone, sensor crack, ridges, *Tiamat* site, base exterior, ascent pad | Chaos-terrain ice blocks are modular. Jupiter is a skybox element. |
| `L04_ConamaraBase` | 5–8 | Medium interior | Airlock, habitat corridors, med bay, comms, Ashford's office, service tunnels, **drill chamber** | Core horror space. Built from a modular kit. |
| `L05_Home` | 10 | Small exterior | Recovery ship deck, beach | Simple; lighting does the work. |

### 9.2 Layout principles
- **Loops, not lines** in Conamara Base: the player must be able to circle and lose a pursuer.
- **Safe rooms** with lockable doors and clear audio cues (the hum fades) where the player can breathe.
- **Sightline control** outdoors: ridges and ice blocks hide the horizon so distant reveals are authored.
- **Landmarking:** Jupiter is a fixed compass on Europa **[SCIENCE: tidally locked, so it never moves in Europa's sky]**. The base is "Jupiter-left", the *Tiamat* site "Jupiter-behind".

### 9.3 Conamara Base (core space) flow
```
[Airlock] → [Ready room] → [Corridor A] ─┬─ [Med bay (Hana)] ─ [Lab]
                                          ├─ [Comms] ─ [Ashford's office]
                                          └─ [Corridor B] → [Service tunnels] ─┐
                                                       ↑ loop ↓                 │
                                          [Galley/quarters] ←───────────────────┘
                                                       ↓
                                               [Drill chamber] → [Ascent tunnel] → [Surface]
```

### 9.4 The vertical slice (build first)
**Chapters 6 → 7 in Conamara Base (~12 min).** It contains every core system: interaction, documents, radio, a choice, Rime stealth, the first Listener encounter, the bolt driver, health/inventory, a radio death (Luca), and a safe room. If this slice is frightening and works, the rest of the game is mostly content.
