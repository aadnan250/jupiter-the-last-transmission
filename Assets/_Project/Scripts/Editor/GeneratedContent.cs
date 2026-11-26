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
            EuropaTextureGenerator.Generate();
            StalkerBuilder.BuildPrefab();
            GreyboxTestRoomBuilder.Build();
            MeridianGreyboxBuilder.Build();
            EuropaSurfaceBuilder.Build();
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

        /// <summary>
        /// Rebuilds only the Meridian (Chapter 2) and the build scene list. Batch:
        /// -executeMethod Jupiter.EditorTools.GeneratedContent.RebuildMeridian
        /// </summary>
        [MenuItem("Jupiter/Rebuild Meridian")]
        public static void RebuildMeridian()
        {
            GreyboxTestRoomBuilder.BuildProfiles(); // the Meridian's spin-gravity profile must exist (and be loaded) first
            MeridianGreyboxBuilder.Build();
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[GeneratedContent] Meridian rebuilt.");
        }

        /// <summary>
        /// Rebuilds the Europa landing (sky and ice textures, combat audio, player prefab, the Stalker, the surface
        /// level) and the Meridian, whose ending leads to it. Batch:
        /// -executeMethod Jupiter.EditorTools.GeneratedContent.RebuildEuropaLanding
        /// </summary>
        [MenuItem("Jupiter/Rebuild Europa Landing")]
        public static void RebuildEuropaLanding()
        {
            EuropaTextureGenerator.Generate();
            CombatAudioGenerator.Generate();
            PlayerRigBuilder.BuildPrefab();
            StalkerBuilder.BuildPrefab();
            ListenerBuilder.BuildPrefab();
            GreyboxTestRoomBuilder.BuildProfiles();
            MeridianGreyboxBuilder.Build();
            EuropaSurfaceBuilder.Build();
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[GeneratedContent] Europa landing rebuilt.");
        }

        static void SetBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                // A build plays the story in order: the Meridian, the landing on Europa, then Conamara Base.
                new EditorBuildSettingsScene(MeridianGreyboxBuilder.ScenePath, true),
                new EditorBuildSettingsScene(EuropaSurfaceBuilder.ScenePath, true),
                new EditorBuildSettingsScene(ConamaraBaseBuilder.ScenePath, true),
                new EditorBuildSettingsScene(GreyboxTestRoomBuilder.ScenePath, true),
            };
        }
    }
}
