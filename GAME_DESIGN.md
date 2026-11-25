# GAME DESIGN: JUPITER: THE LAST TRANSMISSION

A summary of every mechanic, control and feature requested for the game. Full detail lives in
`docs/preproduction/` (story bible, chapters, screenplay, gameplay, audio/visual, technical plan).

## 1. Identity
- **Genre:** first-person survival horror, sci-fi thriller, cinematic narrative adventure, psychological horror.
- **Length:** about 60 minutes. **Platform:** Windows PC (Intel Iris Xe, 16 GB RAM). **Engine:** Unity 6, URP, C#.
- **Year 2183.** The first crewed mission to Europa (Jupiter's moon). You don't land on Jupiter, which has no surface; it fills the sky.
- **Tone:** emotional and peaceful at first, then dread, then terror and grief. Silence and anticipation over jump scares.
- **Inspirations (tone only, nothing copied):** The Last of Us (relationships), Dead Space (enclosed horror), Interstellar (scale), DOOM (creature intensity).

## 2. Story in brief
- **Player:** Dr. Sam Okafor, geophysicist. **Companion:** Mara Lindqvist, who resents Sam at first, saves him, and dies when the creature gets back up.
- **Crew:** Cmdr. Daniel Ashford, Dr. Hana Mori, Luca Ferreira. **Mission control:** Grace Whitfield.
- **Ten chapters:** Goodbye, Earth · The Journey · Jupiter · Signal Loss · Europa · The Discovery · Contact · The Last Crew Member · The Last Transmission · Home.
- **Mystery:** a probe heard something under the ice in 2171 (Feature 7). The mission's secret goal is to bring a living sample home.

## 3. Movement
- Walk, run, crouch (toggle, with a headroom check), jump, mouse or stick look, subtle head bob.
- Gravity by location: Earth 1 g, the ship's spin ring 0.6 g, Europa 0.134 g (the real value).

## 4. Interaction and exploration
- Look at something and press Interact. Prompts show the right button for keyboard, PlayStation or Xbox.
- Readable documents, logs and notes for environmental storytelling. Some unlock hidden dialogue replies.
- Light switches, and **spaceship doors that slide apart automatically** as you approach (Among Us style), with green or red status lights. Locked doors say "Locked".

## 5. Talking to the crew and choices
- Crew stand in the ship and **speak every line** (voiced), with lip sync, blinking and turning to look at you.
- Conversations offer **up to 4 replies**, picked with keys 1–4 or ▲ ● X ■ on PS5 (Y / B / A / X on Xbox).
- **Every reply affects the game:** it changes trust (−5 to +5) with a crew member or sets a story flag that later content reads.
- **Significant decisions** play a **very eerie sound** (reversed breath, deep impact, detuned whispering tones), pulse the screen dark red, rumble the controller and turn the PS5 light bar red.
- Radio calls with a crackly radio filter. Who calls you can depend on earlier choices.

## 6. Survival and combat
- Health 100 with no regeneration; med-patches heal 40. Small inventory (1 weapon, 1 tool, 4 consumables).
- **Bolt driver:** a repurposed construction tool, not a military gun. 6 bolts, slow reload, and every shot is loud.
- On Europa: suit integrity and a radiation dose that acts as a soft timer.

## 7. Creatures and stealth
- **The Listener:** a tall, thin creature with a crest instead of a face. It hunts by **vibration**, not sight.
- Noise radius: crouch 0.8 m, walk 3 m, run 9 m, gunshot 25 m. In vacuum only ground vibrations carry.
- **Hard to kill:** body hits drop it into a fake-dead torpor and it **gets back up** after 20–40 s. Destroying 3 of its 6 glowing nodes kills it.
- **Rime:** living frost that amplifies noise and flashes cyan. Planned: Needles (vent crawlers) and the Keel (huge, under the ice).

## 8. Controls

| Action | Keyboard / Mouse | PS5 | Xbox |
|---|---|---|---|
| Move / Look | WASD / Mouse | Sticks | Sticks |
| Run / Crouch / Jump | Shift / Ctrl or C / Space | L3 / ● / X | LS / B / A |
| Interact / Talk | E | ■ | X |
| Continue dialogue | E / Space / Click | X | A |
| Replies 1–4 | 1–4 | ▲ ● X ■ | Y B A X |
| Fire / Reload / Med-patch | Left click / R / H | R2 / ▲ / D-pad up | RT / Y / D-pad up |
| Save / Load / Performance | F5 / F9 / F3 | autosave | autosave |

## 9. Presentation
- **Ship:** a real spaceship interior with ribbed, panelled corridors, pipes, hazard stripes, light panels and signs. Rooms are full of equipment: bridge (windows to space), science lab, med bay, galley/kitchen, crew quarters, hygiene/bathroom, workshop, comms, airlock and storage.
- **Characters:** realistic humans (MakeHuman, CC0) built per character sheet.
- **Voices:** generated offline with Piper TTS, cast by measured pitch, credited (CC BY 4.0).
- **Audio:** soft footsteps (not metallic), room tones and the Feature 7 pulse. Outside on Europa there's no air, so you hear only your suit, your boots and the radio.
- **Performance:** URP tuned for Iris Xe, with baked lighting in the art pass.

## 10. Portfolio requirements
- Public GitHub repo with a professional README, screenshots, architecture and AI docs, a licence register, CI checks and automated tests.
