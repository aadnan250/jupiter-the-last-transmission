using System.Collections.Generic;
using Jupiter.Audio;
using Jupiter.Narrative;
using Jupiter.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Jupiter.EditorTools.GreyboxKit;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Generates the grey-box interior of the deep-space vessel Meridian's habitat ring (Chapter 2, "Month 4").
    ///
    /// Layout (metres, +Z = forward along the ring):
    ///   central corridor x[-1.5, 1.5] z[0, 38]
    ///   west rooms  x[-9.5,-1.5]: Quarters z0-10, Galley 10-20, Med bay 20-28, Workshop 28-38
    ///   east rooms  x[ 1.5, 9.5]: Command z0-10, Science 10-20, Comms 20-28, Airlock 28-38
    ///   observation deck z[38, 50] with a 14 m window facing Jupiter
    ///
    /// The ring is treated as locally flat: at the 0.6 g design radius (~134 m at 2 rpm) the floor
    /// curvature across a 50 m section is small, so it is left for the art pass.
    /// </summary>
    public static class MeridianGreyboxBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/L02_Meridian.unity";

        const float H = 2.8f;      // ceiling height
        const float T = 0.2f;      // wall thickness
        const float DoorW = 1.4f;
        const float DoorH = 2.2f;

        struct Gap
        {
            public float Center, Width, Height;
            public Gap(float c, float w = DoorW, float h = DoorH) { Center = c; Width = w; Height = h; }
        }

        static Material floorMat, wallMat, ceilingMat, doorMat, propMat, darkMat, screenMat, ledMat, paperMat,
            warningMat, suitMat, frameMat;
        static Color warm = new Color(1f, 0.92f, 0.82f);
        static Color cool = new Color(0.86f, 0.92f, 1f);

        public static void Build()
        {
            floorMat = Mat("MER_Floor", new Color(0.32f, 0.33f, 0.35f), smoothness: 0.3f);
            wallMat = Mat("MER_Wall", new Color(0.72f, 0.73f, 0.74f));
            ceilingMat = Mat("MER_Ceiling", new Color(0.55f, 0.56f, 0.58f));
            doorMat = Mat("MER_Door", new Color(0.3f, 0.42f, 0.55f), smoothness: 0.4f);
            propMat = Mat("MER_Prop", new Color(0.5f, 0.52f, 0.55f));
            darkMat = Mat("MER_Dark", new Color(0.16f, 0.17f, 0.19f));
            screenMat = Mat("MER_Screen", new Color(0.02f, 0.05f, 0.06f), new Color(0.15f, 0.75f, 0.85f) * 1.4f, 0.8f);
            ledMat = Mat("MER_LED", new Color(0.05f, 0.2f, 0.05f), new Color(0.3f, 1f, 0.4f) * 2f);
            paperMat = Mat("MER_Paper", new Color(0.92f, 0.9f, 0.84f));
            warningMat = Mat("MER_Warning", new Color(0.95f, 0.78f, 0.1f));
            suitMat = Mat("MER_Suit", new Color(0.9f, 0.9f, 0.88f));
            frameMat = Mat("MER_WindowFrame", new Color(0.2f, 0.21f, 0.23f), smoothness: 0.5f);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var shell = new GameObject("Shell").transform;
            var props = new GameObject("Props").transform;
            var lights = new GameObject("Lights").transform;
            var audio = new GameObject("Audio").transform;
            var interactables = new GameObject("Interactables").transform;

            BuildShell(shell);
            BuildDoors(interactables);
            BuildRooms(props, lights, audio, interactables);
            BuildObservationDeck(shell, props, lights);
            BuildJupiter();

            // Global ring drone: non-positional, always present, very low.
            var drone = new GameObject("RingDrone");
            drone.transform.SetParent(audio, false);
            var droneSrc = drone.AddComponent<AudioSource>();
            droneSrc.clip = AudioAssetGenerator.Load(AudioAssetGenerator.RingDrone);
            droneSrc.spatialBlend = 0f;
            droneSrc.playOnAwake = false;
            Set(drone.AddComponent<RoomTone>(), "volume", 0.22f);

            // Corridor ventilation
            foreach (float z in new[] { 6f, 19f, 32f })
                Tone(audio, $"Tone_CorridorVent_{z}", new Vector3(0, 2.5f, z), AudioAssetGenerator.Ventilation, 0.18f, 6f);

            // Player starts in the corridor looking toward the observation deck.
            var rig = PlayerRigBuilder.Spawn(new Vector3(0, 0.05f, 3f), 0f, GreyboxTestRoomBuilder.MeridianRing);
            var cam = rig.GetComponentInChildren<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            PrefabUtility.RecordPrefabInstancePropertyModifications(cam); // keep as a scene override

            BuildStory(rig);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.22f, 0.23f, 0.25f);
            RenderSettings.ambientEquatorColor = new Color(0.15f, 0.15f, 0.16f);
            RenderSettings.ambientGroundColor = new Color(0.06f, 0.06f, 0.07f);

            EnsureFolder("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[MeridianGreyboxBuilder] Saved {ScenePath}");
        }

        // ------------------------------------------------------------------ shell

        static void BuildShell(Transform p)
        {
            Span(p, "Floor", new Vector3(-9.6f, -0.2f, -0.1f), new Vector3(9.6f, 0f, 50.1f), floorMat);
            Span(p, "Ceiling", new Vector3(-9.6f, H, -0.1f), new Vector3(9.6f, H + 0.2f, 50.1f), ceilingMat);

            // Outer walls (airlock hatch gap on the east side)
            WallZ(p, "Wall_Outer_W", -9.5f, 0f, 50f);
            WallZ(p, "Wall_Outer_E", 9.5f, 0f, 50f, new Gap(33f));

            // Corridor walls with one door per room
            var doorGaps = new[] { new Gap(5f), new Gap(15f), new Gap(24f), new Gap(33f) };
            WallZ(p, "Wall_Corridor_W", -1.5f, 0f, 38f, doorGaps);
            WallZ(p, "Wall_Corridor_E", 1.5f, 0f, 38f, doorGaps);

            // Room dividers
            foreach (float z in new[] { 10f, 20f, 28f })
            {
                WallX(p, $"Divider_W_{z}", z, -9.5f, -1.5f);
                WallX(p, $"Divider_E_{z}", z, 1.5f, 9.5f);
            }

            // South bulkhead with the (locked) cargo hold door; north bulkhead opens onto the observation deck.
            WallX(p, "Bulkhead_South", 0f, -9.5f, 9.5f, new Gap(0f));
            WallX(p, "Bulkhead_North", 38f, -9.5f, 9.5f, new Gap(0f, 3f, 2.4f));
        }

        /// <summary>Wall running along Z at fixed X, with door-sized gaps and lintels above them.</summary>
        static void WallZ(Transform p, string name, float x, float z0, float z1, params Gap[] gaps)
        {
            float cursor = z0;
            var sorted = new List<Gap>(gaps);
            sorted.Sort((a, b) => a.Center.CompareTo(b.Center));
            int i = 0;
            foreach (var g in sorted)
            {
                float g0 = g.Center - g.Width * 0.5f, g1 = g.Center + g.Width * 0.5f;
                if (g0 > cursor) Span(p, $"{name}_{i++}", new Vector3(x - T / 2, 0, cursor), new Vector3(x + T / 2, H, g0), wallMat);
                Span(p, $"{name}_Lintel_{i++}", new Vector3(x - T / 2, g.Height, g0), new Vector3(x + T / 2, H, g1), wallMat);
                cursor = g1;
            }
            if (z1 > cursor) Span(p, $"{name}_{i}", new Vector3(x - T / 2, 0, cursor), new Vector3(x + T / 2, H, z1), wallMat);
        }

        /// <summary>Wall running along X at fixed Z, with gaps.</summary>
        static void WallX(Transform p, string name, float z, float x0, float x1, params Gap[] gaps)
        {
            float cursor = x0;
            var sorted = new List<Gap>(gaps);
            sorted.Sort((a, b) => a.Center.CompareTo(b.Center));
            int i = 0;
            foreach (var g in sorted)
            {
                float g0 = g.Center - g.Width * 0.5f, g1 = g.Center + g.Width * 0.5f;
                if (g0 > cursor) Span(p, $"{name}_{i++}", new Vector3(cursor, 0, z - T / 2), new Vector3(g0, H, z + T / 2), wallMat);
                Span(p, $"{name}_Lintel_{i++}", new Vector3(g0, g.Height, z - T / 2), new Vector3(g1, H, z + T / 2), wallMat);
                cursor = g1;
            }
            if (x1 > cursor) Span(p, $"{name}_{i}", new Vector3(cursor, 0, z - T / 2), new Vector3(x1, H, z + T / 2), wallMat);
        }

        // ------------------------------------------------------------------ doors

        static void BuildDoors(Transform p)
        {
            string[] west = { "Quarters", "Galley", "MedBay", "Workshop" };
            string[] east = { "Command", "Science", "Comms", "Airlock" };
            float[] zs = { 5f, 15f, 24f, 33f };
            for (int i = 0; i < zs.Length; i++)
            {
                Door(p, $"Door_{west[i]}", new Vector3(-1.5f, 0, zs[i]), 90f, false);
                Door(p, $"Door_{east[i]}", new Vector3(1.5f, 0, zs[i]), 90f, false);
            }
            Door(p, "Door_CargoHold", new Vector3(0, 0, 0f), 0f, true);
            Door(p, "Hatch_AirlockOuter", new Vector3(9.5f, 0, 33f), 90f, true);
        }

        static SlidingDoor Door(Transform parent, string name, Vector3 position, float yaw, bool locked)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));

            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Panel";
            panel.transform.SetParent(root.transform, false);
            panel.transform.localPosition = new Vector3(0, DoorH / 2f, 0);
            panel.transform.localScale = new Vector3(DoorW, DoorH, 0.12f);
            panel.GetComponent<Renderer>().sharedMaterial = locked ? warningMat : doorMat;

            var door = root.AddComponent<SlidingDoor>();
            Set(door, "panel", panel.transform);
            Set(door, "openOffset", new Vector3(DoorW + 0.05f, 0f, 0f));
            Set(door, "locked", locked);
            Set(door, "audioSource", Audio3D(root, null, 1f, 14f));
            Set(door, "openClip", AudioAssetGenerator.Load(AudioAssetGenerator.DoorOpen));
            Set(door, "closeClip", AudioAssetGenerator.Load(AudioAssetGenerator.DoorClose));
            Set(door, "lockedClip", AudioAssetGenerator.Load(AudioAssetGenerator.SwitchClick));
            return door;
        }

        // ------------------------------------------------------------------ rooms

        static void BuildRooms(Transform props, Transform lights, Transform audio, Transform inter)
        {
            // Corridor lights
            foreach (float z in new[] { 3f, 9.5f, 16f, 22.5f, 29f, 35.5f })
                RoomLight(lights, $"Light_Corridor_{z}", new Vector3(0, 2.6f, z), cool, 1.4f, 6f);

            // --- Quarters (west, z 0-10)
            for (int i = 0; i < 4; i++)
                Box(props, $"Bunk_{i + 1}", new Vector3(-8.95f, 0.3f, 1.3f + i * 2.3f), new Vector3(0.9f, 0.6f, 2.0f), propMat);
            Box(props, "Lockers_Quarters", new Vector3(-4.5f, 1f, 9.55f), new Vector3(3f, 2f, 0.5f), darkMat);
            var quartersLight = RoomLight(lights, "Light_Quarters", new Vector3(-5.5f, 2.6f, 5f), warm, 1.8f, 8f);
            var sw = Box(inter, "LightSwitch_Quarters", new Vector3(-1.63f, 1.3f, 6.2f), new Vector3(0.06f, 0.25f, 0.16f), warningMat);
            sw.isStatic = false;
            var ls = sw.AddComponent<LightSwitch>();
            Set(ls, "lights", new[] { quartersLight });
            Set(ls, "audioSource", Audio3D(sw, null, 1f, 8f));
            Set(ls, "clickClip", AudioAssetGenerator.Load(AudioAssetGenerator.SwitchClick));
            Readable(inter, "Readable_ZaraDrawing", new Vector3(-8.95f, 0.62f, 8.2f),
                "Crayon drawing, taped inside Sam's locker",
                "A drawing of Jupiter in orange and brown crayon. Someone has given it a big smiling face, with the red spot for a nose.\n\n" +
                "Across the bottom, in careful capital letters:\n\n" +
                "FOR UNCLE SAM.\nBRING IT BACK AND TELL ME IF IT HAS A FACE.\n\nLOVE ZARA (8)");
            Tone(audio, "Tone_Quarters", new Vector3(-5.5f, 2.4f, 5f), AudioAssetGenerator.Ventilation, 0.22f, 7f);

            // --- Galley (west, z 10-20)
            Box(props, "Galley_Table", new Vector3(-5.5f, 0.375f, 15f), new Vector3(1.2f, 0.75f, 2.4f), propMat);
            Box(props, "Galley_Bench_W", new Vector3(-6.6f, 0.225f, 15f), new Vector3(0.4f, 0.45f, 2.4f), darkMat);
            Box(props, "Galley_Bench_E", new Vector3(-4.4f, 0.225f, 15f), new Vector3(0.4f, 0.45f, 2.4f), darkMat);
            Box(props, "Galley_Counter", new Vector3(-9.05f, 0.475f, 15.5f), new Vector3(0.7f, 0.95f, 5f), propMat);
            Box(props, "Galley_Fridge", new Vector3(-9.05f, 0.95f, 11.0f), new Vector3(0.7f, 1.9f, 0.8f), suitMat);
            RoomLight(lights, "Light_Galley", new Vector3(-5.5f, 2.6f, 15f), warm, 2f, 8f);
            Readable(inter, "Readable_GalleyRota", new Vector3(-5.5f, 0.76f, 14.4f),
                "Galley rota, Month 4",
                "MON      Ferreira\nTUE      Okafor\nWED      Lindqvist\nTHU      Mori\nFRI      Ashford\nSAT/SUN  whoever lost at cards\n\n" +
                "MUGS: everyone. Every time. I mean it.   (H.M.)\n\n" +
                "Handwritten underneath:\nBossa nova is NOT \"elevator music with a tan.\"   (L.F.)\n\n" +
                "Underneath that, in a different pen:\nIt is a little bit.   (M.L.)");
            Tone(audio, "Tone_GalleyFridge", new Vector3(-8.8f, 1f, 11f), AudioAssetGenerator.ElectricalHum, 0.25f, 5f);

            // --- Med bay (west, z 20-28)
            Box(props, "Med_Bed", new Vector3(-6f, 0.4f, 24f), new Vector3(1.0f, 0.8f, 2.1f), suitMat);
            Box(props, "Med_MonitorScreen", new Vector3(-7.4f, 1.4f, 24f), new Vector3(0.08f, 0.5f, 0.7f), screenMat);
            Box(props, "Med_MonitorStand", new Vector3(-7.4f, 0.575f, 24f), new Vector3(0.12f, 1.15f, 0.12f), darkMat, collider: false);
            Box(props, "Med_Cabinet", new Vector3(-4f, 1f, 27.65f), new Vector3(3f, 2f, 0.5f), suitMat);
            RoomLight(lights, "Light_MedBay", new Vector3(-5.5f, 2.6f, 24f), cool, 2f, 7f);
            Readable(inter, "Readable_MedicalNotes", new Vector3(-6f, 0.81f, 25.3f),
                "Crew medical, weekly summary",
                "OKAFOR: sleep 5.1 h average. Again. Talk to him.\n" +
                "LINDQVIST: refused bone density scan (third time). Rescheduled. Again.\n" +
                "FERREIRA: fine. Annoyingly fine.\n" +
                "ASHFORD: blood pressure elevated. Says it's the coffee.\n" +
                "MORI: physician, heal thyself. (Fine.)\n\n(H.M.)");
            Tone(audio, "Tone_MedMonitor", new Vector3(-7.2f, 1.4f, 24f), AudioAssetGenerator.MedicalMonitor, 0.3f, 6f);

            // --- Workshop (west, z 28-38)
            Box(props, "Workshop_Bench", new Vector3(-9.0f, 0.475f, 33f), new Vector3(0.9f, 0.95f, 4f), propMat);
            Box(props, "Workshop_Shelves", new Vector3(-5f, 1.1f, 37.6f), new Vector3(4f, 2.2f, 0.6f), darkMat);
            Box(props, "Workshop_ToolCart", new Vector3(-5f, 0.45f, 31f), new Vector3(0.6f, 0.9f, 1f), warningMat);
            RoomLight(lights, "Light_Workshop", new Vector3(-5.5f, 2.6f, 33f), warm, 2.2f, 8f);
            Readable(inter, "Readable_WorkOrder", new Vector3(-9.0f, 0.96f, 32f),
                "Work order WO-117: Lander Wren",
                "System: aft ramp actuator\nStatus: intermittent stall at 40-45% travel\nCause: suspected thermal binding in the screw jack\n\n" +
                "Action: replace bearing set before Europa descent. Spare is in bin C-4.\n\n" +
                "NOTE: Do NOT cycle the ramp manually above 40% until this is fixed. Yes, Luca, this means you.\n\n" +
                "M. Lindqvist, Flight Engineer");
            Tone(audio, "Tone_Workshop", new Vector3(-8.5f, 1f, 34f), AudioAssetGenerator.ElectricalHum, 0.3f, 7f);

            // --- Command (east, z 0-10)
            Box(props, "Command_ConsoleFront", new Vector3(5.5f, 0.5f, 8.8f), new Vector3(3f, 1f, 0.8f), darkMat);
            Box(props, "Command_ScreenFront", new Vector3(5.5f, 1.3f, 9.25f), new Vector3(3f, 0.6f, 0.08f), screenMat);
            Box(props, "Command_ConsoleSide", new Vector3(8.8f, 0.5f, 5.5f), new Vector3(0.8f, 1f, 3f), darkMat);
            Box(props, "Command_ScreenSide", new Vector3(9.25f, 1.3f, 5.5f), new Vector3(0.08f, 0.6f, 3f), screenMat);
            Box(props, "Command_Chair", new Vector3(5.5f, 0.45f, 6.5f), new Vector3(0.6f, 0.9f, 0.6f), propMat);
            RoomLight(lights, "Light_Command", new Vector3(5.5f, 2.6f, 5f), cool, 1.6f, 8f);
            Readable(inter, "Readable_CommanderLog", new Vector3(4.6f, 1.01f, 8.8f),
                "Commander's log, Day 118",
                "Radar calibration completed two days ahead of schedule. Okafor is thorough. " +
                "He doesn't ask why we need this much resolution on the deep returns, which I'm grateful for.\n\n" +
                "Lindqvist filed another objection to the power allocation. She wants the hours for lander testing. " +
                "She isn't wrong, and I told her so. The priority stands.\n\n" +
                "Crew morale good. Ferreira has started a card league. Mori is winning.\n\nD. Ashford",
                setsFlag: "read_commander_log");
            Tone(audio, "Tone_Command", new Vector3(5.5f, 1f, 9f), AudioAssetGenerator.ElectricalHum, 0.2f, 7f);

            // --- Science bay (east, z 10-20)
            Box(props, "Science_RadarConsole", new Vector3(8.8f, 0.5f, 15f), new Vector3(0.8f, 1f, 4f), darkMat);
            Box(props, "Science_RadarScreen", new Vector3(9.25f, 1.3f, 15f), new Vector3(0.08f, 0.6f, 4f), screenMat);
            Box(props, "Science_Racks", new Vector3(5f, 1.1f, 19.6f), new Vector3(3f, 2.2f, 0.6f), darkMat);
            Box(props, "Science_RackLEDs", new Vector3(5f, 1.6f, 19.28f), new Vector3(2.8f, 0.05f, 0.02f), ledMat, collider: false);
            RoomLight(lights, "Light_Science", new Vector3(5.5f, 2.6f, 15f), cool, 1.8f, 8f);
            Readable(inter, "Readable_RadarCalibration", new Vector3(8.8f, 1.01f, 13.6f),
                "Ice-penetrating radar: calibration summary",
                "Sounding bands: 9 MHz / 60 MHz (dual)\nTarget: Conamara Chaos region, Europa\n" +
                "Depth objective: full ice shell (estimated 15-25 km)\n\nCalibration: PASS\n\n" +
                "Deep-return gain profile set per COMMAND DIRECTIVE 7.\n(Directive 7 reference document: RESTRICTED)",
                setsFlag: "read_directive7");
            Tone(audio, "Tone_Science", new Vector3(5f, 1.2f, 19f), AudioAssetGenerator.ElectricalHum, 0.25f, 7f);

            // --- Comms (east, z 20-28)
            Box(props, "Comms_Console", new Vector3(8.8f, 0.5f, 24f), new Vector3(0.8f, 1f, 3f), darkMat);
            Box(props, "Comms_Screen", new Vector3(9.25f, 1.3f, 24f), new Vector3(0.08f, 0.6f, 3f), screenMat);
            Box(props, "Comms_AntennaRack", new Vector3(4f, 1.1f, 27.6f), new Vector3(2.5f, 2.2f, 0.6f), darkMat);
            RoomLight(lights, "Light_Comms", new Vector3(5.5f, 2.6f, 24f), cool, 1.6f, 7f);
            Readable(inter, "Readable_UplinkQueue", new Vector3(8.8f, 1.01f, 22.9f),
                "Deep-space uplink queue",
                "Earth to Meridian one-way light time: 21 min 40 s (current)\n\n" +
                "Queued downlinks: 3\n  Telemetry bundle 4471\n  Crew personal messages (5)\n  Science: radar calibration set\n\n" +
                "Received: 1 new personal message for OKAFOR, S.\n  From: Okafor-Reid, A.\n  Length: 4 min 51 s");
            Tone(audio, "Tone_Comms", new Vector3(8.5f, 1f, 24f), AudioAssetGenerator.ElectricalHum, 0.2f, 6f);

            // --- Airlock (east, z 28-38)
            for (int i = 0; i < 3; i++)
            {
                var suit = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                suit.name = $"EVA_Suit_{i + 1}";
                suit.transform.SetParent(props, false);
                suit.transform.position = new Vector3(3.5f + i * 1.5f, 1f, 37.2f);
                suit.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
                suit.GetComponent<Renderer>().sharedMaterial = suitMat;
                suit.isStatic = true;
            }
            Box(props, "Airlock_WarningFloor", new Vector3(8.7f, 0.005f, 33f), new Vector3(1.2f, 0.01f, 2f), warningMat, collider: false);
            RoomLight(lights, "Light_Airlock", new Vector3(5.5f, 2.6f, 33f), cool, 1.5f, 8f);
            Tone(audio, "Tone_Airlock", new Vector3(5.5f, 2.4f, 33f), AudioAssetGenerator.Ventilation, 0.3f, 7f);
        }

        // ------------------------------------------------------------------ observation deck

        static void BuildObservationDeck(Transform shell, Transform props, Transform lights)
        {
            // North wall with a 14 m x 2.1 m window (sill 0.4 m, head 2.5 m)
            const float z = 50f, w0 = -7f, w1 = 7f, sill = 0.4f, head = 2.5f;
            Span(shell, "ObsWall_W", new Vector3(-9.5f, 0, z - T / 2), new Vector3(w0, H, z + T / 2), wallMat);
            Span(shell, "ObsWall_E", new Vector3(w1, 0, z - T / 2), new Vector3(9.5f, H, z + T / 2), wallMat);
            Span(shell, "ObsWall_Sill", new Vector3(w0, 0, z - T / 2), new Vector3(w1, sill, z + T / 2), frameMat);
            Span(shell, "ObsWall_Head", new Vector3(w0, head, z - T / 2), new Vector3(w1, H, z + T / 2), frameMat);
            foreach (float x in new[] { -3.5f, 0f, 3.5f })
                Span(shell, $"ObsMullion_{x}", new Vector3(x - 0.08f, sill, z - 0.1f), new Vector3(x + 0.08f, head, z + 0.1f), frameMat);

            // Invisible barrier across the window so the player cannot walk into space.
            var glass = new GameObject("WindowBarrier");
            glass.transform.SetParent(shell, false);
            glass.transform.position = new Vector3(0, (sill + head) / 2f, z);
            glass.AddComponent<BoxCollider>().size = new Vector3(w1 - w0, head - sill, 0.1f);

            Box(props, "Obs_Bench_W", new Vector3(-3f, 0.225f, 44f), new Vector3(3f, 0.45f, 0.6f), darkMat);
            Box(props, "Obs_Bench_E", new Vector3(3f, 0.225f, 44f), new Vector3(3f, 0.45f, 0.6f), darkMat);

            // Dim interior so the view dominates, plus warm "Jupiter-light" falling through the window.
            PointLight(lights, "Light_Obs_W", new Vector3(-6f, 2.6f, 41f), warm, 0.5f, 6f);
            PointLight(lights, "Light_Obs_E", new Vector3(6f, 2.6f, 41f), warm, 0.5f, 6f);
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
            const string matPath = MaterialFolder + "/MER_JupiterPlaceholder.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(mat);

            // ~80° across from the observation deck: Jupiter overfills the window, as in Chapter 3.
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
            => PointLight(parent, name, position, color, intensity * RoomLightBoost, range);

        static void Readable(Transform parent, string name, Vector3 position, string title, string body, string setsFlag = null)
        {
            var go = Box(parent, name, position, new Vector3(0.3f, 0.02f, 0.4f), paperMat);
            go.isStatic = false;
            var readable = go.AddComponent<Readable>();
            readable.SetContent(title, body, setsFlag);
            EditorUtility.SetDirty(readable);
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

            // Placeholder crew: coloured capsules with a dark visor showing which way they face.
            Crew(crew, "LUCA", new Vector3(-7.6f, 0f, 17.2f), 135f, new Color(0.85f, 0.6f, 0.25f), dialogue,
                "Back to work, Okafor. Some of us fly spaceships.", Dialogue(ConvGalley));
            Crew(crew, "HANA", new Vector3(-4.0f, 0f, 17.4f), -135f, new Color(0.45f, 0.7f, 0.45f), dialogue,
                "Sleep, Sam. Doctor's orders.", Dialogue(ConvGalley));
            Crew(crew, "MARA", new Vector3(-7.7f, 0f, 34.5f), -90f, new Color(0.3f, 0.55f, 0.7f), dialogue,
                "I'm working, Okafor.", Dialogue(ConvMara));
            Crew(crew, "ASHFORD", new Vector3(4.0f, 0f, 7.3f), 20f, new Color(0.75f, 0.72f, 0.6f), dialogue,
                "Carry on, Sam.", Dialogue(ConvAshford));

            // Radio call on the observation deck, once every crew conversation is done.
            var trig = new GameObject("Trigger_ObservationRadio");
            trig.transform.position = new Vector3(0f, 1.4f, 44f);
            var col = trig.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(19f, 2.8f, 11.5f);
            var dt = trig.AddComponent<DialogueTrigger>();
            Set(dt, "conversation", Dialogue(ConvObservation));
            Set(dt, "setsFlagOnEnter", "reached_observation");
            Set(dt, "dialogue", dialogue);
            var so = new SerializedObject(dt);
            SetStringArray(so.FindProperty("requiresCompleted"), ConvGalley, ConvMara, ConvAshford);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Objectives
            var mission = new GameObject("Mission").AddComponent<MissionManager>();
            Set(mission, "dialogue", dialogue);
            Set(mission, "objectiveText", rig.transform.Find("HUD/Objective").GetComponent<UnityEngine.UI.Text>());
            Set(mission, "completeText", "Chapter 2 prototype complete");
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
        }

        static void Crew(Transform parent, string id, Vector3 position, float yaw, Color color, DialogueManager dialogue, string idle, TextAsset conversation)
        {
            var root = new GameObject($"Crew_{id}");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            var col = root.AddComponent<CapsuleCollider>();
            col.height = 1.75f;
            col.radius = 0.3f;
            col.center = new Vector3(0f, 0.875f, 0f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.875f, 0f);
            body.transform.localScale = new Vector3(0.55f, 0.875f, 0.45f);
            body.GetComponent<Renderer>().sharedMaterial = Mat($"MER_Crew_{id}", color);

            var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Visor";
            Object.DestroyImmediate(visor.GetComponent<Collider>());
            visor.transform.SetParent(root.transform, false);
            visor.transform.localPosition = new Vector3(0f, 1.52f, 0.2f);
            visor.transform.localScale = new Vector3(0.32f, 0.12f, 0.08f);
            visor.GetComponent<Renderer>().sharedMaterial = darkMat;

            var member = root.AddComponent<CrewMember>();
            Set(member, "characterId", id);
            Set(member, "idleLine", idle);
            Set(member, "dialogue", dialogue);
            Set(member, "conversations", new[] { conversation });
        }

        static void SetStringArray(SerializedProperty prop, params string[] values)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        static void Tone(Transform parent, string name, Vector3 position, string clipPath, float volume, float maxDistance)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            Audio3D(go, AudioAssetGenerator.Load(clipPath), 1.2f, maxDistance);
            Set(go.AddComponent<RoomTone>(), "volume", volume);
        }
    }
}
