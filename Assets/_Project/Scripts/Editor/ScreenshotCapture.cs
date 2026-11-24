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
            Debug.Log($"[ScreenshotCapture] Saved screenshots to {OutputFolder}");
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
