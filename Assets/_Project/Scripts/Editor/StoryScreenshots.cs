using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Renders the README's screenshots of the chapters around Europa (needs a GPU; no -nographics): the goodbye
    /// at Cabo Lumen and the rocket, Jupiter from the Meridian, the silhouette beyond the ridge, the canister in
    /// the hold, and the deck at home. Menu: Jupiter > Capture Story Screenshots.
    /// </summary>
    public static class StoryScreenshots
    {
        [MenuItem("Jupiter/Capture Story Screenshots")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(CaboLumenBuilder.ScenePath);
            var cam = Object.FindAnyObjectByType<Camera>();
            EuropaScreenshots.Shot(cam, "story-cabo-goodbye.png", new Vector3(1.2f, 1.6f, -15f), new Vector3(-0.9f, 1.2f, -11.4f), 55f);
            EuropaScreenshots.Shot(cam, "story-cabo-rocket.png", new Vector3(-14f, 2f, 10f), new Vector3(0f, 30f, 48f), 60f);

            EditorSceneManager.OpenScene(MeridianGreyboxBuilder.ScenePath);
            cam = Object.FindAnyObjectByType<Camera>();
            var jupiter = GameObject.Find("Jupiter_Placeholder");
            if (jupiter != null)
            {
                var j = jupiter.transform;
                Vector3 toShip = (Vector3.zero - j.position).normalized;
                EuropaScreenshots.Shot(cam, "story-jupiter.png", j.position + toShip * j.lossyScale.x * 1.15f, j.position, 50f);
            }

            EditorSceneManager.OpenScene(EuropaSurfaceBuilder.ScenePath);
            cam = Object.FindAnyObjectByType<Camera>();
            var figure = GameObject.Find("Silhouette");
            if (figure != null)
            {
                figure.SetActive(true);
                figure.transform.position += Vector3.up * 260f;
                EuropaScreenshots.Shot(cam, "story-silhouette.png", new Vector3(45f, 26f, -40f), figure.transform.position + Vector3.up * 130f, 62f);
            }

            EditorSceneManager.OpenScene(ReturnBuilder.ScenePath);
            cam = Object.FindAnyObjectByType<Camera>();
            EuropaScreenshots.Shot(cam, "story-canister.png", new Vector3(1.6f, 1.6f, -1.2f), new Vector3(0f, 1.3f, 1.5f), 60f);

            EditorSceneManager.OpenScene(HomeBuilder.ScenePath);
            cam = Object.FindAnyObjectByType<Camera>();
            EuropaScreenshots.Shot(cam, "story-home.png", new Vector3(0.5f, 1.6f, -1.5f), new Vector3(0f, 1.1f, 5f), 60f);
            Debug.Log("[StoryScreenshots] Saved");
        }
    }
}
