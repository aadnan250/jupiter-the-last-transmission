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
| Flare pistol shot | 10 m | Air |
| Thrown object landing | 2.4–6 m (scaled by impact speed) | Ground |
| Decoy thumper (every 1.5 s for 8 s) | 15 m | Ground |
| Climbing into / out of a locker | 1.5 m | Ground |

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
- **Finishing it:** shots into it while it is down (120 damage, `CreatureHealthModel.FinishDamage`) kill it for good. A burst node stops catching shots, so bullets reach the body behind it.
- It only truly dies if **3 of its 6 pressure nodes** are ruptured (the glowing cyan spheres take heavy damage), or if it takes
  explosive damage while in torpor.

## 4. Behaviour (`Creatures/CreatureAI.cs`)

A thin MonoBehaviour state machine drives a `NavMeshAgent` using the two models above. The level builder bakes a NavMesh for
Conamara Base. Because the decision logic lives in plain C# classes, it is covered by EditMode unit tests (suspicion thresholds,
vacuum attenuation, torpor and recovery, node and explosive kills), with PlayMode tests for the integrated behaviour
(a gunshot draws the Listener out of Dormant; body damage → torpor → recovery; three node hits → dead).

## 5. Hiding, distractions and flares (Slice 7)

All the numbers live in `Creatures/StealthRules.cs` (pure C#, EditMode tested).

- **Lockers** (`World/HidingSpot`, `Player/PlayerConcealment`): inside, Sam cannot move, look or fire, and the creature
  **cannot see him at all**, so it cannot attack either. It can still *hear* him: climbing in or out is a 1.5 m noise.
  If it was hunting him, it goes to where it last sensed him and waits there until its suspicion decays.
  A vent slit at eye height lets the player watch it.
- **Throwables** (`Combat/Throwable`, `Combat/PlayerThrower`): one object in hand at a time. A landing is a floor
  vibration whose radius grows with impact speed, so the creature investigates the landing spot, not the thrower.
  A **decoy thumper** pulses 15 m through the floor after it lands, then is spent.
- **Flares** (`Combat/FlarePistol`, `Combat/BurningFlare`): a burning flare within 14 m **lures** the creature straight
  to it (heat and light are easy to place, so there is no position error). Within 2.5 m it is **Blinded** for 5 s: it
  rears back and forgets everything, including where the player was. Each flare can dazzle it only once.

```
any (not torpor) ──flare within 14 m──▶ Investigating (exact point) ──within 2.5 m──▶ Blinded ──5 s──▶ Listening
```

## 6. Scripted stimuli (Chapter 7)

Story beats talk to the creature through the same senses as everything else. `CreatureAI.Alert(position, intensity)`
adds a perceived stimulus (used by the bore eruption and by Ashford's pressure door giving way), so the creature still
investigates or hunts through its normal states. The drill-chamber pressure door carries a carving `NavMeshObstacle`: while
it is shut, the creature genuinely cannot path out.

## 7. Stalkers and the landing party (Europa surface)

- **Stalkers** (`Creatures/StalkerAI`, `Creatures/StalkerRig`): the surface forms that wait under the ice. The
  `AmbushDirector` brings them up one after another in a ring around the squad (`AmbushTimeline`, golden-angle spacing so
  none come up together). Each runs down the nearest person (Sam counts as a little nearer: he is the one they want),
  slashes in reach, flinches when hit and dies to sustained beam fire. `StalkerRig` animates the Blender model's named
  bones procedurally: hunched crouch with sudden head tilts, a low four-limbed run, a two-armed slash, an emergence and a collapse.
- **The landing party** (`Combat/SquadMember`, `Combat/SuitAnimator`): Ashford, Mara and Hana hold formation slots around
  Sam and walk with him with a low-gravity lope. When a Stalker is up and within 32 m they stop, shoulder their rifles and
  fire short beam bursts at the nearest one. They can be knocked about but do not die here: the story still needs them.

## Known limitations

- The Listener is a primitive-shape placeholder; its final model and animations come later.
- Needles (the small vent creatures) and the Keel are not implemented yet.
- It never checks lockers: a hidden player is safe from sight until they make noise. Opening lockers is a candidate for the playtest pass.
- Chapter 6–7 story scripting (dialogue, Luca's radio death, the Directive 7 reveal) in the base is still to come.
