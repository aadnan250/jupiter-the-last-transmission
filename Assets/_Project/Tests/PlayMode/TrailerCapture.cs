using System.Collections;
using System.IO;
using System.Linq;
using Jupiter.Combat;
using Jupiter.Core;
using Jupiter.Creatures;
using Jupiter.Narrative;
using Jupiter.Player;
using Jupiter.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Jupiter.Tests
{
    /// <summary>
    /// Records the trailer's shots from the running game: camera moves through each level at a fixed 30 fps
    /// (Time.captureFramerate), with the crew idling, the Stalkers bursting out of the ice and the silhouette
    /// rising, written as JPG frames to Builds/Trailer/&lt;shot&gt;/. Explicit: only runs when asked for by name.
    /// Tools/TrailerPipeline/edit_trailer.py cuts the frames together with cards, music and voices.
    /// </summary>
    [Explicit("Records trailer frames; run on purpose with -testFilter Jupiter.Tests.TrailerCapture")]
    public class TrailerCapture
    {
        const int Fps = 30, W = 1280, H = 720;
        const string Root = "Builds/Trailer";

        Camera cam;
        PlayerController player;
        RenderTexture rt;
        Texture2D readback;
        float shake;

        IEnumerator Load(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            yield return null;
            GameSession.Current.AutosaveEnabled = false;
            GameSession.Current.NewGame();
            player = Object.FindAnyObjectByType<PlayerController>();
            cam = player.GetComponentInChildren<Camera>();
            var look = cam.GetComponent<CameraController>();
            if (look != null) look.enabled = false;
            foreach (var s in cam.GetComponents<CameraShake>()) s.enabled = false;
            cam.transform.SetParent(null, true);
            // The first-person arms and weapons ride on the camera: keep them out of the trailer.
            foreach (var r in cam.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            cam.fieldOfView = 55f;
            yield return null;
        }

        static Vector3 Ease(Vector3 a, Vector3 b, float t) => Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t));

        /// <summary>Records one shot: the camera glides from one viewpoint to another over the given seconds.</summary>
        IEnumerator Shot(string name, float seconds, Vector3 fromPos, Vector3 fromLook, Vector3 toPos, Vector3 toLook,
            System.Action<int> everyFrame = null, System.Func<Vector3> lookOverride = null)
        {
            string dir = Path.Combine(Root, name);
            Directory.CreateDirectory(dir);
            foreach (var f in Directory.GetFiles(dir, "*.jpg")) File.Delete(f);
            int frames = Mathf.RoundToInt(seconds * Fps);
            var health = player.GetComponent<PlayerHealth>();
            for (int i = 0; i < frames; i++)
            {
                float t = frames > 1 ? i / (float)(frames - 1) : 0f;
                everyFrame?.Invoke(i);
                if (health != null && health.Model.Current < health.Model.Max) health.Model.Heal(1000f);   // nobody dies on camera
                Vector3 pos = Ease(fromPos, toPos, t);
                Vector3 target = lookOverride != null ? lookOverride() : Ease(fromLook, toLook, t);
                shake = Mathf.MoveTowards(shake, 0f, 1f / Fps * 0.5f);
                Vector3 jitter = shake > 0f ? new Vector3(Mathf.PerlinNoise(i * 0.7f, 0f) - 0.5f, Mathf.PerlinNoise(0f, i * 0.7f) - 0.5f, 0f) * shake * 0.25f : Vector3.zero;
                cam.transform.SetPositionAndRotation(pos + jitter, Quaternion.LookRotation(target - pos));
                yield return null;
                Render(Path.Combine(dir, $"f_{i:0000}.jpg"));
            }
        }

        void Render(string file)
        {
            if (rt == null) { rt = new RenderTexture(W, H, 24); readback = new Texture2D(W, H, TextureFormat.RGB24, false); }
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            RenderTexture.active = rt;
            readback.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            readback.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(file, readback.EncodeToJPG(92));
        }

        [UnitySetUp]
        public IEnumerator Begin()
        {
            Time.captureFramerate = Fps;
            AudioListener.volume = 0f;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.captureFramerate = 0;
            AudioListener.volume = 1f;
            if (rt != null) { rt.Release(); Object.Destroy(rt); Object.Destroy(readback); rt = null; }
            yield return null;
        }

        [UnityTest]
        public IEnumerator RecordAllShots()
        {
            // Chapter 1: dawn at Cabo Lumen, the family, the stack on the pad.
            yield return Load("L01_CaboLumen");
            yield return Shot("01_lounge", 4.5f, new Vector3(1.8f, 1.6f, -16.8f), new Vector3(-0.8f, 1.3f, -11.4f),
                new Vector3(0.5f, 1.5f, -13.9f), new Vector3(-0.8f, 1.35f, -11.4f));
            yield return Shot("02_rocket", 3.5f, new Vector3(-14f, 1.8f, 10f), new Vector3(0f, 10f, 48f),
                new Vector3(-13f, 2.2f, 13f), new Vector3(0f, 46f, 48f));

            // The voyage: Jupiter, and the crew in the galley.
            yield return Load("L02_Meridian");
            var j = GameObject.Find("Jupiter_Placeholder").transform;
            Vector3 toShip = (Vector3.zero - j.position).normalized;
            yield return Shot("03_jupiter", 4f, j.position + toShip * j.lossyScale.x * 1.7f, j.position,
                j.position + toShip * j.lossyScale.x * 1.2f, j.position);
            yield return Shot("04_galley", 3.5f, new Vector3(-3.0f, 1.6f, 13.6f), new Vector3(-5.8f, 1.45f, 17.3f),
                new Vector3(-4.4f, 1.6f, 14.7f), new Vector3(-5.8f, 1.45f, 17.3f));

            // Europa.
            yield return Load("L03_EuropaSurface");
            var story = Object.FindAnyObjectByType<DialogueManager>().Story;
            yield return Shot("05_plain", 4f, new Vector3(4f, 2.2f, -12f), new Vector3(0f, 12f, -120f),
                new Vector3(2f, 2.8f, -26f), new Vector3(0f, 14f, -120f));
            var landing = Object.FindAnyObjectByType<LandingSequence>();
            landing.OpenRamp();
            yield return Shot("06_lander", 3.5f, new Vector3(13f, 2.4f, -17f), new Vector3(0f, 2.2f, 0f),
                new Vector3(8.5f, 3.2f, -12f), new Vector3(0f, 2.4f, 0f));

            // The quake: they come up out of the ice all around.
            story.SetFlag("ramp_open");
            foreach (var m in SquadMember.All) m.Release();
            var cc = player.GetComponent<CharacterController>();
            cc.enabled = false; player.transform.position = new Vector3(0f, 0.2f, -60f); cc.enabled = true;
            var ambush = Object.FindAnyObjectByType<AmbushDirector>();
            yield return Shot("07_ambush", 8f, new Vector3(7f, 1.4f, -50f), new Vector3(0f, 1.2f, -62f),
                new Vector3(5f, 1.7f, -54f), new Vector3(-1f, 1.6f, -63f),
                i => { if (i == 15) { ambush.Begin(); shake = 1f; } });

            // One of them, coming straight at the lens.
            var runner = StalkerAI.All.Where(s => s.Active).OrderBy(s => Vector3.Distance(s.transform.position, player.transform.position)).FirstOrDefault();
            if (runner != null)
            {
                Vector3 eye = player.transform.position + Vector3.up * 0.9f;
                yield return Shot("08_charge", 3f, eye, runner.transform.position + Vector3.up, eye + Vector3.back * 0.4f, runner.transform.position + Vector3.up,
                    null, () => runner.transform.position + Vector3.up * 1.1f);
            }

            // Beyond the ridge, black against Jupiter.
            story.SetFlag("ambush_cleared");
            var reveal = Object.FindAnyObjectByType<SilhouetteReveal>();
            reveal.Begin();
            Vector3 figTop = new Vector3(40f, 120f, -480f);
            yield return Shot("09_silhouette", 7f, new Vector3(46f, 26f, -38f), figTop, new Vector3(45f, 24f, -44f), figTop);

            // Chapter 9: the canister, alone on the way home.
            yield return Load("L05_Return");
            yield return Shot("10_canister", 4f, new Vector3(1.9f, 1.55f, -1.8f), new Vector3(0f, 1.3f, 1.5f),
                new Vector3(0.8f, 1.45f, -0.4f), new Vector3(0f, 1.32f, 1.5f));
            Assert.Pass("frames written to " + Path.GetFullPath(Root));
        }
    }
}
