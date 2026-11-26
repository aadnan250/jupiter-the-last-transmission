using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>Renders fixed viewpoints of Conamara Base to docs/images (needs a GPU; no -nographics).</summary>
    public static class ConamaraScreenshots
    {
        [MenuItem("Jupiter/Capture Conamara Screenshots")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(ConamaraBaseBuilder.ScenePath);
            var cam = Object.FindAnyObjectByType<Camera>();
            Directory.CreateDirectory("docs/images");
            // The Listener is hidden behind a story gate until contact: show it for the shot.
            var ai = Object.FindObjectsByType<Jupiter.Creatures.CreatureAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var listener = ai.Length > 0 ? ai[0].gameObject : null;
            if (listener != null) listener.SetActive(true);
            Vector3 l = listener != null ? listener.transform.position : new Vector3(-11f, 0f, 55f);

            Shot(cam, "phase4-listener.png", l + new Vector3(3.2f, 1.7f, -1.6f), l + Vector3.up * 1.7f, 55f);
            Shot(cam, "phase4-drill-chamber.png", new Vector3(-1f, 1.7f, 44.5f), new Vector3(-8f, 0.8f, 54f), 70f);
            Shot(cam, "phase4-corridor.png", new Vector3(0f, 1.62f, 12f), new Vector3(0f, 1.4f, 30f), 70f);
            if (listener != null) listener.SetActive(false);
            Debug.Log("[ConamaraScreenshots] Saved");
        }

        static void Shot(Camera cam, string file, Vector3 pos, Vector3 lookAt, float fov)
        {
            const int w = 1280, h = 720;
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(lookAt - pos));
            float oldFov = cam.fieldOfView;
            cam.fieldOfView = fov;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine("docs/images", file), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            cam.fieldOfView = oldFov;
        }
    }
}
