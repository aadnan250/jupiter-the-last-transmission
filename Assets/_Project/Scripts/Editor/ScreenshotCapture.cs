using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Renders fixed viewpoints of the Meridian level to docs/images for the README and progress tracking.
    /// Menu: Jupiter > Capture Meridian Screenshots. Needs a GPU (do not use -nographics in batch mode).
    /// </summary>
    public static class ScreenshotCapture
    {
        const string OutputFolder = "docs/images";

        [MenuItem("Jupiter/Capture Meridian Screenshots")]
        public static void CaptureMeridian()
        {
            EditorSceneManager.OpenScene(MeridianGreyboxBuilder.ScenePath);
            var cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null) { Debug.LogError("[ScreenshotCapture] No camera in scene"); return; }

            Directory.CreateDirectory(OutputFolder);
            Shot(cam, "phase2-corridor.png", new Vector3(0f, 1.62f, 2f), new Vector3(4f, 0f, 0f));
            Shot(cam, "phase2-observation-deck.png", new Vector3(-1.5f, 1.62f, 41f), new Vector3(-6f, 12f, 0f));
            Shot(cam, "phase2-galley.png", new Vector3(-2.2f, 1.62f, 11.2f), new Vector3(12f, -60f, 0f));
            Shot(cam, "phase2-command.png", new Vector3(2.4f, 1.62f, 1.2f), new Vector3(12f, 45f, 0f));
            // Crew close-ups: aim the camera at each character's head from about 1.3 m in front of them.
            CloseUp(cam, "crew-mara.png", "Crew_MARA", 1.3f);
            CloseUp(cam, "crew-ashford.png", "Crew_ASHFORD", 1.4f);
            CloseUp(cam, "crew-luca.png", "Crew_LUCA", 1.4f);
            CloseUp(cam, "crew-hana.png", "Crew_HANA", 1.3f);
            Shot(cam, "crew-galley.png", new Vector3(-2.4f, 1.62f, 14.2f), new Vector3(6f, -70f, 0f));
            Shot(cam, "ship-hygiene.png", new Vector3(-2.3f, 1.62f, -1.0f), new Vector3(8f, -125f, 0f));
            Shot(cam, "ship-science.png", new Vector3(2.3f, 1.62f, 11.0f), new Vector3(10f, 50f, 0f));
            Shot(cam, "ship-medbay.png", new Vector3(-2.2f, 1.62f, 21.0f), new Vector3(10f, -55f, 0f));
            Shot(cam, "ship-workshop.png", new Vector3(-2.0f, 1.62f, 37.2f), new Vector3(8f, -125f, 0f));
            Shot(cam, "ship-storage.png", new Vector3(2.2f, 1.62f, -0.8f), new Vector3(8f, 125f, 0f));
            Shot(cam, "ship-comms.png", new Vector3(2.2f, 1.62f, 20.8f), new Vector3(8f, 40f, 0f));
            Debug.Log($"[ScreenshotCapture] Saved screenshots to {OutputFolder}");
        }

        static void CloseUp(Camera cam, string file, string crewName, float distance)
        {
            var crew = GameObject.Find(crewName);
            if (crew == null) { Debug.LogWarning($"[ScreenshotCapture] {crewName} not found"); return; }
            var head = System.Array.Find(crew.GetComponentsInChildren<Transform>(), t => t.name == "head");
            Vector3 target = head != null ? head.position + Vector3.up * 0.05f : crew.transform.position + Vector3.up * 1.6f;
            Vector3 pos = target + crew.transform.forward * distance + Vector3.up * 0.05f - Vector3.up * 0.15f * 0f;
            var rot = Quaternion.LookRotation(target - pos - Vector3.up * 0.25f);
            float fov = cam.fieldOfView;
            cam.fieldOfView = 40f;
            Shot(cam, file, pos, rot.eulerAngles);
            cam.fieldOfView = fov;
        }

        static void Shot(Camera cam, string file, Vector3 position, Vector3 euler)
        {
            const int w = 1280, h = 720;
            cam.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));

            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var previous = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = previous;

            var active = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = active;

            File.WriteAllBytes(Path.Combine(OutputFolder, file), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
