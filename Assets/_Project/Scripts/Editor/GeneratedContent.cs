using UnityEditor;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// One entry point that regenerates every generated asset in dependency order.
    /// Menu: Jupiter > Rebuild All Generated Content. Batch mode:
    /// Unity -batchmode -quit -executeMethod Jupiter.EditorTools.GeneratedContent.RebuildAll
    /// </summary>
    public static class GeneratedContent
    {
        [MenuItem("Jupiter/Rebuild All Generated Content")]
        public static void RebuildAll()
        {
            AudioAssetGenerator.Generate();
            GreyboxTestRoomBuilder.BuildProfiles();
            JupiterTextureGenerator.Generate();
            PlayerRigBuilder.BuildPrefab();
            GreyboxTestRoomBuilder.Build();
            MeridianGreyboxBuilder.Build();
            RenderPipelineTuner.Apply();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MeridianGreyboxBuilder.ScenePath, true),
                new EditorBuildSettingsScene(GreyboxTestRoomBuilder.ScenePath, true),
            };
            AssetDatabase.SaveAssets();
            Debug.Log("[GeneratedContent] All generated content rebuilt.");
        }
    }
}
