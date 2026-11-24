using Jupiter.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Jupiter.EditorTools.GreyboxKit;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Generates the Phase 1 grey-box movement test room and the three environment profiles.
    /// Menu: Jupiter > Build Greybox Test Room. Re-running regenerates the scene (hand edits are lost).
    /// </summary>
    public static class GreyboxTestRoomBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Sandbox/GreyboxTestRoom.unity";
        const string ProfileFolder = "Assets/_Project/Data/Environments";

        public static EnvironmentProfile Earth { get; private set; }
        public static EnvironmentProfile MeridianRing { get; private set; }
        public static EnvironmentProfile Europa { get; private set; }

        /// <summary>Creates or updates the environment profiles (values from the design doc).</summary>
        public static void BuildProfiles()
        {
            EnsureFolder(ProfileFolder);
            Earth = Profile("Earth", 9.81f, 2.2f, 4.0f, 1.1f, 0.45f, 18f, 0.35f, false);
            MeridianRing = Profile("MeridianRing", 5.886f, 2.2f, 4.0f, 1.1f, 0.40f, 16f, 0.35f, false); // 0.6 g spin gravity
            Europa = Profile("Europa", 1.315f, 1.6f, 2.8f, 0.9f, 0.55f, 6f, 0.12f, true);               // 0.134 g
        }

        /// <summary>Called by <see cref="GeneratedContent.RebuildAll"/>.</summary>
        public static void Build()
        {
            EnsureFolder("Assets/_Project/Scenes/Sandbox");

            Material floor = Mat("GB_Floor", new Color(0.42f, 0.42f, 0.44f));
            Material wall = Mat("GB_Wall", new Color(0.62f, 0.62f, 0.64f));
            Material accent = Mat("GB_Accent", new Color(0.95f, 0.55f, 0.15f));
            Material europaMat = Mat("GB_EuropaZone", new Color(0.65f, 0.8f, 0.95f));
            Material doorMat = Mat("GB_Door", new Color(0.25f, 0.45f, 0.65f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

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

            // Sliding door in the south doorway
            var doorRoot = new GameObject("SlidingDoor");
            doorRoot.transform.position = new Vector3(0, 0, -12.25f);
            var panel = Box(doorRoot.transform, "Panel", Vector3.zero, new Vector3(2f, 2.4f, 0.2f), doorMat);
            panel.transform.localPosition = new Vector3(0, 1.2f, 0);
            panel.isStatic = false;
            var door = doorRoot.AddComponent<SlidingDoor>();
            Set(door, "panel", panel.transform);
            Set(door, "openOffset", new Vector3(2.1f, 0f, 0f));
            Set(door, "audioSource", Audio3D(doorRoot, null, 1f, 12f));
            Set(door, "openClip", AudioAssetGenerator.Load(AudioAssetGenerator.DoorOpen));
            Set(door, "closeClip", AudioAssetGenerator.Load(AudioAssetGenerator.DoorClose));

            // Ceiling lamp + wall switch
            var lamp = PointLight(null, "Lamp", new Vector3(-4, 3.5f, -4), new Color(1f, 0.92f, 0.8f), 3f, 10f);
            var sw = Box(geo, "LightSwitch", new Vector3(-12f, 1.3f, -4), new Vector3(0.1f, 0.3f, 0.2f), accent);
            sw.isStatic = false;
            var lightSwitch = sw.AddComponent<LightSwitch>();
            Set(lightSwitch, "lights", new[] { lamp });
            Set(lightSwitch, "audioSource", Audio3D(sw, null, 1f, 8f));
            Set(lightSwitch, "clickClip", AudioAssetGenerator.Load(AudioAssetGenerator.SwitchClick));

            // Europa annex: low-gravity zone behind the door
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
            Set(zone.AddComponent<EnvironmentZone>(), "profile", Europa);

            PlayerRigBuilder.Spawn(new Vector3(0, 0.05f, -6), 180f, Earth); // facing the door

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.48f, 0.52f);
            RenderSettings.ambientEquatorColor = new Color(0.3f, 0.3f, 0.32f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.15f, 0.16f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[GreyboxTestRoomBuilder] Saved {ScenePath}");
        }

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
    }
}
