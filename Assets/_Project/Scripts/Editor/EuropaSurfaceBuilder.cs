using System.Collections.Generic;
using Jupiter.Combat;
using Jupiter.Creatures;
using Jupiter.Narrative;
using Jupiter.World;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Jupiter.EditorTools.ShipKit;
using Kit = Jupiter.EditorTools.GreyboxKit;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Generates L03_EuropaSurface: touchdown on Conamara Chaos. The level opens inside the lander Wren (a ribbed,
    /// cylindrical cabin with crash couches, consoles and a cockpit); the rear hatch swings down into a ramp onto
    /// open ice under a black sky, with Jupiter huge and fixed low on the horizon, Io, Ganymede and Callisto, the
    /// Milky Way and a small hard Sun. Sam walks out with Ashford, Mara and Hana in EVA suits, all armed with arc
    /// beam rifles. Out on the plain the moon quakes and ten Stalkers burst from the ice; once they are dead, the
    /// squad heads for Conamara Base, whose airlock leads into L04_ConamaraBase.
    /// </summary>
    public static class EuropaSurfaceBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/L03_EuropaSurface.unity";
        const string Data = "Assets/_Project/Data/Europa";
        const string NavMeshPath = Data + "/L03_EuropaSurface_NavMesh.asset";
        const string DialogueFolder = "Assets/_Project/Narrative/Dialogue/Chapter05";
        const string PressurisedProfile = "Assets/_Project/Data/Environments/EuropaPressurised.asset";

        public const string RampFlag = "ramp_open";
        public const string QuakeFlag = "quake_started";
        public const string ClearedFlag = "ambush_cleared";
        public const string TiamatFlag = "reached_tiamat";
        public const string ManifestFlag = "read_manifest";
        public const string DrillDoneFlag = "drill_done";
        public const string EruptionFlag = "site_eruption";
        public const string KilledFlag = "listener_killed";
        public const string AscentFlag = "ascent_unlocked";
        public const string AscentCode = "4117";
        public const string BoardedFlag = "boarded_halcyon";
        public const int StalkerCount = 10;

        const float FloorH = 1.3f;          // lander cabin floor above the ice
        const float CabinR = 2.3f;          // cabin radius
        const float CabinHalf = 4.3f;       // half length along Z (hatch at -Z, cockpit at +Z)
        static readonly Vector3 BasePos = new Vector3(0f, 0f, -215f);
        static readonly Vector3 TiamatPos = new Vector3(110f, 0f, -70f);
        static readonly Vector3 AscentPos = new Vector3(45f, 0f, -200f);
        const string SurfaceFolder = "Assets/_Project/Narrative/Dialogue/Surface";

        static Material ice, lineae, rime, hullWhite;
        static Terrain terrain;

        [MenuItem("Jupiter/Rebuild Europa Surface")]
        public static void BuildMenu()
        {
            Build();
            AssetDatabase.SaveAssets();
        }

        public static void Build()
        {
            Kit.EnsureFolder(Data);
            GreyboxTestRoomBuilder.BuildProfiles();
            var vacuum = GreyboxTestRoomBuilder.Europa;
            var pressurised = AssetDatabase.LoadAssetAtPath<EnvironmentProfile>(PressurisedProfile);

            ice = IceMaterial();
            lineae = Kit.Mat("EU_Lineae", new Color(0.36f, 0.2f, 0.13f), smoothness: 0.2f);
            rime = Kit.Mat("EU_FissureGlow", new Color(0.2f, 0.5f, 0.6f), new Color(0.3f, 0.95f, 1f) * 4f, 0.8f);
            hullWhite = Kit.Mat("EU_HullWhite", new Color(0.82f, 0.83f, 0.82f), smoothness: 0.45f);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var world = new GameObject("World").transform;

            BuildTerrain(world);
            BuildIceField(world);
            var lander = BuildLander(world, pressurised, out var ramp, out var cabinLights);
            var fissures = BuildFissures(world);
            BuildBase(world);
            BuildSky();

            // Bake the walkable ice (with the ramp down, so the crew can walk out), then close the ramp.
            var rampOpen = Quaternion.Euler(RampOpenEuler(), 0f, 0f);
            ramp.localRotation = rampOpen;
            var navGo = new GameObject("NavMesh");
            var surface = navGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Volume;
            surface.center = new Vector3(45f, 10f, -100f);
            surface.size = new Vector3(240f, 60f, 300f);
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            if (AssetDatabase.LoadAssetAtPath<Object>(NavMeshPath) != null) AssetDatabase.DeleteAsset(NavMeshPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
            ramp.localRotation = Quaternion.identity;

            // Sam: in the cabin, facing the hatch, rifle in hand.
            var rig = PlayerRigBuilder.Spawn(new Vector3(0f, FloorH + 0.05f, 2.2f), 180f, vacuum);
            var inv = rig.GetComponent<PlayerInventory>();
            Kit.Set(inv, "startWithBeamRifle", true);
            Kit.Set(inv, "startingMedPatches", 4);
            var cam = rig.GetComponentInChildren<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.farClipPlane = 2600f;
            PostFxKit.EnableOn(cam);
            PrefabUtility.RecordPrefabInstancePropertyModifications(cam);

            new GameObject("GameSession").AddComponent<Core.GameSession>();
            var dialogue = rig.GetComponentInChildren<DialogueManager>();

            // The landing party (Luca stays at the controls).
            var story = new GameObject("Story").transform;
            var beamMat = CombatRigBuilder.BeamMaterial("FX_ArcBeam", new Color(0.5f, 1.8f, 2.6f));
            var ashford = AstronautBuilder.Build(story, "ASHFORD", new Color(0.85f, 0.2f, 0.15f), new Vector3(-1.1f, FloorH, -1.6f), 180f, true, new Vector3(-2.6f, 0f, 3f), beamMat);
            var mara = AstronautBuilder.Build(story, "MARA", new Color(0.2f, 0.55f, 0.95f), new Vector3(1.1f, FloorH, -1.2f), 180f, true, new Vector3(2.6f, 0f, 0.6f), beamMat);
            var hana = AstronautBuilder.Build(story, "HANA", new Color(0.3f, 0.85f, 0.4f), new Vector3(-1.1f, FloorH, 0.4f), 180f, true, new Vector3(-2.2f, 0f, -2.6f), beamMat);
            AstronautBuilder.Build(lander, "LUCA", new Color(1f, 0.7f, 0.2f), new Vector3(0f, FloorH, 3.3f), 0f, false, Vector3.zero, beamMat);

            var landing = new GameObject("LandingSequence").AddComponent<LandingSequence>();
            landing.transform.SetParent(story, false);
            Kit.Set(landing, "landingRadio", Dialogue("ch05_landing_radio"));
            Kit.Set(landing, "ramp", ramp);
            Kit.Set(landing, "rampOpenEuler", new Vector3(RampOpenEuler(), 0f, 0f));
            Kit.Set(landing, "squad", new Object[] { ashford.GetComponent<SquadMember>(), mara.GetComponent<SquadMember>(), hana.GetComponent<SquadMember>() });
            Kit.Set(landing, "cabinLights", cabinLights.ToArray());
            Kit.Set(landing, "audioSource", Kit.Audio3D(ramp.gameObject, null, 2f, 25f));
            Kit.Set(landing, "hissClip", AudioAssetGenerator.Load(AudioAssetGenerator.DoorOpen));
            Kit.Set(landing, "dialogue", dialogue);

            // Ten Stalkers, buried until the quake.
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StalkerBuilder.PrefabPath);
            var stalkers = new List<Object>();
            var nest = new GameObject("Stalkers").transform;
            for (int i = 0; i < StalkerCount; i++)
            {
                var s = (GameObject)PrefabUtility.InstantiatePrefab(prefab, nest);
                s.transform.position = new Vector3(i * 3f, -60f, 0f);
                stalkers.Add(s.GetComponent<StalkerAI>());
            }

            var ambushGo = new GameObject("Ambush");
            ambushGo.transform.SetParent(story, false);
            var quakeSource = ambushGo.AddComponent<AudioSource>();
            quakeSource.spatialBlend = 0f;
            quakeSource.playOnAwake = false;
            var ambush = ambushGo.AddComponent<AmbushDirector>();
            Kit.Set(ambush, "lander", lander);
            Kit.Set(ambush, "stalkers", stalkers.ToArray());
            Kit.Set(ambush, "fissures", fissures.ToArray());
            Kit.Set(ambush, "panicRadio", Dialogue("ch05_quake_radio"));
            Kit.Set(ambush, "clearedRadio", SurfaceDialogue("eu_route_radio"));
            Kit.Set(ambush, "audioSource", quakeSource);
            Kit.Set(ambush, "quakeClip", CombatAudioGenerator.Load(CombatAudioGenerator.Quake));
            Kit.Set(ambush, "dialogue", dialogue);

            // After the ambush the story stays on the ice: Tiamat's lander, the borehole, the Listener, the ascent vehicle.
            BuildSurfaceStory(world, story, nest, prefab, dialogue,
                hana.GetComponent<SquadMember>(), ashford.GetComponent<SquadMember>(), mara.GetComponent<SquadMember>(), quakeSource);

            BuildSilhouette(world, story, dialogue);
            BuildRimeFields(world);

            // The end of what is built so far: the ascent vehicle's hatch opens and the screen holds on a card.
            var transition = new GameObject("LevelTransition").AddComponent<Core.LevelTransition>();
            Kit.Set(transition, "requiresFlag", BoardedFlag);
            Kit.Set(transition, "nextScene", System.IO.Path.GetFileNameWithoutExtension(ReturnBuilder.ScenePath));
            Kit.Set(transition, "cardTitle", "THE LAST TRANSMISSION");
            Kit.Set(transition, "cardSubtitle", "Halcyon, climbing out of Europa's gravity. One seat taken.");
            Kit.Set(transition, "delaySeconds", 6f);
            Kit.Set(transition, "dialogue", dialogue);

            BuildObjectives(story, rig, dialogue);
            BuildSuitAudio(story);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[EuropaSurfaceBuilder] Saved {ScenePath}");
        }

        static float RampOpenEuler() => -(90f + Mathf.Asin(FloorH / RampLength) * Mathf.Rad2Deg);
        const float RampLength = 2.7f;

        static TextAsset Dialogue(string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>($"{DialogueFolder}/{name}.txt");
            if (asset == null) Debug.LogError($"[EuropaSurfaceBuilder] Missing dialogue {name}");
            return asset;
        }

        // ------------------------------------------------------------------ ground

        static Material IceMaterial()
        {
            var m = Kit.Mat("EU_Ice", new Color(0.86f, 0.9f, 0.97f), smoothness: 0.72f);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(EuropaTextureGenerator.IceAlbedo));
            m.SetTextureScale("_BaseMap", new Vector2(0.35f, 0.35f));
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(EuropaTextureGenerator.IceNormal);
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// Chaos terrain: broken plates, ridges and blocky rafts, flattened around the lander and along the walk to
        /// the base, and rising into a ring of ridges at the edge so the world never shows its border.
        /// </summary>
        static void BuildTerrain(Transform parent)
        {
            const int res = 257;
            const float size = 500f, height = 60f, baseFraction = 0.4f;
            var data = new TerrainData { heightmapResolution = res };
            data.size = new Vector3(size, height, size);
            var h = new float[res, res];
            for (int zi = 0; zi < res; zi++)
                for (int xi = 0; xi < res; xi++)
                {
                    float x = xi / (float)(res - 1) * size - size / 2f;
                    float z = zi / (float)(res - 1) * size - size / 2f;
                    h[zi, xi] = baseFraction + SurfaceHeight(x, z) / height;
                }
            data.SetHeights(0, 0, h);

            var layer = new TerrainLayer
            {
                diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(EuropaTextureGenerator.IceAlbedo),
                normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(EuropaTextureGenerator.IceNormal),
                tileSize = new Vector2(26f, 26f),
                smoothness = 0.6f,
            };
            string layerPath = Data + "/EU_IceLayer.terrainlayer";
            AssetDatabase.DeleteAsset(layerPath);
            AssetDatabase.CreateAsset(layer, layerPath);
            data.terrainLayers = new[] { layer };
            string dataPath = Data + "/EU_Terrain.asset";
            AssetDatabase.DeleteAsset(dataPath);
            AssetDatabase.CreateAsset(data, dataPath);

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "ConamaraChaos";
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(-size / 2f, -baseFraction * height, -size / 2f);
            terrain = go.GetComponent<Terrain>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
            string matPath = Kit.MaterialFolder + "/EU_Terrain.mat";
            AssetDatabase.DeleteAsset(matPath);
            AssetDatabase.CreateAsset(mat, matPath);
            terrain.materialTemplate = mat;
            terrain.heightmapPixelError = 6f;
            terrain.basemapDistance = 300f;
            go.isStatic = true;
        }

        /// <summary>Surface height in metres above the base level at a world point (deterministic).</summary>
        static float SurfaceHeight(float x, float z)
        {
            float ridged = 1f - Mathf.Abs(Mathf.PerlinNoise(x * 0.011f + 100f, z * 0.011f + 40f) * 2f - 1f);
            float plates = Mathf.Floor(Mathf.PerlinNoise(x * 0.018f + 7f, z * 0.018f + 3f) * 5f) / 5f;
            float fine = Mathf.PerlinNoise(x * 0.09f, z * 0.09f);
            float chaos = ridged * ridged * 6f + plates * 3.5f + fine * 0.7f;

            float lander = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(16f, 38f, new Vector2(x, z).magnitude));
            float onPath = z < -10f && z > -232f ? Mathf.Exp(-(x * x) / (20f * 20f)) : 0f;
            float baseFlat = Flat(x, z, BasePos, 22f, 40f) * Flat(x, z, TiamatPos, 18f, 34f) * Flat(x, z, AscentPos, 20f, 34f);
            float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(232f, 250f, Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)))) * 30f;
            return chaos * lander * baseFlat * (1f - 0.93f * onPath) + edge;
        }

        /// <summary>0 at a site's centre (flat ground), rising to 1 between the two radii.</summary>
        static float Flat(float x, float z, Vector3 site, float inner, float outer) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner, outer, Vector2.Distance(new Vector2(x, z), new Vector2(site.x, site.z))));

        static bool Near(float x, float z, Vector3 site, float radius) =>
            Vector2.Distance(new Vector2(x, z), new Vector2(site.x, site.z)) < radius;

        static Vector3 OnGround(Vector3 p) => new Vector3(p.x, GroundY(p.x, p.z), p.z);

        /// <summary>
        /// Halfway to Tiamat, something three hundred metres tall stands up beyond the eastern ridge, holds against
        /// the sky, and sinks back into the ice. It is the Stalker's shape, scaled past belief, in pure black.
        /// </summary>
        static void BuildSilhouette(Transform world, Transform story, DialogueManager dialogue)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(StalkerBuilder.ModelPath);
            var dark = Kit.Mat("EU_Silhouette", new Color(0.005f, 0.006f, 0.01f), smoothness: 0f);
            var figureRoot = new GameObject("Silhouette").transform;
            figureRoot.SetParent(world, false);
            figureRoot.position = new Vector3(430f, -300f, -120f);
            figureRoot.rotation = Quaternion.LookRotation(new Vector3(-1f, 0f, 0.25f));
            if (model != null)
            {
                var body = (GameObject)PrefabUtility.InstantiatePrefab(model);
                PrefabUtility.UnpackPrefabInstance(body, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                body.transform.SetParent(figureRoot, false);
                body.transform.localScale = Vector3.one * 140f;
                foreach (var r in body.GetComponentsInChildren<Renderer>())
                {
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = dark;
                    r.sharedMaterials = mats;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            // Spines along the back, like the Needles' antennae, catching Jupiter's light at the tips.
            for (int i = 0; i < 6; i++)
            {
                var spine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(spine.GetComponent<Collider>());
                spine.name = $"Spine_{i}";
                spine.transform.SetParent(figureRoot, false);
                spine.transform.localPosition = new Vector3((i - 2.5f) * 14f, 230f + Mathf.Sin(i * 1.7f) * 20f, -20f);
                spine.transform.localRotation = Quaternion.Euler(-20f + i * 6f, 0f, (i - 2.5f) * 7f);
                spine.transform.localScale = new Vector3(3f, 45f + i * 4f, 3f);
                spine.GetComponent<Renderer>().sharedMaterial = dark;
            }

            var trig = new GameObject("Trigger_Silhouette");
            trig.transform.SetParent(story, false);
            trig.transform.position = OnGround(new Vector3(55f, 0f, -35f)) + Vector3.up * 3f;
            var box = trig.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(70f, 12f, 30f);
            var rumble = trig.AddComponent<AudioSource>();
            rumble.clip = CombatAudioGenerator.Load(CombatAudioGenerator.Feature7);
            rumble.playOnAwake = false;
            rumble.spatialBlend = 0f;   // felt through the boots
            rumble.volume = 0.9f;
            var reveal = trig.AddComponent<SilhouetteReveal>();
            Kit.Set(reveal, "figure", figureRoot);
            Kit.Set(reveal, "requiresFlag", ClearedFlag);
            Kit.Set(reveal, "dialogue", dialogue);
            Kit.Set(reveal, "radio", SurfaceDialogue("eu_silhouette_radio"));
            Kit.Set(reveal, "rumble", rumble);
        }

        /// <summary>
        /// Rime on the open ice: it looks like hoarfrost, but it is the colony's nerve tissue. Anything that
        /// happens on it is heard twice as far, and it flashes cyan when it hears you.
        /// </summary>
        static void BuildRimeFields(Transform world)
        {
            var root = new GameObject("RimeFields").transform;
            root.SetParent(world, false);
            var mat = Kit.Mat("EU_Rime", new Color(0.86f, 0.93f, 1f), new Color(0.02f, 0.06f, 0.07f), 0.85f);
            Vector3[] spots =
            {
                new Vector3(28f, 0f, -18f), new Vector3(46f, 0f, -26f), new Vector3(70f, 0f, -50f), new Vector3(88f, 0f, -58f),
                new Vector3(96f, 0f, -92f), new Vector3(78f, 0f, -130f), new Vector3(60f, 0f, -168f), new Vector3(30f, 0f, -186f),
            };
            for (int i = 0; i < spots.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = $"RimeField_{i}";
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.transform.SetParent(root, false);
                go.transform.position = OnGround(spots[i]) + Vector3.up * 0.03f;
                float r = 3f + (i % 3);
                go.transform.localScale = new Vector3(r * 2f, 0.01f, r * 2f * 0.8f);
                go.transform.rotation = Quaternion.Euler(0f, i * 37f, 0f);
                go.GetComponent<Renderer>().sharedMaterial = mat;
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var patch = go.AddComponent<RimePatch>();
                Kit.Set(patch, "glow", go.GetComponent<Renderer>());
                Kit.Set(patch, "radius", r);
            }
        }

        static TextAsset SurfaceDialogue(string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>($"{SurfaceFolder}/{name}.txt");
            if (asset == null) Debug.LogError($"[EuropaSurfaceBuilder] Missing dialogue {name}");
            return asset;
        }

        // ------------------------------------------------------------------ the surface story

        /// <summary>
        /// Tiamat's lander (with its data box and the borehole sealed from below), the drill, the Listener waiting
        /// in the bore, four more Stalkers for the drill waves, the ascent vehicle Halcyon on its pad by the base,
        /// and the <see cref="SurfaceStoryDirector"/> that plays it all in order.
        /// </summary>
        static void BuildSurfaceStory(Transform world, Transform story, Transform nest, GameObject stalkerPrefab, DialogueManager dialogue,
            SquadMember hana, SquadMember ashford, SquadMember mara, AudioSource quakeSource)
        {
            var site = new GameObject("TiamatSite").transform;
            site.SetParent(world, false);
            Vector3 T = OnGround(TiamatPos);
            var scorched = Kit.Mat("EU_TiamatHull", new Color(0.3f, 0.29f, 0.27f), smoothness: 0.2f);
            var solar = Kit.Mat("EU_SolarPanel", new Color(0.06f, 0.08f, 0.16f), new Color(0.02f, 0.03f, 0.08f), 0.85f);

            // The old lander: tipped on a broken leg, half buried, frosted.
            var hull = Cylinder(site, "Tiamat_Hull", T + new Vector3(0f, 1.3f, 0f), 1.6f, 3.4f, scorched, new Vector3(0f, 0f, 24f), collider: true);
            hull.transform.rotation = Quaternion.Euler(8f, 30f, 24f);
            Cylinder(site, "Tiamat_Engine", T + new Vector3(-0.9f, 0.3f, 0.3f), 0.7f, 0.8f, Pipe);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad;
                Vector3 foot = T + new Vector3(Mathf.Cos(a) * 2.6f, 0.05f, Mathf.Sin(a) * 2.6f);
                PipeRun(site, $"Tiamat_Leg{i}", T + new Vector3(Mathf.Cos(a) * 1.2f, i == 1 ? 0.4f : 1.4f, Mathf.Sin(a) * 1.2f), foot, 0.07f, Metal);
            }
            Box(site, "Tiamat_SolarWing", T + new Vector3(2.6f, 1.2f, -0.5f), new Vector3(3.2f, 0.05f, 1.5f), solar, Quaternion.Euler(0f, 30f, -18f));
            Box(site, "Tiamat_SolarWingBroken", T + new Vector3(-2.8f, 0.35f, 1.4f), new Vector3(2.4f, 0.05f, 1.4f), solar, Quaternion.Euler(10f, -40f, 35f));
            var frost = Kit.Mat("EU_Frost", new Color(0.88f, 0.94f, 1f), new Color(0.08f, 0.2f, 0.24f) * 0.5f, 0.9f);
            Sphere(site, "Tiamat_Frost", T + new Vector3(0.4f, 2.4f, 0.2f), 1.1f, frost).transform.localScale = new Vector3(2.4f, 0.5f, 2f);
            Sign(site, "TIAMAT  /  ESA-NASA 2167", T + new Vector3(0.2f, 1.4f, -1.65f), Vector3.back, 2.2f);

            // The green beacon the team homes on.
            Cylinder(site, "Beacon_Pole", T + new Vector3(-4f, 3f, -3f), 0.06f, 6f, Metal);
            var bulb = Sphere(site, "Beacon_Green", T + new Vector3(-4f, 6.1f, -3f), 0.2f, LedGreen);
            bulb.isStatic = false;
            Kit.Set(bulb.AddComponent<BlinkingLight>(), "color", new Color(0.3f, 1f, 0.4f));
            Point(site, "Beacon_GreenGlow", T + new Vector3(-4f, 6.1f, -3f), new Color(0.3f, 1f, 0.4f), 4f, 40f).gameObject.isStatic = false;

            // The data box: Tiamat's log, and the cargo manifest that gives Ashford away.
            Box(site, "DataBox", T + new Vector3(-2.2f, 0.4f, -2.4f), new Vector3(1f, 0.8f, 0.7f), White);
            SurfaceReadable(site, "Readable_TiamatLog", T + new Vector3(-2.45f, 0.82f, -2.4f), "Tiamat lander: recovered log",
                "TIAMAT SURFACE PACKAGE: SEISMIC LOG\n\n" +
                "2171-03-14 04:12  Borehole B2 at 212 m. Instrument string nominal.\n" +
                "2171-03-14 04:31  Periodic signal, 0.4 Hz. Four pulses, a gap, repeating. Flagged FEATURE 7.\n" +
                "2171-03-14 05:02  FEATURE 7 amplitude rising. Signal tracking the drill's own rhythm, delayed.\n" +
                "2171-03-14 05:40  B2 pressure loss. Camera: borehole wall frosting over from below.\n" +
                "2171-03-14 06:15  B2 sealed. Seal is not ice.\n" +
                "2171-03-14 06:16  Data package classified at source. Distribution: COMMAND ONLY.", "read_tiamat_log");
            SurfaceReadable(site, "Readable_CargoManifest", T + new Vector3(-1.95f, 0.82f, -2.4f), "Tiamat cargo manifest (amended 2181)",
                "TIAMAT RETURN CARGO: AMENDED FOR MERIDIAN (CREWED) 2181\n\n" +
                "ITEM 7: Sealed sample canister, cryogenic, biological rated.\n" +
                "CONSIGNEE: Cdr. D. R. Ashford, Meridian.\n" +
                "AUTHORITY: Command Directive 7.\n\n" +
                "Directive 7: locate the source of FEATURE 7 and return a viable biological sample to Earth.\n" +
                "Crew are not to be briefed. If the source responds to stimulus, continue. Response is not hostility.",
                ManifestFlag);

            // The borehole, sealed from below with glowing Rime, and the sampling drill standing over it.
            Vector3 B = OnGround(TiamatPos + new Vector3(7f, 0f, 4f));
            var hole = Cylinder(site, "Borehole", B + Vector3.up * 0.02f, 0.7f, 0.02f, Kit.Mat("EU_Bore", new Color(0.01f, 0.01f, 0.015f)));
            var boreGlow = Cylinder(site, "BoreSeal", B + Vector3.up * 0.03f, 0.55f, 0.01f, rime);
            boreGlow.isStatic = false;
            var rimeRing = new List<GameObject>();
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad;
                var r = Cylinder(site, $"EruptionRime_{i}", OnGround(B + new Vector3(Mathf.Cos(a) * 3.5f, 0f, Mathf.Sin(a) * 3.5f)) + Vector3.up * 0.03f, 1.6f, 0.01f, rime);
                r.isStatic = false;
                rimeRing.Add(r);
            }
            var drillGo = new GameObject("DrillRig");
            drillGo.transform.SetParent(site, false);
            drillGo.transform.position = B;
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad;
                PipeRun(drillGo.transform, $"Tripod{i}", B + Vector3.up * 3.2f, B + new Vector3(Mathf.Cos(a) * 1.5f, 0.02f, Mathf.Sin(a) * 1.5f), 0.05f, PipeYellow);
            }
            Box(drillGo.transform, "Motor", B + Vector3.up * 3.1f, new Vector3(0.6f, 0.5f, 0.6f), DarkMetal, collider: false);
            var bit = new GameObject("Bit").transform;
            bit.SetParent(drillGo.transform, false);
            bit.position = B + Vector3.up * 1.4f;
            Cylinder(bit, "Shaft", B + Vector3.up * 1.4f, 0.07f, 3.2f, Pipe).isStatic = false;
            Box(bit, "Flute", B + Vector3.up * 0.6f, new Vector3(0.22f, 0.5f, 0.05f), Metal, collider: false).isStatic = false;
            var work = new GameObject("WorkLight").AddComponent<Light>();
            work.transform.SetParent(drillGo.transform, false);
            work.transform.SetPositionAndRotation(B + Vector3.up * 3.6f, Quaternion.Euler(90f, 0f, 0f));
            work.type = LightType.Spot;
            work.spotAngle = 70f;
            work.range = 12f;
            work.intensity = 4f;
            work.color = new Color(1f, 0.9f, 0.75f);
            work.shadows = LightShadows.None;
            var drillHum = Kit.Audio3D(drillGo, AudioAssetGenerator.Load(AudioAssetGenerator.ElectricalHum), 2f, 30f);
            drillHum.loop = true;
            var rig = drillGo.AddComponent<DrillRig>();
            Kit.Set(rig, "bit", bit);
            Kit.Set(rig, "workLight", work);
            Kit.Set(rig, "hum", drillHum);
            var drillSpot = new GameObject("DrillSpot").transform;
            drillSpot.SetParent(site, false);
            drillSpot.position = B + new Vector3(1.8f, 0f, -1.4f);

            // Arriving at the site.
            var arrive = new GameObject("Trigger_TiamatSite");
            arrive.transform.SetParent(story, false);
            arrive.transform.position = T + Vector3.up * 2f;
            var ac = arrive.AddComponent<BoxCollider>();
            ac.isTrigger = true;
            ac.size = new Vector3(28f, 8f, 28f);
            var at = arrive.AddComponent<DialogueTrigger>();
            Kit.Set(at, "requiresFlag", ClearedFlag);
            Kit.Set(at, "setsFlagOnEnter", TiamatFlag);
            Kit.Set(at, "dialogue", dialogue);

            // Four more Stalkers come up while Hana drills.
            var wave = new List<Object>();
            for (int i = 0; i < 4; i++)
            {
                var s = (GameObject)PrefabUtility.InstantiatePrefab(stalkerPrefab, nest);
                s.name = $"DrillWave_{i}";
                s.transform.position = new Vector3(i * 3f, -70f, 10f);
                wave.Add(s.GetComponent<StalkerAI>());
            }

            // The Listener waits in the bore: it only exists once the bore erupts.
            var listenerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ListenerBuilder.PrefabPath);
            var listener = (GameObject)PrefabUtility.InstantiatePrefab(listenerPrefab, story);
            listener.transform.SetPositionAndRotation(B + new Vector3(0f, 0.05f, 1.2f), Quaternion.Euler(0f, 200f, 0f));
            var ai = listener.GetComponent<CreatureAI>();
            Kit.Set(ai, "inVacuum", true);   // out here it only hears through the ice
            listener.SetActive(false);
            var gate = story.gameObject.AddComponent<StoryGate>();
            Kit.Set(gate, "flag", EruptionFlag);
            Kit.Set(gate, "activeWhenSet", true);
            Kit.Set(gate, "targets", new Object[] { listener });
            Kit.Set(gate, "dialogue", dialogue);

            var director = new GameObject("SurfaceStory").AddComponent<SurfaceStoryDirector>();
            director.transform.SetParent(story, false);
            Kit.Set(director, "arriveRadio", SurfaceDialogue("eu_tiamat_arrive_radio"));
            Kit.Set(director, "manifestRadio", SurfaceDialogue("eu_manifest_radio"));
            Kit.Set(director, "drillRadio", SurfaceDialogue("eu_drill_radio"));
            Kit.Set(director, "eruptionRadio", SurfaceDialogue("eu_eruption_radio"));
            Kit.Set(director, "lucaRadio", SurfaceDialogue("eu_luca_radio"));
            Kit.Set(director, "ashfordRadio", SurfaceDialogue("eu_ashford_radio"));
            Kit.Set(director, "halcyonRadio", SurfaceDialogue("eu_halcyon_radio"));
            Kit.Set(director, "maraRadio", SurfaceDialogue("eu_mara_radio"));
            Kit.Set(director, "aloneRadio", SurfaceDialogue("eu_alone_radio"));
            Kit.Set(director, "mara", mara);
            var siege = new List<Object>();
            for (int i = 0; i < 6; i++)
            {
                var s = (GameObject)PrefabUtility.InstantiatePrefab(stalkerPrefab, nest);
                s.name = $"SiegeWave_{i}";
                s.transform.position = new Vector3(i * 3f, -80f, 20f);
                siege.Add(s.GetComponent<StalkerAI>());
            }
            Kit.Set(director, "siegeWave", siege.ToArray());
            Kit.Set(director, "hana", hana);
            Kit.Set(director, "ashford", ashford);
            Kit.Set(director, "drillSpot", drillSpot);
            Kit.Set(director, "drill", rig);
            Kit.Set(director, "borehole", hole.transform);
            Kit.Set(director, "drillWave", wave.ToArray());
            Kit.Set(director, "eruptionRime", rimeRing.ToArray());
            Kit.Set(director, "listener", ai);
            Kit.Set(director, "audioSource", quakeSource);
            Kit.Set(director, "quakeClip", CombatAudioGenerator.Load(CombatAudioGenerator.Eruption));
            Kit.Set(director, "dialogue", dialogue);

            var halcyon = BuildAscentVehicle(world);
            Kit.Set(director, "halcyon", halcyon);

            // Boarding: once Mara is gone, the hatch at the foot of the rocket takes Sam alone.
            var board = new GameObject("Trigger_BoardHalcyon");
            board.transform.SetParent(story, false);
            board.transform.position = OnGround(AscentPos) + new Vector3(0f, 1.5f, -3f);
            var bc = board.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(8f, 4f, 6f);
            var bt = board.AddComponent<DialogueTrigger>();
            Kit.Set(bt, "requiresFlag", "mara_lost");
            Kit.Set(bt, "setsFlagOnEnter", BoardedFlag);
            Kit.Set(bt, "dialogue", dialogue);
        }

        static void SurfaceReadable(Transform parent, string name, Vector3 position, string title, string body, string flag)
        {
            var go = Kit.Box(parent, name, position, new Vector3(0.22f, 0.04f, 0.3f), ScreenCyan);
            go.isStatic = false;
            var r = go.AddComponent<Readable>();
            r.SetContent(title, body, flag);
            EditorUtility.SetDirty(r);
        }

        /// <summary>
        /// Halcyon, the ascent vehicle: a white rocket on a pad beside the base, a launch tower with a red beacon,
        /// floodlights, and at the foot of the tower the crew hatch's keypad (Ashford's code, 4117).
        /// </summary>
        static Transform BuildAscentVehicle(Transform world)
        {
            var root = new GameObject("Halcyon").transform;
            root.SetParent(world, false);
            Vector3 A = OnGround(AscentPos);
            Cylinder(root, "Pad", A + Vector3.up * 0.1f, 9f, 0.2f, DarkMetal, collider: true);
            // The rocket.
            Cylinder(root, "Body", A + Vector3.up * 9.5f, 1.8f, 18f, hullWhite, collider: true);
            for (int i = 0; i < 3; i++) Cylinder(root, $"Band{i}", A + Vector3.up * (4f + i * 5.5f), 1.83f, 0.5f, DarkMetal);
            float r = 1.8f;
            for (int i = 0; i < 4; i++) { Cylinder(root, $"Nose{i}", A + Vector3.up * (19f + i * 1.1f), r, 1.1f, hullWhite); r *= 0.7f; }
            Sphere(root, "NoseTip", A + Vector3.up * 23.4f, 0.35f, hullWhite);
            for (int i = 0; i < 4; i++)
            {
                var fin = Box(root, $"Fin{i}", A + Quaternion.Euler(0f, i * 90f + 45f, 0f) * new Vector3(0f, 2.2f, 2.3f), new Vector3(0.12f, 3.2f, 1.6f), Orange);
                fin.transform.rotation = Quaternion.Euler(0f, i * 90f + 45f, 0f);
            }
            for (int i = 0; i < 3; i++)
                Cylinder(root, $"Engine{i}", A + Quaternion.Euler(0f, i * 120f, 0f) * new Vector3(0.7f, 0.6f, 0f), 0.5f, 0.9f, Pipe);
            Sign(root, "HALCYON", A + new Vector3(0f, 12f, -1.85f), Vector3.back, 2.4f);
            // The launch tower.
            Vector3 tower = A + new Vector3(5f, 0f, 0f);
            foreach (var (dx, dz) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
                Box(root, $"TowerPost_{dx}_{dz}", tower + new Vector3(dx * 1.2f, 12f, dz * 1.2f), new Vector3(0.25f, 24f, 0.25f), PipeYellow);
            for (float y = 3f; y < 24f; y += 3f)
            {
                Box(root, $"TowerBeam_{y}_a", tower + new Vector3(0f, y, -1.2f), new Vector3(2.6f, 0.15f, 0.15f), PipeYellow, collider: false);
                Box(root, $"TowerBeam_{y}_b", tower + new Vector3(0f, y, 1.2f), new Vector3(2.6f, 0.15f, 0.15f), PipeYellow, collider: false);
            }
            Box(root, "CrewArm", tower + new Vector3(-2.3f, 18f, 0f), new Vector3(2.4f, 0.3f, 1f), DarkMetal, collider: false);
            var beacon = Sphere(root, "TowerBeacon", tower + Vector3.up * 24.4f, 0.3f, LedRed);
            beacon.isStatic = false;
            Kit.Set(beacon.AddComponent<BlinkingLight>(), "color", new Color(1f, 0.15f, 0.1f));
            Point(root, "TowerBeaconGlow", tower + Vector3.up * 24.4f, new Color(1f, 0.2f, 0.1f), 3f, 40f).gameObject.isStatic = false;
            foreach (float s in new[] { -1f, 1f })
            {
                var flood = new GameObject($"Flood_{s}").AddComponent<Light>();
                flood.transform.SetParent(root, false);
                flood.transform.SetPositionAndRotation(A + new Vector3(s * 7f, 1f, -6f), Quaternion.LookRotation(A + Vector3.up * 10f - (A + new Vector3(s * 7f, 1f, -6f))));
                flood.type = LightType.Spot;
                flood.spotAngle = 40f;
                flood.range = 30f;
                flood.intensity = 5f;
                flood.color = new Color(1f, 0.9f, 0.75f);
                flood.shadows = LightShadows.None;
            }

            // The crew hatch keypad at the foot of the tower.
            Vector3 hatch = tower + new Vector3(0f, 1.2f, -1.6f);
            Box(root, "HatchConsole", hatch + Vector3.down * 0.6f, new Vector3(0.8f, 1.2f, 0.5f), Wall);
            var screen = Kit.Box(root, "Terminal_Halcyon", hatch + new Vector3(0f, 0.25f, -0.27f), new Vector3(0.5f, 0.35f, 0.04f), ScreenAmber);
            screen.isStatic = false;
            var armed = new GameObject("LaunchArmed");
            armed.transform.SetParent(screen.transform, false);
            var readable = armed.AddComponent<Readable>();
            readable.SetContent("HALCYON: crew hatch", "ASCENT VEHICLE HALCYON\n\nCode accepted: 4117.\nCrew hatch open. Launch sequence armed.\n\nCrew aboard: 2.", AscentFlag);
            EditorUtility.SetDirty(readable);
            var keypad = screen.AddComponent<KeypadTerminal>();
            Kit.Set(keypad, "code", AscentCode);
            Kit.Set(keypad, "unlockFlag", "halcyon_hatch_open");
            Kit.Set(keypad, "contents", readable);
            Kit.Set(keypad, "audioSource", Kit.Audio3D(screen, null, 0.8f, 8f));
            Kit.Set(keypad, "keyClip", AudioAssetGenerator.Load(AudioAssetGenerator.SwitchClick));
            Kit.Set(keypad, "acceptClip", AudioAssetGenerator.Load(AudioAssetGenerator.DoorOpen));
            return root;
        }

        static float GroundY(float x, float z) => terrain != null ? terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y : 0f;

        /// <summary>Tilted rafts and blocks of ice, the size of vans and houses, scattered across the plain.</summary>
        static void BuildIceField(Transform parent)
        {
            var root = new GameObject("IceBlocks").transform;
            root.SetParent(parent, false);
            var rng = new System.Random(1715);
            int placed = 0;
            for (int i = 0; i < 400 && placed < 90; i++)
            {
                float x = (float)(rng.NextDouble() * 360 - 180), z = (float)(rng.NextDouble() * 440 - 230);
                if (new Vector2(x, z).magnitude < 24f) continue;                                        // the landing site
                if (Mathf.Abs(x) < 10f && z < -14f && z > -228f) continue;                              // the walk to the base
                if (Near(x, z, BasePos, 30f) || Near(x, z, TiamatPos, 26f) || Near(x, z, AscentPos, 28f)) continue;
                var size = new Vector3((float)(4 + rng.NextDouble() * 13), (float)(3 + rng.NextDouble() * 9), (float)(4 + rng.NextDouble() * 13));
                var rot = Quaternion.Euler((float)(rng.NextDouble() * 24 - 12), (float)(rng.NextDouble() * 360), (float)(rng.NextDouble() * 24 - 12));
                var b = Kit.Box(root, $"IceBlock_{placed}", new Vector3(x, GroundY(x, z) + size.y * 0.25f, z), size, ice);
                b.transform.rotation = rot;
                placed++;
            }
        }

        /// <summary>Cracks across the plain ahead of the lander that split and glow when the moon quakes.</summary>
        static List<GameObject> BuildFissures(Transform parent)
        {
            var list = new List<GameObject>();
            var root = new GameObject("Fissures").transform;
            root.SetParent(parent, false);
            var rng = new System.Random(77);
            for (int i = 0; i < 10; i++)
            {
                var f = new GameObject($"Fissure_{i}");
                f.transform.SetParent(root, false);
                Vector3 p = new Vector3((float)(rng.NextDouble() * 50 - 25), 0f, (float)(-30 - rng.NextDouble() * 60));
                float yaw = (float)(rng.NextDouble() * 180);
                for (int s = 0; s < 4; s++)
                {
                    yaw += (float)(rng.NextDouble() * 50 - 25);
                    Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                    float len = (float)(3 + rng.NextDouble() * 5);
                    Vector3 c = p + dir * len * 0.5f;
                    var seg = Kit.Box(f.transform, $"Seg{s}", new Vector3(c.x, GroundY(c.x, c.z) + 0.03f, c.z), new Vector3(0.18f, 0.08f, len), rime, collider: false);
                    seg.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    seg.isStatic = false;
                    p += dir * len;
                }
                var glow = Kit.PointLight(f.transform, "Glow", new Vector3(p.x, GroundY(p.x, p.z) + 0.6f, p.z), new Color(0.3f, 0.9f, 1f), 2.5f, 9f);
                glow.gameObject.isStatic = false;
                list.Add(f);
            }
            return list;
        }

        // ------------------------------------------------------------------ the lander Wren

        static Transform BuildLander(Transform parent, EnvironmentProfile pressurised, out Transform rampPivot, out List<Light> cabinLights)
        {
            var root = new GameObject("Lander_Wren").transform;
            root.SetParent(parent, false);
            root.position = new Vector3(0f, GroundY(0f, 0f), 0f);
            Vector3 O = root.position;
            Vector3 centre = O + new Vector3(0f, FloorH + 1.0f, 0f);
            cabinLights = new List<Light>();

            // Hull: a ring of curved plates around the cabin (the inside is the cabin wall, the outside the hull).
            const int segments = 24;
            float chord = 2f * CabinR * Mathf.Sin(Mathf.PI / segments) + 0.04f;
            for (int i = 0; i < segments; i++)
            {
                float a = i * 360f / segments;
                var dir = Quaternion.Euler(0f, 0f, -a) * Vector3.up;
                if (dir.y < -0.42f) continue; // the floor closes the bottom of the ring
                Box(root, $"Hull_{i}", centre + dir * CabinR, new Vector3(chord, 0.12f, CabinHalf * 2f), i % 3 == 0 ? Frame : Wall, Quaternion.Euler(0f, 0f, -a));
            }
            for (float z = -CabinHalf + 0.6f; z < CabinHalf; z += 1.2f)
                for (int i = 0; i < segments; i++)
                {
                    float a = i * 360f / segments;
                    var dir = Quaternion.Euler(0f, 0f, -a) * Vector3.up;
                    if (dir.y < -0.42f) continue;
                    Box(root, $"Rib_{z:0.0}_{i}", centre + dir * (CabinR - 0.1f) + Vector3.forward * z, new Vector3(chord, 0.08f, 0.1f), Frame, Quaternion.Euler(0f, 0f, -a), collider: false);
                }

            // Floor, underbody, engine bell and four landing legs.
            Block(root, "CabinFloor", O + new Vector3(-2.15f, FloorH - 0.12f, -CabinHalf), O + new Vector3(2.15f, FloorH, CabinHalf), Floor);
            Block(root, "Underbody", O + new Vector3(-1.9f, FloorH - 0.75f, -CabinHalf + 0.3f), O + new Vector3(1.9f, FloorH - 0.12f, CabinHalf - 0.3f), DarkMetal);
            Cylinder(root, "EngineBell", O + new Vector3(0f, FloorH - 0.95f, 1.4f), 0.6f, 0.5f, Pipe);
            foreach (var (lx, lz) in new[] { (-2.4f, -3f), (2.4f, -3f), (-2.4f, 3f), (2.4f, 3f) })
            {
                Vector3 top = O + new Vector3(lx * 0.75f, FloorH - 0.3f, lz * 0.85f), pad = O + new Vector3(lx * 1.25f, 0.06f, lz * 1.1f);
                PipeRun(root, $"Leg_{lx}_{lz}", top, pad, 0.09f, Metal);
                Cylinder(root, $"Pad_{lx}_{lz}", pad, 0.45f, 0.1f, DarkMetal, collider: true);
            }

            // Rear bulkhead with the hatch opening (2.2 m x 2.6 m).
            float back = -CabinHalf - 0.06f;
            RoundWall(root, "RearWall", O, centre, back, Wall, new Rect(-1.1f, FloorH - 0.2f, 2.2f, RampLength + 0.15f));
            Box(root, "HatchFrame_Hazard", O + new Vector3(0f, FloorH + RampLength + 0.05f, back + 0.08f), new Vector3(2.3f, 0.12f, 0.03f), Hazard, collider: false);

            var pivot = new GameObject("RampHinge").transform;
            pivot.SetParent(root, false);
            pivot.position = O + new Vector3(0f, FloorH, back);
            var slab = Box(pivot, "Ramp", pivot.position + new Vector3(0f, RampLength * 0.5f, 0f), new Vector3(2.2f, RampLength, 0.12f), DoorPanel);
            slab.isStatic = false;
            Box(pivot, "RampStripe", pivot.position + new Vector3(0f, RampLength - 0.25f, 0.07f), new Vector3(2.0f, 0.18f, 0.02f), Hazard, collider: false).isStatic = false;
            rampPivot = pivot;

            // Cockpit bulkhead with two windows, and the flight console under them.
            float front = CabinHalf + 0.06f;
            RoundWall(root, "CockpitWall", O, centre, front, Wall, new Rect(-1.6f, FloorH + 1.1f, 1.45f, 0.9f), new Rect(0.15f, FloorH + 1.1f, 1.45f, 0.9f));
            Box(root, "FlightConsole", O + new Vector3(0f, FloorH + 0.75f, front - 0.45f), new Vector3(3.0f, 0.12f, 0.7f), DarkMetal, Quaternion.Euler(-25f, 0f, 0f));
            for (int i = 0; i < 4; i++)
                Box(root, $"ConsoleScreen_{i}", O + new Vector3(-1.1f + i * 0.73f, FloorH + 0.84f, front - 0.42f), new Vector3(0.55f, 0.02f, 0.36f), i % 2 == 0 ? ScreenCyan : ScreenAmber, Quaternion.Euler(-25f, 0f, 0f), collider: false);

            // Crash couches along both walls, lockers, overhead pipes and light strips.
            foreach (float sx in new[] { -1f, 1f })
            {
                for (int i = 0; i < 2; i++)
                {
                    float z = -1.4f + i * 2.0f;
                    Vector3 seat = O + new Vector3(sx * 1.55f, FloorH + 0.45f, z);
                    Box(root, $"Couch_{sx}_{i}", seat, new Vector3(0.6f, 0.12f, 0.62f), Fabric);
                    Box(root, $"CouchBack_{sx}_{i}", seat + new Vector3(sx * 0.32f, 0.45f, 0f), new Vector3(0.12f, 0.9f, 0.62f), Fabric, Quaternion.Euler(0f, 0f, sx * 12f));
                    Box(root, $"CouchStrap_{sx}_{i}", seat + new Vector3(sx * 0.25f, 0.5f, 0f), new Vector3(0.02f, 0.7f, 0.08f), Orange, Quaternion.Euler(0f, 0f, sx * 12f), collider: false);
                    Box(root, $"CouchLeg_{sx}_{i}", seat + new Vector3(0f, -0.25f, 0f), new Vector3(0.12f, 0.38f, 0.12f), Metal);
                }
                Box(root, $"Lockers_{sx}", O + new Vector3(sx * 1.85f, FloorH + 1.2f, 2.6f), new Vector3(0.45f, 1.8f, 1.2f), White);
                PipeRun(root, $"Pipe_{sx}", O + new Vector3(sx * 1.25f, FloorH + 2.75f, -CabinHalf + 0.3f), O + new Vector3(sx * 1.25f, FloorH + 2.75f, CabinHalf - 0.3f), 0.05f, sx > 0 ? PipeYellow : Pipe);
                Box(root, $"LightStrip_{sx}", O + new Vector3(sx * 0.7f, FloorH + 2.95f, 0f), new Vector3(0.15f, 0.03f, 6.5f), LightPanel, collider: false);
                Box(root, $"FloorLeds_{sx}", O + new Vector3(sx * 1.95f, FloorH + 0.08f, 0f), new Vector3(0.03f, 0.03f, 7f), LedRed, collider: false);
            }
            Sign(root, "WREN  /  LANDER 2", O + new Vector3(0f, FloorH + 2.4f, back + 0.12f), Vector3.forward, 1.8f);
            Sign(root, "WREN", centre + new Vector3(CabinR + 0.08f, 0.4f, 0f), Vector3.right, 2.2f);

            cabinLights.Add(Point(root, "CabinLight_A", O + new Vector3(0f, FloorH + 2.5f, -1.5f), new Color(0.85f, 0.92f, 1f), 2.2f, 7f));
            cabinLights.Add(Point(root, "CabinLight_B", O + new Vector3(0f, FloorH + 2.5f, 2f), new Color(0.85f, 0.92f, 1f), 1.8f, 7f));
            Point(root, "CabinRed", O + new Vector3(0f, FloorH + 0.4f, 0f), new Color(1f, 0.2f, 0.1f), 0.8f, 5f);

            LanderExterior(root, O, centre);

            // Exterior: a beacon on top and a floodlight over the ramp.
            var beacon = Sphere(root, "Beacon", centre + Vector3.up * (CabinR + 0.4f), 0.12f, LedRed);
            beacon.isStatic = false;
            var blink = beacon.AddComponent<BlinkingLight>();
            Kit.Set(blink, "color", new Color(1f, 0.15f, 0.1f));
            var flood = new GameObject("RampFlood").AddComponent<Light>();
            flood.transform.SetParent(root, false);
            flood.transform.SetPositionAndRotation(O + new Vector3(0f, FloorH + 3.0f, back - 0.3f), Quaternion.Euler(55f, 180f, 0f));
            flood.type = LightType.Spot;
            flood.spotAngle = 80f;
            flood.range = 16f;
            flood.intensity = 3f;
            flood.color = new Color(1f, 0.92f, 0.8f);
            flood.shadows = LightShadows.None;

            // Inside the cabin the air is in: footsteps and speeds of the pressurised profile.
            var zone = new GameObject("CabinAir");
            zone.transform.SetParent(root, false);
            zone.transform.position = centre;
            var zc = zone.AddComponent<BoxCollider>();
            zc.isTrigger = true;
            zc.size = new Vector3(4f, 3.2f, CabinHalf * 2f - 0.3f);
            Kit.Set(zone.AddComponent<EnvironmentZone>(), "profile", pressurised);
            return root;
        }

        /// <summary>
        /// A round end bulkhead for the cylindrical cabin, built from horizontal slices that follow the circle, with
        /// rectangular openings cut out (the hatch, the cockpit windows). Hole rects are in lander-local X and Y.
        /// </summary>
        static void RoundWall(Transform root, string name, Vector3 O, Vector3 centre, float z, Material mat, params Rect[] holes)
        {
            float cy = centre.y - O.y, top = cy + CabinR + 0.08f;
            const float step = 0.2f;
            int n = 0;
            for (float y = FloorH - 0.12f; y < top; y += step)
            {
                float mid = Mathf.Min(y + step * 0.5f, top) - cy;
                float half = Mathf.Sqrt(Mathf.Max(0f, (CabinR + 0.08f) * (CabinR + 0.08f) - mid * mid));
                if (half < 0.05f) continue;
                // Split this slice around any opening it crosses.
                var spans = new List<Vector2> { new Vector2(-half, half) };
                foreach (var h in holes)
                {
                    if (y + step <= h.yMin || y >= h.yMax) continue;
                    var next = new List<Vector2>();
                    foreach (var s in spans)
                    {
                        if (h.xMin > s.x) next.Add(new Vector2(s.x, Mathf.Min(s.y, h.xMin)));
                        if (h.xMax < s.y) next.Add(new Vector2(Mathf.Max(s.x, h.xMax), s.y));
                    }
                    spans = next;
                }
                foreach (var s in spans)
                    if (s.y - s.x > 0.02f)
                        Block(root, $"{name}_{n++}", O + new Vector3(s.x, y, z - 0.06f), O + new Vector3(s.y, Mathf.Min(y + step, top), z + 0.06f), mat);
            }
            // A frame ring around the opening edge reads as a pressure door, not a hole.
            foreach (var h in holes)
            {
                Block(root, $"{name}_FrameL", O + new Vector3(h.xMin - 0.08f, h.yMin, z - 0.09f), O + new Vector3(h.xMin, h.yMax, z + 0.09f), Frame, collider: false);
                Block(root, $"{name}_FrameR", O + new Vector3(h.xMax, h.yMin, z - 0.09f), O + new Vector3(h.xMax + 0.08f, h.yMax, z + 0.09f), Frame, collider: false);
            }
        }

        /// <summary>The outside of a spacecraft: side tanks, a cluster of engine bells, thruster quads, an antenna.</summary>
        static void LanderExterior(Transform root, Vector3 O, Vector3 centre)
        {
            foreach (float sx in new[] { -1f, 1f })
            {
                Vector3 tank = O + new Vector3(sx * (CabinR + 0.55f), FloorH - 0.1f, 0f);
                Cylinder(root, $"Tank_{sx}", tank, 0.55f, CabinHalf * 1.7f, hullWhite, new Vector3(90f, 0f, 0f), collider: true);
                Sphere(root, $"TankCapF_{sx}", tank + Vector3.forward * CabinHalf * 0.85f, 0.55f, hullWhite);
                Sphere(root, $"TankCapB_{sx}", tank + Vector3.back * CabinHalf * 0.85f, 0.55f, hullWhite);
                Box(root, $"TankStrap_{sx}", tank, new Vector3(1.15f, 1.15f, 0.12f), Orange, collider: false);
                foreach (float qz in new[] { -CabinHalf + 0.5f, CabinHalf - 0.5f })
                {
                    Vector3 q = centre + new Vector3(sx * (CabinR + 0.05f), 0.9f, qz);
                    Box(root, $"Rcs_{sx}_{qz}", q, new Vector3(0.25f, 0.25f, 0.25f), DarkMetal, collider: false);
                    Cylinder(root, $"RcsNozzle_{sx}_{qz}", q + new Vector3(sx * 0.18f, 0f, 0f), 0.06f, 0.12f, Pipe, new Vector3(0f, 0f, 90f));
                }
            }
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 0.9f;
                Cylinder(root, $"Engine_{i}", O + new Vector3(x, FloorH - 0.95f, -1.2f), 0.38f, 0.55f, Pipe);
                Cylinder(root, $"EngineThroat_{i}", O + new Vector3(x, FloorH - 0.62f, -1.2f), 0.2f, 0.2f, DarkMetal);
            }
            Cylinder(root, "AntennaMast", centre + new Vector3(0.9f, CabinR + 0.6f, 1.5f), 0.04f, 1.2f, Metal);
            var dish = Sphere(root, "AntennaDish", centre + new Vector3(0.9f, CabinR + 1.25f, 1.5f), 0.35f, White);
            dish.transform.localScale = new Vector3(0.7f, 0.18f, 0.7f);
            Box(root, "DorsalFairing", centre + new Vector3(0f, CabinR + 0.12f, 0f), new Vector3(0.9f, 0.25f, CabinHalf * 1.4f), hullWhite, collider: false);
        }

        // ------------------------------------------------------------------ Conamara Base, from outside

        static void BuildBase(Transform parent)
        {
            var root = new GameObject("ConamaraBase_Exterior").transform;
            root.SetParent(parent, false);
            Vector3 B = BasePos + new Vector3(0f, GroundY(BasePos.x, BasePos.z), 0f);
            Cylinder(root, "HabModule_A", B + new Vector3(-6f, 3.2f, -4f), 3.2f, 16f, hullWhite, new Vector3(0f, 0f, 90f), collider: true);
            Cylinder(root, "HabModule_B", B + new Vector3(8f, 3.2f, -8f), 3.2f, 12f, hullWhite, new Vector3(90f, 0f, 0f), collider: true);
            var dome = Sphere(root, "Dome", B + new Vector3(4f, 0f, 3f), 5.5f, hullWhite);
            dome.AddComponent<SphereCollider>();
            Box(root, "Airlock", B + new Vector3(0f, 1.9f, 7f), new Vector3(4f, 3.8f, 4.5f), Wall);
            Box(root, "AirlockDoor", B + new Vector3(0f, 1.3f, 9.28f), new Vector3(1.8f, 2.4f, 0.08f), DoorPanel, collider: false);
            Box(root, "AirlockLight", B + new Vector3(0f, 2.75f, 9.3f), new Vector3(1.4f, 0.1f, 0.05f), LedGreen, collider: false);
            Sign(root, "CONAMARA BASE", B + new Vector3(0f, 3.3f, 9.32f), Vector3.forward, 2.6f);
            Cylinder(root, "Mast", B + new Vector3(-3f, 7f, 6f), 0.12f, 14f, Metal);
            var beacon = Sphere(root, "BaseBeacon", B + new Vector3(-3f, 14.2f, 6f), 0.35f, LedRed);
            beacon.isStatic = false;
            Kit.Set(beacon.AddComponent<BlinkingLight>(), "color", new Color(1f, 0.15f, 0.1f));
            Point(root, "BeaconGlow", B + new Vector3(-3f, 14.2f, 6f), new Color(1f, 0.2f, 0.12f), 3f, 25f).gameObject.isStatic = false;
            var flood = new GameObject("AirlockFlood").AddComponent<Light>();
            flood.transform.SetParent(root, false);
            flood.transform.SetPositionAndRotation(B + new Vector3(0f, 4.2f, 10f), Quaternion.Euler(60f, 0f, 0f));
            flood.type = LightType.Spot;
            flood.spotAngle = 70f;
            flood.range = 20f;
            flood.intensity = 4f;
            flood.color = new Color(1f, 0.85f, 0.6f);
            flood.shadows = LightShadows.None;
        }

        // ------------------------------------------------------------------ sky

        static void BuildSky()
        {
            var skyMat = new Material(Shader.Find("Skybox/Panoramic"));
            skyMat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(EuropaTextureGenerator.Sky));
            skyMat.SetFloat("_Exposure", 1.0f);
            string skyPath = Kit.MaterialFolder + "/EU_Sky.mat";
            AssetDatabase.DeleteAsset(skyPath);
            AssetDatabase.CreateAsset(skyMat, skyPath);
            RenderSettings.skybox = skyMat;

            var sky = new GameObject("Sky").transform;
            // Jupiter hangs over the plain beyond the hatch (it never moves in Europa's sky: tidal lock).
            var jupiterTex = JupiterTextureGenerator.Generate();
            var jMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            jMat.SetTexture("_BaseMap", jupiterTex);
            jMat.SetFloat("_Smoothness", 0.05f);
            jMat.SetTexture("_EmissionMap", jupiterTex);
            jMat.SetColor("_EmissionColor", new Color(0.05f, 0.04f, 0.03f));
            jMat.EnableKeyword("_EMISSION");
            string jPath = Kit.MaterialFolder + "/EU_Jupiter.mat";
            AssetDatabase.DeleteAsset(jPath);
            AssetDatabase.CreateAsset(jMat, jPath);
            Body(sky, "Jupiter", Dir(180f, 15f) * 1850f, 1150f, jMat, new Vector3(0f, 40f, 3f));
            Body(sky, "Io", Dir(205f, 27f) * 1950f, 55f, Kit.Mat("EU_Io", new Color(0.95f, 0.82f, 0.42f), smoothness: 0f), Vector3.zero);
            Body(sky, "Ganymede", Dir(150f, 9f) * 1950f, 42f, Kit.Mat("EU_Ganymede", new Color(0.6f, 0.57f, 0.53f), smoothness: 0f), Vector3.zero);
            Body(sky, "Callisto", Dir(110f, 34f) * 1950f, 30f, Kit.Mat("EU_Callisto", new Color(0.33f, 0.3f, 0.28f), smoothness: 0f), Vector3.zero);
            // Saturn and Earth are only bright points from here.
            Body(sky, "Saturn", Dir(60f, 48f) * 2000f, 7f, CombatRigBuilder.BeamMaterial("EU_SaturnPoint", new Color(1.8f, 1.5f, 1.0f)), Vector3.zero);
            Body(sky, "Earth", Dir(300f, 22f) * 2000f, 5f, CombatRigBuilder.BeamMaterial("EU_EarthPoint", new Color(0.6f, 1.0f, 2.0f)), Vector3.zero);

            // A small, hard Sun low on the other side: long shadows, a half-lit Jupiter.
            Vector3 sunDir = Dir(70f, 16f);
            Body(sky, "Sun", sunDir * 2050f, 26f, CombatRigBuilder.BeamMaterial("EU_Sun", new Color(9f, 8.4f, 7.6f)), Vector3.zero);
            var sun = new GameObject("Sunlight").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.LookRotation(-sunDir);
            sun.color = new Color(1f, 0.95f, 0.88f);
            sun.intensity = 2.1f;
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.035f, 0.04f, 0.06f);
            RenderSettings.ambientEquatorColor = new Color(0.1f, 0.07f, 0.045f);   // Jupiter-shine
            RenderSettings.ambientGroundColor = new Color(0.02f, 0.025f, 0.035f);
            RenderSettings.fog = false;

            // Cold, dark, a little grainy: it should feel like footage from somewhere people should not be.
            PostFxKit.CinematicVolume(Data + "/EU_PostFx.asset", 0.15f, -18f, 18f, new Color(0.9f, 0.95f, 1f));
        }

        static Vector3 Dir(float azimuth, float elevation) =>
            Quaternion.Euler(-elevation, azimuth, 0f) * Vector3.forward;

        static void Body(Transform parent, string name, Vector3 position, float diameter, Material mat, Vector3 euler)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
            go.transform.localScale = Vector3.one * diameter;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        // ------------------------------------------------------------------ objectives and sound

        static void BuildObjectives(Transform parent, GameObject rig, DialogueManager dialogue)
        {
            var mission = new GameObject("Mission").AddComponent<MissionManager>();
            mission.transform.SetParent(parent, false);
            Kit.Set(mission, "dialogue", dialogue);
            Kit.Set(mission, "objectiveText", rig.transform.Find("HUD/Objective").GetComponent<UnityEngine.UI.Text>());
            Kit.Set(mission, "completeText", "Launch");
            var so = new SerializedObject(mission);
            var list = so.FindProperty("objectives");
            list.arraySize = 10;
            ConamaraChapterSixBuilder.Objective(list.GetArrayElementAtIndex(0), "Wait for the ramp", new string[0], new[] { RampFlag });
            ConamaraChapterSixBuilder.Objective(list.GetArrayElementAtIndex(1), "Walk out onto Europa with the team", new string[0], new[] { QuakeFlag });
            ConamaraChapterSixBuilder.Objective(list.GetArrayElementAtIndex(2), "SURVIVE", new string[0], new[] { ClearedFlag });
            ConamaraChapterSixBuilder.Objective(list.GetArrayElementAtIndex(3), "Reach Tiamat's lander (the green beacon, north-east)", new string[0], new[] { TiamatFlag });
            ConamaraChapterSixBuilder.Objective(list.GetArrayElementAtIndex(4), "Search Tiamat's data box (read the cargo manifest)", new string[0], new[] { ManifestFlag });
            ConamaraChapterSixBuilder.Objective(list.GetArrayElementAtIndex(5), "Cover Hana while she drills", new string[0], new[] { DrillDoneFlag });
            ConamaraChapterSixBuilder.Objective(list.GetArrayElementAtIndex(6), "KILL IT: knock it down, then keep firing", new string[0], new[] { KilledFlag });
            ConamaraChapterSixBuilder.Objective(list.GetArrayElementAtIndex(7), "Get to the ascent vehicle Halcyon (by the base, red tower)", new string[0], new[] { AscentFlag });
            ConamaraChapterSixBuilder.Objective(list.GetArrayElementAtIndex(8), "HOLD THE PAD until the hatch opens", new string[0], new[] { "mara_lost" });
            ConamaraChapterSixBuilder.Objective(list.GetArrayElementAtIndex(9), "Board Halcyon", new string[0], new[] { BoardedFlag });
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Inside the helmet: slow breathing and the suit's fans. Outside the helmet there is no sound at all.</summary>
        static void BuildSuitAudio(Transform parent)
        {
            var go = new GameObject("SuitAudio");
            go.transform.SetParent(parent, false);
            var breath = go.AddComponent<AudioSource>();
            breath.clip = CombatAudioGenerator.Load(CombatAudioGenerator.SuitBreath);
            breath.loop = true;
            breath.spatialBlend = 0f;
            breath.volume = 0.3f;
            breath.playOnAwake = true;
            var fans = new GameObject("SuitFans").AddComponent<AudioSource>();
            fans.transform.SetParent(go.transform, false);
            fans.clip = AudioAssetGenerator.Load(AudioAssetGenerator.Ventilation);
            fans.loop = true;
            fans.spatialBlend = 0f;
            fans.volume = 0.08f;
            fans.playOnAwake = true;
        }
    }
}
