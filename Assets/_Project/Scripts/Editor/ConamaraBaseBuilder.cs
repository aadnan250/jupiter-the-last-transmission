using Jupiter.Combat;
using Jupiter.Creatures;
using Jupiter.World;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Jupiter.EditorTools.GreyboxKit;
using static Jupiter.EditorTools.GreyboxWalls;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Generates the grey-box of Conamara Base (Chapters 6-7 vertical slice), following
    /// docs/preproduction/04-gameplay-and-levels.md §9.3:
    ///
    ///   [Airlock] → [Ready room] → [Corridor A] ─┬─ Med bay / Lab (west)
    ///                                             ├─ Comms / Ashford's office / Safe room (east)
    ///                                             └─ [Corridor B] ─ Galley/quarters, Drill chamber → Ascent tunnel
    ///   Service tunnel loop: Ready room ↔ Corridor B (west side), low ceiling.
    ///
    /// The base is pressurised (sound carries) but in Europa's 0.134 g. The safe room's door blocks the
    /// creature's NavMesh, so it is genuinely safe. One Listener waits, dormant, by the drill bore.
    /// Hiding lockers line every route; cans, wrenches and decoy thumpers can be thrown; Mara's flare pistol
    /// waits on the ready-room bench. The Chapter 6 story layer is added by <see cref="ConamaraChapterSixBuilder"/>.
    /// </summary>
    public static class ConamaraBaseBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/L04_ConamaraBase.unity";
        const string NavMeshPath = "Assets/_Project/Scenes/L04_ConamaraBase_NavMesh.asset";
        const string ProfilePath = "Assets/_Project/Data/Environments/EuropaPressurised.asset";

        const float H = 3f, T = 0.25f, TunnelH = 2.4f;
        static Material floor, wall, ceiling, prop, dark, rimeMat, door, hatch, boreMat, supply, locker, lockerDoor, can, decoyMat;

        public static void Build()
        {
            floor = Mat("CB_Floor", new Color(0.3f, 0.32f, 0.35f), smoothness: 0.35f);
            wall = Mat("CB_Wall", new Color(0.66f, 0.68f, 0.7f));
            ceiling = Mat("CB_Ceiling", new Color(0.45f, 0.47f, 0.5f));
            prop = Mat("CB_Prop", new Color(0.5f, 0.52f, 0.55f));
            dark = Mat("CB_Dark", new Color(0.15f, 0.16f, 0.18f));
            door = Mat("CB_Door", new Color(0.32f, 0.45f, 0.55f), smoothness: 0.4f);
            hatch = Mat("CB_Hatch", new Color(0.9f, 0.72f, 0.1f));
            boreMat = Mat("CB_Bore", new Color(0.01f, 0.01f, 0.015f));
            supply = Mat("CB_Supply", new Color(0.85f, 0.35f, 0.1f));
            locker = Mat("CB_Locker", new Color(0.42f, 0.47f, 0.5f), smoothness: 0.3f);
            lockerDoor = Mat("CB_LockerDoor", new Color(0.34f, 0.39f, 0.43f), smoothness: 0.3f);
            can = Mat("CB_Can", new Color(0.7f, 0.72f, 0.66f), smoothness: 0.6f);
            decoyMat = Mat("CB_Decoy", new Color(0.2f, 0.22f, 0.2f), new Color(0.9f, 0.6f, 0.1f) * 0.6f);
            rimeMat = Mat("CB_Rime", new Color(0.9f, 0.95f, 1f), new Color(0.02f, 0.06f, 0.07f), 0.9f);

            var profile = EuropaPressurisedProfile();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var level = new GameObject("Level").transform;
            var lights = new GameObject("Lights").transform;
            var items = new GameObject("Items").transform;

            BuildShell(level);
            BuildRooms(level, lights, items);
            BuildRime(level);
            BuildLockers(level);

            // Bake the creature's NavMesh from colliders (closed doors block it: the safe room stays safe).
            var navGo = new GameObject("NavMesh");
            var surface = navGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            if (AssetDatabase.LoadAssetAtPath<Object>(NavMeshPath) != null) AssetDatabase.DeleteAsset(NavMeshPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);

            // Throwables are loose physics props: placed after the bake so they never carve the NavMesh.
            BuildThrowables(items);

            // Player, unarmed (Chapter 6 is an investigation), starting in the airlock facing into the base.
            var rig = PlayerRigBuilder.Spawn(new Vector3(0f, 0.05f, 3f), 0f, profile);
            var inv = rig.GetComponent<PlayerInventory>();
            Set(inv, "startWithBoltDriver", false);
            Set(inv, "startingMedPatches", 1);

            // The Listener: dormant beside the bore, facing it.
            var listenerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ListenerBuilder.PrefabPath);
            var listener = (GameObject)PrefabUtility.InstantiatePrefab(listenerPrefab);
            listener.transform.SetPositionAndRotation(new Vector3(-11f, 0f, 55f), Quaternion.Euler(0f, 120f, 0f));

            // Chapter 6 story: Mara, the mail cache, the documents, Ashford's terminal, the palm prints, objectives.
            // The Listener stays hidden behind a story gate until contact (Chapter 7).
            ConamaraChapterSixBuilder.Build(rig, listener, level, lights, items);
            // Chapter 7 story: the confrontation, the eruption, Ashford's door and the safe room.
            ConamaraChapterSevenBuilder.Build(rig, listener, level, lights);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.2f, 0.23f);
            RenderSettings.ambientEquatorColor = new Color(0.12f, 0.13f, 0.15f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.05f, 0.06f);
            var cam = rig.GetComponentInChildren<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            PrefabUtility.RecordPrefabInstancePropertyModifications(cam);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[ConamaraBaseBuilder] Saved {ScenePath}");
        }

        static EnvironmentProfile EuropaPressurisedProfile()
        {
            var p = AssetDatabase.LoadAssetAtPath<EnvironmentProfile>(ProfilePath);
            if (p == null)
            {
                p = ScriptableObject.CreateInstance<EnvironmentProfile>();
                AssetDatabase.CreateAsset(p, ProfilePath);
            }
            // Europa's real gravity (0.134 g), but inside a pressurised habitat: sound carries.
            p.displayName = "EuropaPressurised";
            p.gravity = 1.315f;
            p.walkSpeed = 1.7f;
            p.runSpeed = 3.0f;
            p.crouchSpeed = 0.9f;
            p.jumpHeight = 0.5f;
            p.groundAcceleration = 7f;
            p.airControl = 0.15f;
            p.isVacuum = false;
            p.terminalFallSpeed = 12f;
            EditorUtility.SetDirty(p);
            return p;
        }

        // ------------------------------------------------------------------ shell

        static void BuildShell(Transform p)
        {
            Span(p, "Floor", new Vector3(-16f, -0.2f, -0.5f), new Vector3(15f, 0f, 76f), floor);
            Span(p, "Ceiling", new Vector3(-16f, H, -0.5f), new Vector3(15f, H + 0.2f, 76f), ceiling);
            Span(p, "TunnelCeiling", new Vector3(-15f, TunnelH, 9f), new Vector3(-12.5f, H, 40f), ceiling);
            Span(p, "SpurCeiling", new Vector3(-12.5f, TunnelH, 9f), new Vector3(-6f, H, 11f), ceiling);

            // Airlock x[-4,4] z[0,6]
            WallX(p, "Airlock_S", 0f, -4f, 4f, H, T, wall, new Gap(0f, 1.6f));
            WallZ(p, "Airlock_W", -4f, 0f, 6f, H, T, wall);
            WallZ(p, "Airlock_E", 4f, 0f, 6f, H, T, wall);
            // Ready room x[-6,6] z[6,14]
            WallX(p, "Ready_S", 6f, -6f, 6f, H, T, wall, new Gap(0f, 2f));
            WallZ(p, "Ready_W", -6f, 6f, 14f, H, T, wall, new Gap(10f, 1.8f, TunnelH - 0.1f));
            WallZ(p, "Ready_E", 6f, 6f, 14f, H, T, wall);
            WallX(p, "Ready_N", 14f, -11f, 11f, H, T, wall, new Gap(0f, 3f));
            // Corridor A x[-1.5,1.5] z[14,40] with rooms either side
            // The lab window (z 37) sits in this wall: the Chapter 6 palm prints are on its inside.
            WallZ(p, "CorrA_W", -1.5f, 14f, 40f, H, T, wall, new Gap(20f), new Gap(33f), new Gap(37f, 1.4f, 2.05f));
            Span(p, "LabWindow_Sill", new Vector3(-1.625f, 0f, 36.3f), new Vector3(-1.375f, 0.95f, 37.7f), wall);
            var glass = Box(p, "LabWindow_Glass", new Vector3(-1.5f, 1.5f, 37f), new Vector3(0.04f, 1.1f, 1.4f), dark);
            Object.DestroyImmediate(glass.GetComponent<MeshRenderer>()); // grey-box glass: a collider you can see through
            WallZ(p, "CorrA_E", 1.5f, 14f, 40f, H, T, wall, new Gap(19f), new Gap(27f), new Gap(35f));
            WallX(p, "West_Div", 26f, -11f, -1.5f, H, T, wall);
            WallZ(p, "West_Outer", -11f, 14f, 40f, H, T, wall);
            WallX(p, "East_Div1", 24f, 1.5f, 11f, H, T, wall);
            WallX(p, "East_Div2", 30f, 1.5f, 11f, H, T, wall);
            WallZ(p, "East_Outer", 11f, 14f, 40f, H, T, wall);
            // Corridor B z[40,43] x[-15,14]
            WallX(p, "CorrB_S", 40f, -12.5f, 14f, H, T, wall, new Gap(0f, 3f));
            WallX(p, "CorrB_N", 43f, -15f, 14f, H, T, wall, new Gap(-7f, 2.4f), new Gap(8f, 1.8f));
            WallZ(p, "CorrB_E", 14f, 40f, 43f, H, T, wall);
            // Service tunnel x[-15,-12.5] z[9,43], spur to the ready room z[9,11]
            WallZ(p, "Tunnel_W", -15f, 9f, 61f, H, T, wall);
            WallZ(p, "Tunnel_E", -12.5f, 11f, 40f, H, T, wall);
            WallX(p, "Spur_S", 9f, -15f, -6f, H, T, wall);
            WallX(p, "Spur_N", 11f, -12.5f, -6f, H, T, wall);
            // Galley/quarters x[3,14] z[43,53]
            WallZ(p, "Galley_W", 3f, 43f, 53f, H, T, wall);
            WallZ(p, "Galley_E", 14f, 43f, 53f, H, T, wall);
            WallX(p, "Galley_N", 53f, 3f, 14f, H, T, wall);
            // Drill chamber x[-15,1] z[43,61]
            WallZ(p, "Drill_E", 1f, 43f, 61f, H, T, wall);
            WallX(p, "Drill_N", 61f, -15f, 1f, H, T, wall, new Gap(-7f, 2f));
            // Ascent tunnel x[-8,-6] z[61,75]
            WallZ(p, "Ascent_W", -8f, 61f, 75f, H, T, wall);
            WallZ(p, "Ascent_E", -6f, 61f, 75f, H, T, wall);
            WallX(p, "Ascent_End", 75f, -8f, -6f, H, T, wall, new Gap(-7f, 1.6f));
        }

        // ------------------------------------------------------------------ rooms

        static void BuildRooms(Transform level, Transform lights, Transform items)
        {
            Color cold = new Color(0.8f, 0.9f, 1f), amber = new Color(1f, 0.65f, 0.3f);

            // Doors: outer airlock and ascent hatch locked; safe room closes (and blocks the creature's NavMesh).
            Door(level, "Hatch_AirlockOuter", new Vector3(0f, 0f, 0f), 0f, 1.6f, true, hatch);
            Door(level, "Hatch_AscentVehicle", new Vector3(-7f, 0f, 75f), 0f, 1.6f, true, hatch);
            Door(level, "Door_SafeRoom", new Vector3(1.5f, 0f, 35f), 90f, 1.6f, false, door);

            // Airlock + ready room
            for (int i = 0; i < 3; i++) Box(level, $"SuitRack_{i}", new Vector3(-3.4f, 1f, 1.5f + i * 1.4f), new Vector3(0.5f, 2f, 1f), prop);
            Box(level, "Ready_Bench", new Vector3(3.5f, 0.25f, 10f), new Vector3(0.6f, 0.5f, 4f), dark);
            PlaceSupply(items, Combat.Pickup.Kind.Bolts, 6, new Vector3(3.5f, 0.6f, 9f));
            // Mara's flare pistol on the bench, three flares loaded.
            PlaceSupply(items, Combat.Pickup.Kind.FlarePistol, 3, new Vector3(3.5f, 0.56f, 11f));
            Lamp(lights, "Light_Airlock", new Vector3(0f, 2.7f, 3f), amber, 1.2f, 7f);
            Lamp(lights, "Light_Ready", new Vector3(0f, 2.7f, 10f), cold, 1.6f, 9f);

            // Corridor A
            for (float z = 17f; z < 40f; z += 6f) Lamp(lights, $"Light_CorrA_{z}", new Vector3(0f, 2.75f, z), cold, 1.3f, 6.5f);

            // Med bay x[-11,-1.5] z[14,26]
            Box(level, "Med_Bed1", new Vector3(-8.5f, 0.4f, 17f), new Vector3(1f, 0.8f, 2.1f), prop);
            Box(level, "Med_Bed2", new Vector3(-8.5f, 0.4f, 21.5f), new Vector3(1f, 0.8f, 2.1f), prop);
            Box(level, "Med_Cabinet", new Vector3(-5.5f, 1f, 25.4f), new Vector3(3f, 2f, 0.6f), prop);
            PlaceSupply(items, Combat.Pickup.Kind.MedPatch, 1, new Vector3(-5f, 2.05f, 25.3f));
            PlaceSupply(items, Combat.Pickup.Kind.MedPatch, 1, new Vector3(-6f, 2.05f, 25.3f));
            Lamp(lights, "Light_Med", new Vector3(-6f, 2.7f, 20f), cold, 1.6f, 8f);

            // Lab x[-11,-1.5] z[26,40]
            Box(level, "Lab_Bench1", new Vector3(-6f, 0.47f, 30f), new Vector3(5f, 0.95f, 1f), prop);
            Box(level, "Lab_Bench2", new Vector3(-6f, 0.47f, 35f), new Vector3(5f, 0.95f, 1f), prop);
            Box(level, "Lab_SampleFreezer", new Vector3(-10.4f, 1f, 38f), new Vector3(0.8f, 2f, 2.5f), dark);
            PlaceSupply(items, Combat.Pickup.Kind.Bolts, 6, new Vector3(-6f, 1.05f, 35f));
            Lamp(lights, "Light_Lab", new Vector3(-6f, 2.7f, 33f), cold, 1.5f, 9f);

            // Comms x[1.5,11] z[14,24]
            Box(level, "Comms_Console", new Vector3(10.4f, 0.5f, 19f), new Vector3(0.8f, 1f, 4f), dark);
            Box(level, "Comms_Rack", new Vector3(6f, 1.1f, 23.4f), new Vector3(3f, 2.2f, 0.6f), dark);
            PlaceSupply(items, Combat.Pickup.Kind.Flares, 2, new Vector3(10.4f, 1.05f, 17.5f));
            Lamp(lights, "Light_Comms", new Vector3(6f, 2.7f, 19f), cold, 1.3f, 8f);

            // Ashford's office x[1.5,11] z[24,30]
            Box(level, "Office_Desk", new Vector3(7f, 0.4f, 27f), new Vector3(2.4f, 0.8f, 1f), prop);
            Lamp(lights, "Light_Office", new Vector3(6f, 2.7f, 27f), amber, 1.1f, 7f);

            // Safe room x[1.5,11] z[30,40]
            Box(level, "Safe_Cot", new Vector3(9f, 0.25f, 37f), new Vector3(2f, 0.5f, 0.9f), prop);
            PlaceSupply(items, Combat.Pickup.Kind.MedPatch, 1, new Vector3(9f, 0.55f, 37f));
            Lamp(lights, "Light_Safe", new Vector3(6f, 2.7f, 35f), amber, 1.2f, 8f);

            // Corridor B, service tunnel (dim, amber emergency strips)
            Lamp(lights, "Light_CorrB_W", new Vector3(-8f, 2.75f, 41.5f), cold, 1.1f, 8f);
            Lamp(lights, "Light_CorrB_E", new Vector3(7f, 2.75f, 41.5f), cold, 1.1f, 8f);
            for (float z = 12f; z < 40f; z += 9f) Lamp(lights, $"Light_Tunnel_{z}", new Vector3(-13.75f, 2.2f, z), amber, 0.7f, 5f);
            Lamp(lights, "Light_Spur", new Vector3(-9f, 2.2f, 10f), amber, 0.7f, 5f);

            // Galley / quarters x[3,14] z[43,53]
            Box(level, "Galley_Table", new Vector3(8.5f, 0.375f, 48f), new Vector3(2.4f, 0.75f, 1.2f), prop);
            Box(level, "Quarters_Bunks", new Vector3(13.4f, 0.6f, 48f), new Vector3(0.9f, 1.2f, 6f), prop);
            Lamp(lights, "Light_Galley", new Vector3(8.5f, 2.7f, 48f), amber, 1.2f, 8f);

            // Drill chamber x[-15,1] z[43,61]: the bore, a railing, the drill rig, one hanging work lamp.
            var bore = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bore.name = "Bore";
            Object.DestroyImmediate(bore.GetComponent<Collider>());
            bore.transform.SetParent(level, false);
            bore.transform.position = new Vector3(-7f, 0.01f, 52f);
            bore.transform.localScale = new Vector3(3.2f, 0.01f, 3.2f);
            bore.GetComponent<Renderer>().sharedMaterial = boreMat;
            bore.isStatic = true;
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                Box(level, $"Railing_{i}", new Vector3(-7f + Mathf.Cos(a) * 2.2f, 0.5f, 52f + Mathf.Sin(a) * 2.2f), new Vector3(0.9f, 1f, 0.1f), hatch)
                    .transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
            }
            Box(level, "DrillRig", new Vector3(-7f, 2.4f, 52f), new Vector3(0.5f, 1.2f, 0.5f), dark, collider: false);
            Box(level, "Drill_Crates", new Vector3(-1f, 0.6f, 58f), new Vector3(2f, 1.2f, 2f), prop);
            Box(level, "Drill_Console", new Vector3(-14.4f, 0.5f, 47f), new Vector3(0.8f, 1f, 3f), dark);
            var lamp = new GameObject("Light_DrillLamp").AddComponent<Light>();
            lamp.transform.SetParent(lights, false);
            lamp.type = LightType.Spot;
            lamp.transform.SetPositionAndRotation(new Vector3(-7f, 2.9f, 52f), Quaternion.Euler(90f, 0f, 0f));
            lamp.color = new Color(0.85f, 0.92f, 1f);
            lamp.intensity = 6f;
            lamp.range = 10f;
            lamp.spotAngle = 95f;
            lamp.shadows = LightShadows.Soft;
            Lamp(lights, "Light_DrillFill", new Vector3(-7f, 2.7f, 46f), cold, 0.5f, 9f);

            // Ascent tunnel
            Lamp(lights, "Light_Ascent", new Vector3(-7f, 2.6f, 68f), amber, 0.9f, 8f);
        }

        static void BuildRime(Transform level)
        {
            var root = new GameObject("Rime").transform;
            root.SetParent(level, false);
            Vector3[] spots = { new Vector3(-10.5f, 0f, 50f), new Vector3(-3.5f, 0f, 54.5f), new Vector3(-7f, 0f, 57f),
                                new Vector3(-13.75f, 0f, 26f), new Vector3(-4f, 0f, 41.5f) };
            for (int i = 0; i < spots.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = $"RimePatch_{i}";
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.transform.SetParent(root, false);
                go.transform.position = spots[i] + Vector3.up * 0.012f;
                go.transform.localScale = new Vector3(2.4f, 0.01f, 2.4f);
                go.GetComponent<Renderer>().sharedMaterial = rimeMat;
                var patch = go.AddComponent<RimePatch>();
                Set(patch, "glow", go.GetComponent<Renderer>());
                Set(patch, "radius", 2.5f);
            }
        }

        // ------------------------------------------------------------------ stealth

        /// <summary>
        /// Hiding lockers on every route the Listener can take, so a chase can always end in one: three in the
        /// ready room, then the service tunnel, corridor B, the galley and the drill chamber.
        /// </summary>
        static void BuildLockers(Transform level)
        {
            var root = new GameObject("Lockers").transform;
            root.SetParent(level, false);
            // Floor position of each locker's centre and the direction its door faces.
            for (int i = 0; i < 3; i++) Locker(root, $"Locker_Ready_{i}", new Vector3(5.47f, 0f, 7f + i * 0.95f), -90f);
            Locker(root, "Locker_Tunnel", new Vector3(-14.47f, 0f, 33f), 90f);
            Locker(root, "Locker_CorrB", new Vector3(2.5f, 0f, 42.47f), 180f);
            Locker(root, "Locker_Galley", new Vector3(3.53f, 0f, 51f), 90f);
            Locker(root, "Locker_Drill", new Vector3(0.47f, 0f, 47f), -90f);
        }

        /// <summary>Cans and a wrench to throw, and two decoy thumpers in the lab and comms.</summary>
        static void BuildThrowables(Transform items)
        {
            PlaceThrowable(items, "can", new Vector3(8.1f, 0.85f, 48.2f), false);
            PlaceThrowable(items, "can", new Vector3(8.9f, 0.85f, 47.8f), false);
            PlaceThrowable(items, "can", new Vector3(3.5f, 0.6f, 12f), false);
            PlaceThrowable(items, "wrench", new Vector3(-4.5f, 1.05f, 30f), false);
            PlaceThrowable(items, "wrench", new Vector3(-1.2f, 1.3f, 58f), false);
            PlaceThrowable(items, "decoy thumper", new Vector3(-7.5f, 1.05f, 35f), true);
            PlaceThrowable(items, "decoy thumper", new Vector3(10.4f, 1.1f, 20.5f), true);
        }

        static void Locker(Transform parent, string name, Vector3 floorPos, float yaw)
        {
            const float w = 0.9f, d = 0.8f, h = 2.1f, t = 0.04f;
            const float slitBottom = 1.56f, slitTop = 1.7f; // vent slit at eye height (1.62 m)

            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(floorPos, Quaternion.Euler(0f, yaw, 0f));

            LocalBox(root.transform, "Back", new Vector3(0f, h / 2f, -d / 2f + t / 2f), new Vector3(w, h, t), locker);
            LocalBox(root.transform, "Side_L", new Vector3(-w / 2f + t / 2f, h / 2f, 0f), new Vector3(t, h, d), locker);
            LocalBox(root.transform, "Side_R", new Vector3(w / 2f - t / 2f, h / 2f, 0f), new Vector3(t, h, d), locker);
            LocalBox(root.transform, "Top", new Vector3(0f, h - t / 2f, 0f), new Vector3(w, t, d), locker);
            LocalBox(root.transform, "Door_Lower", new Vector3(0f, slitBottom / 2f, d / 2f - t / 2f), new Vector3(w, slitBottom, t), lockerDoor);
            LocalBox(root.transform, "Door_Upper", new Vector3(0f, (slitTop + h) / 2f, d / 2f - t / 2f), new Vector3(w, h - slitTop, t), lockerDoor);

            // Targetable from both sides without closing the slit: interaction rays hit triggers, creature sight ignores them.
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, h / 2f, d / 2f + 0.03f);
            trigger.size = new Vector3(w, h, 0.1f);

            var inside = new GameObject("Inside").transform;
            inside.SetParent(root.transform, false);
            inside.localPosition = new Vector3(0f, 0.02f, 0f);
            var exit = new GameObject("Exit").transform;
            exit.SetParent(root.transform, false);
            exit.localPosition = new Vector3(0f, 0.02f, d / 2f + 0.6f);

            var spot = root.AddComponent<HidingSpot>();
            Set(spot, "inside", inside);
            Set(spot, "exit", exit);
            Set(spot, "audioSource", Audio3D(root, null, 1f, 10f));
            Set(spot, "doorClip", CombatAudioGenerator.Load(CombatAudioGenerator.LockerDoor));
        }

        static void LocalBox(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
        }

        static void PlaceThrowable(Transform parent, string displayName, Vector3 pos, bool decoy)
        {
            Vector3 size = decoy ? new Vector3(0.14f, 0.14f, 0.14f)
                : displayName == "wrench" ? new Vector3(0.3f, 0.04f, 0.06f) : new Vector3(0.07f, 0.12f, 0.07f);
            var go = GameObject.CreatePrimitive(decoy || displayName == "wrench" ? PrimitiveType.Cube : PrimitiveType.Cylinder);
            go.name = $"Throwable_{displayName.Replace(' ', '_')}";
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            // Primitive cylinders are 2 units tall: halve Y so the can is the size given.
            go.transform.localScale = displayName == "can" ? new Vector3(size.x, size.y / 2f, size.z) : size;
            go.GetComponent<Renderer>().sharedMaterial = decoy ? decoyMat : can;
            if (go.TryGetComponent<CapsuleCollider>(out var capsule))
            {
                // A capsule rolls forever; a box settles like a can on its side.
                Object.DestroyImmediate(capsule);
                go.AddComponent<BoxCollider>();
            }

            var body = go.AddComponent<Rigidbody>();
            body.mass = decoy ? 1.2f : 0.4f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var throwable = go.AddComponent<Combat.Throwable>();
            throwable.Configure(displayName, decoy, Audio3D(go, null, 1f, 18f),
                CombatAudioGenerator.Load(CombatAudioGenerator.Clatter), CombatAudioGenerator.Load(CombatAudioGenerator.DecoyThump));
            EditorUtility.SetDirty(throwable);
        }

        // ------------------------------------------------------------------ helpers

        internal static SlidingDoor Door(Transform parent, string name, Vector3 pos, float yaw, float width, bool locked, Material mat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Panel";
            panel.transform.SetParent(root.transform, false);
            panel.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            panel.transform.localScale = new Vector3(width, 2.3f, 0.15f);
            panel.GetComponent<Renderer>().sharedMaterial = mat;
            var d = root.AddComponent<SlidingDoor>();
            Set(d, "panel", panel.transform);
            Set(d, "openOffset", new Vector3(width + 0.05f, 0f, 0f));
            Set(d, "locked", locked);
            Set(d, "audioSource", Audio3D(root, null, 1f, 14f));
            Set(d, "openClip", AudioAssetGenerator.Load(AudioAssetGenerator.DoorOpen));
            Set(d, "closeClip", AudioAssetGenerator.Load(AudioAssetGenerator.DoorClose));
            Set(d, "lockedClip", AudioAssetGenerator.Load(AudioAssetGenerator.SwitchClick));
            return d;
        }

        static void PlaceSupply(Transform parent, Combat.Pickup.Kind kind, int amount, Vector3 pos)
        {
            var go = Box(parent, $"Pickup_{kind}", pos, kind == Combat.Pickup.Kind.Bolts ? new Vector3(0.35f, 0.2f, 0.25f) : new Vector3(0.22f, 0.08f, 0.15f), supply);
            go.isStatic = false;
            go.AddComponent<Combat.Pickup>().Configure(kind, amount);
            EditorUtility.SetDirty(go);
        }

        static void Lamp(Transform parent, string name, Vector3 pos, Color c, float intensity, float range) =>
            PointLight(parent, name, pos, c, intensity, range);
    }
}
