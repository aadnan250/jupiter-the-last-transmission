using Jupiter.Combat;
using Jupiter.Narrative;
using Jupiter.World;
using UnityEditor;
using UnityEngine;
using static Jupiter.EditorTools.GreyboxKit;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// The Chapter 6 "The Discovery" layer of Conamara Base (docs/preproduction/02-chapters-and-scenes.md §6):
    /// 6.1 Luca's radio check-in (with the terminal-code joke), 6.2 the mail cache and Tomas's message with Mara,
    /// 6.3 Tiamat's recovered log in the lab, 6.4 Ashford's code-locked terminal and the sealed orders, then Mara's
    /// reaction, and 6.5 the frosted palm prints on the lab window. Reaching the drill chamber afterwards sets
    /// <see cref="ContactFlag"/>, which is what lets the Listener exist (until the Chapter 7 eruption replaces it).
    /// </summary>
    public static class ConamaraChapterSixBuilder
    {
        public const string DialogueFolder = "Assets/_Project/Narrative/Dialogue/Chapter06";
        public const string ConvMail = "ch06_mail_cache";
        public const string ConvLuca = "ch06_luca_radio";
        public const string ConvOrders = "ch06_sealed_orders_mara";
        public const string ConvMaraIdle = "ch06_idle_mara";

        /// <summary>Set on reaching the drill chamber after the sealed orders: the start of contact.</summary>
        public const string ContactFlag = "ch7_contact";
        /// <summary>Ashford's terminal code: his birthday, 17 04, backwards.</summary>
        public const string AshfordCode = "4071";

        static Material paper, screen, frost, shadow;

        public static void Build(GameObject rig, GameObject listener, Transform level, Transform lights, Transform items)
        {
            paper = Mat("CB_Paper", new Color(0.9f, 0.88f, 0.8f));
            screen = Mat("CB_Screen", new Color(0.05f, 0.08f, 0.1f), new Color(0.15f, 0.45f, 0.55f) * 0.8f);
            frost = Mat("CB_Frost", new Color(0.92f, 0.97f, 1f), new Color(0.12f, 0.3f, 0.35f) * 0.5f, 0.9f);
            shadow = Mat("CB_Shadow", new Color(0.01f, 0.01f, 0.012f));

            new GameObject("GameSession").AddComponent<Core.GameSession>();
            var dialogue = rig.GetComponentInChildren<DialogueManager>();
            var story = new GameObject("Story").transform;

            // The Listener is not in the base yet: it comes up the bore in Chapter 7.
            listener.SetActive(false);
            var gate = story.gameObject.AddComponent<StoryGate>();
            Set(gate, "flag", ContactFlag);
            Set(gate, "activeWhenSet", true);
            Set(gate, "targets", new Object[] { listener });
            Set(gate, "dialogue", dialogue);

            // The bolt driver is Hana's anchoring tool, left on the drill console.
            Supply(items, Pickup.Kind.BoltDriver, 0, new Vector3(-14.4f, 1.06f, 47.6f), new Vector3(0.45f, 0.12f, 0.18f));

            Mara(story, dialogue);
            BuildComms(story, dialogue);
            BuildDocuments(items);
            BuildOfficeTerminal(items);
            BuildPalmPrints(level, lights, story, dialogue);
            BuildTriggers(story, dialogue);
            BuildObjectives(story, rig, dialogue);
        }

        static TextAsset Dialogue(string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>($"{DialogueFolder}/{name}.txt");
            if (asset == null) Debug.LogError($"[ConamaraChapterSixBuilder] Missing dialogue {name}");
            return asset;
        }

        // ------------------------------------------------------------------ people and machines

        static void Mara(Transform parent, DialogueManager dialogue)
        {
            // In the comms room, by the console, facing the door.
            var root = new GameObject("Crew_MARA");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(new Vector3(8.2f, 0f, 21.6f), Quaternion.Euler(0f, -110f, 0f));
            var col = root.AddComponent<CapsuleCollider>();
            col.height = 1.75f;
            col.radius = 0.3f;
            col.center = new Vector3(0f, 0.875f, 0f);

            var voiceGo = new GameObject("Voice");
            voiceGo.transform.SetParent(root.transform, false);
            voiceGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var voice = Audio3D(voiceGo, null, 1.5f, 16f);
            voice.spatialBlend = 0.85f;

            if (!CharacterModelImporter.TrySpawnModel("MARA", root.transform, voice))
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                Object.DestroyImmediate(body.GetComponent<Collider>());
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = new Vector3(0f, 0.875f, 0f);
                body.transform.localScale = new Vector3(0.55f, 0.875f, 0.45f);
                body.GetComponent<Renderer>().sharedMaterial = Mat("CB_Crew_MARA", new Color(0.3f, 0.55f, 0.7f));
            }

            var member = root.AddComponent<CrewMember>();
            Set(member, "characterId", "MARA");
            Set(member, "conversations", new Object[] { Dialogue(ConvOrders) });
            var so = new SerializedObject(member);
            SetStrings(so.FindProperty("conversationRequiresFlag"), "read_sealed_orders");
            so.ApplyModifiedPropertiesWithoutUndo();
            Set(member, "idleConversation", Dialogue(ConvMaraIdle));
            Set(member, "voice", voice);
            Set(member, "dialogue", dialogue);
        }

        static void BuildComms(Transform parent, DialogueManager dialogue)
        {
            // Mail cache: a screen on the comms console. Playing it starts Tomas's message (6.2).
            var mail = Box(parent, "MailCache", new Vector3(10.55f, 1.3f, 19f), new Vector3(0.06f, 0.5f, 0.8f), screen);
            mail.isStatic = false;
            var terminal = mail.AddComponent<ConversationTerminal>();
            Set(terminal, "conversation", Dialogue(ConvMail));
            Set(terminal, "prompt", "Open mail cache");
            Set(terminal, "dialogue", dialogue);
        }

        static void BuildDocuments(Transform items)
        {
            // Med bay: the clue to the terminal code is in plain sight.
            Readable(items, "Readable_CrewMedical", new Vector3(-8.5f, 0.82f, 21.5f), "Crew medical summary (H. Mori)",
                "CONAMARA EXPEDITION: CREW MEDICAL SUMMARY\n\n" +
                "ASHFORD, Daniel R.  Commander.  DOB 17 04 2131.  Bone density -3.1%. Sleeping four hours a night; declines sedatives.\n" +
                "FERREIRA, Luca.  Pilot.  DOB 02 11 2149.  Excellent. Complains about everything, which I take as a vital sign.\n" +
                "LINDQVIST, Mara.  Engineer.  DOB 29 08 2147.  Fit. Has stopped coming to her psych check-ins.\n" +
                "MORI, Hana.  Medical officer.  Self-assessment: tired.\n" +
                "OKAFOR, Samuel.  Geophysicist.  DOB 05 06 2144.  Recurring dream about the ice. Logged at his request.\n\n" +
                "Note to self: ask the Commander why the sample freezer in the lab was rated for biological material.",
                "read_crew_medical");

            // Lab: what Tiamat's lander saw in 2171 (6.3). The EVA to the lander itself comes with the surface level.
            Readable(items, "Readable_TiamatLog", new Vector3(-7.6f, 0.97f, 30f), "Tiamat lander: recovered log",
                "TIAMAT SURFACE PACKAGE: SEISMIC LOG (relayed copy)\n\n" +
                "2171-03-14 04:12  Borehole B2 at 212 m. Instrument string nominal.\n" +
                "2171-03-14 04:31  Periodic signal, 0.4 Hz. Four pulses, a gap, repeating. Flagged FEATURE 7.\n" +
                "2171-03-14 05:02  FEATURE 7 amplitude rising. Signal tracking the drill's own rhythm, delayed.\n" +
                "2171-03-14 05:40  B2 pressure loss. Camera: borehole wall frosting over from below.\n" +
                "2171-03-14 06:15  B2 sealed. Seal is not ice. Sample unrecoverable.\n" +
                "2171-03-14 06:16  Data package classified at source by mission authority. Distribution: COMMAND ONLY.",
                "read_tiamat_log");

            // Comms: what the rest of the crew got from home, so the mail cache feels like more than one message.
            Readable(items, "Readable_MailIndex", new Vector3(10.3f, 1.01f, 18.2f), "Mail cache: index",
                "DELAYED MAIL: 11 MONTHS\n\n" +
                "FERREIRA  41 messages  (most from 'Mãe', all marked urgent, none urgent)\n" +
                "MORI  12 messages  (sister: wedding photos, a pressed flower scanned flat)\n" +
                "LINDQVIST  2 messages  (agency HR; one unread from T. Brandt, addressed to OKAFOR)\n" +
                "ASHFORD  1 message  (agency, encrypted, COMMAND ONLY)\n" +
                "OKAFOR  63 messages  (Ada; Zara: 'drawings, lots, sorry not sorry')");
        }

        static void BuildOfficeTerminal(Transform items)
        {
            // Ashford's office terminal (6.4): his birthday, backwards. Opening it shows Directive 7.
            var go = Box(items, "Terminal_Ashford", new Vector3(7f, 1.05f, 27.35f), new Vector3(0.7f, 0.45f, 0.06f), screen);
            go.isStatic = false;

            var contentsGo = new GameObject("SealedOrders");
            contentsGo.transform.SetParent(go.transform, false);
            var orders = contentsGo.AddComponent<Readable>();
            orders.SetContent("COMMAND DIRECTIVE 7 (sealed)",
                "AUTHORITY: Agency Director, eyes of Mission Commander only.\n\n" +
                "PRIMARY OBJECTIVE: Conamara Chaos geophysical survey, as briefed to crew.\n\n" +
                "SECONDARY OBJECTIVE (Directive 7): Locate the source of FEATURE 7 (Tiamat, 2171). " +
                "Use orbital deep radar to map the source before landing. " +
                "Return a viable biological sample to Earth in the sealed canister provided.\n\n" +
                "Crew are not to be briefed on the secondary objective. The Commander may brief the medical officer " +
                "at his discretion for sample handling.\n\n" +
                "If the source responds to stimulus, continue. Response is not hostility.",
                "read_sealed_orders");
            EditorUtility.SetDirty(orders);

            var keypad = go.AddComponent<KeypadTerminal>();
            Set(keypad, "code", AshfordCode);
            Set(keypad, "unlockFlag", "unlocked_ashford_terminal");
            Set(keypad, "contents", orders);
            Set(keypad, "audioSource", Audio3D(go, null, 0.8f, 8f));
            Set(keypad, "keyClip", AudioAssetGenerator.Load(AudioAssetGenerator.SwitchClick));
            Set(keypad, "acceptClip", AudioAssetGenerator.Load(AudioAssetGenerator.DoorOpen));
            Set(keypad, "rejectClip", CombatAudioGenerator.Load(CombatAudioGenerator.LockerDoor));
        }

        // ------------------------------------------------------------------ 6.5 the palm prints

        static void BuildPalmPrints(Transform level, Transform lights, Transform story, DialogueManager dialogue)
        {
            // Frosted hand prints on the inside (lab side) of the corridor window, all at a creature's reach.
            var prints = new GameObject("PalmPrints").transform;
            prints.SetParent(level, false);
            PalmPrint(prints, new Vector3(-1.535f, 1.9f, 36.6f), 8f);
            PalmPrint(prints, new Vector3(-1.535f, 1.75f, 37.25f), -12f);
            PalmPrint(prints, new Vector3(-1.535f, 1.4f, 36.85f), 20f);
            PalmPrint(prints, new Vector3(-1.535f, 1.15f, 37.5f), -4f);

            // The silhouette: in the dark lab behind the glass, for a fraction of a second.
            var silhouette = new GameObject("Silhouette").transform;
            silhouette.SetParent(level, false);
            silhouette.position = new Vector3(-3.2f, 0f, 37.6f);
            var torso = Shape(silhouette, PrimitiveType.Capsule, new Vector3(0f, 1.35f, 0f), new Vector3(0.45f, 1.3f, 0.35f));
            var crest = Shape(silhouette, PrimitiveType.Cube, new Vector3(0f, 2.75f, 0f), new Vector3(0.9f, 0.5f, 0.06f));
            crest.localRotation = Quaternion.Euler(0f, 90f, 0f);

            var go = new GameObject("Scare_PalmPrints");
            go.transform.SetParent(story, false);
            go.transform.position = new Vector3(0f, 1.4f, 37f);
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(3f, 2.8f, 1.6f);
            var scare = go.AddComponent<ScareEvent>();
            Set(scare, "requiresCompleted", ConvOrders);
            Set(scare, "setsFlag", "saw_palm_prints");
            Set(scare, "lights", new Object[] { lights.Find("Light_CorrA_29").GetComponent<Light>(), lights.Find("Light_CorrA_35").GetComponent<Light>() });
            Set(scare, "reveal", new Object[] { prints.gameObject });
            Set(scare, "silhouette", silhouette.gameObject);
            Set(scare, "audioSource", Audio3D(go, null, 2f, 20f));
            Set(scare, "sting", AudioAssetGenerator.Load(AudioAssetGenerator.ConsequenceSting));
            Set(scare, "dialogue", dialogue);
            silhouette.gameObject.SetActive(false);
        }

        static void PalmPrint(Transform parent, Vector3 centre, float roll)
        {
            var hand = new GameObject("Print").transform;
            hand.SetParent(parent, false);
            hand.position = centre;
            hand.rotation = Quaternion.Euler(roll, 0f, 0f);
            // Long, thin fingers: the hand is not quite human.
            Flat(hand, new Vector3(0f, 0f, 0f), new Vector3(0.004f, 0.12f, 0.1f));
            for (int i = 0; i < 4; i++)
                Flat(hand, new Vector3(0f, 0.15f, -0.042f + i * 0.028f), new Vector3(0.004f, 0.17f, 0.016f));
            Flat(hand, new Vector3(0f, 0.02f, 0.07f), new Vector3(0.004f, 0.1f, 0.016f)).localRotation = Quaternion.Euler(-35f, 0f, 0f);
        }

        static Transform Flat(Transform parent, Vector3 local, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = frost;
            return go.transform;
        }

        static Transform Shape(Transform parent, PrimitiveType type, Vector3 local, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = shadow;
            return go.transform;
        }

        // ------------------------------------------------------------------ triggers and objectives

        static void BuildTriggers(Transform parent, DialogueManager dialogue)
        {
            // 6.1 Luca checks in as Sam leaves the ready room.
            var luca = Trigger(parent, "Trigger_LucaRadio", new Vector3(0f, 1.4f, 16f), new Vector3(3f, 2.8f, 2f), dialogue);
            Set(luca, "conversation", Dialogue(ConvLuca));

            // Reaching the drill chamber once the orders are known is the start of contact.
            var drill = Trigger(parent, "Trigger_DrillChamber", new Vector3(-7f, 1.4f, 44.6f), new Vector3(2.4f, 2.8f, 1.6f), dialogue);
            Set(drill, "setsFlagOnEnter", ContactFlag);
            var so = new SerializedObject(drill);
            SetStrings(so.FindProperty("requiresCompleted"), ConvOrders);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static DialogueTrigger Trigger(Transform parent, string name, Vector3 pos, Vector3 size, DialogueManager dialogue)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;
            var t = go.AddComponent<DialogueTrigger>();
            Set(t, "dialogue", dialogue);
            return t;
        }

        static void BuildObjectives(Transform parent, GameObject rig, DialogueManager dialogue)
        {
            var mission = new GameObject("Mission").AddComponent<MissionManager>();
            mission.transform.SetParent(parent, false);
            Set(mission, "dialogue", dialogue);
            Set(mission, "objectiveText", rig.transform.Find("HUD/Objective").GetComponent<UnityEngine.UI.Text>());
            Set(mission, "completeText", "Chapter 6 complete");
            var so = new SerializedObject(mission);
            var list = so.FindProperty("objectives");
            list.arraySize = 4;
            Objective(list.GetArrayElementAtIndex(0), "Check the mail cache in comms", new[] { ConvMail }, new string[0]);
            Objective(list.GetArrayElementAtIndex(1), "Find out what Ashford is hiding", new string[0], new[] { "read_tiamat_log", "read_sealed_orders" });
            Objective(list.GetArrayElementAtIndex(2), "Talk to Mara", new[] { ConvOrders }, new string[0]);
            Objective(list.GetArrayElementAtIndex(3), "Go down to the drill chamber", new string[0], new[] { ContactFlag });
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Objective(SerializedProperty o, string text, string[] conversations, string[] flags)
        {
            o.FindPropertyRelative("text").stringValue = text;
            SetStrings(o.FindPropertyRelative("requiredConversations"), conversations);
            SetStrings(o.FindPropertyRelative("requiredFlags"), flags);
        }

        // ------------------------------------------------------------------ helpers

        static void Readable(Transform parent, string name, Vector3 position, string title, string body, string setsFlag = null)
        {
            var go = Box(parent, name, position, new Vector3(0.3f, 0.02f, 0.4f), paper);
            go.isStatic = false;
            var readable = go.AddComponent<Readable>();
            readable.SetContent(title, body, setsFlag);
            EditorUtility.SetDirty(readable);
        }

        static void Supply(Transform parent, Pickup.Kind kind, int amount, Vector3 pos, Vector3 size)
        {
            var go = Box(parent, $"Pickup_{kind}", pos, size, Mat("CB_Supply", new Color(0.85f, 0.35f, 0.1f)));
            go.isStatic = false;
            go.AddComponent<Pickup>().Configure(kind, amount);
            EditorUtility.SetDirty(go);
        }

        static void SetStrings(SerializedProperty prop, params string[] values)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).stringValue = values[i];
        }
    }
}
