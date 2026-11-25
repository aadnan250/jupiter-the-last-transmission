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
            SciFiTextureGenerator.GenerateAll();
            CharacterMaterialBuilder.Build();
            CombatAudioGenerator.Generate();
            PlayerRigBuilder.BuildPrefab();
            ListenerBuilder.BuildPrefab();
            GreyboxTestRoomBuilder.Build();
            MeridianGreyboxBuilder.Build();
            ConamaraBaseBuilder.Build();
            RenderPipelineTuner.Apply();

            SetBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[GeneratedContent] All generated content rebuilt.");
        }

        /// <summary>
        /// Rebuilds only the Phase 4 content (combat audio, player prefab, Listener, Conamara Base) without
        /// regenerating the other levels. Batch: -executeMethod Jupiter.EditorTools.GeneratedContent.RebuildPhase4
        /// </summary>
        [MenuItem("Jupiter/Rebuild Phase 4 Content")]
        public static void RebuildPhase4()
        {
            CombatAudioGenerator.Generate();
            PlayerRigBuilder.BuildPrefab();
            ListenerBuilder.BuildPrefab();
            ConamaraBaseBuilder.Build();
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[GeneratedContent] Phase 4 content rebuilt.");
        }

        static void SetBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                // The newest chapter first: a build starts in Conamara Base.
                new EditorBuildSettingsScene(ConamaraBaseBuilder.ScenePath, true),
                new EditorBuildSettingsScene(MeridianGreyboxBuilder.ScenePath, true),
                new EditorBuildSettingsScene(GreyboxTestRoomBuilder.ScenePath, true),
            };
        }
    }
}
