using System.Collections.Generic;
using Jupiter.Audio;
using Jupiter.Narrative;
using Jupiter.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Jupiter.EditorTools.ShipKit;
using Kit = Jupiter.EditorTools.GreyboxKit;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Generates the interior of the deep-space vessel Meridian's habitat ring (Chapter 2, "Month 4"):
    /// a ribbed, panelled spaceship corridor with split auto-doors, and ten furnished rooms.
    ///
    /// Layout (metres, +Z = forward along the ring):
    ///   central corridor x[-1.5, 1.5] z[-10, 38]
    ///   west  x[-9.5,-1.5]: Hygiene z-10..0, Quarters 0..10, Galley 10..20, Med bay 20..28, Workshop 28..38
    ///   east  x[ 1.5, 9.5]: Storage z-10..0, Bridge 0..10 (windows to space), Science lab 10..20, Comms 20..28, Airlock 28..38
    ///   observation deck z[38, 50] with a 14 m window facing Jupiter
    ///
    /// The ring is treated as locally flat: at the 0.6 g design radius (~134 m at 2 rpm) the floor
    /// curvature across a 50 m section is small.
    /// </summary>
    public static class MeridianGreyboxBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/L02_Meridian.unity";

        const float H = 2.8f;      // ceiling height
        const float T = 0.2f;      // wall thickness
        const float DoorW = 1.4f;
        const float DoorH = 2.2f;
        const float SouthZ = -10f;

        static readonly float[] RoomDoorZ = { -5f, 5f, 15f, 24f, 33f };

        struct Gap
        {
            public float Center, Width, Sill, Head;
            public Gap(float c, float w = DoorW, float sill = 0f, float head = DoorH) { Center = c; Width = w; Sill = sill; Head = head; }
            public static Gap Window(float c, float w, float sill, float head) => new Gap(c, w, sill, head);
        }

        static Color warm = new Color(1f, 0.9f, 0.78f);
        static Color cool = new Color(0.84f, 0.92f, 1f);
        static Material paperMat;

        public static void Build()
        {
            paperMat = Kit.Mat("MER_Paper", new Color(0.92f, 0.9f, 0.84f));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var shell = new GameObject("Shell").transform;
            var corridor = new GameObject("CorridorDressing").transform;
            var props = new GameObject("Props").transform;
            var lights = new GameObject("Lights").transform;
            var audio = new GameObject("Audio").transform;
            var inter = new GameObject("Interactables").transform;

            BuildShell(shell);
            BuildCorridor(corridor, lights);
            BuildDoors(inter);
            BuildWestRooms(props, lights, audio, inter);
            BuildEastRooms(props, lights, audio, inter);
            BuildObservationDeck(shell, props, lights);
            BuildJupiter();

            // Global ring drone: non-positional, always present, very low.
            var drone = new GameObject("RingDrone");
            drone.transform.SetParent(audio, false);
            var droneSrc = drone.AddComponent<AudioSource>();
            droneSrc.clip = AudioAssetGenerator.Load(AudioAssetGenerator.RingDrone);
            droneSrc.spatialBlend = 0f;
            droneSrc.playOnAwake = false;
            Kit.Set(drone.AddComponent<RoomTone>(), "volume", 0.22f);
            foreach (float z in new[] { -6f, 6f, 19f, 32f })
                Tone(audio, $"Tone_CorridorVent_{z}", new Vector3(0, 2.5f, z), AudioAssetGenerator.Ventilation, 0.18f, 6f);

            // Player starts in the corridor looking toward the observation deck.
            var rig = PlayerRigBuilder.Spawn(new Vector3(0, 0.05f, 3f), 0f, GreyboxTestRoomBuilder.MeridianRing);
            var cam = rig.GetComponentInChildren<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;   // stars through the windows
            PrefabUtility.RecordPrefabInstancePropertyModifications(cam);

            BuildStory(rig);

            var sky = new Material(Shader.Find("Skybox/Panoramic"));
            sky.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(SciFiTextureGenerator.Stars));
            sky.SetFloat("_Exposure", 1.1f);
            string skyPath = $"{Kit.MaterialFolder}/SHIP_Starfield.mat";
            AssetDatabase.DeleteAsset(skyPath);
            AssetDatabase.CreateAsset(sky, skyPath);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.24f, 0.26f, 0.3f);
            RenderSettings.ambientEquatorColor = new Color(0.16f, 0.17f, 0.19f);
            RenderSettings.ambientGroundColor = new Color(0.07f, 0.07f, 0.08f);

            Kit.EnsureFolder("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[MeridianGreyboxBuilder] Saved {ScenePath}");
        }

        // ------------------------------------------------------------------ shell

        static void BuildShell(Transform p)
        {
            Block(p, "Floor", new Vector3(-9.6f, -0.2f, SouthZ - 0.1f), new Vector3(9.6f, 0f, 50.1f), Floor);
            Block(p, "Ceiling", new Vector3(-9.6f, H, SouthZ - 0.1f), new Vector3(9.6f, H + 0.2f, 50.1f), Ceiling);

            // Outer hull walls: three bridge windows to space and the airlock hatch.
            WallZ(p, "Hull_W", -9.5f, SouthZ, 50f);
            WallZ(p, "Hull_E", 9.5f, SouthZ, 50f,
                Gap.Window(2.5f, 2.2f, 0.9f, 2.4f), Gap.Window(5f, 2.2f, 0.9f, 2.4f), Gap.Window(7.5f, 2.2f, 0.9f, 2.4f),
                new Gap(33f));
            foreach (float z in new[] { 2.5f, 5f, 7.5f })
                WindowGlass(p, $"Window_Bridge_{z}", new Vector3(9.5f, 1.65f, z), new Vector3(T, 1.5f, 2.2f));

            var doors = new List<Gap>();
            foreach (float z in RoomDoorZ) doors.Add(new Gap(z));
            WallZ(p, "Wall_Corridor_W", -1.5f, SouthZ, 38f, doors.ToArray());
            WallZ(p, "Wall_Corridor_E", 1.5f, SouthZ, 38f, doors.ToArray());

            foreach (float z in new[] { 0f, 10f, 20f, 28f })
            {
                WallX(p, $"Divider_W_{z}", z, -9.5f, -1.5f);
                WallX(p, $"Divider_E_{z}", z, 1.5f, 9.5f);
            }

            WallX(p, "Bulkhead_South", SouthZ, -9.5f, 9.5f, new Gap(0f));
            WallX(p, "Bulkhead_North", 38f, -9.5f, 9.5f, new Gap(0f, 3f, 0f, 2.4f));
        }

        /// <summary>Invisible collider plus a thin frame so windows read as windows and keep the player in.</summary>
        static void WindowGlass(Transform p, string name, Vector3 center, Vector3 size)
        {
            var g = new GameObject(name);
            g.transform.SetParent(p, false);
            g.transform.position = center;
            g.AddComponent<BoxCollider>().size = size;
            bool alongZ = size.z > size.x;
            float w = alongZ ? size.z : size.x;
            Vector3 along = alongZ ? Vector3.forward : Vector3.right;
            Vector3 frameSize = alongZ ? new Vector3(T + 0.1f, 0.08f, w + 0.16f) : new Vector3(w + 0.16f, 0.08f, T + 0.1f);
            Box(p, $"{name}_FrameTop", center + Vector3.up * (size.y / 2f + 0.04f), frameSize, Frame, collider: false);
            Box(p, $"{name}_FrameBottom", center - Vector3.up * (size.y / 2f + 0.04f), frameSize, Frame, collider: false);
            Vector3 side = alongZ ? new Vector3(T + 0.1f, size.y + 0.16f, 0.08f) : new Vector3(0.08f, size.y + 0.16f, T + 0.1f);
            Box(p, $"{name}_FrameA", center + along * (w / 2f + 0.04f), side, Frame, collider: false);
            Box(p, $"{name}_FrameB", center - along * (w / 2f + 0.04f), side, Frame, collider: false);
        }

        static void WallZ(Transform p, string name, float x, float z0, float z1, params Gap[] gaps)
        {
            float cursor = z0;
            var sorted = new List<Gap>(gaps);
            sorted.Sort((a, b) => a.Center.CompareTo(b.Center));
            int i = 0;
            foreach (var g in sorted)
            {
                float g0 = g.Center - g.Width * 0.5f, g1 = g.Center + g.Width * 0.5f;
                if (g0 > cursor) Block(p, $"{name}_{i++}", new Vector3(x - T / 2, 0, cursor), new Vector3(x + T / 2, H, g0), Wall);
                if (g.Sill > 0f) Block(p, $"{name}_Sill_{i++}", new Vector3(x - T / 2, 0, g0), new Vector3(x + T / 2, g.Sill, g1), Wall);
                Block(p, $"{name}_Lintel_{i++}", new Vector3(x - T / 2, g.Head, g0), new Vector3(x + T / 2, H, g1), Wall);
                cursor = g1;
            }
            if (z1 > cursor) Block(p, $"{name}_{i}", new Vector3(x - T / 2, 0, cursor), new Vector3(x + T / 2, H, z1), Wall);
        }

        static void WallX(Transform p, string name, float z, float x0, float x1, params Gap[] gaps)
        {
            float cursor = x0;
            var sorted = new List<Gap>(gaps);
            sorted.Sort((a, b) => a.Center.CompareTo(b.Center));
            int i = 0;
            foreach (var g in sorted)
            {
                float g0 = g.Center - g.Width * 0.5f, g1 = g.Center + g.Width * 0.5f;
                if (g0 > cursor) Block(p, $"{name}_{i++}", new Vector3(cursor, 0, z - T / 2), new Vector3(g0, H, z + T / 2), Wall);
                if (g.Sill > 0f) Block(p, $"{name}_Sill_{i++}", new Vector3(g0, 0, z - T / 2), new Vector3(g1, g.Sill, z + T / 2), Wall);
                Block(p, $"{name}_Lintel_{i++}", new Vector3(g0, g.Head, z - T / 2), new Vector3(g1, H, z + T / 2), Wall);
                cursor = g1;
            }
            if (x1 > cursor) Block(p, $"{name}_{i}", new Vector3(cursor, 0, z - T / 2), new Vector3(x1, H, z + T / 2), Wall);
        }

        // ------------------------------------------------------------------ corridor

        /// <summary>
        /// Structural ribs every 2 m (pillars, ceiling beam and 45-degree corner braces give the octagonal profile),
        /// a ceiling light panel per bay, floor guide LEDs, pipe runs along both upper walls, vents and extinguishers.
        /// </summary>
        static void BuildCorridor(Transform p, Transform lights)
        {
            const float inner = 1.4f; // corridor wall inner face
            for (float z = SouthZ + 1f; z < 37.5f; z += 2f)
            {
                bool nearDoor = false;
                foreach (float d in RoomDoorZ) nearDoor |= Mathf.Abs(z - d) < 1.15f;
                if (nearDoor) continue;

                var rib = new GameObject($"Rib_{z}").transform;
                rib.SetParent(p, false);
                foreach (int s in new[] { -1, 1 })
                {
                    Block(rib, "Pillar", new Vector3(s < 0 ? -inner : inner - 0.18f, 0f, z - 0.15f), new Vector3(s < 0 ? -inner + 0.18f : inner, H, z + 0.15f), Frame);
                    Box(rib, "BraceTop", new Vector3(s * 1.12f, 2.52f, z), new Vector3(0.18f, 0.62f, 0.28f), Frame, Quaternion.Euler(0, 0, s * 45f), collider: false);
                    Box(rib, "BraceLow", new Vector3(s * 1.18f, 0.22f, z), new Vector3(0.14f, 0.5f, 0.26f), Frame, Quaternion.Euler(0, 0, -s * 45f), collider: false);
                    Box(rib, "Accent", new Vector3(s * (inner - 0.19f), 1.3f, z), new Vector3(0.02f, 0.5f, 0.06f), LedCyan, collider: false);
                }
                Block(rib, "Beam", new Vector3(-inner, H - 0.16f, z - 0.15f), new Vector3(inner, H, z + 0.15f), Frame, collider: false);

                // Bay between this rib and the next: light panel, floor LEDs.
                float bz = z + 1f;
                Box(p, $"LightPanel_{bz}", new Vector3(0f, H - 0.02f, bz), new Vector3(1.1f, 0.03f, 0.45f), LightPanel, collider: false);
                Box(p, $"FloorLED_W_{bz}", new Vector3(-1.0f, 0.004f, bz), new Vector3(0.04f, 0.008f, 1.4f), LedCyan, collider: false);
                Box(p, $"FloorLED_E_{bz}", new Vector3(1.0f, 0.004f, bz), new Vector3(0.04f, 0.008f, 1.4f), LedCyan, collider: false);
            }

            // Real lights every 4 m (the panels only glow; these light the room).
            for (float z = SouthZ + 2f; z < 38f; z += 4f)
                RoomLight(lights, $"Light_Corridor_{z}", new Vector3(0, 2.55f, z), cool, 1.5f, 5.5f);

            // Pipe runs along both upper walls.
            PipeRun(p, "Pipe_W_A", new Vector3(-1.3f, 2.32f, SouthZ + 0.3f), new Vector3(-1.3f, 2.32f, 37.7f), 0.05f, Pipe);
            PipeRun(p, "Pipe_W_B", new Vector3(-1.3f, 2.18f, SouthZ + 0.3f), new Vector3(-1.3f, 2.18f, 37.7f), 0.035f, PipeYellow);
            PipeRun(p, "Pipe_E_A", new Vector3(1.3f, 2.32f, SouthZ + 0.3f), new Vector3(1.3f, 2.32f, 37.7f), 0.05f, Pipe);
            PipeRun(p, "Pipe_E_B", new Vector3(1.3f, 2.18f, SouthZ + 0.3f), new Vector3(1.3f, 2.18f, 37.7f), 0.035f, Orange);

            // Low wall vents and fire extinguishers between doors.
            foreach (float z in new[] { -8f, -2f, 10f, 22f, 28f, 36f })
            {
                Box(p, $"Vent_W_{z}", new Vector3(-1.38f, 0.35f, z), new Vector3(0.04f, 0.3f, 0.7f), DarkMetal, collider: false);
                Box(p, $"Vent_E_{z}", new Vector3(1.38f, 0.35f, z), new Vector3(0.04f, 0.3f, 0.7f), DarkMetal, collider: false);
            }
            foreach (float z in new[] { 2f, 20f, 30f })
            {
                Cylinder(p, $"Extinguisher_{z}", new Vector3(-1.3f, 0.9f, z), 0.08f, 0.55f, LedRed);
                Box(p, $"ExtinguisherSign_{z}", new Vector3(-1.38f, 1.35f, z), new Vector3(0.02f, 0.18f, 0.18f), LedRed, collider: false);
            }
        }

        // ------------------------------------------------------------------ doors

        static void BuildDoors(Transform p)
        {
            string[] west = { "Hygiene", "Quarters", "Galley", "MedBay", "Workshop" };
            string[] east = { "Storage", "Bridge", "Science", "Comms", "Airlock" };
            string[] westSign = { "HYGIENE", "CREW QUARTERS", "GALLEY", "MED BAY", "WORKSHOP" };
            string[] eastSign = { "STORAGE", "BRIDGE", "SCIENCE LAB", "COMMS", "AIRLOCK" };
            for (int i = 0; i < RoomDoorZ.Length; i++)
            {
                float z = RoomDoorZ[i];
                Door(p, $"Door_{west[i]}", new Vector3(-1.5f, 0, z), 90f, false);
                Door(p, $"Door_{east[i]}", new Vector3(1.5f, 0, z), 90f, false);
                Sign(p, westSign[i], new Vector3(-1.36f, 2.6f, z), Vector3.right, 1.3f);
                Sign(p, eastSign[i], new Vector3(1.36f, 2.6f, z), Vector3.left, 1.3f);
            }
            Door(p, "Door_CargoHold", new Vector3(0, 0, SouthZ), 0f, true);
            Sign(p, "CARGO HOLD - RESTRICTED", new Vector3(0f, 2.6f, SouthZ + 0.14f), Vector3.forward, 2.1f, new Color(1f, 0.35f, 0.25f));
            Door(p, "Hatch_AirlockOuter", new Vector3(9.5f, 0, 33f), 90f, true);
        }

        /// <summary>Two-leaf split door with a hazard-striped frame and a status light above it.</summary>
        static SplitDoor Door(Transform parent, string name, Vector3 position, float yaw, bool locked)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            var rot = root.transform.rotation;
            Vector3 right = rot * Vector3.right, fwd = rot * Vector3.forward;

            Transform Leaf(string n, float offsetX)
            {
                var leaf = new GameObject(n).transform;
                leaf.SetParent(root.transform, false);
                leaf.localPosition = new Vector3(offsetX, DoorH / 2f, 0f);
                var vis = Box(leaf, "Panel", leaf.position, new Vector3(DoorW / 2f + 0.01f, DoorH, 0.08f), DoorPanel, rot, collider: true);
                vis.isStatic = false;
                var stripe = Box(leaf, "Stripe", leaf.position + Vector3.up * 0.35f + fwd * 0.045f, new Vector3(DoorW / 2f, 0.06f, 0.01f), locked ? LedRed : LedAmber, rot, collider: false);
                stripe.isStatic = false;
                var stripeB = Box(leaf, "StripeBack", leaf.position + Vector3.up * 0.35f - fwd * 0.045f, new Vector3(DoorW / 2f, 0.06f, 0.01f), locked ? LedRed : LedAmber, rot, collider: false);
                stripeB.isStatic = false;
                return leaf;
            }
            var left = Leaf("LeafLeft", -DoorW / 4f);
            var rightLeaf = Leaf("LeafRight", DoorW / 4f);

            // Frame: hazard-striped jambs and header, proud of the wall on both sides.
            var frame = new GameObject("Frame").transform;
            frame.SetParent(root.transform, false);
            foreach (int s in new[] { -1, 1 })
                Box(frame, $"Jamb{s}", position + right * (s * (DoorW / 2f + 0.06f)) + Vector3.up * (DoorH / 2f), new Vector3(0.12f, DoorH, T + 0.16f), Hazard, rot, collider: false);
            Box(frame, "Header", position + Vector3.up * (DoorH + 0.07f), new Vector3(DoorW + 0.24f, 0.14f, T + 0.16f), Hazard, rot, collider: false);
            var status = Box(frame, "StatusLight", position + Vector3.up * (DoorH + 0.2f), new Vector3(0.24f, 0.06f, T + 0.2f), LedGreen, rot, collider: false);
            status.isStatic = false;

            var door = root.AddComponent<SplitDoor>();
            Kit.Set(door, "leftLeaf", left);
            Kit.Set(door, "rightLeaf", rightLeaf);
            Kit.Set(door, "travel", DoorW / 2f + 0.02f);
            Kit.Set(door, "locked", locked);
            Kit.Set(door, "statusLight", status.GetComponent<Renderer>());
            Kit.Set(door, "audioSource", Kit.Audio3D(root, null, 1f, 14f));
            Kit.Set(door, "openClip", AudioAssetGenerator.Load(AudioAssetGenerator.DoorOpen));
            Kit.Set(door, "closeClip", AudioAssetGenerator.Load(AudioAssetGenerator.DoorClose));
            Kit.Set(door, "deniedClip", AudioAssetGenerator.Load(AudioAssetGenerator.SwitchClick));
            return door;
        }

        // ------------------------------------------------------------------ west rooms

        static void BuildWestRooms(Transform props, Transform lights, Transform audio, Transform inter)
        {
            // ---------- Hygiene (z -10..0): toilet stall, shower, vanity with mirror, laundry, towels
            var hy = Group(props, "Hygiene");
            Block(hy, "Stall_Partition", new Vector3(-7.6f, 0, -9.9f), new Vector3(-7.5f, 2.2f, -7.9f), White);
            Block(hy, "Stall_Back", new Vector3(-9.4f, 0, -7.95f), new Vector3(-7.5f, 2.2f, -7.85f), White);
            Box(hy, "Toilet_Base", new Vector3(-8.6f, 0.22f, -9.55f), new Vector3(0.42f, 0.44f, 0.55f), White);
            Cylinder(hy, "Toilet_Seat", new Vector3(-8.6f, 0.46f, -9.45f), 0.2f, 0.05f, White);
            Cylinder(hy, "Toilet_Hose", new Vector3(-9.1f, 0.7f, -9.6f), 0.04f, 0.6f, PipeYellow, new Vector3(0, 0, 25f));
            Box(hy, "Toilet_Panel", new Vector3(-8.6f, 1.2f, -9.85f), new Vector3(0.5f, 0.35f, 0.04f), ScreenGreen, collider: false);
            Box(hy, "Grab_L", new Vector3(-9.35f, 0.9f, -9.3f), new Vector3(0.05f, 0.05f, 0.6f), Metal, collider: false);
            // Shower stall
            Block(hy, "Shower_Base", new Vector3(-9.4f, 0, -7.6f), new Vector3(-8.1f, 0.06f, -6.2f), DarkMetal);
            Block(hy, "Shower_Glass", new Vector3(-8.15f, 0.06f, -7.6f), new Vector3(-8.1f, 2.1f, -6.6f), Glass);
            Block(hy, "Shower_Wall", new Vector3(-9.4f, 0, -6.25f), new Vector3(-8.1f, 2.2f, -6.15f), White);
            Cylinder(hy, "Shower_Head", new Vector3(-9.2f, 2.05f, -6.9f), 0.12f, 0.04f, Metal);
            Box(hy, "Shower_Status", new Vector3(-9.38f, 1.4f, -6.9f), new Vector3(0.02f, 0.12f, 0.2f), LedCyan, collider: false);
            // Vanity, sinks and mirror along the west wall
            Block(hy, "Vanity", new Vector3(-9.4f, 0, -5.4f), new Vector3(-8.8f, 0.85f, -2.6f), White);
            foreach (float z in new[] { -4.7f, -3.3f })
            {
                Box(hy, $"Basin_{z}", new Vector3(-9.1f, 0.83f, z), new Vector3(0.4f, 0.05f, 0.5f), DarkMetal, collider: false);
                Cylinder(hy, $"Tap_{z}", new Vector3(-9.33f, 0.98f, z), 0.025f, 0.25f, Metal);
            }
            Block(hy, "Mirror", new Vector3(-9.41f, 1.15f, -5.3f), new Vector3(-9.38f, 2.0f, -2.7f), Mirror, collider: false);
            Box(hy, "MirrorLight", new Vector3(-9.35f, 2.07f, -4f), new Vector3(0.05f, 0.04f, 2.6f), WarmPanel, collider: false);
            // Laundry and towels near the divider
            Box(hy, "Laundry", new Vector3(-5.2f, 0.45f, -0.5f), new Vector3(0.65f, 0.9f, 0.6f), White);
            Cylinder(hy, "Laundry_Window", new Vector3(-5.2f, 0.5f, -0.81f), 0.2f, 0.02f, Glass, new Vector3(90, 0, 0));
            Box(hy, "TowelRack", new Vector3(-3.6f, 1.2f, -0.18f), new Vector3(1.1f, 0.04f, 0.04f), Metal, collider: false);
            foreach (float x in new[] { -3.95f, -3.6f, -3.25f })
                Box(hy, $"Towel_{x}", new Vector3(x, 0.95f, -0.2f), new Vector3(0.28f, 0.5f, 0.03f), Fabric, collider: false);
            Box(hy, "SupplyCabinet", new Vector3(-6.6f, 1.0f, -0.4f), new Vector3(1.0f, 2.0f, 0.5f), Metal);
            CeilingPanel(lights, "Hygiene", new Vector3(-5.5f, 0, -5f), cool, 1.6f);
            Tone(audio, "Tone_Hygiene", new Vector3(-5.5f, 2.4f, -5f), AudioAssetGenerator.Ventilation, 0.22f, 7f);

            // ---------- Crew quarters (z 0..10): sleep pods, lockers, desk, porthole
            var q = Group(props, "Quarters");
            for (int i = 0; i < 4; i++)
            {
                float z = 1.3f + i * 2.3f;
                Box(q, $"Pod{i}_Bed", new Vector3(-8.95f, 0.25f, z), new Vector3(0.9f, 0.5f, 2.0f), Frame);
                Box(q, $"Pod{i}_Mattress", new Vector3(-8.95f, 0.55f, z), new Vector3(0.84f, 0.1f, 1.9f), Fabric, collider: false);
                Box(q, $"Pod{i}_Canopy", new Vector3(-8.95f, 1.75f, z), new Vector3(1.0f, 0.1f, 2.1f), White, collider: false);
                Box(q, $"Pod{i}_Back", new Vector3(-9.38f, 1.15f, z), new Vector3(0.04f, 1.1f, 2.0f), White, collider: false);
                Box(q, $"Pod{i}_Curtain", new Vector3(-8.45f, 1.25f, z + 0.65f), new Vector3(0.03f, 0.9f, 0.7f), Fabric, collider: false);
                Box(q, $"Pod{i}_Lamp", new Vector3(-9.0f, 1.68f, z - 0.6f), new Vector3(0.3f, 0.03f, 0.12f), WarmPanel, collider: false);
                Box(q, $"Pod{i}_Tablet", new Vector3(-9.3f, 1.0f, z - 0.4f), new Vector3(0.02f, 0.18f, 0.26f), ScreenCyan, collider: false);
            }
            string[] lockerNames = { "OKAFOR", "LINDQVIST", "MORI", "FERREIRA", "ASHFORD" };
            for (int i = 0; i < 5; i++)
            {
                float x = -7.6f + i * 0.72f;
                Box(q, $"Locker_{lockerNames[i]}", new Vector3(x, 1.0f, 9.62f), new Vector3(0.68f, 2.0f, 0.5f), Metal);
                Box(q, $"LockerVent_{i}", new Vector3(x, 1.75f, 9.36f), new Vector3(0.4f, 0.12f, 0.02f), DarkMetal, collider: false);
                Sign(q, lockerNames[i], new Vector3(x, 1.45f, 9.35f), Vector3.back, 0.6f, Color.white);
            }
            Box(q, "Desk", new Vector3(-4.2f, 0.37f, 2.0f), new Vector3(1.4f, 0.06f, 0.7f), Wood);
            Box(q, "DeskLeg", new Vector3(-4.2f, 0.18f, 2.0f), new Vector3(1.3f, 0.36f, 0.6f), Frame);
            Box(q, "DeskScreen", new Vector3(-4.2f, 0.75f, 1.75f), new Vector3(0.6f, 0.35f, 0.03f), ScreenCyan, collider: false);
            Cylinder(q, "Chair", new Vector3(-4.2f, 0.25f, 2.6f), 0.22f, 0.5f, Fabric, collider: true);
            var quartersLight = CeilingPanel(lights, "Quarters", new Vector3(-5.5f, 0, 5f), warm, 1.8f);
            var sw = Kit.Box(inter, "LightSwitch_Quarters", new Vector3(-1.63f, 1.3f, 6.2f), new Vector3(0.06f, 0.25f, 0.16f), Hazard);
            sw.isStatic = false;
            var ls = sw.AddComponent<LightSwitch>();
            Kit.Set(ls, "lights", new[] { quartersLight });
            Kit.Set(ls, "audioSource", Kit.Audio3D(sw, null, 1f, 8f));
            Kit.Set(ls, "clickClip", AudioAssetGenerator.Load(AudioAssetGenerator.SwitchClick));
            Readable(inter, "Readable_ZaraDrawing", new Vector3(-8.95f, 0.62f, 8.2f),
                "Crayon drawing, taped inside Sam's locker",
                "A drawing of Jupiter in orange and brown crayon. Someone has given it a big smiling face, with the red spot for a nose.\n\n" +
                "Across the bottom, in careful capital letters:\n\n" +
                "FOR UNCLE SAM.\nBRING IT BACK AND TELL ME IF IT HAS A FACE.\n\nLOVE ZARA (8)");
            Tone(audio, "Tone_Quarters", new Vector3(-5.5f, 2.4f, 5f), AudioAssetGenerator.Ventilation, 0.22f, 7f);

            // ---------- Galley / kitchen (z 10..20)
            var g = Group(props, "Galley");
            Block(g, "Counter", new Vector3(-9.4f, 0, 11.6f), new Vector3(-8.7f, 0.92f, 19.6f), Metal);
            Block(g, "Countertop", new Vector3(-9.42f, 0.92f, 11.6f), new Vector3(-8.65f, 0.96f, 19.6f), DarkMetal, collider: false);
            for (float z = 12.0f; z < 19.4f; z += 0.8f)
                Box(g, $"CabinetDoor_{z}", new Vector3(-8.69f, 0.47f, z + 0.38f), new Vector3(0.02f, 0.75f, 0.72f), White, collider: false);
            Block(g, "UpperCabinets", new Vector3(-9.4f, 1.55f, 11.6f), new Vector3(-9.0f, 2.25f, 19.6f), White, collider: false);
            // Induction hob, sink, food rehydrator, coffee machine
            Box(g, "Hob", new Vector3(-9.05f, 0.965f, 13.4f), new Vector3(0.55f, 0.01f, 0.7f), DarkMetal, collider: false);
            foreach (var o in new[] { new Vector2(-0.15f, -0.18f), new Vector2(0.15f, 0.18f) })
                Cylinder(g, "HobRing", new Vector3(-9.05f + o.x, 0.972f, 13.4f + o.y), 0.11f, 0.004f, Hob);
            Box(g, "Sink", new Vector3(-9.05f, 0.95f, 15.2f), new Vector3(0.45f, 0.03f, 0.6f), DarkMetal, collider: false);
            Cylinder(g, "SinkTap", new Vector3(-9.33f, 1.1f, 15.2f), 0.025f, 0.3f, Metal);
            Box(g, "Rehydrator", new Vector3(-9.1f, 1.2f, 17.2f), new Vector3(0.55f, 0.48f, 0.6f), Metal);
            Box(g, "RehydratorScreen", new Vector3(-8.81f, 1.25f, 17.2f), new Vector3(0.02f, 0.2f, 0.35f), ScreenAmber, collider: false);
            Box(g, "CoffeeMachine", new Vector3(-9.15f, 1.15f, 18.8f), new Vector3(0.35f, 0.4f, 0.3f), DarkMetal);
            Box(g, "CoffeeLed", new Vector3(-8.97f, 1.25f, 18.8f), new Vector3(0.01f, 0.03f, 0.03f), LedRed, collider: false);
            Box(g, "Galley_Fridge", new Vector3(-9.05f, 0.95f, 11.0f), new Vector3(0.7f, 1.9f, 0.8f), White);
            Box(g, "FridgeHandle", new Vector3(-8.68f, 1.1f, 11.25f), new Vector3(0.03f, 0.6f, 0.04f), Metal, collider: false);
            // Table and stools
            Box(g, "Galley_Table", new Vector3(-5.5f, 0.72f, 15f), new Vector3(1.2f, 0.06f, 2.4f), Wood);
            Box(g, "Galley_TableBase", new Vector3(-5.5f, 0.35f, 15f), new Vector3(0.3f, 0.7f, 1.6f), Frame);
            foreach (float x in new[] { -6.5f, -4.5f })
                foreach (float z in new[] { 14.2f, 15.8f })
                    Cylinder(g, $"Stool_{x}_{z}", new Vector3(x, 0.24f, z), 0.2f, 0.48f, Fabric, collider: true);
            Box(g, "MenuScreen", new Vector3(-5.5f, 1.6f, 19.88f), new Vector3(1.6f, 0.8f, 0.03f), ScreenAmber, collider: false);
            Box(g, "Recycler", new Vector3(-2.2f, 0.45f, 19.6f), new Vector3(0.5f, 0.9f, 0.5f), Metal);
            CeilingPanel(lights, "Galley", new Vector3(-5.5f, 0, 15f), warm, 2.0f);
            Readable(inter, "Readable_GalleyRota", new Vector3(-5.5f, 0.76f, 14.4f),
                "Galley rota, Month 4",
                "MON      Ferreira\nTUE      Okafor\nWED      Lindqvist\nTHU      Mori\nFRI      Ashford\nSAT/SUN  whoever lost at cards\n\n" +
                "MUGS: everyone. Every time. I mean it.   (H.M.)\n\n" +
                "Handwritten underneath:\nBossa nova is NOT \"elevator music with a tan.\"   (L.F.)\n\n" +
                "Underneath that, in a different pen:\nIt is a little bit.   (M.L.)");
            Tone(audio, "Tone_GalleyFridge", new Vector3(-8.8f, 1f, 11f), AudioAssetGenerator.ElectricalHum, 0.25f, 5f);

            // ---------- Med bay (z 20..28)
            var m = Group(props, "MedBay");
            foreach (float x in new[] { -6f, -3.4f })
            {
                Box(m, $"Bed_{x}", new Vector3(x, 0.4f, 24f), new Vector3(1.0f, 0.8f, 2.1f), White);
                Box(m, $"BedPad_{x}", new Vector3(x, 0.82f, 24f), new Vector3(0.9f, 0.05f, 2.0f), Fabric, collider: false);
                // Overhead scanner arch
                Box(m, $"ScannerL_{x}", new Vector3(x - 0.6f, 1.25f, 24.6f), new Vector3(0.08f, 0.9f, 0.12f), Frame, collider: false);
                Box(m, $"ScannerR_{x}", new Vector3(x + 0.6f, 1.25f, 24.6f), new Vector3(0.08f, 0.9f, 0.12f), Frame, collider: false);
                Box(m, $"ScannerTop_{x}", new Vector3(x, 1.72f, 24.6f), new Vector3(1.28f, 0.1f, 0.14f), LightPanel, collider: false);
            }
            Box(m, "Med_MonitorScreen", new Vector3(-7.4f, 1.4f, 24f), new Vector3(0.08f, 0.5f, 0.7f), ScreenGreen, collider: false);
            Box(m, "Med_MonitorStand", new Vector3(-7.4f, 0.575f, 24f), new Vector3(0.12f, 1.15f, 0.12f), Frame, collider: false);
            Cylinder(m, "IVStand", new Vector3(-6.75f, 0.9f, 25.4f), 0.02f, 1.8f, Metal);
            Box(m, "IVBag", new Vector3(-6.75f, 1.65f, 25.4f), new Vector3(0.14f, 0.22f, 0.05f), Glass, collider: false);
            Box(m, "Med_Cabinet", new Vector3(-4f, 1f, 27.65f), new Vector3(3f, 2f, 0.5f), White);
            Box(m, "RedCrossV", new Vector3(-4f, 1.5f, 27.38f), new Vector3(0.12f, 0.4f, 0.02f), RedCross, collider: false);
            Box(m, "RedCrossH", new Vector3(-4f, 1.5f, 27.38f), new Vector3(0.4f, 0.12f, 0.02f), RedCross, collider: false);
            Box(m, "Defib", new Vector3(-9.35f, 1.3f, 21.4f), new Vector3(0.12f, 0.35f, 0.3f), Orange, collider: false);
            Cylinder(m, "Centrifuge", new Vector3(-8.9f, 1.05f, 26.8f), 0.22f, 0.18f, White);
            Box(m, "CentrifugeBench", new Vector3(-8.9f, 0.47f, 26.8f), new Vector3(0.9f, 0.94f, 1.2f), Metal);
            CeilingPanel(lights, "MedBay", new Vector3(-5.5f, 0, 24f), cool, 2.0f);
            Readable(inter, "Readable_MedicalNotes", new Vector3(-6f, 0.86f, 25.3f),
                "Crew medical, weekly summary",
                "OKAFOR: sleep 5.1 h average. Again. Talk to him.\n" +
                "LINDQVIST: refused bone density scan (third time). Rescheduled. Again.\n" +
                "FERREIRA: fine. Annoyingly fine.\n" +
                "ASHFORD: blood pressure elevated. Says it's the coffee.\n" +
                "MORI: physician, heal thyself. (Fine.)\n\n(H.M.)");
            Tone(audio, "Tone_MedMonitor", new Vector3(-7.2f, 1.4f, 24f), AudioAssetGenerator.MedicalMonitor, 0.3f, 6f);

            // ---------- Workshop (z 28..38)
            var w = Group(props, "Workshop");
            Box(w, "Workshop_Bench", new Vector3(-9.0f, 0.475f, 33f), new Vector3(0.9f, 0.95f, 4f), Metal);
            Block(w, "Pegboard", new Vector3(-9.42f, 1.2f, 31.2f), new Vector3(-9.38f, 2.2f, 34.8f), DarkMetal, collider: false);
            var rng = new System.Random(5);
            for (int i = 0; i < 14; i++)
            {
                float z = 31.4f + (float)rng.NextDouble() * 3.2f, y = 1.3f + (float)rng.NextDouble() * 0.8f;
                Box(w, $"Tool_{i}", new Vector3(-9.35f, y, z), new Vector3(0.04f, 0.08f + (float)rng.NextDouble() * 0.25f, 0.05f), i % 3 == 0 ? Orange : Metal, collider: false);
            }
            Box(w, "Printer", new Vector3(-9.0f, 0.6f, 35.8f), new Vector3(0.7f, 1.2f, 0.7f), White);
            Box(w, "PrinterWindow", new Vector3(-8.64f, 0.8f, 35.8f), new Vector3(0.02f, 0.4f, 0.5f), ScreenAmber, collider: false);
            Box(w, "Workshop_Shelves", new Vector3(-5f, 1.1f, 37.6f), new Vector3(4f, 2.2f, 0.6f), Frame);
            for (int i = 0; i < 6; i++)
                Box(w, $"Crate_{i}", new Vector3(-6.6f + i * 0.65f, 0.25f + (i % 2) * 0.9f, 37.55f), new Vector3(0.55f, 0.45f, 0.45f), i % 2 == 0 ? Wood : Orange, collider: false);
            Box(w, "Workshop_ToolCart", new Vector3(-5.2f, 0.45f, 30.4f), new Vector3(0.6f, 0.9f, 1f), Hazard);
            Cylinder(w, "LanderActuator", new Vector3(-3.6f, 0.55f, 31.2f), 0.22f, 1.4f, Metal, new Vector3(0, 0, 90));
            Box(w, "ActuatorStand", new Vector3(-3.6f, 0.2f, 31.2f), new Vector3(1.1f, 0.4f, 0.5f), Frame);
            Box(w, "CrateStack_A", new Vector3(-2.3f, 0.4f, 29.2f), new Vector3(0.8f, 0.8f, 0.8f), Wood);
            Box(w, "CrateStack_B", new Vector3(-2.3f, 1.05f, 29.2f), new Vector3(0.6f, 0.5f, 0.6f), Orange);
            CeilingPanel(lights, "Workshop", new Vector3(-5.5f, 0, 33f), warm, 2.2f);
            Readable(inter, "Readable_WorkOrder", new Vector3(-9.0f, 0.96f, 32f),
                "Work order WO-117: Lander Wren",
                "System: aft ramp actuator\nStatus: intermittent stall at 40-45% travel\nCause: suspected thermal binding in the screw jack\n\n" +
                "Action: replace bearing set before Europa descent. Spare is in bin C-4.\n\n" +
                "NOTE: Do NOT cycle the ramp manually above 40% until this is fixed. Yes, Luca, this means you.\n\n" +
                "M. Lindqvist, Flight Engineer");
            Tone(audio, "Tone_Workshop", new Vector3(-8.5f, 1f, 34f), AudioAssetGenerator.ElectricalHum, 0.3f, 7f);
        }

        // ------------------------------------------------------------------ east rooms

        static void BuildEastRooms(Transform props, Transform lights, Transform audio, Transform inter)
        {
            // ---------- Storage (z -10..0): shelving, crates, tanks, containers
            var s = Group(props, "Storage");
            foreach (float z in new[] { -9.4f, -5.6f })
            {
                Box(s, $"Shelf_{z}_Frame", new Vector3(6.2f, 1.1f, z + 1.2f), new Vector3(5.2f, 2.2f, 0.06f), Frame, collider: false);
                for (int level = 0; level < 3; level++)
                {
                    float y = 0.15f + level * 0.75f;
                    Box(s, $"Shelf_{z}_{level}", new Vector3(6.2f, y, z + 0.6f), new Vector3(5.2f, 0.05f, 1.2f), Metal);
                    for (int i = 0; i < 6; i++)
                        Box(s, $"Box_{z}_{level}_{i}", new Vector3(4.0f + i * 0.85f, y + 0.25f, z + 0.6f), new Vector3(0.7f, 0.45f, 0.9f), (i + level) % 3 == 0 ? Orange : (i + level) % 3 == 1 ? Wood : White, collider: false);
                }
            }
            foreach (float z in new[] { -2.5f, -1.2f })
                Cylinder(s, $"WaterTank_{z}", new Vector3(8.7f, 0.9f, z), 0.5f, 1.8f, White, collider: true);
            Sign(s, "POTABLE WATER", new Vector3(9.38f, 2.1f, -1.85f), Vector3.left, 1.4f, new Color(0.5f, 0.8f, 1f));
            Box(s, "Container_A", new Vector3(3.2f, 0.6f, -1.2f), new Vector3(1.2f, 1.2f, 1.6f), Hazard);
            Box(s, "Container_B", new Vector3(4.6f, 0.45f, -1.0f), new Vector3(1.0f, 0.9f, 1.0f), Metal);
            CeilingPanel(lights, "Storage", new Vector3(5.5f, 0, -5f), cool, 1.4f);
            Tone(audio, "Tone_Storage", new Vector3(5.5f, 2.4f, -5f), AudioAssetGenerator.Ventilation, 0.16f, 7f);

            // ---------- Bridge (z 0..10): console horseshoe under three windows to space
            var b = Group(props, "Bridge");
            // Window-wall console bank with a row of screens
            Box(b, "Bridge_WindowConsole", new Vector3(8.75f, 0.45f, 5f), new Vector3(0.9f, 0.9f, 8.6f), DarkMetal);
            for (int i = 0; i < 7; i++)
            {
                float z = 1.4f + i * 1.2f;
                Box(b, $"Bridge_Screen_{i}", new Vector3(8.45f, 1.05f, z), new Vector3(0.05f, 0.42f, 0.9f), i % 3 == 1 ? ScreenAmber : ScreenCyan,
                    Quaternion.Euler(0, 0, -18f), collider: false);
                Box(b, $"Bridge_Keys_{i}", new Vector3(8.35f, 0.92f, z), new Vector3(0.35f, 0.02f, 0.8f), Frame, collider: false);
            }
            // Command console (holds the commander's log) and side stations
            Box(b, "Command_ConsoleFront", new Vector3(5.5f, 0.5f, 8.8f), new Vector3(3f, 1f, 0.8f), DarkMetal);
            Box(b, "Command_ScreenFront", new Vector3(5.5f, 1.3f, 9.25f), new Vector3(3f, 0.6f, 0.08f), ScreenCyan, collider: false);
            Box(b, "Command_ConsoleSouth", new Vector3(5.5f, 0.5f, 1.0f), new Vector3(3f, 1f, 0.8f), DarkMetal);
            Box(b, "Command_ScreenSouth", new Vector3(5.5f, 1.3f, 0.6f), new Vector3(3f, 0.6f, 0.08f), ScreenGreen, collider: false);
            // Captain's chair facing the windows
            Box(b, "Chair_Seat", new Vector3(6.4f, 0.5f, 5f), new Vector3(0.65f, 0.12f, 0.65f), Fabric);
            Box(b, "Chair_Back", new Vector3(6.1f, 0.95f, 5f), new Vector3(0.12f, 0.8f, 0.6f), Fabric, collider: false);
            Cylinder(b, "Chair_Post", new Vector3(6.4f, 0.22f, 5f), 0.08f, 0.44f, Metal);
            Box(b, "Chair_ArmL", new Vector3(6.4f, 0.7f, 4.65f), new Vector3(0.5f, 0.06f, 0.08f), Frame, collider: false);
            Box(b, "Chair_ArmR", new Vector3(6.4f, 0.7f, 5.35f), new Vector3(0.5f, 0.06f, 0.08f), Frame, collider: false);
            Box(b, "Bridge_Holotable", new Vector3(3.8f, 0.45f, 3.2f), new Vector3(1.0f, 0.9f, 1.0f), DarkMetal);
            Box(b, "Bridge_HoloTop", new Vector3(3.8f, 0.91f, 3.2f), new Vector3(0.9f, 0.02f, 0.9f), ScreenCyan, collider: false);
            CeilingPanel(lights, "Bridge", new Vector3(4.5f, 0, 5f), cool, 1.2f);
            Readable(inter, "Readable_CommanderLog", new Vector3(4.6f, 1.01f, 8.8f),
                "Commander's log, Day 118",
                "Radar calibration completed two days ahead of schedule. Okafor is thorough. " +
                "He doesn't ask why we need this much resolution on the deep returns, which I'm grateful for.\n\n" +
                "Lindqvist filed another objection to the power allocation. She wants the hours for lander testing. " +
                "She isn't wrong, and I told her so. The priority stands.\n\n" +
                "Crew morale good. Ferreira has started a card league. Mori is winning.\n\nD. Ashford",
                setsFlag: "read_commander_log");
            Tone(audio, "Tone_Command", new Vector3(8.5f, 1f, 5f), AudioAssetGenerator.ElectricalHum, 0.2f, 7f);

            // ---------- Science lab (z 10..20)
            var lab = Group(props, "Science");
            Box(lab, "Science_RadarConsole", new Vector3(8.8f, 0.5f, 15f), new Vector3(0.8f, 1f, 4f), DarkMetal);
            Box(lab, "Science_RadarScreen", new Vector3(9.25f, 1.3f, 15f), new Vector3(0.08f, 0.6f, 4f), ScreenCyan, collider: false);
            foreach (float z in new[] { 12.6f, 16.6f })
            {
                Box(lab, $"LabBench_{z}", new Vector3(4.8f, 0.46f, z), new Vector3(2.2f, 0.92f, 0.9f), White);
                Box(lab, $"LabBenchTop_{z}", new Vector3(4.8f, 0.93f, z), new Vector3(2.3f, 0.03f, 1.0f), DarkMetal, collider: false);
                Cylinder(lab, $"Microscope_{z}", new Vector3(4.2f, 1.12f, z), 0.06f, 0.35f, DarkMetal);
                Box(lab, $"MicroscopeBase_{z}", new Vector3(4.2f, 0.97f, z), new Vector3(0.2f, 0.05f, 0.25f), White, collider: false);
                Box(lab, $"SampleRack_{z}", new Vector3(5.3f, 1.0f, z), new Vector3(0.5f, 0.12f, 0.2f), Frame, collider: false);
                for (int i = 0; i < 5; i++)
                    Cylinder(lab, $"Vial_{z}_{i}", new Vector3(5.12f + i * 0.09f, 1.1f, z), 0.015f, 0.12f, i % 2 == 0 ? LedCyan : LedGreen);
            }
            Box(lab, "SampleFreezer", new Vector3(2.3f, 0.95f, 19.5f), new Vector3(1.2f, 1.9f, 0.6f), White);
            Box(lab, "FreezerDisplay", new Vector3(2.3f, 1.55f, 19.19f), new Vector3(0.4f, 0.12f, 0.02f), LedCyan, collider: false);
            Box(lab, "FumeHood", new Vector3(6.8f, 1.0f, 19.5f), new Vector3(1.6f, 2.0f, 0.7f), Metal);
            Box(lab, "FumeHoodGlass", new Vector3(6.8f, 1.2f, 19.14f), new Vector3(1.4f, 0.8f, 0.02f), Glass, collider: false);
            CeilingPanel(lights, "Science", new Vector3(5.5f, 0, 15f), cool, 1.9f);
            Readable(inter, "Readable_RadarCalibration", new Vector3(8.8f, 1.01f, 13.6f),
                "Ice-penetrating radar: calibration summary",
                "Sounding bands: 9 MHz / 60 MHz (dual)\nTarget: Conamara Chaos region, Europa\n" +
                "Depth objective: full ice shell (estimated 15-25 km)\n\nCalibration: PASS\n\n" +
                "Deep-return gain profile set per COMMAND DIRECTIVE 7.\n(Directive 7 reference document: RESTRICTED)",
                setsFlag: "read_directive7");
            Tone(audio, "Tone_Science", new Vector3(5f, 1.2f, 19f), AudioAssetGenerator.ElectricalHum, 0.25f, 7f);

            // ---------- Comms (z 20..28): server racks with blinking status LEDs
            var c = Group(props, "Comms");
            Box(c, "Comms_Console", new Vector3(8.8f, 0.5f, 24f), new Vector3(0.8f, 1f, 3f), DarkMetal);
            Box(c, "Comms_Screen", new Vector3(9.25f, 1.3f, 24f), new Vector3(0.08f, 0.6f, 3f), ScreenCyan, collider: false);
            Box(c, "Comms_MapScreen", new Vector3(5.5f, 1.6f, 20.12f), new Vector3(2.6f, 1.0f, 0.03f), ScreenAmber, collider: false);
            for (int r = 0; r < 4; r++)
            {
                float x = 3.0f + r * 0.85f;
                Box(c, $"ServerRack_{r}", new Vector3(x, 1.0f, 27.5f), new Vector3(0.7f, 2.0f, 0.7f), DarkMetal);
                for (int k = 0; k < 8; k++)
                {
                    var led = Box(c, $"RackLED_{r}_{k}", new Vector3(x - 0.2f + (k % 4) * 0.12f, 0.4f + (k / 4) * 0.9f + 0.3f, 27.14f), new Vector3(0.04f, 0.03f, 0.01f), k % 3 == 0 ? LedAmber : LedGreen, collider: false);
                    led.isStatic = false;
                    var blink = led.AddComponent<BlinkingLight>();
                    Kit.Set(blink, "mode", (int)BlinkingLight.Mode.Random);
                    Kit.Set(blink, "color", k % 3 == 0 ? new Color(1f, 0.6f, 0.1f) : new Color(0.3f, 1f, 0.4f));
                    Kit.Set(blink, "period", 0.6f + k * 0.07f);
                }
            }
            Box(c, "Comms_AntennaRack", new Vector3(7.3f, 1.1f, 27.6f), new Vector3(1.4f, 2.2f, 0.6f), Frame);
            CeilingPanel(lights, "Comms", new Vector3(5.5f, 0, 24f), cool, 1.6f);
            Readable(inter, "Readable_UplinkQueue", new Vector3(8.8f, 1.01f, 22.9f),
                "Deep-space uplink queue",
                "Earth to Meridian one-way light time: 21 min 40 s (current)\n\n" +
                "Queued downlinks: 3\n  Telemetry bundle 4471\n  Crew personal messages (5)\n  Science: radar calibration set\n\n" +
                "Received: 1 new personal message for OKAFOR, S.\n  From: Okafor-Reid, A.\n  Length: 4 min 51 s");
            Tone(audio, "Tone_Comms", new Vector3(5f, 1f, 27f), AudioAssetGenerator.ElectricalHum, 0.28f, 6f);

            // ---------- Airlock (z 28..38): suits, oxygen, warning beacon
            var a = Group(props, "Airlock");
            for (int i = 0; i < 3; i++)
            {
                var suit = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                suit.name = $"EVA_Suit_{i + 1}";
                suit.transform.SetParent(a, false);
                suit.transform.position = new Vector3(3.5f + i * 1.5f, 1f, 37.2f);
                suit.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
                suit.GetComponent<Renderer>().sharedMaterial = White;
                suit.isStatic = true;
                Sphere(a, $"EVA_Helmet_{i + 1}", new Vector3(3.5f + i * 1.5f, 2.15f, 37.2f), 0.2f, Glass);
                Box(a, $"SuitRack_{i}", new Vector3(3.5f + i * 1.5f, 2.45f, 37.55f), new Vector3(0.8f, 0.06f, 0.2f), Frame, collider: false);
            }
            foreach (float z in new[] { 29.0f, 29.6f })
                Cylinder(a, $"O2Tank_{z}", new Vector3(2.0f, 0.7f, z), 0.18f, 1.4f, Orange, collider: true);
            Sign(a, "O2", new Vector3(1.66f, 1.6f, 29.3f), Vector3.right, 0.4f, Color.white);
            Box(a, "Airlock_Bench", new Vector3(5.5f, 0.23f, 30.0f), new Vector3(2.2f, 0.45f, 0.5f), Frame);
            Box(a, "Airlock_WarningFloor", new Vector3(8.7f, 0.005f, 33f), new Vector3(1.2f, 0.01f, 2f), Hazard, collider: false);
            var beacon = Cylinder(a, "Beacon", new Vector3(9.25f, 2.45f, 33f), 0.08f, 0.14f, LedAmber);
            beacon.isStatic = false;
            var bl = beacon.AddComponent<BlinkingLight>();
            Kit.Set(bl, "mode", (int)BlinkingLight.Mode.Pulse);
            Kit.Set(bl, "color", new Color(1f, 0.55f, 0.1f));
            Kit.Set(bl, "period", 1.6f);
            Sign(a, "PRESSURE HATCH - SUIT UP", new Vector3(9.36f, 2.47f, 34.6f), Vector3.left, 1.6f, new Color(1f, 0.6f, 0.2f));
            CeilingPanel(lights, "Airlock", new Vector3(5.5f, 0, 33f), cool, 1.5f);
            Tone(audio, "Tone_Airlock", new Vector3(5.5f, 2.4f, 33f), AudioAssetGenerator.Ventilation, 0.3f, 7f);
        }

        // ------------------------------------------------------------------ observation deck

        static void BuildObservationDeck(Transform shell, Transform props, Transform lights)
        {
            const float z = 50f, w0 = -7f, w1 = 7f, sill = 0.4f, head = 2.5f;
            Block(shell, "ObsWall_W", new Vector3(-9.5f, 0, z - T / 2), new Vector3(w0, H, z + T / 2), Wall);
            Block(shell, "ObsWall_E", new Vector3(w1, 0, z - T / 2), new Vector3(9.5f, H, z + T / 2), Wall);
            Block(shell, "ObsWall_Sill", new Vector3(w0, 0, z - T / 2), new Vector3(w1, sill, z + T / 2), Frame);
            Block(shell, "ObsWall_Head", new Vector3(w0, head, z - T / 2), new Vector3(w1, H, z + T / 2), Frame);
            foreach (float x in new[] { -3.5f, 0f, 3.5f })
                Block(shell, $"ObsMullion_{x}", new Vector3(x - 0.08f, sill, z - 0.1f), new Vector3(x + 0.08f, head, z + 0.1f), Frame);

            var glass = new GameObject("WindowBarrier");
            glass.transform.SetParent(shell, false);
            glass.transform.position = new Vector3(0, (sill + head) / 2f, z);
            glass.AddComponent<BoxCollider>().size = new Vector3(w1 - w0, head - sill, 0.1f);

            Box(props, "Obs_Bench_W", new Vector3(-3f, 0.225f, 44f), new Vector3(3f, 0.45f, 0.6f), Fabric);
            Box(props, "Obs_Bench_E", new Vector3(3f, 0.225f, 44f), new Vector3(3f, 0.45f, 0.6f), Fabric);
            Box(props, "Obs_FloorLED", new Vector3(0f, 0.004f, 48.6f), new Vector3(13.5f, 0.008f, 0.04f), LedCyan, collider: false);

            RoomLight(lights, "Light_Obs_W", new Vector3(-6f, 2.6f, 41f), warm, 0.5f / RoomLightBoost, 6f);
            RoomLight(lights, "Light_Obs_E", new Vector3(6f, 2.6f, 41f), warm, 0.5f / RoomLightBoost, 6f);
            var jl = new GameObject("Light_JupiterGlow").AddComponent<Light>();
            jl.transform.SetParent(lights, false);
            jl.type = LightType.Spot;
            jl.transform.SetPositionAndRotation(new Vector3(0, 2.3f, 49.6f), Quaternion.Euler(25f, 180f, 0f));
            jl.color = new Color(1f, 0.74f, 0.5f);
            jl.intensity = 3f;
            jl.range = 16f;
            jl.spotAngle = 110f;
            jl.shadows = LightShadows.None;
        }

        static void BuildJupiter()
        {
            var tex = JupiterTextureGenerator.Generate();
            const string matPath = Kit.MaterialFolder + "/MER_JupiterPlaceholder.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(mat);

            // ~80 degrees across from the observation deck: Jupiter overfills the window, as in Chapter 3.
            var jupiter = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            jupiter.name = "Jupiter_Placeholder";
            Object.DestroyImmediate(jupiter.GetComponent<Collider>());
            // Offset to the right so the planet's limb is visible against black space on the left.
            jupiter.transform.SetPositionAndRotation(new Vector3(95f, 30f, 420f), Quaternion.Euler(0f, 150f, 3f));
            jupiter.transform.localScale = Vector3.one * 520f;
            var r = jupiter.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Interior practical light. One multiplier tunes overall room brightness during grey-boxing.</summary>
        const float RoomLightBoost = 1.35f;

        static Light RoomLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
            => Point(parent, name, position, color, intensity * RoomLightBoost, range);

        /// <summary>Two glowing ceiling panels plus the room's point light. Returns the light (for switches).</summary>
        static Light CeilingPanel(Transform lights, string room, Vector3 center, Color color, float intensity)
        {
            var mat = color.r > color.b ? WarmPanel : LightPanel;
            Box(lights, $"CeilingPanel_{room}_A", new Vector3(center.x - 1.4f, H - 0.02f, center.z), new Vector3(1.6f, 0.03f, 0.6f), mat, collider: false);
            Box(lights, $"CeilingPanel_{room}_B", new Vector3(center.x + 1.4f, H - 0.02f, center.z), new Vector3(1.6f, 0.03f, 0.6f), mat, collider: false);
            return RoomLight(lights, $"Light_{room}", new Vector3(center.x, 2.55f, center.z), color, intensity, 8f);
        }

        static Transform Group(Transform parent, string name)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            return g;
        }

        static void Readable(Transform parent, string name, Vector3 position, string title, string body, string setsFlag = null)
        {
            var go = Kit.Box(parent, name, position, new Vector3(0.3f, 0.02f, 0.4f), paperMat);
            go.isStatic = false;
            var readable = go.AddComponent<Readable>();
            readable.SetContent(title, body, setsFlag);
            EditorUtility.SetDirty(readable);
        }

        static void Tone(Transform parent, string name, Vector3 position, string clipPath, float volume, float maxDistance)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            Kit.Audio3D(go, AudioAssetGenerator.Load(clipPath), 1.2f, maxDistance);
            Kit.Set(go.AddComponent<RoomTone>(), "volume", volume);
        }

        // ------------------------------------------------------------------ crew and story

        const string DialogueFolder = "Assets/_Project/Narrative/Dialogue/Chapter02";
        const string ConvGalley = "ch02_galley_luca_hana";
        const string ConvMara = "ch02_workshop_mara";
        const string ConvAshford = "ch02_command_ashford";
        const string ConvObservation = "ch02_observation_radio";

        static TextAsset Dialogue(string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>($"{DialogueFolder}/{name}.txt");
            if (asset == null) Debug.LogError($"[MeridianGreyboxBuilder] Missing dialogue {name}");
            return asset;
        }

        static void BuildStory(GameObject rig)
        {
            new GameObject("GameSession").AddComponent<Core.GameSession>();

            var dialogue = rig.GetComponentInChildren<DialogueManager>();
            var crew = new GameObject("Crew").transform;

            // Each faces roughly toward their room's door, so the player sees faces on entering.
            Crew(crew, "LUCA", new Vector3(-7.6f, 0f, 17.2f), 110f, new Color(0.85f, 0.6f, 0.25f), dialogue, Dialogue(ConvGalley));
            Crew(crew, "HANA", new Vector3(-4.0f, 0f, 17.4f), 150f, new Color(0.45f, 0.7f, 0.45f), dialogue, Dialogue(ConvGalley));
            Crew(crew, "MARA", new Vector3(-7.7f, 0f, 34.5f), 100f, new Color(0.3f, 0.55f, 0.7f), dialogue, Dialogue(ConvMara));
            Crew(crew, "ASHFORD", new Vector3(4.0f, 0f, 7.3f), -120f, new Color(0.75f, 0.72f, 0.6f), dialogue, Dialogue(ConvAshford));

            // Radio call on the observation deck, once every crew conversation is done.
            var trig = new GameObject("Trigger_ObservationRadio");
            trig.transform.position = new Vector3(0f, 1.4f, 44f);
            var col = trig.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(19f, 2.8f, 11.5f);
            var dt = trig.AddComponent<DialogueTrigger>();
            Kit.Set(dt, "conversation", Dialogue(ConvObservation));
            Kit.Set(dt, "setsFlagOnEnter", "reached_observation");
            Kit.Set(dt, "dialogue", dialogue);
            var so = new SerializedObject(dt);
            SetStringArray(so.FindProperty("requiresCompleted"), ConvGalley, ConvMara, ConvAshford);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Objectives
            var mission = new GameObject("Mission").AddComponent<MissionManager>();
            Kit.Set(mission, "dialogue", dialogue);
            Kit.Set(mission, "objectiveText", rig.transform.Find("HUD/Objective").GetComponent<UnityEngine.UI.Text>());
            var mso = new SerializedObject(mission);
            var list = mso.FindProperty("objectives");
            list.arraySize = 2;
            var o1 = list.GetArrayElementAtIndex(0);
            o1.FindPropertyRelative("text").stringValue = "Check in with the crew";
            SetStringArray(o1.FindPropertyRelative("requiredConversations"), ConvGalley, ConvMara, ConvAshford);
            SetStringArray(o1.FindPropertyRelative("requiredFlags"));
            var o2 = list.GetArrayElementAtIndex(1);
            o2.FindPropertyRelative("text").stringValue = "Go to the observation deck";
            SetStringArray(o2.FindPropertyRelative("requiredConversations"));
            SetStringArray(o2.FindPropertyRelative("requiredFlags"), "reached_observation");
            mso.ApplyModifiedPropertiesWithoutUndo();
            Kit.Set(mission, "completeText", "Chapter 2 complete");

            // The end of the chapter: after the observation deck radio call, on to the landing on Europa. Chapters 3-4 are not built
            // yet, so a time card bridges the gap. The GameSession carries every Chapter 2 choice into the base.
            var transition = new GameObject("LevelTransition").AddComponent<Core.LevelTransition>();
            Kit.Set(transition, "requiresCompleted", ConvObservation);
            Kit.Set(transition, "nextScene", System.IO.Path.GetFileNameWithoutExtension(EuropaSurfaceBuilder.ScenePath));
            Kit.Set(transition, "cardTitle", "SEVEN MONTHS LATER");
            Kit.Set(transition, "cardSubtitle", "Europa. Conamara Chaos.");
            Kit.Set(transition, "dialogue", dialogue);
        }

        static void Crew(Transform parent, string id, Vector3 position, float yaw, Color color, DialogueManager dialogue, TextAsset conversation)
        {
            var root = new GameObject($"Crew_{id}");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            var col = root.AddComponent<CapsuleCollider>();
            col.height = 1.75f;
            col.radius = 0.3f;
            col.center = new Vector3(0f, 0.875f, 0f);

            // The voice comes from head height so it is positioned where the character's mouth is.
            var voiceGo = new GameObject("Voice");
            voiceGo.transform.SetParent(root.transform, false);
            voiceGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var voice = Kit.Audio3D(voiceGo, null, 1.5f, 16f);
            voice.spatialBlend = 0.85f; // mostly positional, never fully lost when facing away

            if (!CharacterModelImporter.TrySpawnModel(id, root.transform, voice))
                PlaceholderBody(root.transform, id, color);

            var member = root.AddComponent<CrewMember>();
            Kit.Set(member, "characterId", id);
            Kit.Set(member, "idleConversation", Dialogue($"ch02_idle_{id.ToLowerInvariant()}"));
            Kit.Set(member, "voice", voice);
            Kit.Set(member, "dialogue", dialogue);
            Kit.Set(member, "conversations", new[] { conversation });
        }

        static void PlaceholderBody(Transform root, string id, Color color)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root, false);
            body.transform.localPosition = new Vector3(0f, 0.875f, 0f);
            body.transform.localScale = new Vector3(0.55f, 0.875f, 0.45f);
            body.GetComponent<Renderer>().sharedMaterial = Kit.Mat($"MER_Crew_{id}", color);
        }

        static void SetStringArray(SerializedProperty prop, params string[] values)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).stringValue = values[i];
        }
    }
}
