using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Mosquito.Runtime;

namespace Mosquito.Editor
{
    public static class ProjectSetup
    {
        private const string ScenePath = "Assets/Game/Scenes/Observatory.unity";
        [MenuItem("Mosquito/Set up prototype")]
        public static void Setup()
        {
            Directory.CreateDirectory("Assets/Game/Scenes");
            Directory.CreateDirectory("Assets/Game/Settings");
            Directory.CreateDirectory("Assets/Game/Resources");
            AssetDatabase.Refresh();
            if (Shader.Find("TextMeshPro/Distance Field") == null)
            {
                throw new InvalidOperationException("TMP resources are missing. Run Tools/ImportTmpResources.ps1 with the installed TMP Essential Resources.unitypackage before Setup.");
            }
            // Replace the early empty settings placeholder with the complete TMP resources.
            if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset") && File.Exists("Assets/Game/Resources/TMP Settings.asset"))
                AssetDatabase.DeleteAsset("Assets/Game/Resources/TMP Settings.asset");
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Game/Settings/ObservatoryURP.asset");
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, "Assets/Game/Settings/ObservatoryRenderer.asset");
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.msaaSampleCount = 4; pipeline.shadowDistance = 25;
                AssetDatabase.CreateAsset(pipeline, "Assets/Game/Settings/ObservatoryURP.asset");
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline; QualitySettings.vSyncCount = 0;
            if (AssetDatabase.LoadAssetAtPath<GameSettings>("Assets/Game/Resources/GameSettings.asset") == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<GameSettings>(), "Assets/Game/Resources/GameSettings.asset");
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length == 0)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<TMP_Settings>(), "Assets/Game/Resources/TMP Settings.asset");

            var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var included = graphics.FindProperty("m_AlwaysIncludedShaders");
            included.ClearArray();
            // Lit variants are collected from the authored room and weapon materials.
            // Keeping every Lit variant here greatly inflates Windows shader compilation.
            foreach (string name in new[] { "Mosquito/InstancedLit", "TextMeshPro/Distance Field", "TextMeshPro/Mobile/Distance Field", "UI/Default" })
            {
                var shader = Shader.Find(name);
                if (shader == null) { Debug.LogWarning("Shader not found: " + name); continue; }
                bool found = false;
                for (int i = 0; i < included.arraySize; i++) if (included.GetArrayElementAtIndex(i).objectReferenceValue == shader) found = true;
                if (!found) { int index = included.arraySize++; included.GetArrayElementAtIndex(index).objectReferenceValue = shader; }
            }
            var instancing = graphics.FindProperty("m_InstancingStripping");
            if (instancing != null)
                for (int i = 0; i < instancing.enumDisplayNames.Length; i++)
                    if (instancing.enumDisplayNames[i].IndexOf("Keep", StringComparison.OrdinalIgnoreCase) >= 0) instancing.enumValueIndex = i;
            graphics.ApplyModifiedPropertiesWithoutUndo();
            var player = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var handler = player.FindProperty("activeInputHandler"); if (handler != null) handler.intValue = 1;
            player.ApplyModifiedPropertiesWithoutUndo();
            PlayerSettings.companyName = "Songjiujiu"; PlayerSettings.productName = "Mosquito Observatory";
            PlayerSettings.bundleVersion = "0.1.4";
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true; PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            var importer = AssetImporter.GetAtPath("Assets/Game/Resources/Models/Mosquito.fbx") as ModelImporter;
            if (importer != null && !importer.isReadable) { importer.isReadable = true; importer.SaveAndReimport(); }
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Mosquito Observatory").AddComponent<GameController>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            else EditorSceneManager.OpenScene(ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("MOSQUITO_SETUP_SUCCESS");
        }

        [MenuItem("Mosquito/Build Windows prototype")]
        public static void BuildWindows()
        {
            Setup(); Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = "Builds/Windows/MosquitoObservatory.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            Directory.CreateDirectory("TestResults");
            File.WriteAllText("TestResults/build-summary.txt", $"Result: {report.summary.result}\nBytes: {report.summary.totalSize}\nDuration: {report.summary.totalTime}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\n");
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed.");
            Debug.Log("MOSQUITO_BUILD_SUCCESS");
        }
    }
}
