// sets the project up and builds it, from the menu or the command line:
//   Unity -batchmode -quit -projectPath . -executeMethod ShadowSet.EditorTools.Build.Windows
//   Unity -batchmode -quit -projectPath . -executeMethod ShadowSet.EditorTools.Build.WebGL
// the game builds itself in code when it starts, so the scene is empty
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShadowSet.EditorTools
{
    public static class Build
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Shadow Set/Set up project")]
        public static void Setup()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.companyName = "Yasmin Tazi";
            PlayerSettings.productName = "Shadow Set";
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;   // the most reliable on itch.io and other hosts
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = false;      // browser storage can stall inside itch.io's iframe
            PlayerSettings.WebGL.template = "PROJECT:ShadowSet";   // a full-window page (Assets/WebGLTemplates)
            QualitySettings.antiAliasing = 4;
            AssetDatabase.SaveAssets();
            Debug.Log("Shadow Set: project set up");
        }

        [MenuItem("Shadow Set/Build for Windows")]
        public static void Windows() { Run(BuildTarget.StandaloneWindows64, "Builds/Windows/ShadowSet.exe"); }

        [MenuItem("Shadow Set/Build for the web")]
        public static void WebGL() { Run(BuildTarget.WebGL, "Builds/Web"); }

        static void Run(BuildTarget target, string path)
        {
            Setup();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = path,
                target = target,
                options = BuildOptions.None
            });
            Debug.Log("Shadow Set: build " + report.summary.result + " -> " + path + " (" + report.summary.totalSize / 1024 / 1024 + " MB, " + report.summary.totalErrors + " errors)");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }
    }
}
