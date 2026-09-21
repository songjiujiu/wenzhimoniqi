using NUnit.Framework;
using UnityEngine;
using Mosquito.Runtime;

namespace Mosquito.Tests
{
    public sealed class RoomAssetTests
    {
        [TestCase("Hand")]
        [TestCase("Zapper")]
        [TestCase("Incense")]
        [TestCase("LuckyCat")]
        [TestCase("Tower")]
        public void ReferenceAssetsLoadAtGameScaleAndAreNotDuplicated(string asset)
        {
            var root = new GameObject("Asset import test");
            try
            {
                var view = root.AddComponent<RoomView>();
                var model = view.LoadReferenceModel(asset);
                Assert.That(view.LoadReferenceModel(asset), Is.SameAs(model));
                Assert.That(root.transform.childCount, Is.EqualTo(1));
                var renderers = model.GetComponentsInChildren<MeshRenderer>();
                Assert.That(renderers.Length, Is.GreaterThan(0));
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                    Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
                }
                Assert.That(bounds.size.magnitude, Is.InRange(.3f, 3f), asset + " must use metres, not centimetres.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void RuntimeRoomLoadsTexturedUrpMaterials()
        {
            var root = new GameObject("Room material test");
            try
            {
                Assert.That(Resources.Load<TextAsset>("Models/RoomMaterials"), Is.Not.Null, "Material palette must import as TextAsset.");
                foreach (var texture in new[] { "Wood", "Fabric", "Plaster" })
                    Assert.That(Resources.Load<Texture2D>("Models/RoomTextures/" + texture + "_Albedo"), Is.Not.Null, texture + " texture must be a runtime resource.");
                Assert.That(root.AddComponent<RoomView>().LoadBlenderRoom(), Is.True);
                int textured = 0;
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
                    foreach (var material in renderer.sharedMaterials)
                    {
                        Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
                        if (material.name.StartsWith("Room_Wood") || material.name.StartsWith("Room_Fabric") || material.name == "Room_Plaster")
                        {
                            Assert.That(material.GetTexture("_BaseMap"), Is.Not.Null, material.name);
                            Assert.That(material.GetTexture("_BumpMap"), Is.Not.Null, material.name + " normal map");
                            Assert.That(material.GetTexture("_MetallicGlossMap"), Is.Not.Null, material.name + " smoothness map");
                            Assert.That(material.IsKeywordEnabled("_NORMALMAP"), Is.True);
                            Assert.That(material.IsKeywordEnabled("_METALLICSPECGLOSSMAP"), Is.True);
                            textured++;
                        }
                    }
                Assert.That(textured, Is.GreaterThanOrEqualTo(9));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(-2f, 3.8f, 3.875f, true, true)]
        [TestCase(2.3f, 2f, 3.875f, true, true)]
        [TestCase(-1.6f, -.2f, .3f, false, true)]
        [TestCase(4.6f, -.2f, -1f, false, false)]
        [TestCase(-5f, 3f, 3.875f, true, false)]
        public void EggsAvoidReferenceRoomFurniture(float x, float y, float z, bool wall, bool occupied)
        {
            Assert.That(SurfaceEggView.IsStudyFurniture(new Vector3(x, y, z), wall), Is.EqualTo(occupied));
        }

        [Test]
        public void BlenderRoomImportsAtGameplayScaleAndOrientation()
        {
            var prefab = Resources.Load<GameObject>("Models/Room");
            Assert.That(prefab, Is.Not.Null, "Blender FBX must be available at runtime.");
            var room = Object.Instantiate(prefab);
            try
            {
                var renderers = room.GetComponentsInChildren<MeshRenderer>();
                Assert.That(renderers.Length, Is.InRange(1, 30));
                Bounds bounds = renderers[0].bounds;
                MeshRenderer walls = null;
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                    if (renderer.name == "Room_Plaster") walls = renderer;
                    foreach (var material in renderer.sharedMaterials) Assert.That(material, Is.Not.Null);
                }
                Assert.That(Vector3.Distance(bounds.min, new Vector3(-7.5f, -.5f, -6.5f)), Is.LessThan(.01f));
                Assert.That(Vector3.Distance(bounds.max, new Vector3(7.5f, 6.5f, 6.5f)), Is.LessThan(.01f));
                Assert.That(walls, Is.Not.Null);
                Assert.That(walls.bounds.min.z, Is.EqualTo(-4.5f).Within(.01f));
                Assert.That(walls.bounds.max.z, Is.EqualTo(4.5f).Within(.01f));
                Assert.That(walls.bounds.min.x, Is.EqualTo(-7.5f).Within(.01f));
                bool hasLeftWallCorner = false;
                var filter = walls.GetComponent<MeshFilter>();
                foreach (var vertex in filter.sharedMesh.vertices)
                    if (Vector3.Distance(filter.transform.TransformPoint(vertex), new Vector3(-6.125f, 6.5f, -4.5f)) < .01f)
                        hasLeftWallCorner = true;
                Assert.That(hasLeftWallCorner, Is.True, "Left wall must not be mirrored by FBX axis conversion.");
            }
            finally { Object.DestroyImmediate(room); }
        }
    }
}
