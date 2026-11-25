# Creature AI: hearing, suspicion and torpor

The Listener hunts by **vibration**, not sight. That one rule drives the stealth, the combat and the science of the setting:
on Europa's airless surface, sound cannot travel through air, only through the ground.

![The Listener (placeholder)](images/phase4-listener.png)

## 1. Vibration events (`Creatures/Vibration.cs`)

Every noisy action emits a `VibrationEvent { position, radius, medium (Air / Ground), inVacuum, source }`.

| Action | Radius | Medium |
|---|---|---|
| Crouch step | 0.8 m | Ground |
| Walk step | 3 m | Ground |
| Run step | 9 m | Ground |
| Landing | 5 m | Ground |
| Bolt driver shot | 25 m | Air + Ground |

**Vacuum rule:** if the source or the listener is in a vacuum environment, airborne events are dropped; ground events still travel.
**Rime patches** (`RimePatch`) double the radius of events near them and flash cyan. They are the colony's "ears", and the player learns to route around them.

## 2. Suspicion (`Creatures/SuspicionModel.cs`, pure C#)

Perceived intensity = loudness falloff with distance × medium factor × Rime amplifier.
Suspicion rises with intensity and decays over time; thresholds move the creature between states:

```
Dormant ──hear──▶ Listening (kneels, palms on the floor)
Listening ──more──▶ Investigating (walks to the estimated source; error grows with distance)
Investigating ──loud / seen within 4 m──▶ Hunting ──in reach──▶ Attacking
any ──quiet for long enough──▶ back down the chain
```

Sight only works at short range (it is nearly blind), so standing still in the dark is the safest thing a player can do.

## 3. Health, torpor and nodes (`Creatures/CreatureHealthModel.cs`, pure C#)

- **Body hits** wear it down to a threshold, then it collapses into **torpor**, which looks like death.
- After **20–40 s** of torpor it **recovers** with partial health. This is why it "gets back up" in Chapter 8.
- It only truly dies if **3 of its 6 pressure nodes** are ruptured (the glowing cyan spheres take heavy damage), or if it takes
  explosive damage while in torpor.

## 4. Behaviour (`Creatures/CreatureAI.cs`)

A thin MonoBehaviour state machine drives a `NavMeshAgent` using the two models above. The level builder bakes a NavMesh for
Conamara Base. Because the decision logic lives in plain C# classes, it is covered by EditMode unit tests (suspicion thresholds,
vacuum attenuation, torpor and recovery, node and explosive kills), with PlayMode tests for the integrated behaviour
(a gunshot draws the Listener out of Dormant; body damage → torpor → recovery; three node hits → dead).

## Known limitations (Phase 4a)

- The Listener is a primitive-shape placeholder; its final model and animations come later.
- Needles (the small vent creatures) and the Keel are not implemented yet.
- Chapter 6–7 story scripting (dialogue, Luca's radio death, the Directive 7 reveal) in the base is the next step of the vertical slice.
