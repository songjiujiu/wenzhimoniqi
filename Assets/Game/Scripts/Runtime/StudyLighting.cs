using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Mosquito.Runtime
{
    // Shared by the authored scene, runtime and the editor capture.
    public static class StudyLighting
    {
        public static void Configure(Camera camera, Transform parent)
        {
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.10f, .13f, .20f);
            RenderSettings.ambientEquatorColor = new Color(.12f, .10f, .085f);
            RenderSettings.ambientGroundColor = new Color(.055f, .042f, .035f);
            // Face the back wall squarely: no sideways yaw, downward pitch or roll.
            camera.transform.position = new Vector3(0f, 2.6f, -7.5f);
            camera.transform.rotation = Quaternion.identity;
            camera.fieldOfView = 40; camera.nearClipPlane = .1f; camera.farClipPlane = 80;
            camera.backgroundColor = new Color(.012f, .018f, .033f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = true;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true; data.renderShadows = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;

            var sun = Child("Faint moonlight", parent).AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(.36f, .48f, .78f);
            sun.intensity = .12f; sun.transform.rotation = Quaternion.Euler(42, 145, 0);
            sun.shadows = LightShadows.Soft; sun.shadowStrength = .85f;
            sun.shadowBias = .025f; sun.shadowNormalBias = .10f;
            RenderSettings.sun = sun;
            var fill = Child("Warm room ceiling lamp", parent).AddComponent<Light>();
            fill.type = LightType.Point; fill.transform.position = new Vector3(-.5f, 5.5f, .3f);
            fill.color = new Color(1f, .77f, .48f); fill.intensity = 14f; fill.range = 12;
            fill.shadows = LightShadows.Soft; fill.shadowStrength = .8f;
            fill.shadowBias = .035f; fill.shadowNormalBias = .12f;
            var lamp = Child("Desk lamp warm pool", parent).AddComponent<Light>();
            lamp.type = LightType.Point; lamp.transform.position = new Vector3(-2.988f, 2.516f, 2.657f);
            lamp.color = new Color(1f, .70f, .33f); lamp.intensity = 3.8f; lamp.range = 3.8f;
            lamp.shadows = LightShadows.Soft; lamp.shadowBias = .02f; lamp.shadowNormalBias = .08f;

            var volume = Child("Study colour grading", parent).AddComponent<Volume>();
            volume.isGlobal = true; volume.sharedProfile = Resources.Load<VolumeProfile>("StudyGrading");
            var probe = Child("Study room reflections", parent).AddComponent<ReflectionProbe>();
            probe.transform.position = new Vector3(-.8f, 2.2f, 1);
            probe.mode = ReflectionProbeMode.Realtime; probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.resolution = 128; probe.size = new Vector3(14, 7, 13); probe.boxProjection = true;
            probe.clearFlags = ReflectionProbeClearFlags.SolidColor; probe.backgroundColor = new Color(.025f, .035f, .06f);
        }

        private static GameObject Child(string name, Transform parent)
        {
            var result = new GameObject(name); result.transform.SetParent(parent, false); return result;
        }
    }
}
