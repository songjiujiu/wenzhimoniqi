using System.IO;
using Mosquito.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Mosquito.Editor
{
    // Batch-only preview in an isolated project. Does not build or save a gameplay scene.
    public static class RoomAssetPreview
    {
        public static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Room preview").AddComponent<RoomView>();
            if (!root.LoadBlenderRoom()) throw new System.InvalidOperationException("Room model missing.");
            var camera = new GameObject("Game camera").AddComponent<Camera>();
            StudyLighting.Configure(camera, root.transform);
            var target = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            var capture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target; capture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); capture.Apply();
                Directory.CreateDirectory("TestResults"); File.WriteAllBytes("TestResults/room-unity.png", capture.EncodeToPNG());
                Debug.Log("ROOM_PREVIEW_READY");
            }
            finally
            {
                RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Object.DestroyImmediate(capture);
            }
        }
    }
}
