using System.Collections.Generic;
using Jupiter.Creatures;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using static Jupiter.EditorTools.GreyboxKit;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Builds the placeholder Listener prefab from primitives, following the story bible silhouette (§8.1):
    /// 2.8 m tall and very thin, long forelimbs ending in flat palms, digitigrade hind legs, no head but a
    /// vertical fan crest, and six glowing cyan pressure nodes along the spine. Every part has its own
    /// collider so the bolt driver can tell a node hit from a body hit.
    /// </summary>
    public static class ListenerBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Creatures/Listener.prefab";

        public static GameObject BuildPrefab()
        {
            EnsureFolder("Assets/_Project/Prefabs/Creatures");
            var hide = Mat("CR_ListenerHide", new Color(0.52f, 0.6f, 0.66f), smoothness: 0.65f);
            var dark = Mat("CR_ListenerOrgans", new Color(0.12f, 0.14f, 0.17f), smoothness: 0.4f);
            var crestMat = Mat("CR_ListenerCrest", new Color(0.7f, 0.82f, 0.88f), new Color(0.1f, 0.35f, 0.45f), 0.8f);
            var nodeMat = Mat("CR_ListenerNode", new Color(0.1f, 0.5f, 0.6f), new Color(0.2f, 0.9f, 1f) * 2.5f, 0.9f);

            var root = new GameObject("Listener");
            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.45f;
            agent.height = 2.6f;
            agent.speed = 1.6f;
            agent.angularSpeed = 240f;
            agent.acceleration = 10f;
            agent.stoppingDistance = 0.6f;
            var health = root.AddComponent<CreatureHealth>();

            // Body pivots at the hips so kneeling and collapsing rotate around them.
            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            body.localPosition = new Vector3(0f, 1.15f, 0f);

            // Hind legs (digitigrade: thigh forward, shin back) stay on the root.
            foreach (float side in new[] { -1f, 1f })
            {
                Part(root.transform, $"Thigh_{(side < 0 ? "L" : "R")}", PrimitiveType.Capsule, new Vector3(0.18f * side, 0.85f, 0.12f), new Vector3(0.14f, 0.38f, 0.14f), new Vector3(-25f, 0f, 0f), hide, health);
                Part(root.transform, $"Shin_{(side < 0 ? "L" : "R")}", PrimitiveType.Capsule, new Vector3(0.18f * side, 0.32f, 0.02f), new Vector3(0.1f, 0.33f, 0.1f), new Vector3(20f, 0f, 0f), hide, health);
            }

            // Torso: long, thin, ribbed (dark organs visible through a narrower inner core).
            Part(body, "Torso", PrimitiveType.Capsule, new Vector3(0f, 0.65f, 0f), new Vector3(0.36f, 0.75f, 0.26f), Vector3.zero, hide, health);
            Part(body, "Organs", PrimitiveType.Capsule, new Vector3(0f, 0.62f, 0.02f), new Vector3(0.2f, 0.6f, 0.16f), Vector3.zero, dark, null);
            Part(body, "Neck", PrimitiveType.Capsule, new Vector3(0f, 1.42f, 0.04f), new Vector3(0.14f, 0.18f, 0.14f), Vector3.zero, hide, health);

            // Long forelimbs hanging to below the knees, ending in flat wide palms.
            foreach (float side in new[] { -1f, 1f })
            {
                string s = side < 0 ? "L" : "R";
                Part(body, $"UpperArm_{s}", PrimitiveType.Capsule, new Vector3(0.3f * side, 1.0f, 0.02f), new Vector3(0.1f, 0.42f, 0.1f), new Vector3(0f, 0f, 12f * side), hide, health);
                Part(body, $"Forearm_{s}", PrimitiveType.Capsule, new Vector3(0.42f * side, 0.2f, 0.12f), new Vector3(0.08f, 0.45f, 0.08f), new Vector3(-10f, 0f, 4f * side), hide, health);
                Part(body, $"Palm_{s}", PrimitiveType.Cube, new Vector3(0.45f * side, -0.32f, 0.2f), new Vector3(0.26f, 0.03f, 0.32f), Vector3.zero, hide, health);
            }

            // Crest: a vertical fan of filaments where a head would be.
            var crest = new GameObject("Crest").transform;
            crest.SetParent(body, false);
            crest.localPosition = new Vector3(0f, 1.55f, 0.02f);
            for (int i = 0; i < 7; i++)
            {
                float a = -45f + i * 15f;
                Part(crest, $"Filament_{i}", PrimitiveType.Cube, Quaternion.Euler(0f, 0f, a) * new Vector3(0f, 0.28f, 0f),
                    new Vector3(0.025f, 0.56f, 0.12f), new Vector3(0f, 0f, a), crestMat, health);
            }

            // Six pressure nodes down the spine and shoulders: the weak points.
            var nodeRenderers = new List<Renderer>();
            Vector3[] nodePos =
            {
                new Vector3(0f, 1.28f, -0.14f), new Vector3(0f, 1.0f, -0.17f), new Vector3(0f, 0.72f, -0.17f),
                new Vector3(0f, 0.44f, -0.15f), new Vector3(-0.22f, 1.18f, -0.08f), new Vector3(0.22f, 1.18f, -0.08f)
            };
            for (int i = 0; i < nodePos.Length; i++)
            {
                var n = Part(body, $"Node_{i}", PrimitiveType.Sphere, nodePos[i], Vector3.one * 0.13f, Vector3.zero, nodeMat, null);
                var hb = n.AddComponent<CreatureHitbox>();
                Set(hb, "owner", health);
                Set(hb, "isNode", true);
                Set(hb, "nodeIndex", i);
                nodeRenderers.Add(n.GetComponent<Renderer>());
            }

            var visual = root.AddComponent<ListenerVisual>();
            Set(visual, "body", body);
            Set(visual, "crest", crest);
            Set(visual, "nodes", nodeRenderers.ToArray());
            Set(visual, "health", health);

            var ai = root.AddComponent<CreatureAI>();
            Set(ai, "health", health);
            Set(ai, "visual", visual);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>A primitive body part. Body parts get a body hitbox; pass null to leave the collider off.</summary>
        static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 scale, Vector3 euler, Material mat, CreatureHealth hitboxOwner)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (hitboxOwner != null)
            {
                var hb = go.AddComponent<CreatureHitbox>();
                Set(hb, "owner", hitboxOwner);
            }
            else if (type != PrimitiveType.Sphere || !name.StartsWith("Node"))
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            return go;
        }
    }
}
