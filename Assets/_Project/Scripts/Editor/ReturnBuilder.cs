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
    /// Chapter 9, "The Last Transmission": back aboard the Meridian, alone, Jupiter swinging past the window. In the
    /// cargo hold, Ashford's canister ("FOR HOME") pulses at 0.4 Hz: vent it or keep it. At the comms console, Sam
    /// records the mission's last message and chooses how it ends. The answer from Earth was sent hours ago.
    /// </summary>
    public static class ReturnBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/L05_Return.unity";
        const string Folder = "Assets/_Project/Narrative/Dialogue/Chapter09";
        const float H = 3.2f;

        [MenuItem("Jupiter/Rebuild Chapter 9 (Return)")]
        public static void BuildMenu() { Build(); AssetDatabase.SaveAssets(); }

        static TextAsset Dialogue(string name)
        {
            var a = AssetDatabase.LoadAssetAtPath<TextAsset>($"{Folder}/{name}.txt");
            if (a == null) Debug.LogError($"[ReturnBuilder] Missing dialogue {name}");
            return a;
        }

        public static void Build()
        {
            GreyboxTestRoomBuilder.BuildProfiles();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var hold = new GameObject("CargoHold").transform;

            // The hold: ribbed walls, racks of strapped crates, a long window down one side.
            Block(hold, "Floor", new Vector3(-7f, -0.2f, -7f), new Vector3(7f, 0f, 7f), Floor);
            Block(hold, "Ceiling", new Vector3(-7f, H, -7f), new Vector3(7f, H + 0.2f, 7f), Ceiling);
            Block(hold, "WallN", new Vector3(-7f, 0f, 6.9f), new Vector3(7f, H, 7f), Wall);
            Block(hold, "WallS", new Vector3(-7f, 0f, -7f), new Vector3(7f, H, -6.9f), Wall);
            Block(hold, "WallW", new Vector3(-7f, 0f, -7f), new Vector3(-6.9f, H, 7f), Wall);
            // East wall: a window from shoulder height to near the ceiling.
            Block(hold, "WallE_Low", new Vector3(6.9f, 0f, -7f), new Vector3(7f, 1.1f, 7f), Wall);
            Block(hold, "WallE_High", new Vector3(6.9f, 2.6f, -7f), new Vector3(7f, H, 7f), Wall);
            for (float z = -6f; z <= 6f; z += 3f)
                Block(hold, $"WindowFrame_{z}", new Vector3(6.88f, 1.1f, z - 0.1f), new Vector3(7f, 2.6f, z + 0.1f), Frame);
            for (float z = -5.5f; z < 6f; z += 2.2f)
            {
                Box(hold, $"CrateRack_{z}", new Vector3(-6.2f, 0.6f, z), new Vector3(1.2f, 1.2f, 1.6f), Orange);
                Box(hold, $"CrateRackTop_{z}", new Vector3(-6.2f, 1.6f, z), new Vector3(1.1f, 0.8f, 1.4f), White);
                Box(hold, $"Strap_{z}", new Vector3(-5.58f, 1f, z), new Vector3(0.02f, 1.9f, 0.08f), PipeYellow, collider: false);
            }
            for (float z = -6f; z < 7f; z += 2f) Box(hold, $"Rib_{z}", new Vector3(0f, H - 0.15f, z), new Vector3(14f, 0.2f, 0.15f), Frame, collider: false);
            Box(hold, "Airlock", new Vector3(0f, 1.25f, 6.82f), new Vector3(2f, 2.5f, 0.1f), DoorPanel, collider: false);
            Sign(hold, "CARGO AIRLOCK", new Vector3(0f, 2.75f, 6.8f), Vector3.back, 1.8f);
            Point(hold, "HoldLight_A", new Vector3(-3f, 2.8f, 0f), new Color(0.8f, 0.88f, 1f), 1.4f, 9f);
            Point(hold, "HoldLight_B", new Vector3(3f, 2.8f, -3f), new Color(0.8f, 0.88f, 1f), 1.2f, 9f);

            // Outside: stars, and Jupiter, enormous and indifferent.
            var sky = AssetDatabase.LoadAssetAtPath<Material>(Kit.MaterialFolder + "/SHIP_Starfield.mat");
            if (sky != null) RenderSettings.skybox = sky;
            var jMat = AssetDatabase.LoadAssetAtPath<Material>(Kit.MaterialFolder + "/MER_JupiterPlaceholder.mat");
            var jupiter = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            jupiter.name = "Jupiter";
            Object.DestroyImmediate(jupiter.GetComponent<Collider>());
            jupiter.transform.SetPositionAndRotation(new Vector3(600f, 40f, 250f), Quaternion.Euler(0f, 120f, 4f));
            jupiter.transform.localScale = Vector3.one * 700f;
            if (jMat != null) jupiter.GetComponent<Renderer>().sharedMaterial = jMat;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.16f, 0.17f, 0.2f);
            RenderSettings.ambientEquatorColor = new Color(0.14f, 0.11f, 0.08f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.05f, 0.06f);

            var rig = PlayerRigBuilder.Spawn(new Vector3(3.5f, 0.05f, -4.5f), -40f, GreyboxTestRoomBuilder.MeridianRing);
            var cam = rig.GetComponentInChildren<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.farClipPlane = 2000f;
            PostFxKit.EnableOn(cam);
            PrefabUtility.RecordPrefabInstancePropertyModifications(cam);
            PostFxKit.CinematicVolume("Assets/_Project/Data/Europa/RT_PostFx.asset", 0f, -25f, 12f, new Color(0.88f, 0.94f, 1f));

            new GameObject("GameSession").AddComponent<Core.GameSession>();
            var dialogue = rig.GetComponentInChildren<DialogueManager>();
            MusicKit.Add(dialogue, ("", CombatAudioGenerator.MusicShip, 0.18f), ("sample_decided", CombatAudioGenerator.MusicEnding, 0.22f));
            LightingKit.Probe(null, "ReflectionProbe_Hold", new Vector3(0f, 1.6f, 0f), new Vector3(14f, 3.4f, 14f));
            var story = new GameObject("Story").transform;

            // Ashford's canister on its cradle, the colony's piece pulsing inside.
            Box(hold, "Cradle", new Vector3(0f, 0.5f, 1.5f), new Vector3(0.9f, 1f, 0.9f), DarkMetal);
            var glassCan = Cylinder(hold, "Canister", new Vector3(0f, 1.35f, 1.5f), 0.32f, 0.7f, Glass);
            var core = Sphere(hold, "Sample", new Vector3(0f, 1.35f, 1.5f), 0.14f, Kit.Mat("RT_Sample", new Color(0.4f, 0.8f, 0.9f), new Color(0.2f, 0.9f, 1f) * 2f, 0.9f));
            core.isStatic = false;
            var pulse = core.AddComponent<BlinkingLight>();
            Kit.Set(pulse, "mode", 1);              // Pulse
            Kit.Set(pulse, "period", 2.5f);         // 0.4 Hz
            Kit.Set(pulse, "color", new Color(0.3f, 0.95f, 1f));
            var glow = Point(hold, "SampleGlow", new Vector3(0f, 1.6f, 1.5f), new Color(0.3f, 0.9f, 1f), 1.5f, 4f);
            glow.gameObject.isStatic = false;
            Sign(hold, "FOR HOME   - D.A.", new Vector3(0f, 0.8f, 1.03f), Vector3.back, 1.3f, new Color(0.95f, 0.95f, 0.9f));
            var hum = new GameObject("SampleHum");
            hum.transform.SetParent(core.transform, false);
            var humSrc = Kit.Audio3D(hum, CombatAudioGenerator.Load(CombatAudioGenerator.Feature7), 1f, 8f);
            humSrc.loop = true;
            humSrc.playOnAwake = true;
            humSrc.volume = 0.5f;

            var canTerminal = glassCan.AddComponent<ConversationTerminal>();
            glassCan.AddComponent<CapsuleCollider>();
            Kit.Set(canTerminal, "conversation", Dialogue("ch09_sample"));
            Kit.Set(canTerminal, "prompt", "Look at the canister");
            Kit.Set(canTerminal, "dialogue", dialogue);
            // Vented: the light goes out of the hold.
            var gate = story.gameObject.AddComponent<StoryGate>();
            Kit.Set(gate, "flag", "sample_vented");
            Kit.Set(gate, "activeWhenSet", false);
            Kit.Set(gate, "targets", new Object[] { core, glow.gameObject });
            Kit.Set(gate, "dialogue", dialogue);

            // The comms console for the last transmission.
            Box(hold, "CommsDesk", new Vector3(-3.5f, 0.45f, -6.2f), new Vector3(2.4f, 0.9f, 0.9f), DarkMetal);
            var comms = Box(hold, "CommsScreen", new Vector3(-3.5f, 1.4f, -6.55f), new Vector3(1.6f, 0.9f, 0.06f), ScreenAmber);
            comms.isStatic = false;
            var commsTerminal = comms.AddComponent<ConversationTerminal>();
            Kit.Set(commsTerminal, "conversation", Dialogue("ch09_transmission"));
            Kit.Set(commsTerminal, "prompt", "Record the transmission");
            var so = new SerializedObject(commsTerminal);
            Six.SetStrings(so.FindProperty("requiresFlags"), "sample_decided");
            so.ApplyModifiedPropertiesWithoutUndo();
            Kit.Set(commsTerminal, "dialogue", dialogue);

            // Mara's music player, clipped to the strap where she left it.
            var player = Box(hold, "Readable_MusicPlayer", new Vector3(-5.55f, 1.3f, 0.6f), new Vector3(0.08f, 0.14f, 0.04f), DarkMetal, collider: true);
            player.isStatic = false;
            var mp = player.AddComponent<Readable>();
            mp.SetContent("Mara's music player", "Tomas's old player, clipped to a cargo strap where she must have left it on the way out.\n\n" +
                "You press play. Six seconds of the bossa nova Luca used to tease her about. Then the battery dies.", "found_music_player");
            EditorUtility.SetDirty(mp);

            var mission = new GameObject("Mission").AddComponent<MissionManager>();
            mission.transform.SetParent(story, false);
            Kit.Set(mission, "dialogue", dialogue);
            Kit.Set(mission, "objectiveText", rig.transform.Find("HUD/Objective").GetComponent<UnityEngine.UI.Text>());
            Kit.Set(mission, "completeText", "Transmission sent");
            var mso = new SerializedObject(mission);
            var list = mso.FindProperty("objectives");
            list.arraySize = 2;
            Six.Objective(list.GetArrayElementAtIndex(0), "Ashford's canister, in the cargo hold", new[] { "ch09_sample" }, new string[0]);
            Six.Objective(list.GetArrayElementAtIndex(1), "Record the last transmission (comms console)", new[] { "ch09_transmission" }, new string[0]);
            mso.ApplyModifiedPropertiesWithoutUndo();

            var transition = new GameObject("LevelTransition").AddComponent<Core.LevelTransition>();
            Kit.Set(transition, "requiresCompleted", "ch09_transmission");
            Kit.Set(transition, "nextScene", System.IO.Path.GetFileNameWithoutExtension(HomeBuilder.ScenePath));
            Kit.Set(transition, "cardTitle", "TEN MONTHS LATER");
            Kit.Set(transition, "cardSubtitle", "The Pacific Ocean. A recovery ship.");
            Kit.Set(transition, "delaySeconds", 3f);
            Kit.Set(transition, "dialogue", dialogue);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[ReturnBuilder] Saved {ScenePath}");
        }
    }
}
