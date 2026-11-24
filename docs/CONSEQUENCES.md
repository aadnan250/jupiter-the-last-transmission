# Consequence Ledger

Every reply in the game changes the story state: trust with a crew member (−5 to +5) or a story flag.
Significant replies (★) play the consequence sting: a low impact and dissonant whispering tones, a dark pulse on screen,
a slow rumble, and on PlayStation controllers the light bar bleeding red. The game never tells the player *what* changed.

This ledger tracks what each decision sets and where it pays off. "Planned" payoffs are designed but belong to chapters not built yet.

## Chapter 2: Month 4, aboard the Meridian

### Galley: Luca and Hana (`ch02_galley_luca_hana`)

| Reply | Effect | Payoff |
|---|---|---|
| "I'm not fighting the person who does my blood tests." | Hana +1 | Trust |
| "It's better than whatever Mara plays…" | Luca +1, Mara −1, `teased_mara_music` | Mara overheard on the radio; she opens her workshop conversation with it |
| "Hana's right. It's elevator music." | Luca −1, Hana +1 | Trust |
| "Play me something. I'll decide." | Luca +2, `heard_luca_song` | Planned: the song returns in Chapter 9 |
| "The radar schedule keeps me up." | Hana +1, `told_hana_radar` | Planned: Hana raises the schedule with Ashford |
| "I'm fine. Really." | Hana −1 | Trust |
| "I keep dreaming about the ice." | Hana +1, `told_hana_dream` | Planned: Hana's notebook (Ch. 9) mentions Sam's dream |
| ★ "Ashford wants deep radar returns every orbit…" | Hana +1, Ashford −1, `hana_suspicious` | Ashford mentions Hana's questions; Hana's radio call on the observation deck; planned: Hana starts her notebook early |

### Workshop: Mara (`ch02_workshop_mara`)

| Reply | Effect | Payoff |
|---|---|---|
| "Need a hand?" | Mara +1 | Trust |
| "Take it up with Ashford." | `blamed_ashford` | Planned: Ch. 7 confrontation line |
| "The radar matters more than the lander." | Mara −2, Ashford +1, `sided_with_radar` | Ashford thanks Sam in command |
| "(Look at the music player.)" | `noticed_music_player` | Opens the music-player branch |
| "I could fix it." | Mara +1, `offered_fix_player` | Mara's radio call on the observation deck; planned: the player is clipped to her seat in Ch. 9 |
| "Sorry. I'll leave it." | Mara +1 | Trust |
| "You kept his music player?" | `asked_about_player` | Mara explains Tomas gave it to her |
| "…You just don't want to fix it." | Mara −1, `pushed_mara` | Trust |
| ★ "He asked me not to tell you why they dropped him." | Mara +2, `told_mara_truth_early` | Mara thanks Sam over the radio; planned: changes the Chapter 6 Tomas scene |
| "I know. I'm sorry." | Mara +1 | Trust |
| "I didn't ask for his seat." | Mara −1 | Trust |
| ★ "(Say nothing. Keep the promise.)" | Mara −1, `kept_tomas_promise` | Planned: Chapter 6, Mara finds Tomas's message herself |

### Command: Ashford (`ch02_command_ashford`)

| Reply | Effect | Payoff |
|---|---|---|
| "Why do we need returns from that deep?" | `asked_why_deep` | Planned: Ashford's confession references it |
| "Mara needs those power hours." | Mara +1, Ashford −1, `argued_for_mara` | Trust |
| "Happy to help, Commander." | Ashford +1, `loyal_to_ashford` | Planned: Ashford's confession is warmer |
| ★ "What is Command Directive 7?" *(only after reading the radar calibration summary)* | Ashford −2, `asked_directive7` | Opens the Directive 7 branch |
| "It's on the calibration summary." | Ashford +1 | Trust |
| "Doesn't matter. What is it?" | Ashford −1 | Trust |
| "Forget I asked." | Ashford +1, `dropped_directive7` | Trust |
| ★ "If there's something the crew should know, tell us now." | Ashford −1, Mara +1, `demanded_truth` | Planned: Ashford keeps his word: his Chapter 7 confession opens with "I promised you'd hear it from me" |

### Observation deck radio (`ch02_observation_radio`)

Plays once all three crew conversations are done. **Who calls depends on earlier choices:** Mara if you told her the truth
(or offered to fix the music player), Hana if you shared your suspicion, otherwise Luca.
