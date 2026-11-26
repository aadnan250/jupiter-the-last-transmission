using Jupiter.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Builds a crew member in a full EVA suit from parts: white pressure suit, life-support backpack with status
    /// lights, a domed helmet with a gold sun visor and a helmet lamp, a coloured arm stripe so the player can tell
    /// them apart, and a beam rifle. Pivots are named for <see cref="SuitAnimator"/>. With <c>squad</c> set, they
    /// also get a NavMesh agent and <see cref="SquadMember"/> so they walk and fight beside Sam.
    /// </summary>
    public static class AstronautBuilder
    {
        static Material suit, joint, visor, dark, led, gun, coil;

        public static GameObject Build(Transform parent, string id, Color stripe, Vector3 position, float yaw, bool squad,
            Vector3 formationOffset, Material beamMat)
        {
            suit = GreyboxKit.Mat("EVA_Suit", new Color(0.88f, 0.89f, 0.87f), smoothness: 0.3f);
            joint = GreyboxKit.Mat("EVA_Joint", new Color(0.55f, 0.57f, 0.58f), smoothness: 0.35f);
            visor = GreyboxKit.Mat("EVA_Visor", new Color(0.55f, 0.4f, 0.12f), new Color(0.12f, 0.08f, 0.02f), 0.95f);
            visor.SetFloat("_Metallic", 1f);
            dark = GreyboxKit.Mat("EVA_Dark", new Color(0.12f, 0.13f, 0.14f), smoothness: 0.5f);
            led = GreyboxKit.Mat("EVA_Led", new Color(0.1f, 0.3f, 0.2f), new Color(0.3f, 1f, 0.6f) * 2f);
            gun = GreyboxKit.Mat("VM_GunDark", new Color(0.09f, 0.1f, 0.11f), smoothness: 0.65f);
            coil = GreyboxKit.Mat("VM_Coil", new Color(0.1f, 0.3f, 0.35f), new Color(0.3f, 0.85f, 1f) * 1.5f, 0.8f);
            var stripeMat = GreyboxKit.Mat($"EVA_Stripe_{id}", stripe, stripe * 0.6f);

            var root = new GameObject($"Astronaut_{id}");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

            var hips = Pivot(root.transform, "Hips", new Vector3(0f, 1.0f, 0f));
            Part(hips, PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.42f, 0.26f, 0.3f), suit);

            var torso = Pivot(hips, "Torso", new Vector3(0f, 0.12f, 0f));
            Part(torso, PrimitiveType.Capsule, new Vector3(0f, 0.3f, 0f), new Vector3(0.52f, 0.36f, 0.4f), suit);
            Part(torso, PrimitiveType.Cube, new Vector3(0f, 0.38f, 0.02f), new Vector3(0.48f, 0.32f, 0.34f), suit);
            Part(torso, PrimitiveType.Cube, new Vector3(0f, 0.34f, 0.19f), new Vector3(0.24f, 0.13f, 0.05f), dark);
            for (int i = 0; i < 3; i++) Part(torso, PrimitiveType.Cube, new Vector3(-0.06f + i * 0.06f, 0.37f, 0.217f), new Vector3(0.025f, 0.02f, 0.01f), led);
            Part(torso, PrimitiveType.Cube, new Vector3(0f, 0.38f, -0.3f), new Vector3(0.46f, 0.6f, 0.26f), suit);       // backpack
            Part(torso, PrimitiveType.Cube, new Vector3(0f, 0.64f, -0.3f), new Vector3(0.4f, 0.08f, 0.22f), joint);
            Part(torso, PrimitiveType.Cube, new Vector3(0.12f, 0.2f, -0.435f), new Vector3(0.05f, 0.03f, 0.01f), led);

            var helmet = Pivot(torso, "Helmet", new Vector3(0f, 0.62f, 0f));
            Part(helmet, PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(0.3f, 0.03f, 0.3f), joint);  // neck ring
            Part(helmet, PrimitiveType.Sphere, new Vector3(0f, 0.18f, 0f), Vector3.one * 0.38f, suit);
            Part(helmet, PrimitiveType.Sphere, new Vector3(0f, 0.17f, 0.075f), new Vector3(0.31f, 0.25f, 0.24f), visor);
            Part(helmet, PrimitiveType.Cylinder, new Vector3(0.14f, 0.3f, 0.08f), new Vector3(0.05f, 0.03f, 0.05f), dark, new Vector3(90f, 0f, 0f));
            var lamp = new GameObject("HelmetLamp").AddComponent<Light>();
            lamp.transform.SetParent(helmet, false);
            lamp.transform.localPosition = new Vector3(0.14f, 0.3f, 0.12f);
            lamp.type = LightType.Spot;
            lamp.spotAngle = 55f;
            lamp.range = 14f;
            lamp.intensity = 2.2f;
            lamp.color = new Color(0.9f, 0.95f, 1f);
            lamp.shadows = LightShadows.None;

            Transform shoulderR = null, elbowR = null, shoulderL = null, elbowL = null;
            foreach (var (side, sx) in new[] { ("L", -1f), ("R", 1f) })
            {
                var sh = Pivot(torso, $"Shoulder.{side}", new Vector3(0.31f * sx, 0.5f, 0f));
                Part(sh, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.17f, joint);
                Part(sh, PrimitiveType.Capsule, new Vector3(0f, -0.18f, 0f), new Vector3(0.15f, 0.2f, 0.15f), suit);
                Part(sh, PrimitiveType.Cylinder, new Vector3(0f, -0.1f, 0f), new Vector3(0.155f, 0.03f, 0.155f), stripeMat);
                var el = Pivot(sh, $"Elbow.{side}", new Vector3(0f, -0.36f, 0f));
                Part(el, PrimitiveType.Capsule, new Vector3(0f, -0.15f, 0f), new Vector3(0.13f, 0.18f, 0.13f), suit);
                Part(el, PrimitiveType.Sphere, new Vector3(0f, -0.33f, 0f), new Vector3(0.11f, 0.12f, 0.13f), dark);
                if (side == "R") { shoulderR = sh; elbowR = el; } else { shoulderL = sh; elbowL = el; }
            }

            // Rifle along the right forearm: when the arm is raised it points where they look.
            Part(elbowR, PrimitiveType.Cube, new Vector3(-0.05f, -0.36f, 0.06f), new Vector3(0.06f, 0.6f, 0.08f), gun);
            Part(elbowR, PrimitiveType.Cylinder, new Vector3(-0.05f, -0.6f, 0.06f), new Vector3(0.065f, 0.08f, 0.065f), coil);
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(elbowR, false);
            muzzle.localPosition = new Vector3(-0.05f, -0.7f, 0.06f);

            Transform thighL = null, thighR = null, kneeL = null, kneeR = null;
            foreach (var (side, sx) in new[] { ("L", -1f), ("R", 1f) })
            {
                var th = Pivot(hips, $"Thigh.{side}", new Vector3(0.12f * sx, -0.1f, 0f));
                Part(th, PrimitiveType.Capsule, new Vector3(0f, -0.22f, 0f), new Vector3(0.19f, 0.24f, 0.19f), suit);
                var kn = Pivot(th, $"Knee.{side}", new Vector3(0f, -0.45f, 0f));
                Part(kn, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.16f, joint);
                Part(kn, PrimitiveType.Capsule, new Vector3(0f, -0.2f, 0f), new Vector3(0.17f, 0.21f, 0.17f), suit);
                Part(kn, PrimitiveType.Cube, new Vector3(0f, -0.41f, 0.05f), new Vector3(0.18f, 0.12f, 0.32f), joint);
                if (side == "R") { thighR = th; kneeR = kn; } else { thighL = th; kneeL = kn; }
            }

            var anim = root.AddComponent<SuitAnimator>();
            GreyboxKit.Set(anim, "hips", hips);
            GreyboxKit.Set(anim, "torso", torso);
            GreyboxKit.Set(anim, "helmet", helmet);
            GreyboxKit.Set(anim, "shoulderL", shoulderL);
            GreyboxKit.Set(anim, "shoulderR", shoulderR);
            GreyboxKit.Set(anim, "elbowL", elbowL);
            GreyboxKit.Set(anim, "elbowR", elbowR);
            GreyboxKit.Set(anim, "thighL", thighL);
            GreyboxKit.Set(anim, "thighR", thighR);
            GreyboxKit.Set(anim, "kneeL", kneeL);
            GreyboxKit.Set(anim, "kneeR", kneeR);

            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.height = 1.9f;
            col.radius = 0.35f;

            if (!squad) return root;

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 1.9f;
            agent.speed = 1.8f;
            agent.acceleration = 6f;
            agent.angularSpeed = 240f;
            agent.stoppingDistance = 0.6f;

            var beamGo = new GameObject("Beam");
            beamGo.transform.SetParent(root.transform, false);
            var line = beamGo.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.sharedMaterial = beamMat;
            line.startWidth = 0.03f;
            line.endWidth = 0.02f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.enabled = false;

            var hum = GreyboxKit.Audio3D(beamGo, CombatAudioGenerator.Load(CombatAudioGenerator.BeamHum), 2f, 30f);
            hum.loop = true;
            hum.volume = 0.35f;

            var member = root.AddComponent<SquadMember>();
            GreyboxKit.Set(member, "characterId", id);
            GreyboxKit.Set(member, "formationOffset", formationOffset);
            GreyboxKit.Set(member, "anim", anim);
            GreyboxKit.Set(member, "muzzle", muzzle);
            GreyboxKit.Set(member, "beam", line);
            GreyboxKit.Set(member, "gunAudio", hum);
            return root;
        }

        static Transform Pivot(Transform parent, string name, Vector3 local)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = local;
            return t;
        }

        static void Part(Transform parent, PrimitiveType type, Vector3 local, Vector3 scale, Material mat, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
