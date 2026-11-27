using Jupiter.Narrative;
using Jupiter.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Jupiter.EditorTools.ShipKit;
using Kit = Jupiter.EditorTools.GreyboxKit;
using Six = Jupiter.EditorTools.ConamaraChapterSixBuilder;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Chapter 1, "Goodbye, Earth": Cabo Lumen Spaceport at dawn, on the Atlantic. Sam starts in the glass family
    /// lounge with Ada and Zara (Mara alone at the far window), says goodbye, walks the long gantry out to the launch
    /// tower under a huge white rocket, and is strapped into the capsule for the launch. The sea, the wind and the
    /// warm low sun are everything the rest of the game takes away.
    /// </summary>
    public static class CaboLumenBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/L01_CaboLumen.unity";
        const string Folder = "Assets/_Project/Narrative/Dialogue/Chapter01";
        static readonly Vector3 CapsulePos = new Vector3(160f, 70f, 20f);

        [MenuItem("Jupiter/Rebuild Chapter 1 (Cabo Lumen)")]
        public static void BuildMenu() { Build(); AssetDatabase.SaveAssets(); }

        static TextAsset Dialogue(string name)
        {
            var a = AssetDatabase.LoadAssetAtPath<TextAsset>($"{Folder}/{name}.txt");
            if (a == null) Debug.LogError($"[CaboLumenBuilder] Missing dialogue {name}");
            return a;
        }

        public static void Build()
        {
            GreyboxTestRoomBuilder.BuildProfiles();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var world = new GameObject("World").transform;

            EarthKit.SkyAndSun("CL", 8f, 75f, new Color(1f, 0.72f, 0.48f), 1.6f, new Color(0.55f, 0.6f, 0.75f), new Color(0.85f, 0.72f, 0.62f));
            EarthKit.Ocean(world, -2.5f);
            EarthKit.SeaAndWind(world, 0.55f);

            var concrete = Kit.Mat("CL_Concrete", new Color(0.62f, 0.6f, 0.57f), smoothness: 0.15f);
            var glass = Kit.Mat("CL_Glass", new Color(0.55f, 0.7f, 0.78f), new Color(0.03f, 0.05f, 0.06f), 0.95f);
            var hull = Kit.Mat("CL_RocketWhite", new Color(0.9f, 0.9f, 0.88f), smoothness: 0.5f);
            Kit.Box(world, "Apron", new Vector3(0f, -0.25f, 18f), new Vector3(46f, 0.5f, 80f), concrete);
            Kit.Box(world, "Seawall", new Vector3(0f, -1.5f, 18f), new Vector3(48f, 2f, 82f), Kit.Mat("CL_Rock", new Color(0.32f, 0.3f, 0.28f)));

            BuildLounge(world, concrete, glass);
            BuildGantryAndRocket(world, hull);
            var seat = BuildCapsule(world);

            // Sam wakes into launch day in the family lounge, facing the window and the rocket.
            var rig = PlayerRigBuilder.Spawn(new Vector3(0f, 0.05f, -16.5f), 0f, GreyboxTestRoomBuilder.Earth);
            var cam = rig.GetComponentInChildren<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.farClipPlane = 3000f;
            PostFxKit.EnableOn(cam);
            PrefabUtility.RecordPrefabInstancePropertyModifications(cam);
            PostFxKit.CinematicVolume("Assets/_Project/Data/Europa/CL_PostFx.asset", 0.1f, 5f, 8f, new Color(1f, 0.95f, 0.88f));

            new GameObject("GameSession").AddComponent<Core.GameSession>();
            var dialogue = rig.GetComponentInChildren<DialogueManager>();
            MusicKit.Add(dialogue, ("", CombatAudioGenerator.MusicTheme, 0.3f), ("at_tower", CombatAudioGenerator.MusicShip, 0.25f));
            var story = new GameObject("Story").transform;

            // The family, and Mara at the far window on her own.
            Six.Crew(story, "Crew_ADA", "ADA", new Vector3(-1.4f, 0f, -11.2f), 175f, new Color(0.7f, 0.5f, 0.6f), dialogue,
                new[] { Dialogue("ch01_family") }, new string[0], Dialogue("ch01_idle_ada"));
            Six.Crew(story, "Crew_ZARA", "ZARA", new Vector3(-0.3f, 0f, -11.6f), 190f, new Color(0.9f, 0.6f, 0.7f), dialogue,
                null, null, Dialogue("ch01_idle_zara"));
            Six.Crew(story, "Crew_MARA", "MARA", new Vector3(4.6f, 0f, -8.9f), 10f, new Color(0.3f, 0.55f, 0.7f), dialogue,
                null, null, Dialogue("ch01_idle_mara"));

            var radio = story.gameObject.AddComponent<RadioOnStart>();
            Kit.Set(radio, "conversation", Dialogue("ch01_mara_radio"));
            Kit.Set(radio, "delaySeconds", 3f);
            Kit.Set(radio, "dialogue", dialogue);

            var tower = Six.Trigger(story, "Trigger_LaunchTower", new Vector3(0f, 1.4f, 38f), new Vector3(8f, 3f, 4f), dialogue);
            Kit.Set(tower, "setsFlagOnEnter", "at_tower");
            var so = new SerializedObject(tower);
            Six.SetStrings(so.FindProperty("requiresCompleted"), "ch01_family");
            so.ApplyModifiedPropertiesWithoutUndo();

            var rumble = new GameObject("Rumble").AddComponent<AudioSource>();
            rumble.transform.SetParent(story, false);
            rumble.clip = CombatAudioGenerator.Load(CombatAudioGenerator.Quake);
            rumble.loop = true;
            rumble.playOnAwake = false;
            rumble.spatialBlend = 0f;
            var launch = story.gameObject.AddComponent<LaunchSequence>();
            Kit.Set(launch, "seat", seat);
            Kit.Set(launch, "launchRadio", Dialogue("ch01_launch_radio"));
            Kit.Set(launch, "rumble", rumble);
            Kit.Set(launch, "dialogue", dialogue);

            var mission = new GameObject("Mission").AddComponent<MissionManager>();
            mission.transform.SetParent(story, false);
            Kit.Set(mission, "dialogue", dialogue);
            Kit.Set(mission, "objectiveText", rig.transform.Find("HUD/Objective").GetComponent<UnityEngine.UI.Text>());
            Kit.Set(mission, "completeText", "Liftoff");
            var mso = new SerializedObject(mission);
            var list = mso.FindProperty("objectives");
            list.arraySize = 3;
            Six.Objective(list.GetArrayElementAtIndex(0), "Say goodbye to Ada and Zara", new[] { "ch01_family" }, new string[0]);
            Six.Objective(list.GetArrayElementAtIndex(1), "Walk out along the gantry to the launch tower", new string[0], new[] { "at_tower" });
            Six.Objective(list.GetArrayElementAtIndex(2), "Launch", new[] { "ch01_launch_radio" }, new string[0]);
            mso.ApplyModifiedPropertiesWithoutUndo();

            var transition = new GameObject("LevelTransition").AddComponent<Core.LevelTransition>();
            Kit.Set(transition, "requiresCompleted", "ch01_launch_radio");
            Kit.Set(transition, "nextScene", System.IO.Path.GetFileNameWithoutExtension(MeridianGreyboxBuilder.ScenePath));
            Kit.Set(transition, "cardTitle", "MONTH 4");
            Kit.Set(transition, "cardSubtitle", "The Meridian, on the way to Jupiter.");
            Kit.Set(transition, "delaySeconds", 2f);
            Kit.Set(transition, "dialogue", dialogue);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[CaboLumenBuilder] Saved {ScenePath}");
        }

        static void BuildLounge(Transform world, Material concrete, Material glass)
        {
            var lounge = new GameObject("FamilyLounge").transform;
            lounge.SetParent(world, false);
            var wall = Kit.Mat("CL_LoungeWall", new Color(0.86f, 0.84f, 0.8f));
            var carpet = Kit.Mat("CL_Carpet", new Color(0.32f, 0.36f, 0.4f));
            Kit.Box(lounge, "Floor", new Vector3(0f, 0.02f, -13f), new Vector3(12f, 0.04f, 10f), carpet);
            Kit.Box(lounge, "Ceiling", new Vector3(0f, 3.2f, -13f), new Vector3(12.4f, 0.2f, 10.4f), wall);
            Kit.Box(lounge, "BackWall", new Vector3(0f, 1.6f, -18.1f), new Vector3(12.4f, 3.2f, 0.2f), wall);
            foreach (float sx in new[] { -1f, 1f })
            {
                Kit.Box(lounge, $"SideWall_{sx}", new Vector3(sx * 6.1f, 1.6f, -13f), new Vector3(0.2f, 3.2f, 10.4f), wall);
                // The window wall facing the pad: tall glass panes between thin mullions, and a door in the middle.
                for (int i = 0; i < 3; i++)
                {
                    float x = sx * (1.5f + i * 1.6f);
                    Kit.Box(lounge, $"Mullion_{sx}_{i}", new Vector3(x, 1.6f, -8f), new Vector3(0.08f, 3.2f, 0.12f), Frame);
                    var pane = Kit.Box(lounge, $"Pane_{sx}_{i}", new Vector3(x + sx * 0.8f, 1.6f, -8f), new Vector3(1.5f, 3.1f, 0.03f), glass);
                    Object.DestroyImmediate(pane.GetComponent<MeshRenderer>()); // clear glass: the collider stays
                }
            }
            for (int i = 0; i < 2; i++)
            {
                Kit.Box(lounge, $"Sofa_{i}", new Vector3(-3.5f + i * 7f, 0.3f, -16.6f), new Vector3(2.4f, 0.6f, 0.9f), Fabric);
                Kit.Box(lounge, $"SofaBack_{i}", new Vector3(-3.5f + i * 7f, 0.75f, -17.0f), new Vector3(2.4f, 0.6f, 0.2f), Fabric);
            }
            Kit.Box(lounge, "CoffeeTable", new Vector3(-2.2f, 0.25f, -14f), new Vector3(1.2f, 0.5f, 0.7f), Wood);
            var drawing = Kit.Box(lounge, "Readable_ZaraDrawing", new Vector3(-2.2f, 0.51f, -14f), new Vector3(0.3f, 0.01f, 0.22f), Kit.Mat("CL_Paper", new Color(0.95f, 0.93f, 0.86f)));
            drawing.isStatic = false;
            var r = drawing.AddComponent<Readable>();
            r.SetContent("Zara's drawing", "A crayon Jupiter, orange and brown stripes, with a big smiling face in the middle.\n\n" +
                "Under it, in careful letters: FOR UNCLE SAM. TAKE IT ALL THE WAY.\n\nOn the back: DOES IT HAVE A FACE? TELL ME.", "took_zara_drawing");
            EditorUtility.SetDirty(r);
            Point(lounge, "LoungeLight_A", new Vector3(-2.5f, 2.9f, -13f), new Color(1f, 0.85f, 0.7f), 1.8f, 8f);
            Point(lounge, "LoungeLight_B", new Vector3(2.5f, 2.9f, -13f), new Color(1f, 0.85f, 0.7f), 1.8f, 8f);
            Sign(lounge, "CABO LUMEN SPACEPORT  /  FAMILY LOUNGE", new Vector3(0f, 2.6f, -17.95f), Vector3.forward, 3.2f);
        }

        static void BuildGantryAndRocket(Transform world, Material hull)
        {
            var site = new GameObject("LaunchSite").transform;
            site.SetParent(world, false);
            // The gantry walk: a raised walkway with yellow rails all the way to the tower.
            Kit.Box(site, "Gantry", new Vector3(0f, 0.15f, 15f), new Vector3(3f, 0.3f, 46f), Frame);
            foreach (float sx in new[] { -1f, 1f })
                PipeRun(site, $"GantryRail_{sx}", new Vector3(sx * 1.45f, 1.1f, -7.5f), new Vector3(sx * 1.45f, 1.1f, 37.5f), 0.04f, PipeYellow);

            // The stack: a tall white rocket with two boosters, standing over the sea.
            Vector3 R = new Vector3(0f, 0f, 48f);
            Cylinder(site, "Core", R + Vector3.up * 21f, 2.6f, 42f, hull, collider: true);
            for (int i = 0; i < 4; i++) Cylinder(site, $"Band{i}", R + Vector3.up * (6f + i * 9f), 2.63f, 0.7f, DarkMetal);
            float rad = 2.6f;
            for (int i = 0; i < 5; i++) { Cylinder(site, $"Nose{i}", R + Vector3.up * (42.6f + i * 1.3f), rad, 1.3f, hull); rad *= 0.72f; }
            foreach (float sx in new[] { -1f, 1f })
            {
                Cylinder(site, $"Booster_{sx}", R + new Vector3(sx * 3.9f, 14f, 0f), 1.2f, 28f, hull, collider: true);
                for (int k = 0; k < 3; k++) Cylinder(site, $"BoosterNose_{sx}_{k}", R + new Vector3(sx * 3.9f, 28.6f + k * 0.8f, 0f), 1.2f * Mathf.Pow(0.65f, k), 0.8f, hull);
                Cylinder(site, $"BoosterBell_{sx}", R + new Vector3(sx * 3.9f, 0.6f, 0f), 0.9f, 1.2f, Pipe);
            }
            Sign(site, "HALCYON  /  MERIDIAN CREW", R + new Vector3(0f, 24f, -2.65f), Vector3.back, 5f);
            Kit.Box(site, "Pad", R + Vector3.down * 0.05f, new Vector3(20f, 0.3f, 20f), DarkMetal);
            // The tower and its crew arm.
            Vector3 T = R + new Vector3(6.5f, 0f, 0f);
            foreach (var (dx, dz) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
                Kit.Box(site, $"TowerPost_{dx}_{dz}", T + new Vector3(dx * 1.4f, 24f, dz * 1.4f), new Vector3(0.3f, 48f, 0.3f), PipeYellow);
            for (float y = 4f; y < 48f; y += 4f)
                Kit.Box(site, $"TowerBeam_{y}", T + new Vector3(0f, y, -1.4f), new Vector3(3f, 0.2f, 0.2f), PipeYellow, collider: false);
            Kit.Box(site, "CrewArm", T + new Vector3(-2.6f, 41f, 0f), new Vector3(2.8f, 0.4f, 1.4f), DarkMetal, collider: false);
            var beacon = Sphere(site, "TowerBeacon", T + Vector3.up * 48.5f, 0.35f, LedRed);
            beacon.isStatic = false;
            Kit.Set(beacon.AddComponent<BlinkingLight>(), "color", new Color(1f, 0.15f, 0.1f));
            // Venting cryogenics: a cold white plume at the base.
            var vent = new GameObject("CryoVent").AddComponent<ParticleSystem>();
            vent.transform.SetParent(site, false);
            vent.transform.SetPositionAndRotation(R + new Vector3(2.2f, 8f, -2f), Quaternion.Euler(-10f, 0f, 0f));
            var main = vent.main;
            main.startLifetime = 3f;
            main.startSpeed = 1.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(1f, 2.5f);
            main.startColor = new Color(1f, 1f, 1f, 0.25f);
            main.gravityModifier = 0.1f;
            var em = vent.emission;
            em.rateOverTime = 6f;
            var pr = vent.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.DeleteAsset(Kit.MaterialFolder + "/CL_Vent.mat");
            AssetDatabase.CreateAsset(pr.sharedMaterial, Kit.MaterialFolder + "/CL_Vent.mat");
        }

        /// <summary>The crew capsule at the top of the stack: couches, a console, one window onto the sea far below.</summary>
        static Transform BuildCapsule(Transform world)
        {
            var c = new GameObject("CrewCapsule").transform;
            c.SetParent(world, false);
            Vector3 P = CapsulePos;
            Block(c, "Floor", P + new Vector3(-2f, -0.1f, -2f), P + new Vector3(2f, 0f, 2f), Floor);
            Block(c, "Ceiling", P + new Vector3(-2f, 2.6f, -2f), P + new Vector3(2f, 2.7f, 2f), Ceiling);
            Block(c, "WallBack", P + new Vector3(-2f, 0f, 1.9f), P + new Vector3(2f, 2.6f, 2f), Wall);
            Block(c, "WallL", P + new Vector3(-2f, 0f, -2f), P + new Vector3(-1.9f, 2.6f, 2f), Wall);
            Block(c, "WallR", P + new Vector3(1.9f, 0f, -2f), P + new Vector3(2f, 2.6f, 2f), Wall);
            // The front wall has the window: Sam's couch faces it.
            Block(c, "WallFrontLow", P + new Vector3(-2f, 0f, -2f), P + new Vector3(2f, 1.2f, -1.9f), Wall);
            Block(c, "WallFrontHigh", P + new Vector3(-2f, 2.1f, -2f), P + new Vector3(2f, 2.6f, -1.9f), Wall);
            Block(c, "WallFrontL", P + new Vector3(-2f, 1.2f, -2f), P + new Vector3(-0.9f, 2.1f, -1.9f), Wall);
            Block(c, "WallFrontR", P + new Vector3(0.9f, 1.2f, -2f), P + new Vector3(2f, 2.1f, -1.9f), Wall);
            Box(c, "Console", P + new Vector3(0f, 0.9f, -1.6f), new Vector3(3.4f, 0.12f, 0.5f), DarkMetal, Quaternion.Euler(-30f, 0f, 0f));
            for (int i = 0; i < 3; i++)
                Box(c, $"ConsoleScreen{i}", P + new Vector3(-1f + i, 0.96f, -1.62f), new Vector3(0.7f, 0.02f, 0.35f), i == 1 ? ScreenAmber : ScreenCyan, Quaternion.Euler(-30f, 0f, 0f), collider: false);
            for (int i = 0; i < 5; i++)
                Box(c, $"Couch{i}", P + new Vector3(-1.6f + i * 0.8f, 0.35f, 0.6f), new Vector3(0.6f, 0.2f, 1.1f), Fabric, Quaternion.Euler(-20f, 0f, 0f));
            Point(c, "CapsuleLight", P + new Vector3(0f, 2.3f, 0f), new Color(0.9f, 0.95f, 1f), 1.2f, 5f);
            var seat = new GameObject("SamSeat").transform;
            seat.SetParent(c, false);
            seat.SetPositionAndRotation(P + new Vector3(0f, 0.05f, 0.3f), Quaternion.Euler(0f, 180f, 0f));
            return seat;
        }
    }
}
