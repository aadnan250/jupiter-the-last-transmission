using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Builds the Windows player (64-bit) with every level in the build list, starting at Cabo Lumen, into
    /// Builds/Windows. Batch: -executeMethod Jupiter.EditorTools.WindowsBuild.Build
    /// </summary>
    public static class WindowsBuild
    {
        public const string Folder = "Builds/Windows";
        public const string ExeName = "JupiterTheLastTransmission.exe";

        [MenuItem("Jupiter/Build Windows Player")]
        public static void Build()
        {
            // The story levels only: the grey-box test room and the retired Conamara Base stay in the editor for tests.
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled && !s.path.Contains("TestRoom") && !s.path.Contains("L04_"))
                .Select(s => s.path).ToArray();
            Directory.CreateDirectory(Folder);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(Folder, ExeName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[WindowsBuild] {s.result}: {s.outputPath}, {s.totalSize / (1024 * 1024)} MB, {s.totalErrors} errors, {s.totalTime.TotalSeconds:0}s");
            if (Application.isBatchMode && s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
