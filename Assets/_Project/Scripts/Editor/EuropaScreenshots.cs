using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Renders fixed viewpoints of the Europa landing to docs/images (needs a GPU; no -nographics): the cabin
    /// before and after the ramp drops, the plain under Jupiter, the landing party and the base.
    /// Menu: Jupiter > Capture Europa Screenshots.
    /// </summary>
    public static class EuropaScreenshots
    {
        [MenuItem("Jupiter/Capture Europa Screenshots")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(EuropaSurfaceBuilder.ScenePath);
            var cam = Object.FindAnyObjectByType<Camera>();
            Directory.CreateDirectory("docs/images");
            var ramp = GameObject.Find("RampHinge").transform;
            float floor = 1.3f + 1.62f;

            Shot(cam, "europa-cabin.png", new Vector3(0.3f, floor, 2.6f), new Vector3(0f, floor - 0.3f, -4f), 75f);
            float open = -(90f + Mathf.Asin(1.3f / 2.7f) * Mathf.Rad2Deg);
            ramp.localRotation = Quaternion.Euler(open, 0f, 0f);
            Shot(cam, "europa-ramp-open.png", new Vector3(0.2f, floor, 2.4f), new Vector3(0f, floor + 0.5f, -30f), 75f);
            Shot(cam, "europa-plain.png", new Vector3(4f, 2.2f, -12f), new Vector3(0f, 12f, -120f), 70f);
            Shot(cam, "europa-lander.png", new Vector3(9f, 2.5f, -14f), new Vector3(0f, 2.2f, 0f), 60f);
            Shot(cam, "europa-crew.png", new Vector3(0.2f, floor, 1.4f), new Vector3(-0.5f, 2.3f, -1.4f), 60f);
            Shot(cam, "europa-base.png", new Vector3(6f, 3f, -180f), new Vector3(0f, 3f, -210f), 60f);
            ramp.localRotation = Quaternion.identity;
            Debug.Log("[EuropaScreenshots] Saved");
        }

        public static void Shot(Camera cam, string file, Vector3 pos, Vector3 lookAt, float fov)
        {
            const int w = 1280, h = 720;
            var t = cam.transform;
            Vector3 oldPos = t.position;
            Quaternion oldRot = t.rotation;
            t.SetPositionAndRotation(pos, Quaternion.LookRotation(lookAt - pos));
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
            t.SetPositionAndRotation(oldPos, oldRot);
        }
    }
}
