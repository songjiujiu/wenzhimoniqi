using System.IO;
using Mosquito.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Mosquito.Editor
{
    public static class ReferenceSceneSetup
    {
        public const string ScenePath = "Assets/Game/Scenes/Observatory.unity";

        [MenuItem("Mosquito/打开游戏场景")]
        public static void OpenGame()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        // Run in batch mode against a closed project. Preserve any separate recovery scenes.
        [MenuItem("Mosquito/刷新书房模型与材质")]
        public static void RefreshRoom()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("请先退出播放模式，再刷新书房模型与材质。"); return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare();
        }

        public static void Prepare()
        {
            AssetDatabase.Refresh();
            ConfigureRendering();
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var game = Object.FindFirstObjectByType<GameController>();
            if (game == null) throw new System.InvalidOperationException("Game entry is missing from Observatory.");
            // Rebuild only our generated children so changed FBX geometry and palette
            // actually replace the previous serialized meshes/materials in the scene.
            foreach (string name in new[] { "Room", "Incense", "LuckyCat", "Tower", "Hand", "Zapper" })
            {
                var previous = game.transform.Find("Blender " + name);
                if (previous != null) Object.DestroyImmediate(previous.gameObject);
            }
            var oldPreview = game.transform.Find("Editor preview");
            if (oldPreview != null) Object.DestroyImmediate(oldPreview.gameObject);
            var view = game.gameObject.AddComponent<RoomView>();
            view.LoadBlenderRoom();
            var incense = view.LoadReferenceModel("Incense").transform;
            incense.localPosition = new Vector3(-3.22f, 1.866f, 1.757f); incense.localScale = Vector3.one * .60f;
            var cat = view.LoadReferenceModel("LuckyCat").transform;
            cat.localPosition = new Vector3(-.444f, 1.846f, 2.936f); cat.localScale = Vector3.one * .65f;
            var tower = view.LoadReferenceModel("Tower").transform;
            tower.localPosition = new Vector3(3.22f, 3.47f, 3.20f); tower.localScale = Vector3.one * .60f;
            view.LoadReferenceModel("Hand").SetActive(false);
            view.LoadReferenceModel("Zapper").SetActive(false);
            const string materialFolder = "Assets/Game/Art/Materials/Reference";
            Directory.CreateDirectory(materialFolder); AssetDatabase.Refresh();
            foreach (var renderer in game.GetComponentsInChildren<MeshRenderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    string path = materialFolder + "/" + source.name + ".mat";
                    var saved = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (saved == null) { saved = new Material(source); AssetDatabase.CreateAsset(saved, path); }
                    else { EditorUtility.CopySerialized(source, saved); EditorUtility.SetDirty(saved); }
                    materials[i] = saved;
                }
                renderer.sharedMaterials = materials;
            }
            Object.DestroyImmediate(view);
            if (game.transform.Find("Editor preview") == null)
            {
                var preview = new GameObject("Editor preview"); preview.tag = "EditorOnly";
                preview.transform.SetParent(game.transform, false);
                var camera = new GameObject("Scene preview camera").AddComponent<Camera>();
                camera.transform.SetParent(preview.transform, false);
                StudyLighting.Configure(camera, preview.transform);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("REFERENCE_SCENE_READY " + ScenePath);
        }

        private static void ConfigureRendering()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Game/Settings/ObservatoryURP.asset");
            var serialized = new SerializedObject(pipeline);
            serialized.FindProperty("m_SoftShadowsSupported").boolValue = true;
            serialized.FindProperty("m_AdditionalLightShadowsSupported").boolValue = true;
            serialized.FindProperty("m_MainLightShadowmapResolution").intValue = 4096;
            serialized.FindProperty("m_ShadowCascadeCount").intValue = 2;
            serialized.FindProperty("m_ShadowDepthBias").floatValue = .1f;
            serialized.FindProperty("m_ShadowNormalBias").floatValue = .2f;
            serialized.FindProperty("m_RequireDepthTexture").boolValue = true;
            serialized.FindProperty("m_ReflectionProbeBlending").boolValue = true;
            serialized.FindProperty("m_ReflectionProbeBoxProjection").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(pipeline);

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Game/Settings/ObservatoryRenderer.asset");
            var rendererSerialized = new SerializedObject(renderer);
            rendererSerialized.FindProperty("postProcessData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            rendererSerialized.ApplyModifiedPropertiesWithoutUndo();
            ScreenSpaceAmbientOcclusion ao = null;
            foreach (var feature in renderer.rendererFeatures) if (feature is ScreenSpaceAmbientOcclusion existing) ao = existing;
            if (ao == null)
            {
                ao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>(); ao.name = "Study contact shadows";
                AssetDatabase.AddObjectToAsset(ao, renderer); renderer.rendererFeatures.Add(ao);
            }
            var settings = new SerializedObject(ao);
            settings.FindProperty("m_Settings.AOMethod").enumValueIndex = 1;
            settings.FindProperty("m_Settings.AfterOpaque").boolValue = true;
            settings.FindProperty("m_Settings.Intensity").floatValue = 1.15f;
            settings.FindProperty("m_Settings.Radius").floatValue = .22f;
            settings.FindProperty("m_Settings.DirectLightingStrength").floatValue = .15f;
            settings.FindProperty("m_Settings.Downsample").boolValue = false;
            settings.ApplyModifiedPropertiesWithoutUndo(); ao.SetActive(true); ao.Create();
            EditorUtility.SetDirty(ao); renderer.SetDirty(); EditorUtility.SetDirty(renderer);

            const string path = "Assets/Game/Resources/StudyGrading.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path); }
            if (!profile.TryGet<Tonemapping>(out var tone)) { tone = profile.Add<Tonemapping>(true); AssetDatabase.AddObjectToAsset(tone, profile); }
            tone.mode.Override(TonemappingMode.ACES);
            if (!profile.TryGet<ColorAdjustments>(out var colour)) { colour = profile.Add<ColorAdjustments>(true); AssetDatabase.AddObjectToAsset(colour, profile); }
            colour.postExposure.Override(.3f); colour.saturation.Override(-8f); colour.contrast.Override(5f);
            EditorUtility.SetDirty(tone); EditorUtility.SetDirty(colour); EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }
    }
}
