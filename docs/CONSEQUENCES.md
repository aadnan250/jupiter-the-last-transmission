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
| ★ "He asked me not to tell you why they dropped him." | Mara +2, `told_mara_truth_early` | Mara thanks Sam over the radio; Chapter 6: she already knows, and asks to hear Tomas's message anyway |
| "I know. I'm sorry." | Mara +1 | Trust |
| "I didn't ask for his seat." | Mara −1 | Trust |
| ★ "(Say nothing. Keep the promise.)" | Mara −1, `kept_tomas_promise` | Chapter 6: the same choice comes back at the mail cache, with Tomas's own words |

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

## Chapter 6: The Discovery, Conamara Base

### Mail cache: Tomas's message (`ch06_mail_cache`)

| Reply | Effect | Payoff |
|---|---|---|
| "(Put on the headset and play it.)" | `played_tomas_privately` | Trust |
| "Do you want to hear it first?" | Mara +1 | Trust |
| ★ "(Turn the screen toward her.)" | Mara +2, `showed_mara_message` | Mara softens: "Idiot. Both of you."; planned: her Chapter 8 trust line |
| ★ "(Close it. It wasn't yours to hear.)" | `kept_tomas_message` | Mara finds it herself and opens the sealed-orders conversation with it |
| "Just an old friend wishing me luck." | Mara −2, `kept_tomas_message`, `lied_to_mara` | As above, and she quotes the lie back |
| *If told on the ship:* "He'd have wanted you to hear it." | Mara +1 | Trust |
| *If told on the ship:* ★ "I should have told you the day I got his seat." | Mara +1, `apologised_to_mara` | Planned: Chapter 8 |

### Luca's radio check-in (`ch06_luca_radio`)

No replies. Luca's joke ("his birthday, backwards") plus the crew medical summary in the med bay (DOB 17 04) give Ashford's
terminal code, **4071**. If Sam asked Ashford about Directive 7 on the ship (`asked_directive7`), Luca mentions it.

### Sealed orders: Mara (`ch06_sealed_orders_mara`)

Offered once Sam has read Directive 7 on Ashford's terminal (`read_sealed_orders`). Opens differently if Sam kept the message.
Mentions Tiamat's sealed borehole if Sam read the lab log (`read_tiamat_log`), and Sam's question on the ship if he asked (`demanded_truth`).

| Reply | Effect | Payoff |
|---|---|---|
| *If kept:* "It wasn't mine to break." | Mara +1, `mara_found_message` | Trust |
| *If kept:* "I'm sorry. I should have shown you." | `mara_found_message`, `apologised_to_mara` | Planned: Chapter 8 |
| ★ "We go down there and confront him. Now." | Mara +1, `confront_ashford` | Planned: Chapter 7 confrontation opens on the attack |
| ★ "We get Hana out first. Quietly." | Hana +1, `warn_hana_first` | Planned: Chapter 7, Sam reaches Hana before the argument |
| "Maybe there's a reason he couldn't tell us." | Mara −1, Ashford +1, `defended_ashford` | Planned: Ashford's confession is warmer |
| "Luca should know. Call the pad." | Luca +1, `wanted_to_tell_luca` | Planned: Luca's last transmission |

## Chapter 7: Contact, Conamara Base

### The confrontation (`ch07_confrontation`)

Plays on walking into the drill chamber after the sealed orders. Opens on the attack if Sam chose to confront Ashford
(`confront_ashford`), with Sam calling Hana away if he chose to warn her (`warn_hana_first`), and otherwise with Ashford's
greeting. Ashford keeps his promise line if Sam demanded the truth on the ship (`demanded_truth`); Mara needles him if Sam
defended him (`defended_ashford`). Every branch ends with the drill seizing (`drill_seized`), which starts the eruption.

| Reply | Effect | Payoff |
|---|---|---|
| ★ "Then why is Hana collecting a sample to take home?" | Ashford −1, Hana +1, `accused_ashford` | Planned: Ashford's confession |
| ★ "Hana, stop the drill. Now." | Hana +1, `ordered_drill_stop` | Planned: Hana's notebook (Ch. 9) |
| "Commander, we deserved to know." | Ashford +1 | Ashford apologises |
| "(Look at the bore.)" | `noticed_bore_frost` | Mara remembers it in the safe room |

### The safe room (`ch07_safe_room`)

Plays on reaching the safe room after the blackout. Mara mentions the frost if Sam saw it, and the door if it has given way.

| Reply | Effect | Payoff |
|---|---|---|
| ★ "We go back for Ashford." | Ashford +1, `go_back_for_ashford` | Planned: Ashford's confession |
| ★ "Ashford chose that door. We get to the pad." | Mara +1, `leave_ashford` | Planned: Ashford's confession |
| "(Sit down next to her. Say nothing.)" | Mara +1, `comforted_mara` | Planned: Chapter 8 tunnels conversation |
| "I hit it. It didn't matter." | `knows_bolts_fail` | Mara points to Hana's research (the node weakness, Ch. 9) |

Radio on the move (no replies): `ch07_blackout_radio` (Mara, as the lights fail) and `ch07_door_radio` (Ashford, as the
pressure door shuts).
