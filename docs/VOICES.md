# Voices

Every line of dialogue is spoken. Voices are generated offline with **[Piper](https://github.com/rhasspy/piper)**
(a local neural text-to-speech engine, MIT licence) using the **`en_US-libritts_r-medium`** multi-speaker model.

## Credit (required by the voice data licence)

> Voices synthesised with a Piper model trained on **LibriTTS-R** (Koizumi et al., Google), derived from LibriTTS / LibriSpeech
> public-domain LibriVox recordings. LibriTTS-R is licensed under **[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/)**
> ([openslr.org/141](http://www.openslr.org/141/)). Model: `rhasspy/piper-voices`, `en/en_US/libritts_r/medium`.

## Casting

The model contains 904 speakers. Voices were cast by **measured median pitch** (an autocorrelation pitch tracker run over the
same test sentence for 120 speakers), so every character is clearly distinct:

| Character | Speaker id | Median pitch | Pace | Direction |
|---|---|---|---|---|
| Ashford | 50 | ~93 Hz | 1.08 (slower) | Deepest voice: calm, paternal |
| Sam (player) | 34 | ~122 Hz | 1.00 | Soft, measured |
| Luca | 71 | ~140 Hz | 0.92 (quicker) | Bright, energetic |
| Tomas | 113 | ~100 Hz | 1.10 | Tired, kind |
| Mara | 2 | ~176 Hz | 1.00 | Low female, clipped |
| Hana | 112 | ~199 Hz | 1.02 | Gentle |
| Grace (CAPCOM) | 45 | ~204 Hz | 1.00 | Professional |
| Ada | 77 | ~200 Hz | 1.00 | Warm |
| Zara | 54 | ~259 Hz | 0.95 | Highest voice |

## Pipeline

1. `VoiceText.Clean` turns a subtitle into speakable text: stage directions like `(laughing)` are removed and `(beat)` becomes a pause.
2. `VoiceGenerator` (menu **Jupiter → Generate Voices**) collects every spoken line, including Sam's replies, and runs Piper once per character.
   It then trims silence, normalises loudness and writes `Resources/Voice/<conversation>/<conversation>_L<line>.wav`.
3. At runtime `VoiceLibrary` finds the clip by conversation and script line number. `DialogueManager` plays it from the speaking
   crew member's own 3D audio source (which drives their **lip sync**), from the player for Sam, or through a band-pass and
   distortion **radio filter** for radio lines.
4. A content test fails if any spoken line is missing its clip.

**Replacing with real actors:** record a line, save it with the same file name, and it plays instead. No code changes are needed.

## Tools (not in the repo)

Piper and the voice model are build tools kept in `../_tools` next to the project, or in the folder set by the `JUPITER_TOOLS`
environment variable. Generated clips *are* committed, so the game runs without them.
