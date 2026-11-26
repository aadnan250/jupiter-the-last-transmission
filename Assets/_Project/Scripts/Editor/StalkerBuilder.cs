using Jupiter.Creatures;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Builds the Stalker prefab from the Blender model (Tools/CreaturePipeline/make_stalker.py): wet, veined skin
    /// (baked albedo + normal), black mouth and eyes, dark claws; a body collider; the AI, procedural rig and
    /// NavMesh agent; a voice; and the burst of ice shards and frost it comes up through (slow in Europa's gravity).
    /// </summary>
    public static class StalkerBuilder
    {
        public const string ModelPath = CharacterModelImporter.CharactersFolder + "/stalker/stalker.fbx";
        public const string PrefabPath = "Assets/_Project/Prefabs/Creatures/Stalker.prefab";
        const string Textures = CharacterModelImporter.CharactersFolder + "/stalker/Textures";

        public static GameObject BuildPrefab()
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (modelAsset == null) { Debug.LogError($"[StalkerBuilder] Missing {ModelPath}. Run Tools/CreaturePipeline/make_stalker.py."); return null; }

            var skin = SkinMaterial();
            var mouth = GreyboxKit.Mat("ST_Mouth", new Color(0.03f, 0.005f, 0.01f), smoothness: 0.6f);
            var teeth = GreyboxKit.Mat("ST_Teeth", new Color(0.78f, 0.75f, 0.62f), smoothness: 0.5f);
            var eye = GreyboxKit.Mat("ST_Eye", new Color(0.01f, 0.01f, 0.012f), new Color(0.05f, 0.25f, 0.3f) * 0.6f, 0.95f);
            var claw = GreyboxKit.Mat("ST_Claw", new Color(0.12f, 0.13f, 0.16f), smoothness: 0.55f);

            var root = new GameObject("Stalker");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);

            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.name;
                r.sharedMaterial = n.StartsWith("Mouth") ? mouth : n.StartsWith("Tooth") ? teeth : n.StartsWith("Eye") ? eye
                                 : n.StartsWith("Claw") ? claw : skin;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = false;
            }

            var body = root.AddComponent<CapsuleCollider>();
            body.center = new Vector3(0f, 0.85f, 0.05f);
            body.height = 1.7f;
            body.radius = 0.42f;

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.45f;
            agent.height = 1.6f;
            agent.speed = 6.2f;
            agent.acceleration = 30f;
            agent.angularSpeed = 540f;
            agent.autoBraking = false;

            var rig = root.AddComponent<StalkerRig>();
            GreyboxKit.Set(rig, "model", model.transform);

            var voiceGo = new GameObject("Voice");
            voiceGo.transform.SetParent(root.transform, false);
            voiceGo.transform.localPosition = new Vector3(0f, 1.5f, 0.3f);
            var voice = GreyboxKit.Audio3D(voiceGo, null, 2f, 45f);

            var burst = BuildBurst(root.transform);

            var ai = root.AddComponent<StalkerAI>();
            GreyboxKit.Set(ai, "rig", rig);
            GreyboxKit.Set(ai, "burst", burst);
            GreyboxKit.Set(ai, "voice", voice);
            GreyboxKit.Set(ai, "shriek", CombatAudioGenerator.Load(CombatAudioGenerator.Shriek));
            GreyboxKit.Set(ai, "burstClip", CombatAudioGenerator.Load(CombatAudioGenerator.IceBurst));

            GreyboxKit.EnsureFolder("Assets/_Project/Prefabs/Creatures");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[StalkerBuilder] Saved {PrefabPath}");
            return prefab;
        }

        static Material SkinMaterial()
        {
            var m = GreyboxKit.Mat("ST_Skin", Color.white, smoothness: 0.55f);
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/stalker_skin_albedo.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/stalker_skin_normal.png");
            if (normal != null)
            {
                var imp = (TextureImporter)AssetImporter.GetAtPath(Textures + "/stalker_skin_normal.png");
                if (imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; imp.SaveAndReimport(); }
                normal = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/stalker_skin_normal.png");
            }
            if (albedo != null) m.SetTexture("_BaseMap", albedo);
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", 1.2f);
                m.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Ice shards thrown up and a frost cloud; low gravity so the debris hangs in the air.</summary>
        static ParticleSystem BuildBurst(Transform parent)
        {
            var go = new GameObject("IceBurst");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 11f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.134f;
            main.maxParticles = 120;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 70), new ParticleSystem.Burst(0.25f, 30) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.6f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-4f, 4f);
            rot.y = new ParticleSystem.MinMaxCurve(-4f, 4f);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            renderer.sharedMaterial = GreyboxKit.Mat("ST_IceShard", new Color(0.8f, 0.88f, 0.96f), new Color(0.1f, 0.25f, 0.3f) * 0.5f, 0.85f);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }
    }
}
