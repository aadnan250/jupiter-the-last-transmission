# Asset Licence Register

Every third-party asset in this repository is listed here with its source and licence.
Only assets whose licence allows redistribution in a public repository are committed
(original work, CC0, CC BY with credit, or public domain).

Unity Asset Store assets **may not be redistributed publicly**. If any are used, they live in
`Assets/ThirdParty/Private/` (git-ignored) and are listed in the second table so contributors know what to import.

## Committed third-party assets

| Asset | Author | Source | Licence | Used in |
|---|---|---|---|---|
| URP pipeline and volume settings (`Assets/Settings/`) | Unity Technologies | Unity 6 URP Blank template | Unity Companion License | Rendering configuration |

## Original generated assets

These are produced by the project's own code and are original work:

| Asset | Generator |
|---|---|
| `Assets/_Project/Audio/Generated/*.wav` (room tones, doors, switch, footsteps) | `ProceduralAudio` + `AudioAssetGenerator` |
| `Assets/_Project/Art/Textures/Generated/jupiter_placeholder.png` | `JupiterTextureGenerator` (placeholder; to be replaced with public-domain mission imagery, credited here) |
| Grey-box materials, scenes and the PlayerRig prefab | Editor builders in `Assets/_Project/Scripts/Editor/` |

All other content is original work by the project author.

## Non-redistributable assets (not in the repository)

None yet.
