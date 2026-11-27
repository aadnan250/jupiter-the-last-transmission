using Jupiter.Narrative;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Jupiter.EditorTools.ShipKit;
using Kit = Jupiter.EditorTools.GreyboxKit;
using Six = Jupiter.EditorTools.ConamaraChapterSixBuilder;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Chapter 10, "Home": the deck of a Pacific recovery ship at golden hour, ten months later. The scorched capsule
    /// sits on deck in its flotation collar. Grace, Ada and Zara are waiting. Then the ending: the names, the variant
    /// that depends on the sample, the credits. Sea, wind and gulls, the first natural sound since Chapter 1.
    /// </summary>
    public static class HomeBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/L06_Home.unity";
        const string Folder = "Assets/_Project/Narrative/Dialogue/Chapter10";

        [MenuItem("Jupiter/Rebuild Chapter 10 (Home)")]
        public static void BuildMenu() { Build(); AssetDatabase.SaveAssets(); }

        static TextAsset Dialogue(string name)
        {
            var a = AssetDatabase.LoadAssetAtPath<TextAsset>($"{Folder}/{name}.txt");
            if (a == null) Debug.LogError($"[HomeBuilder] Missing dialogue {name}");
            return a;
        }

        public static void Build()
        {
            GreyboxTestRoomBuilder.BuildProfiles();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var world = new GameObject("World").transform;

            EarthKit.SkyAndSun("HM", 7f, 250f, new Color(1f, 0.68f, 0.4f), 1.8f, new Color(0.6f, 0.58f, 0.7f), new Color(0.95f, 0.76f, 0.6f));
            EarthKit.Ocean(world, -3f);
            EarthKit.SeaAndWind(world, 0.6f);

            // The recovery ship's after deck.
            var deck = Kit.Mat("HM_Deck", new Color(0.42f, 0.44f, 0.45f), smoothness: 0.2f);
            var paint = Kit.Mat("HM_HullPaint", new Color(0.88f, 0.88f, 0.86f), smoothness: 0.35f);
            Kit.Box(world, "Deck", new Vector3(0f, -0.2f, 4f), new Vector3(14f, 0.4f, 34f), deck);
            Kit.Box(world, "Hull", new Vector3(0f, -2.2f, 4f), new Vector3(14.4f, 3.6f, 34.4f), paint);
            foreach (float sx in new[] { -1f, 1f })
                PipeRun(world, $"Rail_{sx}", new Vector3(sx * 6.8f, 1f, -12.5f), new Vector3(sx * 6.8f, 1f, 20.5f), 0.04f, White);
            PipeRun(world, "Rail_Stern", new Vector3(-6.8f, 1f, 20.5f), new Vector3(6.8f, 1f, 20.5f), 0.04f, White);
            Kit.Box(world, "Bridge", new Vector3(0f, 4f, -11f), new Vector3(12f, 8f, 5f), paint);
            for (int i = 0; i < 5; i++)
                Kit.Box(world, $"BridgeWindow{i}", new Vector3(-4f + i * 2f, 6f, -8.45f), new Vector3(1.4f, 1f, 0.05f), WarmPanel, collider: false);
            Kit.Box(world, "CraneBase", new Vector3(4.5f, 1f, 14f), new Vector3(1.2f, 2f, 1.2f), Orange);
            Kit.Box(world, "CraneArm", new Vector3(2f, 5f, 14f), new Vector3(6f, 0.5f, 0.5f), Orange, collider: false).transform.rotation = Quaternion.Euler(0f, 0f, 35f);
            // The capsule that brought him home: scorched, still in its flotation collar.
            var scorch = Kit.Mat("HM_Scorched", new Color(0.2f, 0.18f, 0.16f), smoothness: 0.25f);
            Cylinder(world, "Capsule", new Vector3(-3f, 1.2f, 12f), 1.6f, 2.2f, scorch, collider: true);
            Cylinder(world, "CapsuleTop", new Vector3(-3f, 2.6f, 12f), 0.9f, 0.6f, scorch);
            Cylinder(world, "FlotationCollar", new Vector3(-3f, 0.4f, 12f), 2.0f, 0.6f, Orange);

            var rig = PlayerRigBuilder.Spawn(new Vector3(0f, 0.05f, -5f), 0f, GreyboxTestRoomBuilder.Earth);
            var cam = rig.GetComponentInChildren<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.farClipPlane = 3000f;
            PostFxKit.EnableOn(cam);
            PrefabUtility.RecordPrefabInstancePropertyModifications(cam);
            PostFxKit.CinematicVolume("Assets/_Project/Data/Europa/HM_PostFx.asset", 0.15f, 8f, 6f, new Color(1f, 0.93f, 0.85f));

            new GameObject("GameSession").AddComponent<Core.GameSession>();
            var dialogue = rig.GetComponentInChildren<DialogueManager>();
            var story = new GameObject("Story").transform;

            Six.Crew(story, "Crew_GRACE", "GRACE", new Vector3(1.6f, 0f, 2.5f), 200f, new Color(0.7f, 0.7f, 0.75f), dialogue,
                null, null, Dialogue("ch10_idle_grace"));
            Six.Crew(story, "Crew_ADA", "ADA", new Vector3(-1.2f, 0f, 6.5f), 175f, new Color(0.7f, 0.5f, 0.6f), dialogue,
                null, null, Dialogue("ch10_idle_ada"));
            Six.Crew(story, "Crew_ZARA", "ZARA", new Vector3(0.1f, 0f, 5.2f), 185f, new Color(0.9f, 0.6f, 0.7f), dialogue,
                new[] { Dialogue("ch10_home") }, new string[0], null);

            var ending = story.gameObject.AddComponent<Core.EndingDirector>();
            Kit.Set(ending, "dialogue", dialogue);

            var mission = new GameObject("Mission").AddComponent<MissionManager>();
            mission.transform.SetParent(story, false);
            Kit.Set(mission, "dialogue", dialogue);
            Kit.Set(mission, "objectiveText", rig.transform.Find("HUD/Objective").GetComponent<UnityEngine.UI.Text>());
            Kit.Set(mission, "completeText", "Home");
            var mso = new SerializedObject(mission);
            var list = mso.FindProperty("objectives");
            list.arraySize = 1;
            Six.Objective(list.GetArrayElementAtIndex(0), "Zara and Ada are waiting", new[] { "ch10_home" }, new string[0]);
            mso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[HomeBuilder] Saved {ScenePath}");
        }
    }
}
