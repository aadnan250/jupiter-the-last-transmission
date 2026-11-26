using Jupiter.Creatures;
using Jupiter.Narrative;
using Jupiter.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using static Jupiter.EditorTools.GreyboxKit;
using Six = Jupiter.EditorTools.ConamaraChapterSixBuilder;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// The Chapter 7 "Contact" layer of Conamara Base (docs/preproduction/02-chapters-and-scenes.md §7, scenes 7.1-7.3):
    /// Ashford and Hana at the drill; walking in after the sealed orders plays the confrontation; the drill seizes
    /// and <see cref="EruptionSequence"/> opens the bore (Rime, blackout, Hana gone, the Listener out); Ashford holds
    /// the drill-chamber pressure door (<see cref="PressureDoorHold"/>) while Sam runs for the safe room, where Mara
    /// is waiting. Story gates move Mara (comms, drill chamber, safe room) and remove Ashford and Hana.
    /// </summary>
    public static class ConamaraChapterSevenBuilder
    {
        public const string DialogueFolder = "Assets/_Project/Narrative/Dialogue/Chapter07";
        public const string ConvConfrontation = "ch07_confrontation";
        public const string ConvBlackout = "ch07_blackout_radio";
        public const string ConvDoor = "ch07_door_radio";
        public const string ConvSafeRoom = "ch07_safe_room";
        public const string ConvLuca = "ch07_luca_radio";
        public const string ConvAshford = "ch07_ashford_radio";
        public const string AscentFlag = "reached_ascent_tunnel";

        public const string BlackoutFlag = "eruption_blackout";
        public const string LeftDrillFlag = "left_drill_chamber";
        public const string SafeRoomFlag = "reached_safe_room";

        public static void Build(GameObject rig, GameObject listener, Transform level, Transform lights)
        {
            var dialogue = rig.GetComponentInChildren<DialogueManager>();
            var story = GameObject.Find("Story").transform;
            var creature = listener.GetComponent<CreatureAI>();

            BuildCrew(story, dialogue);
            BuildConfrontationTrigger(story, dialogue);
            BuildEruption(level, lights, story, creature, dialogue);
            BuildPressureDoor(level, story, creature, dialogue);
            BuildSafeRoom(story, dialogue);
            BuildLastTransmissions(level, story, dialogue);
            ExtendObjectives();
        }

        static TextAsset Dialogue(string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>($"{DialogueFolder}/{name}.txt");
            if (asset == null) Debug.LogError($"[ConamaraChapterSevenBuilder] Missing dialogue {name}");
            return asset;
        }

        // ------------------------------------------------------------------ 7.1 people

        static void BuildCrew(Transform story, DialogueManager dialogue)
        {
            var maraIdle = Dialogue("ch07_idle_mara");

            // Ashford by the bore railing, watching the entrance; Hana at the drill console (the bolt driver beside her).
            var ashford = Six.Crew(story, "Crew_ASHFORD", "ASHFORD", new Vector3(-9.6f, 0f, 49.2f), 157f, new Color(0.75f, 0.72f, 0.6f),
                dialogue, null, null, Dialogue("ch07_idle_ashford"));
            var hana = Six.Crew(story, "Crew_HANA", "HANA", new Vector3(-13.5f, 0f, 45.6f), 60f, new Color(0.45f, 0.7f, 0.45f),
                dialogue, null, null, Dialogue("ch07_idle_hana"));
            // Mara follows Sam down (just behind him in the doorway), and after the blackout she is in the safe room.
            var maraDrill = Six.Crew(story, "Crew_MARA_Drill", "MARA", new Vector3(-5.8f, 0f, 45.2f), -20f, new Color(0.3f, 0.55f, 0.7f),
                dialogue, null, null, maraIdle);
            var maraSafe = Six.Crew(story, "Crew_MARA_Safe", "MARA", new Vector3(8.5f, 0f, 33f), -100f, new Color(0.3f, 0.55f, 0.7f),
                dialogue, null, null, maraIdle);
            maraDrill.SetActive(false);
            maraSafe.SetActive(false);
            var maraComms = story.Find("Crew_MARA").gameObject;

            Gate(story, Six.ReachedDrillFlag, false, null, dialogue, maraComms);
            Gate(story, Six.ReachedDrillFlag, true, BlackoutFlag, dialogue, maraDrill);
            Gate(story, BlackoutFlag, false, null, dialogue, ashford, hana);
            Gate(story, BlackoutFlag, true, null, dialogue, maraSafe);
        }

        static void Gate(Transform story, string flag, bool activeWhenSet, string unless, DialogueManager dialogue, params GameObject[] targets)
        {
            var gate = story.gameObject.AddComponent<StoryGate>();
            Set(gate, "flag", flag);
            Set(gate, "activeWhenSet", activeWhenSet);
            Set(gate, "unlessFlag", unless ?? string.Empty);
            Set(gate, "targets", targets);
            Set(gate, "dialogue", dialogue);
        }

        static void BuildConfrontationTrigger(Transform story, DialogueManager dialogue)
        {
            var t = Six.Trigger(story, "Trigger_DrillChamber", new Vector3(-7f, 1.4f, 44.6f), new Vector3(2.4f, 2.8f, 1.6f), dialogue);
            Set(t, "conversation", Dialogue(ConvConfrontation));
            Set(t, "setsFlagOnEnter", Six.ReachedDrillFlag);
            var so = new SerializedObject(t);
            Six.SetStrings(so.FindProperty("requiresCompleted"), Six.ConvOrders);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ 7.2 the eruption

        static void BuildEruption(Transform level, Transform lights, Transform story, CreatureAI creature, DialogueManager dialogue)
        {
            // Rime that bursts out of the bore across the chamber floor (hidden until the eruption).
            var rimeMat = Mat("CB_Rime", new Color(0.9f, 0.95f, 1f), new Color(0.02f, 0.06f, 0.07f), 0.9f);
            var burst = new GameObject("EruptionRime").transform;
            burst.SetParent(level, false);
            Vector3[] spots = { new Vector3(-7f, 0f, 48.5f), new Vector3(-10.2f, 0f, 53.5f), new Vector3(-4f, 0f, 51f),
                                new Vector3(-7.5f, 0f, 45.5f), new Vector3(-12.5f, 0f, 47.5f) };
            for (int i = 0; i < spots.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = $"EruptionRime_{i}";
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.transform.SetParent(burst, false);
                go.transform.position = spots[i] + Vector3.up * 0.013f;
                go.transform.localScale = new Vector3(2.6f, 0.01f, 2.6f);
                go.GetComponent<Renderer>().sharedMaterial = rimeMat;
                var patch = go.AddComponent<RimePatch>();
                Set(patch, "glow", go.GetComponent<Renderer>());
                Set(patch, "radius", 2.6f);
            }

            var bore = level.Find("Bore");
            var go2 = new GameObject("Eruption");
            go2.transform.SetParent(story, false);
            go2.transform.position = bore.position;
            var seq = go2.AddComponent<EruptionSequence>();
            Set(seq, "blackoutFlag", BlackoutFlag);
            Set(seq, "contactFlag", Six.ContactFlag);
            Set(seq, "lights", new Object[]
            {
                lights.Find("Light_DrillLamp").GetComponent<Light>(), lights.Find("Light_DrillFill").GetComponent<Light>(),
                lights.Find("Light_CorrB_W").GetComponent<Light>(), lights.Find("Light_CorrB_E").GetComponent<Light>(),
            });
            Set(seq, "rimeBurst", new Object[] { burst.gameObject });
            Set(seq, "bore", bore);
            Set(seq, "creature", creature);
            Set(seq, "radio", Dialogue(ConvBlackout));
            Set(seq, "audioSource", Audio3D(go2, null, 4f, 60f));
            Set(seq, "eruptionClip", CombatAudioGenerator.Load(CombatAudioGenerator.Eruption));
            Set(seq, "dialogue", dialogue);
        }

        // ------------------------------------------------------------------ 7.3 Ashford's door

        static void BuildPressureDoor(Transform level, Transform story, CreatureAI creature, DialogueManager dialogue)
        {
            var door = ConamaraBaseBuilder.Door(level, "Door_DrillPressure", new Vector3(-7f, 0f, 43f), 0f, 2.4f, false,
                Mat("CB_Hatch", new Color(0.9f, 0.72f, 0.1f)));
            Set(door, "startsOpen", true);
            // The NavMesh is baked with this doorway open; a carving obstacle shuts it for the creature while the door is shut.
            var panel = door.transform.Find("Panel");
            var obstacle = panel.gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = Vector3.one;
            obstacle.carving = true;

            var go = new GameObject("Hold_DrillPressureDoor");
            go.transform.SetParent(story, false);
            go.transform.position = new Vector3(-7f, 1.4f, 41.5f);
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(2.6f, 2.8f, 1.4f);
            var hold = go.AddComponent<PressureDoorHold>();
            Set(hold, "door", door);
            Set(hold, "requiresFlag", BlackoutFlag);
            Set(hold, "setsFlagOnClose", LeftDrillFlag);
            Set(hold, "radio", Dialogue(ConvDoor));
            Set(hold, "creature", creature);
            Set(hold, "audioSource", Audio3D(door.gameObject, null, 2f, 40f));
            Set(hold, "poundClip", CombatAudioGenerator.Load(CombatAudioGenerator.DoorPound));
            Set(hold, "breakClip", CombatAudioGenerator.Load(CombatAudioGenerator.Eruption));
            Set(hold, "dialogue", dialogue);
        }

        // ------------------------------------------------------------------ the safe room

        static void BuildSafeRoom(Transform story, DialogueManager dialogue)
        {
            var t = Six.Trigger(story, "Trigger_SafeRoom", new Vector3(6f, 1.4f, 36f), new Vector3(8f, 2.8f, 7f), dialogue);
            Set(t, "conversation", Dialogue(ConvSafeRoom));
            Set(t, "requiresFlag", BlackoutFlag);
            Set(t, "setsFlagOnEnter", SafeRoomFlag);
        }

        // ------------------------------------------------------------------ 7.5 Luca, 7.6 Ashford

        /// <summary>
        /// Leaving the safe room, Luca's last transmission plays on the move. Ashford has sealed himself in the galley:
        /// at its pressure door his confession comes over the radio, with the ascent vehicle's codes. Then the way on
        /// is the ascent tunnel past the drill chamber (Chapter 8).
        /// </summary>
        static void BuildLastTransmissions(Transform level, Transform story, DialogueManager dialogue)
        {
            var galley = ConamaraBaseBuilder.Door(level, "Door_GalleyPressure", new Vector3(8f, 0f, 43f), 0f, 1.8f, true,
                Mat("CB_Hatch", new Color(0.9f, 0.72f, 0.1f)));
            var panel = galley.transform.Find("Panel");
            var obstacle = panel.gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = Vector3.one;
            obstacle.carving = true;
            Box(level, "Galley_DoorLight", new Vector3(8f, 2.5f, 42.86f), new Vector3(0.5f, 0.08f, 0.05f), Mat("CB_LedRed", new Color(0.3f, 0.02f, 0.02f), new Color(1f, 0.1f, 0.05f) * 3f), collider: false);

            var luca = Six.Trigger(story, "Trigger_LucaLastRadio", new Vector3(0f, 1.4f, 34f), new Vector3(3f, 2.8f, 8f), dialogue);
            Set(luca, "conversation", Dialogue(ConvLuca));
            Set(luca, "onTheMove", true);
            var so = new SerializedObject(luca);
            Six.SetStrings(so.FindProperty("requiresCompleted"), ConvSafeRoom);
            so.ApplyModifiedPropertiesWithoutUndo();

            var ash = Six.Trigger(story, "Trigger_AshfordDoor", new Vector3(8f, 1.4f, 41.5f), new Vector3(3.5f, 2.8f, 3f), dialogue);
            Set(ash, "conversation", Dialogue(ConvAshford));
            Set(ash, "onTheMove", true);
            so = new SerializedObject(ash);
            Six.SetStrings(so.FindProperty("requiresCompleted"), ConvLuca);
            so.ApplyModifiedPropertiesWithoutUndo();

            var tunnel = Six.Trigger(story, "Trigger_AscentTunnel", new Vector3(-7f, 1.4f, 66f), new Vector3(2f, 2.8f, 4f), dialogue);
            Set(tunnel, "requiresFlag", "knows_ascent_code");
            Set(tunnel, "setsFlagOnEnter", AscentFlag);
        }

        static void ExtendObjectives()
        {
            var mission = Object.FindAnyObjectByType<MissionManager>();
            Set(mission, "completeText", "Chapter 7 complete. To be continued...");
            var so = new SerializedObject(mission);
            var list = so.FindProperty("objectives");
            int start = list.arraySize;
            list.arraySize = start + 5;
            Six.Objective(list.GetArrayElementAtIndex(start), "Get out of the drill chamber", new string[0], new[] { LeftDrillFlag });
            Six.Objective(list.GetArrayElementAtIndex(start + 1), "Get to the safe room", new string[0], new[] { SafeRoomFlag });
            Six.Objective(list.GetArrayElementAtIndex(start + 2), "Head for the lander pad", new[] { ConvLuca }, new string[0]);
            Six.Objective(list.GetArrayElementAtIndex(start + 3), "Find Ashford (the galley, off corridor B)", new[] { ConvAshford }, new string[0]);
            Six.Objective(list.GetArrayElementAtIndex(start + 4), "Reach the ascent tunnel past the drill chamber", new string[0], new[] { AscentFlag });
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
