using NUnit.Framework;
using UnityEngine;
using Mosquito.Core;
using Mosquito.Runtime;

namespace Mosquito.Tests
{
    public sealed class SurfaceEggTests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void EggBottomTouchesTheActualSurface(int face)
        {
            var surface = new GameObject("Surface placement test");
            try
            {
                surface.transform.position = new Vector3(3, 2, -1);
                surface.transform.localScale = new Vector3(15, 7, .25f);
                surface.transform.rotation = Quaternion.Euler(0, 18, 0);
                var normal = face == 0 ? Vector3.up : face == 1 ? Vector3.back : Vector3.right;
                var local = normal * .5f;
                var matrix = SurfaceEggView.Placement(surface.transform, local, normal, 73);
                var bottom = matrix.MultiplyPoint3x4(Vector3.down * .5f);
                Assert.That(Vector3.Distance(bottom, surface.transform.TransformPoint(local)), Is.LessThan(.00001f));
                var actualNormal = surface.transform.TransformDirection(normal).normalized;
                Assert.That(Vector3.Dot(matrix.MultiplyVector(Vector3.up).normalized, actualNormal), Is.GreaterThan(.9999f));
            }
            finally { Object.DestroyImmediate(surface); }
        }

        [Test] public void EggsPersistThroughIncenseAndDisappearAtTheirHatchDeadline()
        {
            var root = new GameObject("Egg lifecycle test");
            try
            {
                Transform Surface(string name, Vector3 position, Vector3 scale)
                {
                    var obj = new GameObject(name); obj.transform.SetParent(root.transform);
                    obj.transform.position = position; obj.transform.localScale = scale; return obj.transform;
                }
                var floor = Surface("Floor", new Vector3(0, -.35f, 0), new Vector3(15, .3f, 13));
                var back = Surface("Wall", new Vector3(0, 3, 4), new Vector3(15, 7, .25f));
                var left = Surface("Wall", new Vector3(-6, 3, 0), new Vector3(.25f, 7, 9));
                var camera = new GameObject("Camera").AddComponent<Camera>(); camera.transform.SetParent(root.transform);
                camera.transform.position = new Vector3(7.4f, 6, -10.7f);
                camera.transform.LookAt(new Vector3(0, 1.5f, .1f)); camera.fieldOfView = 43; camera.aspect = 16f / 9;
                var view = root.AddComponent<SurfaceEggView>(); view.Initialize(floor, back, left, camera);
                var game = new Simulation(seed: 7);
                for (int i = 0; i < 120; i++) game.Step();
                view.Sync(game, false); Assert.That(view.VisibleCount, Is.EqualTo(2));
                view.RemoveTarget(0); Assert.That(view.VisibleCount, Is.EqualTo(1));
                view.Sync(game, false); Assert.That(view.VisibleCount, Is.EqualTo(2));
                var snapshot = game.Capture(); snapshot.maxAdult = "1000"; snapshot.unlockFlags = 7;
                game = Simulation.Restore(snapshot); game.Step(Weapon.Incense);
                view.Sync(game, false); Assert.That(view.VisibleCount, Is.EqualTo(2));
                for (int i = 0; i < 59; i++) game.Step();
                view.Sync(game, false); Assert.That(view.VisibleCount, Is.Zero);
                view.Sync(game, true); Assert.That(view.VisibleCount, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
