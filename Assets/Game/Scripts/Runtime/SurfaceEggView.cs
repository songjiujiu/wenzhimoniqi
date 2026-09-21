using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Rendering;
using Mosquito.Core;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;
using Matrix4x4 = UnityEngine.Matrix4x4;

namespace Mosquito.Runtime
{
    // Decorative representatives follow real hatch deadlines; their random placement never uses simulation RNG.
    public sealed class SurfaceEggView : MonoBehaviour
    {
        public const int Limit = 300;
        public const float Thickness = .045f;
        private struct Egg { public long due; public Matrix4x4 matrix; }
        private readonly List<Egg> eggs = new List<Egg>();
        private readonly Matrix4x4[] matrices = new Matrix4x4[Limit];
        private readonly System.Random random = new System.Random(910273);
        private Transform floor, backWall, leftWall;
        private Camera cameraView;
        private Mesh mesh;
        private Material material;
        private long lastTick = -1;
        private string runId;
        private bool studyRoom;
        public int VisibleCount => eggs.Count;

        public bool FindTarget(UnityEngine.Vector2 pointer, out int index, out long due, out float distance)
        {
            index = -1; due = 0; float radius = 45f * Screen.height / 900f; distance = radius * radius;
            for (int i = 0; i < eggs.Count; i++)
            {
                Vector3 screen = cameraView.WorldToScreenPoint(eggs[i].matrix.GetColumn(3));
                if (screen.z <= 0) continue;
                float candidate = ((UnityEngine.Vector2)screen - pointer).sqrMagnitude;
                if (candidate <= distance) { index = i; due = eggs[i].due; distance = candidate; }
            }
            return index >= 0;
        }

        public Vector3 RemoveTarget(int index)
        {
            Vector3 position = eggs[index].matrix.GetColumn(3);
            eggs.RemoveAt(index); lastTick = -1;
            return position;
        }

        public void Initialize(Transform floorSurface, Transform backSurface, Transform leftSurface, Camera camera, bool useStudyRoom = false)
        {
            studyRoom = useStudyRoom;
            floor = floorSurface; backWall = backSurface; leftWall = leftSurface; cameraView = camera;
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            const int rings = 8, sides = 12;
            for (int y = 0; y <= rings; y++) for (int x = 0; x <= sides; x++)
            {
                float latitude = y * Mathf.PI / rings, longitude = x * Mathf.PI * 2 / sides;
                vertices.Add(new Vector3(Mathf.Sin(latitude) * Mathf.Cos(longitude), Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(longitude)) * .5f);
            }
            for (int y = 0; y < rings; y++) for (int x = 0; x < sides; x++)
            {
                int a = y * (sides + 1) + x, b = a + sides + 1;
                triangles.Add(a); triangles.Add(a + 1); triangles.Add(b);
                triangles.Add(a + 1); triangles.Add(b + 1); triangles.Add(b);
            }
            mesh = new Mesh { name = "Surface egg" }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
            material = new Material(Shader.Find("Mosquito/InstancedLit")) { name = "Warm ivory eggs", enableInstancing = true };
            material.SetColor("_BaseColor", new Color(.94f, .87f, .57f));
        }

        // Placement is on actual cube faces, not an approximate height in open space.
        public static Matrix4x4 Placement(Transform surface, Vector3 localPoint, Vector3 localNormal, float angle)
        {
            Vector3 normal = surface.TransformDirection(localNormal).normalized;
            Vector3 contact = surface.TransformPoint(localPoint);
            return Matrix4x4.TRS(contact + normal * (Thickness * .5f),
                Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, angle, 0), new Vector3(.11f, Thickness, .18f));
        }

        private bool TryPlace(out Matrix4x4 matrix)
        {
            for (int attempt = 0; attempt < 80; attempt++)
            {
                int face = random.Next(3);
                float a = (float)random.NextDouble(), b = (float)random.NextDouble();
                Transform surface; Vector3 local, normal;
                if (face == 0)
                {
                    surface = floor; normal = Vector3.up;
                    local = new Vector3(Mathf.Lerp(-.36f, .36f, a), .5f, Mathf.Lerp(-.3f, .27f, b));
                    Vector3 world = surface.TransformPoint(local);
                    // Bare floor only: avoid the rug, furniture footprint and plant pot.
                    if (studyRoom && IsStudyFurniture(world, false)) continue;
                    if (!studyRoom)
                    {
                    if (Mathf.Abs(world.x) < 3.65f && world.z > -3.65f && world.z < 1.65f) continue;
                    if (Mathf.Abs(world.x - 3.5f) < .65f && Mathf.Abs(world.z - 1.7f) < .65f) continue;
                    }
                }
                else if (face == 1)
                {
                    surface = backWall; normal = Vector3.back;
                    local = new Vector3(Mathf.Lerp(-.36f, .36f, a), Mathf.Lerp(-.37f, .23f, b), -.5f);
                    Vector3 world = surface.TransformPoint(local);
                    if (studyRoom && IsStudyFurniture(world, true)) continue;
                    if (!studyRoom)
                    {
                    if (world.x > -3.75f && world.x < -.65f && world.y > 1.55f && world.y < 4.65f) continue;
                    if (world.x > 1.4f && world.x < 4.2f && world.y > 2.55f && world.y < 3.7f) continue;
                    }
                }
                else
                {
                    surface = leftWall; normal = Vector3.right;
                    local = new Vector3(.5f, Mathf.Lerp(-.37f, .23f, a), Mathf.Lerp(-.4f, .35f, b));
                }
                matrix = Placement(surface, local, normal, (float)random.NextDouble() * 360);
                Vector3 viewport = cameraView.WorldToViewportPoint(matrix.GetColumn(3));
                if (viewport.z > 0 && viewport.x > .05f && viewport.x < .77f && viewport.y > .25f && viewport.y < .79f) return true;
            }
            matrix = Matrix4x4.identity; return false; // Never fall back to an airborne point.
        }

        public static bool IsStudyFurniture(Vector3 point, bool backWall)
        {
            if (backWall)
                return (point.x > -4.4f && point.x < -.7f && point.y > 2.05f && point.y < 5.60f) || // Window.
                    (point.x > .82f && point.x < 3.78f && point.y < 5.5f) || // Bookcase and trailing plant.
                    (point.x > -.44f && point.x < .64f && point.y > 2.70f && point.y < 4.30f) || // Framed print.
                    (point.x > -4.4f && point.x < .6f && point.y < 1.7f); // Desk behind which eggs would be hidden.
            return (point.x > -5.0f && point.x < 1.8f && point.z > -2.05f && point.z < 2.60f) || // Rug/chair.
                (point.x > -4.4f && point.x < .6f && point.z > 1.0f && point.z < 3.4f) || // Desk.
                (point.x > .82f && point.x < 3.78f && point.z > 2.60f) || // Bookcase.
                (Mathf.Abs(point.x - .90f) < .55f && Mathf.Abs(point.z - 2.46f) < .55f); // Planter.
        }

        public void Sync(Simulation game, bool menu)
        {
            if (menu || game == null) { eggs.Clear(); lastTick = -1; return; }
            if (runId != game.RunId) { eggs.Clear(); runId = game.RunId; lastTick = -1; }
            if (lastTick == game.Tick) return;
            lastTick = game.Tick;
            var shown = new Dictionary<long, int>();
            for (int i = eggs.Count - 1; i >= 0; i--)
            {
                long due = eggs[i].due; shown.TryGetValue(due, out int count);
                if (!game.EggBuckets.TryGetValue(due, out BigInteger actual) || count >= actual) eggs.RemoveAt(i);
                else shown[due] = count + 1;
            }
            foreach (var bucket in game.EggBuckets)
            {
                shown.TryGetValue(bucket.Key, out int count);
                int add = (int)BigInteger.Min(bucket.Value - count, Limit - eggs.Count);
                for (int i = 0; i < add; i++)
                {
                    if (!TryPlace(out var matrix)) break;
                    eggs.Add(new Egg { due = bucket.Key, matrix = matrix });
                }
                if (eggs.Count == Limit) break;
            }
        }

        public void Draw()
        {
            for (int i = 0; i < eggs.Count; i++) matrices[i] = eggs[i].matrix;
            if (eggs.Count > 0) Graphics.DrawMeshInstanced(mesh, 0, material, matrices, eggs.Count, null, ShadowCastingMode.Off, false);
        }

        private void OnDestroy()
        {
            if (Application.isPlaying) { if (mesh != null) Destroy(mesh); if (material != null) Destroy(material); }
            else { if (mesh != null) DestroyImmediate(mesh); if (material != null) DestroyImmediate(material); }
        }
    }
}
