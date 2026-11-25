# Project Rules: JUPITER: THE LAST TRANSMISSION

## Engine
- Unity **6000.6.10f1** (Unity 6), **URP** (Forward+), **Input System** 1.20.1, AI Navigation 2.0.17, Test Framework 1.8.0.
- Target: Windows PC on Intel Iris Xe. Keep to the performance budget (no extra shadow lights, 60 fps at 720p).

## Folder structure
- `Assets/_Project/Scripts/`: Core, Player, Narrative, Creatures, Combat, World, Audio, UI, Editor (editor tools)
- `Assets/_Project/Tests/`: EditMode (pure logic) and PlayMode (scene) tests
- `Assets/_Project/Narrative/Dialogue/`: dialogue scripts (.txt, format in docs/DIALOGUE_FORMAT.md)
- `docs/`: design and technical documents. `Tools/`: Blender and voice pipelines (outside Unity)

## C# conventions
- Namespaces `Jupiter.<Area>`; one public type per file; file name = type name.
- PascalCase types/methods/properties; camelCase fields; `[SerializeField] private` fields, never public fields.
- Logic that can be pure C# lives in plain classes with EditMode tests; MonoBehaviours stay thin.
- Use events over polling; no new singletons (only `GameSession.Current`).
- `///` summary on every public type; comments explain *why*, not *what*.
- Levels, prefabs and generated assets are built by editor scripts (`Jupiter → Rebuild All Generated Content`), not by hand.
- Every change keeps all tests passing.

## File-reading rules (strict)
- NEVER read .unity, .prefab, .asset, or .meta files.
- NEVER search the Library/, Temp/, or Obj/ folders.
- Only read and write .cs files inside Assets/_Project/Scripts/ (and their tests in Assets/_Project/Tests/).
- Whenever you write a script, tell me which GameObjects and components I need to wire up manually in the Unity Inspector.
