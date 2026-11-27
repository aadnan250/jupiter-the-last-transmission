using System;
using System.Linq;
using Jupiter.Narrative;
using UnityEditor;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Places a crew member's generated character model (FBX exported from MakeHuman/MPFB, see docs/CHARACTERS.md)
    /// under their crew root and wires procedural life onto its skeleton: lip sync on the jaw, breathing,
    /// relaxed arms, blinking and looking at the player. Bones are found by name so the rig can change
    /// without code changes. Falls back (returns false) when no model has been generated yet.
    /// </summary>
    public static class CharacterModelImporter
    {
        public const string CharactersFolder = "Assets/_Project/Art/Characters";

        public static string ModelPath(string id) => $"{CharactersFolder}/{id.ToLowerInvariant()}/{id.ToLowerInvariant()}.fbx";

        public static bool TrySpawnModel(string id, Transform crewRoot, AudioSource voice)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(id));
            if (model == null) return false;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            go.name = "Model";
            go.transform.SetParent(crewRoot, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            foreach (var c in go.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);

            var t = go.transform;
            var jaw = Find(t, "jaw");
            var head = Find(t, "head");
            var neck = Find(t, "neck01", "neck_01", "neck");
            var spine = Find(t, "spine03", "spine_03", "spine02", "spine_02");
            var uaL = Find(t, "upperarm01.L", "upperarm_l", "upper_arm.L", "upperarm01_L");
            var uaR = Find(t, "upperarm01.R", "upperarm_r", "upper_arm.R", "upperarm01_R");
            var laL = Find(t, "lowerarm01.L", "lowerarm_l", "forearm.L", "lowerarm01_L");
            var laR = Find(t, "lowerarm01.R", "lowerarm_r", "forearm.R", "lowerarm01_R");
            var lidL = Find(t, "orbicularis03.L", "eyelid_upper_l", "orbicularis03_L");
            var lidR = Find(t, "orbicularis03.R", "eyelid_upper_r", "orbicularis03_R");

            if (jaw != null && voice != null)
            {
                var lip = go.AddComponent<LipSync>();
                GreyboxKit.Set(lip, "voice", voice);
                GreyboxKit.Set(lip, "jaw", jaw);
            }

            if (head != null)
            {
                var face = go.AddComponent<FaceExpression>();
                GreyboxKit.Set(face, "head", head);
                GreyboxKit.Set(face, "browInnerL", Find(t, "oculi01.L"));
                GreyboxKit.Set(face, "browInnerR", Find(t, "oculi01.R"));
                GreyboxKit.Set(face, "browOuterL", Find(t, "oculi02.L"));
                GreyboxKit.Set(face, "browOuterR", Find(t, "oculi02.R"));
                GreyboxKit.Set(face, "cornerL", Find(t, "risorius03.L"));
                GreyboxKit.Set(face, "cornerR", Find(t, "risorius03.R"));
            }

            var idle = go.AddComponent<CharacterIdleMotion>();
            GreyboxKit.Set(idle, "spine", spine);
            GreyboxKit.Set(idle, "neck", neck);
            GreyboxKit.Set(idle, "head", head);
            GreyboxKit.Set(idle, "upperArmL", uaL);
            GreyboxKit.Set(idle, "upperArmR", uaR);
            GreyboxKit.Set(idle, "lowerArmL", laL);
            GreyboxKit.Set(idle, "lowerArmR", laR);
            GreyboxKit.Set(idle, "eyelidL", lidL);
            GreyboxKit.Set(idle, "eyelidR", lidR);
            // Bake the relaxed stance into the scene so it is visible in the editor too (runtime repeats it harmlessly).
            BoneAxes.LowerArm(uaL, laL, go.transform, 9f);
            BoneAxes.LowerArm(uaR, laR, go.transform, 9f);
            if (uaL != null) PrefabUtility.RecordPrefabInstancePropertyModifications(uaL);
            if (uaR != null) PrefabUtility.RecordPrefabInstancePropertyModifications(uaR);

            if (voice != null && head != null) voice.transform.position = head.position;
            return true;
        }

        static Transform Find(Transform root, params string[] names)
        {
            foreach (string n in names)
            {
                var hit = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(x => string.Equals(x.name, n, StringComparison.OrdinalIgnoreCase));
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
