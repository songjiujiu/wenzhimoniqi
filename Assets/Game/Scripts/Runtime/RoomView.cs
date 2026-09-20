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
    public sealed class RoomView : MonoBehaviour
    {
        private sealed class Part { public Mesh mesh; public Material material; public int wing; }
        private readonly List<Part> parts = new List<Part>();
        private readonly Matrix4x4[] matrices = new Matrix4x4[300];
        private readonly Vector3[] positions = new Vector3[300];
        private readonly Quaternion[] rotations = new Quaternion[300];
        private readonly float[] phases = new float[300];
        private readonly float[] hiddenUntil = new float[300];
        private readonly Vector3[] deathPositions = new Vector3[30];
        private readonly float[] deathUntil = new float[30];
        private readonly Matrix4x4[] deathMatrices = new Matrix4x4[30];
        private int deathCursor;
        private Camera sceneCamera;
        private Transform hand, zapper;
        private Vector3 cameraPosition;
        private LineRenderer arc;
        private ParticleSystem smoke;
        private float clock, actionTime;
        private int population;
        private Weapon actionWeapon;
        private bool missed;
        private readonly Color amber = new Color(0.97f, .64f, .25f);

        public void Initialize()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.43f, .52f, .52f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.075f, .13f, .16f);
            RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 17; RenderSettings.fogEndDistance = 35;
            var cameraObject = new GameObject("Room Camera");
            sceneCamera = cameraObject.AddComponent<Camera>(); cameraObject.tag = "MainCamera";
            cameraPosition = new Vector3(7.4f, 6.0f, -10.7f);
            sceneCamera.transform.position = cameraPosition; sceneCamera.transform.LookAt(new Vector3(0, 1.5f, .1f));
            sceneCamera.fieldOfView = 43; sceneCamera.backgroundColor = new Color(.045f, .08f, .10f);
            sceneCamera.clearFlags = CameraClearFlags.SolidColor; sceneCamera.nearClipPlane = .1f; sceneCamera.farClipPlane = 80;
            cameraObject.AddComponent<AudioListener>();
            var sun = new GameObject("Warm ceiling light").AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1f, .89f, .71f); sun.intensity = 1.8f;
            sun.transform.rotation = Quaternion.Euler(48, -35, 0); sun.shadows = LightShadows.Soft;
            var fill = new GameObject("Window blue light").AddComponent<Light>(); fill.type = LightType.Point;
            fill.transform.position = new Vector3(-2, 3.5f, 1.5f); fill.color = new Color(.35f, .79f, .88f); fill.intensity = 6; fill.range = 9;

            var wall = Mat("Wall muted teal", new Color(.20f, .32f, .33f));
            var floor = Mat("Warm floor", new Color(.35f, .30f, .24f));
            var wood = Mat("Oak", new Color(.59f, .43f, .27f));
            var dark = Mat("Graphite", new Color(.055f, .09f, .10f));
            var cream = Mat("Warm ivory", new Color(.83f, .82f, .68f));
            Cube("Floor", new Vector3(0, -.35f, 0), new Vector3(15, .3f, 13), floor);
            Cube("Back wall", new Vector3(0, 3, 4), new Vector3(15, 7, .25f), wall);
            Cube("Left wall", new Vector3(-6, 3, 0), new Vector3(.25f, 7, 9), wall);
            Cube("Baseboard", new Vector3(0, -.02f, 3.8f), new Vector3(12, .22f, .12f), wood);
            Cube("Rug", new Vector3(0, -.18f, -1), new Vector3(7, .025f, 5), Mat("Rug", new Color(.22f, .33f, .31f)));
            Cube("Tabletop", new Vector3(0, .8f, 0), new Vector3(5.5f, .17f, 2.5f), wood);
            foreach (float x in new[] { -2.4f, 2.4f }) foreach (float z in new[] { -.95f, .95f })
                Cube("Table leg", new Vector3(x, .28f, z), new Vector3(.15f, 1, .15f), dark);
            Cube("Desk mat", new Vector3(.5f, .90f, -.1f), new Vector3(2.7f, .03f, 1.55f), wall);
            Cube("Window inset", new Vector3(-2.2f, 3.1f, 3.82f), new Vector3(2.8f, 2.8f, .07f), dark);
            var night = Mat("Blue night", new Color(.10f, .24f, .31f));
            Cube("Glass", new Vector3(-2.2f, 3.1f, 3.76f), new Vector3(2.5f, 2.5f, .05f), night);
            Cube("Window vertical", new Vector3(-2.2f, 3.1f, 3.7f), new Vector3(.07f, 2.6f, .07f), wood);
            Cube("Window crossbar", new Vector3(-2.2f, 3.1f, 3.7f), new Vector3(2.6f, .07f, .07f), wood);
            Sphere("Moon", new Vector3(-2.9f, 3.8f, 3.65f), new Vector3(.32f, .32f, .035f), cream);
            Cube("Shelf", new Vector3(2.8f, 2.75f, 3.6f), new Vector3(2.4f, .11f, .75f), wood);
            for (int i = 0; i < 5; i++)
            {
                var book = Cube("Book " + i, new Vector3(2.1f + i * .22f, 3.1f, 3.55f), new Vector3(.17f, .58f + (i % 2) * .12f, .35f),
                    i % 2 == 0 ? cream : Mat("Rust " + i, new Color(.55f, .27f + i * .015f, .17f)));
                book.transform.localRotation = Quaternion.Euler(0, 0, i == 4 ? -12 : 0);
            }
            var pot = Cylinder("Plant pot", new Vector3(3.5f, .3f, 1.7f), new Vector3(.65f, .5f, .65f), cream);
            var green = Mat("Leaves", new Color(.15f, .35f, .25f));
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60 * Mathf.Deg2Rad;
                var leaf = Sphere("Leaf", new Vector3(3.5f + Mathf.Cos(a) * .25f, 1 + (i % 3) * .14f, 1.7f + Mathf.Sin(a) * .25f), new Vector3(.3f, 1, .12f), green);
                leaf.transform.rotation = Quaternion.Euler(20, i * 60, i % 2 == 0 ? 20 : -20);
            }
            Cylinder("Incense dish", new Vector3(-1.7f, .93f, -.1f), new Vector3(.68f, .05f, .68f), cream);
            for (int i = 0; i < 4; i++) Ring("Incense coil", new Vector3(-1.7f, 1, -.1f), .10f + i * .065f, .013f, dark);
            Cylinder("Mug", new Vector3(1.85f, 1.12f, .45f), new Vector3(.3f, .25f, .3f), cream);
            Cube("Notebook", new Vector3(.9f, .95f, .5f), new Vector3(.7f, .06f, .9f), cream).transform.rotation = Quaternion.Euler(0, -12, 0);

            hand = new GameObject("Hand feedback").transform;
            var palm = Sphere("Palm", Vector3.zero, new Vector3(.6f, .17f, .65f), cream); palm.transform.SetParent(hand, false);
            for (int i = 0; i < 4; i++)
            { var finger = Sphere("Finger", new Vector3(-.21f + i * .14f, 0, .41f), new Vector3(.12f, .13f, .48f - Mathf.Abs(i - 1.5f) * .06f), cream); finger.transform.SetParent(hand, false); }
            hand.gameObject.SetActive(false);
            zapper = new GameObject("Zapper feedback").transform;
            var racket = Ring("Electric frame", Vector3.zero, .54f, .055f, Mat("Zapper amber", amber)); racket.transform.SetParent(zapper, false);
            var handle = Cube("Handle", new Vector3(0, 0, -.9f), new Vector3(.17f, .13f, .8f), dark); handle.transform.SetParent(zapper, false);
            for (int i = -3; i <= 3; i++)
            {
                float x = i * .13f, length = 2 * Mathf.Sqrt(.48f * .48f - x * x);
                var wire = Cube("Grid", new Vector3(x, 0, 0), new Vector3(.016f, .015f, length), cream); wire.transform.SetParent(zapper, false);
            }
            zapper.gameObject.SetActive(false);
            arc = new GameObject("Electric arc").AddComponent<LineRenderer>();
            arc.material = Mat("Arc", new Color(.70f, 1f, 1f)); arc.startWidth = .024f; arc.endWidth = .01f; arc.positionCount = 7; arc.enabled = false;
            smoke = new GameObject("Clear smoke").AddComponent<ParticleSystem>(); smoke.transform.position = new Vector3(0, 1.5f, 0);
            var main = smoke.main; main.startLifetime = .65f; main.startSpeed = 1.5f; main.startSize = .6f; main.maxParticles = 80;
            main.startColor = new Color(.67f, .77f, .69f, .4f); main.playOnAwake = false;
            var emission = smoke.emission; emission.enabled = false;
            smoke.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mat("Smoke", new Color(.52f, .66f, .59f));
            smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            BuildMosquitoMeshes();
            for (int i = 0; i < phases.Length; i++) phases[i] = i * 2.399963f;
        }

        private void BuildMosquitoMeshes()
        {
            var model = Resources.Load<GameObject>("Models/Mosquito");
            if (model == null) { Debug.LogError("Missing Blender mosquito FBX."); return; }
            var groups = new Dictionary<string, List<CombineInstance>>();
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                for (int s = 0; s < filter.sharedMesh.subMeshCount; s++)
                {
                    string name = filter.name.StartsWith("Wing_") ? filter.name : renderer.sharedMaterials[s].name;
                    if (!groups.ContainsKey(name)) groups[name] = new List<CombineInstance>();
                    groups[name].Add(new CombineInstance { mesh = filter.sharedMesh, subMeshIndex = s, transform = model.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                }
            }
            foreach (var pair in groups)
            {
                var mesh = new Mesh { name = "Instanced " + pair.Key }; mesh.CombineMeshes(pair.Value.ToArray(), true, true);
                Color color = pair.Key.Contains("Amber") ? amber : pair.Key.Contains("Eye") ? new Color(.98f, .37f, .19f) :
                    pair.Key.StartsWith("Wing") ? new Color(.60f, .83f, .82f) : new Color(.035f, .05f, .055f);
                var material = Mat(pair.Key, color); material.enableInstancing = true;
                parts.Add(new Part { mesh = mesh, material = material, wing = pair.Key.StartsWith("Wing_L") ? -1 : pair.Key.StartsWith("Wing_R") ? 1 : 0 });
            }
        }

        public void SetPopulation(Simulation game, int limit, bool menu)
        { population = menu || game == null ? 4 : (int)BigInteger.Min(game.Adults, Mathf.Clamp(limit, 1, 300)); }
        public void ResetVisuals() { clock = 0; actionTime = 0; Array.Clear(hiddenUntil, 0, hiddenUntil.Length); Array.Clear(deathUntil, 0, deathUntil.Length); smoke?.Clear(); }
        public void Attack(SimEvent e)
        {
            actionWeapon = e.Weapon; actionTime = .38f; missed = e.Type == SimEventType.Missed;
            if (e.Weapon == Weapon.Incense) smoke.Emit(40);
            if (!missed && population > 0)
            {
                int deaths = (int)BigInteger.Min(e.Count, Math.Min(30, population));
                for (int i = 0; i < deaths; i++)
                {
                    int target = (deathCursor + i) % population;
                    int slot = deathCursor++ % 30; deathPositions[slot] = positions[target]; deathUntil[slot] = Time.unscaledTime + .45f;
                    if (e.Weapon != Weapon.Incense) hiddenUntil[target] = Time.unscaledTime + .22f;
                }
                if (e.Weapon == Weapon.Incense) Array.Clear(hiddenUntil, 0, hiddenUntil.Length);
            }
        }

        public void Animate(bool running, bool reducedFlash, bool shake)
        {
            if (running || population == 4) clock += Time.unscaledDeltaTime;
            actionTime = Mathf.Max(0, actionTime - Time.unscaledDeltaTime);
            float scale = population < 15 ? 1.4f : population < 100 ? 1f : .76f;
            for (int i = 0; i < population; i++)
            {
                float p = phases[i], t = clock * (.45f + i % 7 * .045f);
                positions[i] = new Vector3(Mathf.Sin(t + p) * (1.2f + i % 5 * .35f),
                    1.9f + Mathf.Sin(t * 1.4f + p * 2) * .7f + i % 3 * .3f, Mathf.Cos(t * .8f + p) * 1.15f);
                rotations[i] = Quaternion.Euler(Mathf.Sin(t + p) * 14, (t + p) * Mathf.Rad2Deg, Mathf.Sin(t * 2 + p) * 15);
            }
            foreach (var part in parts)
            {
                for (int i = 0; i < population; i++)
                {
                    var flutter = part.wing == 0 ? Quaternion.identity : Quaternion.Euler(0, 0, part.wing * Mathf.Sin(clock * 90 + phases[i]) * 25);
                    float visibility = hiddenUntil[i] == 0 ? 1 : Mathf.Clamp01((Time.unscaledTime - hiddenUntil[i]) * 6);
                    matrices[i] = Matrix4x4.TRS(positions[i], rotations[i] * flutter, Vector3.one * scale * visibility);
                }
                if (population > 0) Graphics.DrawMeshInstanced(part.mesh, 0, part.material, matrices, population, null, ShadowCastingMode.Off, false);
                int deathCount = 0;
                for (int i = 0; i < deathUntil.Length; i++)
                {
                    float remaining = deathUntil[i] - Time.unscaledTime;
                    if (remaining <= 0) continue;
                    float elapsed = .45f - remaining;
                    deathMatrices[deathCount++] = Matrix4x4.TRS(deathPositions[i] + Vector3.down * elapsed * 3,
                        Quaternion.Euler(90, elapsed * 400, 180), Vector3.one * scale * (remaining / .45f));
                }
                if (deathCount > 0) Graphics.DrawMeshInstanced(part.mesh, 0, part.material, deathMatrices, deathCount, null, ShadowCastingMode.Off, false);
            }
            bool visible = actionTime > 0;
            hand.gameObject.SetActive(visible && actionWeapon == Weapon.Hand);
            zapper.gameObject.SetActive(visible && actionWeapon == Weapon.Zapper);
            float progress = 1 - actionTime / .38f;
            var location = new Vector3(missed ? 1.2f : .2f, 2.9f - Mathf.Sin(progress * Mathf.PI) * .8f, -.5f);
            hand.position = location; hand.rotation = Quaternion.Euler(12 + progress * 30, -20, -12);
            zapper.position = location; zapper.rotation = Quaternion.Euler(18 + progress * 40, 0, -20);
            arc.enabled = visible && actionWeapon == Weapon.Zapper && !reducedFlash;
            if (arc.enabled) for (int i = 0; i < 7; i++) arc.SetPosition(i, location + new Vector3((i - 3) * .15f, .12f + (i % 2) * .15f, i * .04f));
            sceneCamera.transform.position = cameraPosition + (shake && visible && actionWeapon == Weapon.Incense ? new Vector3(Mathf.Sin(progress * 53) * .025f, 0, 0) : Vector3.zero);
        }

        private static Material Mat(string name, Color color)
        {
            var m = new Material(Shader.Find("Mosquito/InstancedLit")) { name = name };
            m.SetColor("_BaseColor", color); return m;
        }
        private static GameObject Primitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material mat)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name; obj.transform.position = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = mat; Destroy(obj.GetComponent<Collider>()); return obj;
        }
        private static GameObject Cube(string name, Vector3 p, Vector3 s, Material m) => Primitive(PrimitiveType.Cube, name, p, s, m);
        private static GameObject Sphere(string name, Vector3 p, Vector3 s, Material m) => Primitive(PrimitiveType.Sphere, name, p, s, m);
        private static GameObject Cylinder(string name, Vector3 p, Vector3 s, Material m) => Primitive(PrimitiveType.Cylinder, name, p, s, m);
        private static GameObject Ring(string name, Vector3 position, float radius, float tube, Material material)
        {
            const int segments = 32, sides = 6;
            var vertices = new Vector3[segments * sides]; var indices = new int[segments * sides * 6];
            for (int i = 0; i < segments; i++) for (int j = 0; j < sides; j++)
            {
                float a = i * Mathf.PI * 2 / segments, b = j * Mathf.PI * 2 / sides;
                vertices[i * sides + j] = new Vector3(Mathf.Cos(a) * (radius + tube * Mathf.Cos(b)), tube * Mathf.Sin(b), Mathf.Sin(a) * (radius + tube * Mathf.Cos(b)));
                int v = i * sides + j, next = ((i + 1) % segments) * sides + j, k = v * 6;
                indices[k] = v; indices[k + 1] = next; indices[k + 2] = i * sides + (j + 1) % sides;
                indices[k + 3] = next; indices[k + 4] = ((i + 1) % segments) * sides + (j + 1) % sides; indices[k + 5] = indices[k + 2];
            }
            var mesh = new Mesh { vertices = vertices, triangles = indices }; mesh.RecalculateNormals();
            var obj = new GameObject(name); obj.transform.position = position;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = material; return obj;
        }
    }
}
