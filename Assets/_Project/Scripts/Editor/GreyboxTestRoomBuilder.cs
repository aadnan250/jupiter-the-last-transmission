using Jupiter.Player;
using Jupiter.UI;
using Jupiter.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Generates the Phase 1 grey-box test room and the three environment profiles.
    /// Menu: Jupiter > Build Greybox Test Room. Also runs in batch mode:
    /// Unity -batchmode -executeMethod Jupiter.EditorTools.GreyboxTestRoomBuilder.Build
    /// Re-running it regenerates the scene, so hand edits to this scene will be lost.
    /// </summary>
    public static class GreyboxTestRoomBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Sandbox/GreyboxTestRoom.unity";
        const string ProfileFolder = "Assets/_Project/Data/Environments";
        const string MaterialFolder = "Assets/_Project/Art/Materials/Greybox";
        const string InputAssetPath = "Assets/_Project/Settings/Input/JupiterControls.inputactions";

        [MenuItem("Jupiter/Build Greybox Test Room")]
        public static void Build()
        {
            EnsureFolder(ProfileFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder("Assets/_Project/Scenes/Sandbox");

            // --- Environment profiles (values from the design doc) ---
            var earth = Profile("Earth", 9.81f, 2.2f, 4.0f, 1.1f, 0.45f, 18f, 0.35f, false);
            Profile("MeridianRing", 5.886f, 2.2f, 4.0f, 1.1f, 0.40f, 16f, 0.35f, false); // 0.6 g spin gravity
            var europa = Profile("Europa", 1.315f, 1.6f, 2.8f, 0.9f, 0.55f, 6f, 0.12f, true); // 0.134 g

            // --- Materials ---
            Material floor = Mat("GB_Floor", new Color(0.42f, 0.42f, 0.44f));
            Material wall = Mat("GB_Wall", new Color(0.62f, 0.62f, 0.64f));
            Material accent = Mat("GB_Accent", new Color(0.95f, 0.55f, 0.15f));
            Material europaMat = Mat("GB_EuropaZone", new Color(0.65f, 0.8f, 0.95f));
            Material doorMat = Mat("GB_Door", new Color(0.25f, 0.45f, 0.65f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Lighting ---
            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var geo = new GameObject("Geometry").transform;

            // Main hall 24 x 24, walls 4 m high
            Box(geo, "Floor", new Vector3(0, -0.25f, 0), new Vector3(24, 0.5f, 24), floor);
            Box(geo, "Wall_North", new Vector3(0, 2, 12.25f), new Vector3(24.5f, 4, 0.5f), wall);
            Box(geo, "Wall_East", new Vector3(12.25f, 2, 0), new Vector3(0.5f, 4, 24), wall);
            Box(geo, "Wall_West", new Vector3(-12.25f, 2, 0), new Vector3(0.5f, 4, 24), wall);
            // South wall with a doorway (gap 2 m wide, 2.4 m tall) leading to the Europa annex
            Box(geo, "Wall_South_L", new Vector3(-6.5f, 2, -12.25f), new Vector3(11, 4, 0.5f), wall);
            Box(geo, "Wall_South_R", new Vector3(6.5f, 2, -12.25f), new Vector3(11, 4, 0.5f), wall);
            Box(geo, "Wall_South_Lintel", new Vector3(0, 3.2f, -12.25f), new Vector3(2, 1.6f, 0.5f), wall);

            // Ramp (15°) up to a ledge
            var ramp = Box(geo, "Ramp", new Vector3(-8, 0.9f, 4), new Vector3(3, 0.3f, 7), accent);
            ramp.transform.rotation = Quaternion.Euler(-15f, 0, 0);
            Box(geo, "Ledge", new Vector3(-8, 1.75f, 9.5f), new Vector3(7, 0.3f, 4.5f), floor);

            // Stairs: 8 steps of 0.2 m (CharacterController step offset is 0.3 m)
            for (int i = 0; i < 8; i++)
            {
                float h = 0.2f * (i + 1);
                Box(geo, $"Step_{i + 1}", new Vector3(6, h * 0.5f, -2 + i * 0.4f), new Vector3(3, h, 0.4f), floor);
            }
            Box(geo, "Stairs_Top", new Vector3(6, 0.8f, 2.0f), new Vector3(3, 1.6f, 2), floor);

            // Crouch tunnel: 1.3 m clearance (standing height is 1.8 m)
            Box(geo, "Tunnel_Roof", new Vector3(0, 1.55f, 7), new Vector3(3, 0.5f, 5), wall);
            Box(geo, "Tunnel_Side_L", new Vector3(-1.75f, 0.9f, 7), new Vector3(0.5f, 1.8f, 5), wall);
            Box(geo, "Tunnel_Side_R", new Vector3(1.75f, 0.9f, 7), new Vector3(0.5f, 1.8f, 5), wall);

            // Jump platforms (Earth jump apex 0.45 m)
            Box(geo, "Jump_1", new Vector3(8, 0.2f, -8), new Vector3(1.5f, 0.4f, 1.5f), accent);
            Box(geo, "Jump_2", new Vector3(8, 0.35f, -5.5f), new Vector3(1.5f, 0.7f, 1.5f), accent);

            // --- Interactables ---
            // Sliding door in the south doorway
            var doorRoot = new GameObject("SlidingDoor");
            doorRoot.transform.position = new Vector3(0, 0, -12.25f);
            var panel = Box(doorRoot.transform, "Panel", Vector3.zero, new Vector3(2f, 2.4f, 0.2f), doorMat);
            panel.transform.localPosition = new Vector3(0, 1.2f, 0);
            panel.isStatic = false;
            var door = doorRoot.AddComponent<SlidingDoor>();
            SetField(door, "panel", panel.transform);
            SetField(door, "openOffset", new Vector3(2.1f, 0f, 0f));

            // Ceiling lamp + wall switch
            var lamp = new GameObject("Lamp").AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.range = 10f;
            lamp.intensity = 3f;
            lamp.color = new Color(1f, 0.92f, 0.8f);
            lamp.transform.position = new Vector3(-4, 3.5f, -4);
            var sw = Box(geo, "LightSwitch", new Vector3(-12f, 1.3f, -4), new Vector3(0.1f, 0.3f, 0.2f), accent);
            sw.isStatic = false;
            var lightSwitch = sw.AddComponent<LightSwitch>();
            SetField(lightSwitch, "lights", new[] { lamp });

            // --- Europa annex: low-gravity zone behind the door ---
            Box(geo, "Annex_Floor", new Vector3(0, -0.25f, -24.5f), new Vector3(16, 0.5f, 24), europaMat);
            Box(geo, "Annex_Wall_E", new Vector3(8.25f, 4, -24.5f), new Vector3(0.5f, 8, 24), wall);
            Box(geo, "Annex_Wall_W", new Vector3(-8.25f, 4, -24.5f), new Vector3(0.5f, 8, 24), wall);
            Box(geo, "Annex_Wall_S", new Vector3(0, 4, -36.75f), new Vector3(17, 8, 0.5f), wall);
            Box(geo, "Annex_Block_1", new Vector3(-3, 1f, -22), new Vector3(2, 2, 2), europaMat);
            Box(geo, "Annex_Block_2", new Vector3(3, 1.75f, -28), new Vector3(2, 3.5f, 2), europaMat);
            var zone = new GameObject("EuropaZone");
            zone.transform.position = new Vector3(0, 4, -25);
            var zoneCol = zone.AddComponent<BoxCollider>();
            zoneCol.isTrigger = true;
            zoneCol.size = new Vector3(16, 8, 23);
            var envZone = zone.AddComponent<EnvironmentZone>();
            SetField(envZone, "profile", europa);

            // --- Player ---
            var player = BuildPlayer(earth, out var interaction);
            player.transform.SetPositionAndRotation(new Vector3(0, 0.05f, -6), Quaternion.Euler(0, 180f, 0)); // facing the door

            // --- HUD ---
            BuildHud(interaction);

            // Environment lighting suited to an interior grey-box
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.48f, 0.52f);
            RenderSettings.ambientEquatorColor = new Color(0.3f, 0.3f, 0.32f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.15f, 0.16f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[GreyboxTestRoomBuilder] Saved {ScenePath}");
        }

        static GameObject BuildPlayer(EnvironmentProfile startProfile, out PlayerInteraction interaction)
        {
            var player = new GameObject("Player");
            player.tag = "Player";

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 45f;
            cc.skinWidth = 0.03f;
            // Default 0.001 silently drops tiny moves at high frame rates, which breaks isGrounded.
            cc.minMoveDistance = 0f;

            var input = player.AddComponent<InputReader>();
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (inputAsset == null) Debug.LogError($"Input actions not found at {InputAssetPath}");
            SetField(input, "actions", inputAsset);

            var controller = player.AddComponent<PlayerController>();
            SetField(controller, "input", input);
            SetField(controller, "defaultEnvironment", startProfile);
            player.AddComponent<CursorLock>();

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(player.transform, false);
            pivot.transform.localPosition = new Vector3(0, 1.62f, 0);

            var cam = pivot.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = 70f;
            pivot.AddComponent<AudioListener>();

            var look = pivot.AddComponent<CameraController>();
            SetField(look, "input", input);
            SetField(look, "player", controller);

            interaction = pivot.AddComponent<PlayerInteraction>();
            SetField(interaction, "input", input);
            return player;
        }

        static void BuildHud(PlayerInteraction interaction)
        {
            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var crossGo = new GameObject("Crosshair");
            crossGo.transform.SetParent(canvasGo.transform, false);
            var cross = crossGo.AddComponent<Image>();
            cross.rectTransform.sizeDelta = new Vector2(4, 4);
            cross.raycastTarget = false;

            var promptGo = new GameObject("Prompt");
            promptGo.transform.SetParent(canvasGo.transform, false);
            var prompt = promptGo.AddComponent<Text>();
            prompt.font = font;
            prompt.fontSize = 18;
            prompt.alignment = TextAnchor.UpperCenter;
            prompt.color = new Color(1f, 1f, 1f, 0.9f);
            prompt.raycastTarget = false;
            prompt.rectTransform.anchoredPosition = new Vector2(0, -24);
            prompt.rectTransform.sizeDelta = new Vector2(300, 30);

            var help = new GameObject("ControlsHelp").AddComponent<Text>();
            help.transform.SetParent(canvasGo.transform, false);
            help.font = font;
            help.fontSize = 14;
            help.color = new Color(1f, 1f, 1f, 0.55f);
            help.alignment = TextAnchor.LowerLeft;
            help.raycastTarget = false;
            help.rectTransform.anchorMin = help.rectTransform.anchorMax = help.rectTransform.pivot = Vector2.zero;
            help.rectTransform.anchoredPosition = new Vector2(16, 12);
            help.rectTransform.sizeDelta = new Vector2(900, 24);
            help.text = "WASD move   Shift run   Ctrl/C crouch   Space jump   E interact   Esc free cursor   |   Door leads to the Europa low-gravity zone";

            var ui = canvasGo.AddComponent<InteractionPromptUI>();
            SetField(ui, "interaction", interaction);
            SetField(ui, "promptText", prompt);
            SetField(ui, "crosshair", cross);
        }

        // ---------- helpers ----------

        static EnvironmentProfile Profile(string name, float g, float walk, float run, float crouch, float jump, float accel, float air, bool vacuum)
        {
            string path = $"{ProfileFolder}/{name}.asset";
            var p = AssetDatabase.LoadAssetAtPath<EnvironmentProfile>(path);
            if (p == null)
            {
                p = ScriptableObject.CreateInstance<EnvironmentProfile>();
                AssetDatabase.CreateAsset(p, path);
            }
            p.displayName = name;
            p.gravity = g;
            p.walkSpeed = walk;
            p.runSpeed = run;
            p.crouchSpeed = crouch;
            p.jumpHeight = jump;
            p.groundAcceleration = accel;
            p.airControl = air;
            p.isVacuum = vacuum;
            p.terminalFallSpeed = vacuum ? 12f : 30f;
            EditorUtility.SetDirty(p);
            return p;
        }

        static Material Mat(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            m.color = color;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        /// <summary>Assigns a private [SerializeField] through SerializedObject, exactly as the Inspector would.</summary>
        static void SetField(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"Field '{field}' not found on {target.GetType().Name}");
                return;
            }

            switch (value)
            {
                case Object o: prop.objectReferenceValue = o; break;
                case Vector3 v: prop.vector3Value = v; break;
                case Light[] lights:
                    prop.arraySize = lights.Length;
                    for (int i = 0; i < lights.Length; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = lights[i];
                    break;
                default: Debug.LogError($"Unsupported field type for '{field}'"); break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
