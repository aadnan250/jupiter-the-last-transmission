using System.Collections.Generic;
using Jupiter.Creatures;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using static Jupiter.EditorTools.GreyboxKit;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Builds the Listener prefab on the same Blender creature as the Stalkers (Tools/CreaturePipeline/make_stalker.py),
    /// grown to 2.6 m and paler, almost luminous: the older, larger thing that waits under the drill. A fan crest of
    /// filaments rises from its skull (it hears through it), and six glowing cyan pressure nodes run down its back:
    /// the weak points. Every body part that can be shot has a collider on its bone, so a node hit and a body hit are
    /// told apart, and the colliders move with the animation.
    /// </summary>
    public static class ListenerBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Creatures/Listener.prefab";
        const float Scale = 1.3f;

        // Bone, collider radius (metres, before scaling).
        static readonly (string bone, float radius)[] BodyColliders =
        {
            ("hips", 0.15f), ("spine", 0.14f), ("chest", 0.17f), ("neck", 0.07f), ("head", 0.12f),
            ("upperarm.L", 0.06f), ("upperarm.R", 0.06f), ("forearm.L", 0.055f), ("forearm.R", 0.055f),
            ("thigh.L", 0.1f), ("thigh.R", 0.1f), ("shin.L", 0.07f), ("shin.R", 0.07f),
        };

        public static GameObject BuildPrefab()
        {
            EnsureFolder("Assets/_Project/Prefabs/Creatures");
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(StalkerBuilder.ModelPath);
            if (modelAsset == null) { Debug.LogError($"[ListenerBuilder] Missing {StalkerBuilder.ModelPath}"); return null; }

            var skin = ListenerSkin();
            var mouth = Mat("ST_Mouth", new Color(0.03f, 0.005f, 0.01f), smoothness: 0.6f);
            var teeth = Mat("ST_Teeth", new Color(0.78f, 0.75f, 0.62f), smoothness: 0.5f);
            var eye = Mat("CR_ListenerEye", new Color(0.02f, 0.05f, 0.06f), new Color(0.2f, 0.8f, 1f) * 1.5f, 0.95f);
            var claw = Mat("ST_Claw", new Color(0.12f, 0.13f, 0.16f), smoothness: 0.55f);
            var crestMat = Mat("CR_ListenerCrest", new Color(0.7f, 0.82f, 0.88f), new Color(0.15f, 0.55f, 0.7f) * 1.2f, 0.8f);
            var nodeMat = Mat("CR_ListenerNode", new Color(0.1f, 0.5f, 0.6f), new Color(0.2f, 0.9f, 1f) * 2.5f, 0.9f);

            var root = new GameObject("Listener");
            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.5f;
            agent.height = 2.4f;
            agent.speed = 1.6f;
            agent.angularSpeed = 300f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0.6f;
            var health = root.AddComponent<CreatureHealth>();

            var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            model.transform.localScale = Vector3.one * Scale;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.name;
                r.sharedMaterial = n.StartsWith("Mouth") ? mouth : n.StartsWith("Tooth") ? teeth : n.StartsWith("Eye") ? eye
                                 : n.StartsWith("Claw") ? claw : skin;
            }

            var bones = new Dictionary<string, Transform>();
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (!bones.ContainsKey(t.name)) bones[t.name] = t;

            // What sets the Listener apart from the Stalkers it leads: longer forearms and hands that reach the floor
            // from a crouch, and a ridge of spines down the back like the thing beyond the ridge.
            foreach (var arm in new[] { "forearm.L", "forearm.R" })
                if (bones.TryGetValue(arm, out var fa)) fa.localScale *= 1.22f;
            var spineMat = Mat("CR_ListenerSpine", new Color(0.16f, 0.18f, 0.2f), new Color(0.05f, 0.2f, 0.25f), 0.7f);
            (string bone, float up, float back, float len)[] spines =
            {
                ("chest", 0.2f, 0.12f, 0.34f), ("chest", 0.08f, 0.15f, 0.42f), ("chest", -0.04f, 0.16f, 0.38f),
                ("spine", 0.1f, 0.15f, 0.32f), ("spine", -0.02f, 0.15f, 0.26f), ("hips", 0.08f, 0.14f, 0.2f),
            };
            for (int i = 0; i < spines.Length; i++)
            {
                if (!bones.TryGetValue(spines[i].bone, out var sb)) continue;
                var sp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sp.name = $"Spine_{i}";
                Object.DestroyImmediate(sp.GetComponent<Collider>());
                sp.transform.SetParent(sb, true);
                var up = root.transform.up; var back = -root.transform.forward;
                sp.transform.position = sb.position + (up * spines[i].up + back * spines[i].back) * Scale;
                var dir = (up * 0.8f + back * 0.6f).normalized;
                sp.transform.rotation = Quaternion.LookRotation(Vector3.Cross(dir, root.transform.right), dir);
                sp.transform.position += dir * spines[i].len * 0.5f * Scale;
                float ls = sb.lossyScale.x;
                sp.transform.localScale = new Vector3(0.025f, spines[i].len, 0.06f) * Scale / ls;
                sp.GetComponent<Renderer>().sharedMaterial = spineMat;
            }

            // Body hitboxes on the bones, sized to the flesh and pointing along each bone to its child.
            foreach (var (boneName, radius) in BodyColliders)
            {
                if (!bones.TryGetValue(boneName, out var bone)) { Debug.LogWarning($"[ListenerBuilder] No bone {boneName}"); continue; }
                var child = FirstBoneChild(bone);
                Vector3 local = child != null ? child.localPosition : Vector3.up * 0.25f;
                var cap = bone.gameObject.AddComponent<CapsuleCollider>();
                int axis = Mathf.Abs(local.x) > Mathf.Abs(local.y) ? (Mathf.Abs(local.x) > Mathf.Abs(local.z) ? 0 : 2) : (Mathf.Abs(local.y) > Mathf.Abs(local.z) ? 1 : 2);
                cap.direction = axis;
                cap.center = local * 0.5f;
                // Bone transforms carry the model's scale, so sizes are given in the bone's own (unscaled) units.
                float s = bone.lossyScale.x;
                cap.radius = radius * Scale / s;
                cap.height = local.magnitude + 2f * cap.radius;
                var hb = bone.gameObject.AddComponent<CreatureHitbox>();
                Set(hb, "owner", health);
            }

            // The crest: a fan of filaments rising from the back of the skull.
            if (bones.TryGetValue("head", out var head))
            {
                var crest = new GameObject("Crest").transform;
                crest.SetParent(head, false);
                crest.position = head.position + root.transform.up * 0.16f * Scale + root.transform.forward * 0.05f * Scale;
                crest.rotation = root.transform.rotation;
                for (int i = 0; i < 9; i++)
                {
                    float a = -60f + i * 15f;
                    var f = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    f.name = $"Filament_{i}";
                    Object.DestroyImmediate(f.GetComponent<Collider>());
                    f.transform.SetParent(crest, false);
                    f.transform.localRotation = Quaternion.Euler(-20f, 0f, a);
                    f.transform.localPosition = f.transform.localRotation * new Vector3(0f, 0.2f * Scale / head.lossyScale.x, 0f);
                    f.transform.localScale = new Vector3(0.018f, 0.42f, 0.09f) * Scale / head.lossyScale.x;
                    f.GetComponent<Renderer>().sharedMaterial = crestMat;
                }
            }

            // Six pressure nodes down the back and shoulders, sitting just proud of the skin.
            var nodeRenderers = new List<Renderer>();
            (string bone, Vector3 offset)[] nodes =
            {
                ("chest", new Vector3(0f, 0.12f, 0.16f)), ("chest", new Vector3(0f, -0.02f, 0.17f)), ("spine", new Vector3(0f, 0.08f, 0.15f)),
                ("hips", new Vector3(0f, 0.1f, 0.15f)), ("chest", new Vector3(-0.15f, 0.12f, 0.1f)), ("chest", new Vector3(0.15f, 0.12f, 0.1f)),
            };
            for (int i = 0; i < nodes.Length; i++)
            {
                if (!bones.TryGetValue(nodes[i].bone, out var bone)) continue;
                var n = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                n.name = $"Node_{i}";
                n.transform.SetParent(bone, false);
                // Offsets are in the creature's frame (x right, y up, z back), applied in world space at rest.
                Vector3 world = bone.position + root.transform.TransformDirection(new Vector3(nodes[i].offset.x, nodes[i].offset.y, -nodes[i].offset.z)) * Scale;
                n.transform.position = world;
                n.transform.localScale = Vector3.one * 0.13f * Scale / bone.lossyScale.x;
                n.GetComponent<Renderer>().sharedMaterial = nodeMat;
                var hb = n.AddComponent<CreatureHitbox>();
                Set(hb, "owner", health);
                Set(hb, "isNode", true);
                Set(hb, "nodeIndex", i);
                nodeRenderers.Add(n.GetComponent<Renderer>());
            }

            var rig = root.AddComponent<StalkerRig>();
            Set(rig, "model", model.transform);
            Set(rig, "crouchDrop", 0.36f * Scale);

            var visual = root.AddComponent<ListenerVisual>();
            Set(visual, "rig", rig);
            Set(visual, "nodes", nodeRenderers.ToArray());
            Set(visual, "health", health);

            var ai = root.AddComponent<CreatureAI>();
            Set(ai, "health", health);
            Set(ai, "visual", visual);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[ListenerBuilder] Saved {PrefabPath}");
            return prefab;
        }

        static Transform FirstBoneChild(Transform bone)
        {
            foreach (Transform c in bone)
                if (c.GetComponent<Renderer>() == null && !c.name.StartsWith("Node") && c.name != "Crest") return c;
            return null;
        }

        static Material ListenerSkin()
        {
            var m = Mat("CR_ListenerSkin", new Color(0.86f, 0.93f, 1f), new Color(0.03f, 0.06f, 0.08f), 0.6f);
            const string tex = CharacterModelImporter.CharactersFolder + "/stalker/Textures";
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "/stalker_skin_albedo.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "/stalker_skin_normal.png");
            if (albedo != null) m.SetTexture("_BaseMap", albedo);
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
