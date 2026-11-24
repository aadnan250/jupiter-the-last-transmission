# Characters

The crew are realistic, rigged human characters built from **MakeHuman** assets, which are **CC0** (public domain):
"All core assets are shared under Creative Commons, CC0" ([licence page](https://static.makehumancommunity.org/about/license.html)).

| | |
|---|---|
| ![Mara](images/crew-mara.png) | ![Ashford](images/crew-ashford.png) |
| *Mara Lindqvist, flight engineer* | *Commander Daniel Ashford* |
| ![Luca](images/crew-luca.png) | ![Hana](images/crew-hana.png) |
| *Luca Ferreira, pilot* | *Dr. Hana Mori, astrobiologist* |

## Pipeline

1. **`Tools/CharacterPipeline/make_crew.py`** runs headless in Blender 4.5 LTS with the MPFB 2 extension
   (MakeHuman Plugin For Blender) and the CC0 `makehuman_system_assets` pack. Each character is a data sheet:
   gender, age (converted to MakeHuman's age scale), muscle, weight, height, ethnicity mix, skin, hair, eyebrows and clothes,
   all taken from the story bible. MPFB builds the human, fits the clothes, rigs it with MakeHuman's 163-bone "default"
   skeleton (it includes **jaw**, **eye** and **eyelid** bones), and swaps the body to the low-poly game proxy.
2. The script exports FBX plus textures, and a `materials.json` manifest listing each material's colour texture.
   MPFB's node materials do not survive FBX, so the manifest is how Unity rebuilds them.
3. **`CharacterMaterialBuilder`** creates URP materials from the manifest: alpha-tested, double-sided hair, eyebrows and
   eyelashes; soft skin and fabric; per-character hair colour. It then remaps the FBX onto them.
4. **`CharacterModelImporter`** places each model on its crew member, bakes a relaxed arm pose, and adds:
   - **`LipSync`**: opens the jaw from the loudness of the character's voice (RMS of the audio output).
   - **`CharacterIdleMotion`**: breathing, weight shift, blinking, and turning the head to look at the player.

Rotation axes for the jaw, eyelids and arms are derived from the skeleton itself (`BoneAxes`), so the code does not depend on
how the exporter oriented the bones.

Rebuild: `blender --background --python Tools/CharacterPipeline/make_crew.py -- Assets/_Project/Art/Characters`,
then **Jupiter → Rebuild All Generated Content** in Unity.

## Licences

| Item | Licence | Shipped? |
|---|---|---|
| MakeHuman base mesh, skins, hair, clothes, eyes, proxies | CC0 | Yes (exported models and textures) |
| Blender | GPL | No, build tool only |
| MPFB 2 | GPL v3+ | No, build tool only |

## Known limitations

- Expressions are limited to jaw movement and blinking. No full facial animation or phoneme visemes yet.
- No body animation clips yet. Motion is procedural (breathing, sway, head look, turning to face the player).
- Clothing choice is limited to the CC0 system pack, so outfits are civilian rather than flight suits.
