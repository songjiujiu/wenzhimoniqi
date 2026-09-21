using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Rendering;
using Mosquito.Core;
using Vector2 = UnityEngine.Vector2;
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
        private readonly Vector3[] flightCenters = new Vector3[300];
        private readonly Vector3[] velocities = new Vector3[300];
        private readonly Vector3[] desiredVelocities = new Vector3[300];
        private readonly float[] nextTurn = new float[300];
        private readonly System.Random flightRandom = new System.Random(739391);
        private bool inMenu;
        private readonly Quaternion[] rotations = new Quaternion[300];
        private readonly float[] phases = new float[300];
        private readonly float[] hiddenUntil = new float[300];
        private readonly Vector3[] deathPositions = new Vector3[300];
        private readonly Quaternion[] deathRotations = new Quaternion[300];
        private readonly float[] deathScales = new float[300];
        private readonly float[] deathUntil = new float[300];
        private readonly Matrix4x4[] deathMatrices = new Matrix4x4[300];
        private const float DeathDuration = 1.35f;
        private float MosquitoScale => (population < 15 ? 1.4f : population < 100 ? 1f : .76f) * .45f;
        private int deathCursor, flightSample = 1;
        private Vector3 actionPosition;
        private Camera sceneCamera;
        private Transform hand, zapper, incense;
        private Vector3 cameraPosition;
        private LineRenderer arc;
        private LineRenderer hitRing;
        private ParticleSystem smoke;
        private SurfaceEggView eggView;
        private float clock, actionTime, actionUntil;
        private int population;
        private Weapon actionWeapon;
        private bool missed;
        private readonly Color amber = new Color(0.97f, .64f, .25f);

        public void Initialize()
        {
            var editorPreview = transform.Find("Editor preview");
            if (editorPreview != null) editorPreview.gameObject.SetActive(false);
            var cameraObject = new GameObject("Room Camera");
            sceneCamera = cameraObject.AddComponent<Camera>(); cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(transform, false);
            var lighting = new GameObject("Runtime study lighting"); lighting.transform.SetParent(transform, false);
            StudyLighting.Configure(sceneCamera, lighting.transform);
            cameraPosition = sceneCamera.transform.position;
            cameraObject.AddComponent<AudioListener>();

            var wall = Mat("Wall muted teal", new Color(.20f, .32f, .33f));
            var floor = Mat("Warm floor", new Color(.35f, .30f, .24f));
            var wood = Mat("Oak", new Color(.59f, .43f, .27f));
            var dark = Mat("Graphite", new Color(.055f, .09f, .10f));
            var cream = Mat("Warm ivory", new Color(.83f, .82f, .68f));
            var floorSurface = Cube("Floor", new Vector3(0, -.35f, 0), new Vector3(15, .3f, 13), floor);
            var backSurface = Cube("Back wall", new Vector3(0, 3, 4), new Vector3(15, 7, .25f), wall);
            var leftSurface = Cube("Left wall", new Vector3(-6, 3, 0), new Vector3(.25f, 7, 9), wall);
            bool studyRoom = LoadBlenderRoom();
            eggView = gameObject.AddComponent<SurfaceEggView>();
            eggView.Initialize(floorSurface.transform, backSurface.transform, leftSurface.transform, sceneCamera, studyRoom);
            if (studyRoom)
            {
                // Retain the exact surface transforms used by egg placement.
                floorSurface.GetComponent<Renderer>().enabled = false;
                backSurface.GetComponent<Renderer>().enabled = false;
                leftSurface.GetComponent<Renderer>().enabled = false;
            }
            else
            {
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
            }

            hand = LoadReferenceModel("Hand").transform;
            hand.name = "Blender Hand feedback"; hand.gameObject.SetActive(false);
            zapper = LoadReferenceModel("Zapper").transform;
            zapper.name = "Blender Zapper feedback"; zapper.gameObject.SetActive(false);
            incense = LoadReferenceModel("Incense").transform;
            incense.localPosition = new Vector3(-3.22f, 1.866f, 1.757f);
            incense.localScale = Vector3.one * .60f;
            var cat = LoadReferenceModel("LuckyCat").transform;
            cat.localPosition = new Vector3(-.444f, 1.846f, 2.936f); cat.localScale = Vector3.one * .65f;
            var tower = LoadReferenceModel("Tower").transform;
            tower.localPosition = new Vector3(3.22f, 3.47f, 3.20f); tower.localScale = Vector3.one * .60f;
            arc = new GameObject("Electric arc").AddComponent<LineRenderer>();
            arc.material = Mat("Arc", new Color(.70f, 1f, 1f)); arc.startWidth = .024f; arc.endWidth = .01f; arc.positionCount = 7; arc.enabled = false;
            hitRing = new GameObject("Hit confirmation").AddComponent<LineRenderer>();
            hitRing.material = Mat("Hit amber", amber); hitRing.startWidth = hitRing.endWidth = .035f;
            hitRing.positionCount = 24; hitRing.loop = true; hitRing.enabled = false;
            smoke = new GameObject("Clear smoke").AddComponent<ParticleSystem>(); smoke.transform.position = new Vector3(0, 1.5f, 0);
            var main = smoke.main; main.startLifetime = .65f; main.startSpeed = 1.5f; main.startSize = .6f; main.maxParticles = 80;
            main.startColor = new Color(.67f, .77f, .69f, .4f); main.playOnAwake = false;
            var emission = smoke.emission; emission.enabled = false;
            smoke.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mat("Smoke", new Color(.52f, .66f, .59f));
            smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            BuildMosquitoMeshes();
            for (int i = 0; i < phases.Length; i++)
            {
                phases[i] = i * 2.399963f;
                flightCenters[i] = positions[i] = NextFlightCenter();
            }
        }

        private Vector3 NextFlightCenter()
        {
            Vector3 center, viewport;
            do
            {
                center = new Vector3(Mathf.Lerp(-4.6f, 4.2f, SpreadSample(flightSample, 2)),
                    Mathf.Lerp(1.45f, 4.1f, SpreadSample(flightSample, 3)),
                    Mathf.Lerp(-2.5f, 2.6f, SpreadSample(flightSample, 5)));
                flightSample++;
                viewport = sceneCamera.WorldToViewportPoint(center);
            } while (viewport.x < .08f || viewport.x > .76f || viewport.y < .29f || viewport.y > .78f);
            return center;
        }

        private static float SpreadSample(int index, int radix)
        {
            float value = 0, fraction = 1f / radix;
            while (index > 0) { value += index % radix * fraction; index /= radix; fraction /= radix; }
            return value;
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
                    string name = filter.name.StartsWith("Wing_") ? filter.name.Substring(0, 6) + "|" + renderer.sharedMaterials[s].name : renderer.sharedMaterials[s].name;
                    if (!groups.ContainsKey(name)) groups[name] = new List<CombineInstance>();
                    groups[name].Add(new CombineInstance { mesh = filter.sharedMesh, subMeshIndex = s, transform = model.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                }
            }
            foreach (var pair in groups)
            {
                var mesh = new Mesh { name = "Instanced " + pair.Key }; mesh.CombineMeshes(pair.Value.ToArray(), true, true);
                Color color = pair.Key.Contains("Amber") ? amber : pair.Key.Contains("Eye") ? new Color(.98f, .37f, .19f) :
                    pair.Key.StartsWith("Wing") ? new Color(.60f, .83f, .82f) : new Color(.035f, .05f, .055f);
                string materialName = pair.Key.Contains("|") ? pair.Key.Substring(pair.Key.IndexOf('|') + 1) : pair.Key;
                var paletteAsset = Resources.Load<TextAsset>("Models/ReferenceMaterials");
                if (paletteAsset != null)
                    foreach (var entry in JsonUtility.FromJson<RoomPalette>(paletteAsset.text).materials)
                        if (entry.name == materialName) color = new Color(entry.color[0], entry.color[1], entry.color[2], entry.color[3]);
                var material = Mat(pair.Key, color); material.enableInstancing = true;
                parts.Add(new Part { mesh = mesh, material = material, wing = pair.Key.StartsWith("Wing_L") ? -1 : pair.Key.StartsWith("Wing_R") ? 1 : 0 });
            }
        }

        public void SetPopulation(Simulation game, int limit, bool menu)
        {
            inMenu = menu;
            eggView.Sync(game, menu);
            int desired = menu || game == null ? 4 : (int)BigInteger.Min(game.Adults, Mathf.Clamp(limit, 1, 300));
            while (population < desired)
            {
                // A newborn or a replacement representative gets a new location, never its victim's path.
                flightCenters[population] = positions[population] = NextFlightCenter();
                rotations[population] = Quaternion.identity;
                velocities[population] = desiredVelocities[population] = Vector3.zero;
                nextTurn[population] = 0;
                population++;
            }
            population = desired;
        }
        public int ActiveDeathCount
        {
            get { int count = 0; foreach (float until in deathUntil) if (until > Time.unscaledTime) count++; return count; }
        }
        public void ResetVisuals() { population = 0; clock = 0; actionTime = actionUntil = 0; Array.Clear(hiddenUntil, 0, hiddenUntil.Length); Array.Clear(deathUntil, 0, deathUntil.Length); smoke?.Clear(); }
        public Vector2 VisibleTargetScreenPosition()
        {
            for (int i = 0; i < population; i++)
                if (hiddenUntil[i] <= Time.unscaledTime) return sceneCamera.WorldToScreenPoint(positions[i]);
            return new Vector2(Screen.width * .5f, Screen.height * .5f);
        }

        public bool FindEggTarget(Vector2 pointer, int[] adultTargets, out int index, out long due)
        {
            if (!eggView.FindTarget(pointer, out index, out due, out float distance)) return false;
            if (adultTargets != null && adultTargets.Length > 0)
            {
                Vector2 adult = sceneCamera.WorldToScreenPoint(positions[adultTargets[0]]);
                if ((adult - pointer).sqrMagnitude < distance) return false;
            }
            return true;
        }

        public void CrushEgg(int index)
        {
            actionPosition = eggView.RemoveTarget(index);
            actionWeapon = Weapon.Hand; actionTime = .38f; missed = false;
            actionUntil = Time.unscaledTime + actionTime;
        }

        public int[] FindTargets(Vector2 pointer, Weapon weapon)
        {
            float radius = (weapon == Weapon.Hand ? 45f : 95f) * Screen.height / 900f;
            var candidates = new List<KeyValuePair<int, float>>();
            for (int i = 0; i < population; i++)
            {
                if (hiddenUntil[i] > Time.unscaledTime) continue;
                Vector3 screen = sceneCamera.WorldToScreenPoint(positions[i]);
                if (screen.z <= 0 || screen.x < 0 || screen.x > Screen.width || screen.y < 0 || screen.y > Screen.height) continue;
                float distance = ((Vector2)screen - pointer).sqrMagnitude;
                if (distance <= radius * radius) candidates.Add(new KeyValuePair<int, float>(i, distance));
            }
            candidates.Sort((a, b) => a.Value.CompareTo(b.Value));
            int count = Mathf.Min(weapon == Weapon.Hand ? 1 : 10, candidates.Count);
            var targets = new int[count];
            for (int i = 0; i < count; i++) targets[i] = candidates[i].Key;
            return targets;
        }

        public void Attack(SimEvent e, int[] targets = null)
        {
            actionWeapon = e.Weapon; actionTime = .38f; missed = e.Type == SimEventType.Missed;
            actionUntil = Time.unscaledTime + actionTime;
            actionPosition = population > 0 ? positions[deathCursor % population] : new Vector3(0, 2, 0);
            if (targets != null && targets.Length > 0) actionPosition = positions[targets[0]];
            if (e.Weapon == Weapon.Incense)
            {
                smoke.transform.position = incense.position + Vector3.up * .20f;
                smoke.Emit(40);
            }
            if (!missed && population > 0)
            {
                float scale = MosquitoScale;
                int deaths = (int)BigInteger.Min(e.Count, population);
                int[] selected = null;
                if (e.Weapon != Weapon.Incense)
                {
                    deaths = Math.Min(deaths, targets == null ? 0 : targets.Length);
                    selected = new int[deaths]; if (deaths > 0) Array.Copy(targets, selected, deaths);
                    // Remove highest indices first so compaction cannot redirect another selected hit.
                    Array.Sort(selected); Array.Reverse(selected);
                }
                for (int i = 0; i < deaths; i++)
                {
                    int target = selected == null ? population - 1 : selected[i];
                    int slot = deathCursor++ % deathUntil.Length;
                    deathPositions[slot] = positions[target]; deathRotations[slot] = rotations[target]; deathScales[slot] = scale;
                    deathUntil[slot] = Time.unscaledTime + DeathDuration;
                    // Compact the live list: survivors keep their exact positions and flight paths.
                    int last = --population;
                    positions[target] = positions[last]; rotations[target] = rotations[last];
                    flightCenters[target] = flightCenters[last]; phases[target] = phases[last]; hiddenUntil[target] = hiddenUntil[last];
                    velocities[target] = velocities[last]; desiredVelocities[target] = desiredVelocities[last]; nextTurn[target] = nextTurn[last];
                    hiddenUntil[last] = e.Weapon == Weapon.Incense ? 0 : Time.unscaledTime + DeathDuration;
                }
            }
        }

        public void Animate(bool running, bool reducedFlash, bool shake)
        {
            eggView.Draw();
            float dt = running || inMenu ? Mathf.Min(Time.unscaledDeltaTime, .05f) : 0;
            clock += dt;
            actionTime = Mathf.Max(0, actionUntil - Time.unscaledTime);
            float scale = MosquitoScale;
            for (int i = 0; i < population; i++)
            {
                if (dt <= 0) continue;
                Vector3 viewport = sceneCamera.WorldToViewportPoint(positions[i]);
                bool nearEdge = viewport.x < .08f || viewport.x > .76f || viewport.y < .29f || viewport.y > .78f ||
                    positions[i].y < 1.25f || positions[i].y > 4.3f || Mathf.Abs(positions[i].x) > 4.8f || Mathf.Abs(positions[i].z) > 2.8f;
                if (clock >= nextTurn[i] || nearEdge)
                {
                    flightSample += flightRandom.Next(1, 17);
                    flightCenters[i] = NextFlightCenter();
                    Vector3 direction = nearEdge ? (flightCenters[i] - positions[i]).normalized :
                        new Vector3((float)flightRandom.NextDouble() * 2 - 1, (float)flightRandom.NextDouble() - .5f, (float)flightRandom.NextDouble() * 2 - 1).normalized;
                    float speed = .65f + (float)flightRandom.NextDouble() * 1.7f;
                    if (flightRandom.NextDouble() < .16) speed *= 1.6f;
                    desiredVelocities[i] = direction * speed;
                    nextTurn[i] = clock + .25f + (float)flightRandom.NextDouble() * 1.05f;
                    if (nearEdge) velocities[i] = desiredVelocities[i];
                }
                velocities[i] = Vector3.Lerp(velocities[i], desiredVelocities[i], 1 - Mathf.Exp(-7 * dt));
                positions[i] += velocities[i] * dt;
                if (velocities[i].sqrMagnitude > .01f)
                    rotations[i] = Quaternion.Slerp(rotations[i], Quaternion.LookRotation(velocities[i]), 1 - Mathf.Exp(-9 * dt));
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
                    float elapsed = DeathDuration - remaining;
                    float fade = Mathf.Clamp01(remaining / .35f);
                    Vector3 fall = deathPositions[i] + new Vector3(Mathf.Sin(i * 2.4f) * elapsed * .25f, -1.8f * elapsed * elapsed, 0);
                    fall.y = Mathf.Max(.05f, fall.y);
                    deathMatrices[deathCount++] = Matrix4x4.TRS(fall,
                        deathRotations[i] * Quaternion.Euler(0, elapsed * 160, Mathf.Min(180, elapsed * 900)), Vector3.one * deathScales[i] * fade);
                }
                if (deathCount > 0) Graphics.DrawMeshInstanced(part.mesh, 0, part.material, deathMatrices, deathCount, null, ShadowCastingMode.Off, false);
            }
            bool visible = actionTime > 0;
            hand.gameObject.SetActive(visible && actionWeapon == Weapon.Hand);
            zapper.gameObject.SetActive(visible && actionWeapon == Weapon.Zapper);
            float progress = 1 - actionTime / .38f;
            var location = actionPosition + new Vector3(missed ? .7f : 0, .6f - Mathf.Sin(progress * Mathf.PI) * .6f, 0);
            // Keep the palm and racket face legible from the fixed gameplay camera.
            var facingCamera = Quaternion.LookRotation(sceneCamera.transform.up, -sceneCamera.transform.forward);
            hand.position = location; hand.rotation = facingCamera * Quaternion.Euler(15 - progress * 30, 0, -12);
            zapper.position = location; zapper.rotation = facingCamera * Quaternion.Euler(12 + progress * 20, 0, -20);
            arc.enabled = visible && actionWeapon == Weapon.Zapper && !reducedFlash;
            if (arc.enabled) for (int i = 0; i < 7; i++) arc.SetPosition(i, location + new Vector3((i - 3) * .15f, .12f + (i % 2) * .15f, i * .04f));
            hitRing.enabled = visible && !missed && actionWeapon != Weapon.Incense;
            if (hitRing.enabled) for (int i = 0; i < hitRing.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2 / hitRing.positionCount;
                hitRing.SetPosition(i, actionPosition + (sceneCamera.transform.right * Mathf.Cos(angle) + sceneCamera.transform.up * Mathf.Sin(angle)) * (.16f + progress * .45f));
            }
            sceneCamera.transform.position = cameraPosition + (shake && visible && actionWeapon == Weapon.Incense ? new Vector3(Mathf.Sin(progress * 53) * .025f, 0, 0) : Vector3.zero);
        }

        private readonly List<Material> roomMaterials = new List<Material>();
        [Serializable] private sealed class RoomPalette { public RoomMaterial[] materials; }
        [Serializable] private sealed class RoomMaterial
        {
            public string name, texture, normal, mask;
            public float[] color;
            public float metallic, roughness, normalStrength, emission;
        }
        public bool LoadBlenderRoom()
        {
            return LoadReferenceModel("Room") != null;
        }

        public GameObject LoadReferenceModel(string modelName)
        {
            string objectName = "Blender " + modelName;
            var existing = transform.Find(objectName);
            if (existing != null) return existing.gameObject;
            var asset = Resources.Load<GameObject>("Models/" + modelName);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing Blender asset: Models/" + modelName + ". Run the Blender asset scripts and import the project.");
            }
            var room = Instantiate(asset, transform);
            room.name = objectName;
            room.transform.localPosition = Vector3.zero;
            room.transform.localRotation = Quaternion.identity;
            room.transform.localScale = Vector3.one;
            var paletteAsset = Resources.Load<TextAsset>(modelName == "Room" ? "Models/RoomMaterials" : "Models/ReferenceMaterials");
            var palette = paletteAsset == null ? null : JsonUtility.FromJson<RoomPalette>(paletteAsset.text);
            var settings = new Dictionary<string, RoomMaterial>();
            if (palette?.materials != null) foreach (var entry in palette.materials) settings[entry.name] = entry;
            var converted = new Dictionary<Material, Material>();
            foreach (var renderer in room.GetComponentsInChildren<MeshRenderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (source == null) continue;
                    if (!converted.TryGetValue(source, out var replacement))
                    {
                        Color color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") :
                            source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
                        replacement = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = source.name };
                        if (settings.TryGetValue(source.name, out var entry))
                        {
                            color = new Color(entry.color[0], entry.color[1], entry.color[2], entry.color[3]);
                            replacement.SetFloat("_Metallic", entry.metallic);
                            replacement.SetFloat("_Smoothness", 1 - entry.roughness);
                            if (!string.IsNullOrEmpty(entry.texture))
                                replacement.SetTexture("_BaseMap", Resources.Load<Texture2D>("Models/RoomTextures/" + entry.texture));
                            if (!string.IsNullOrEmpty(entry.normal))
                            {
                                replacement.SetTexture("_BumpMap", Resources.Load<Texture2D>("Models/RoomTextures/" + entry.normal));
                                replacement.SetFloat("_BumpScale", entry.normalStrength);
                                replacement.EnableKeyword("_NORMALMAP");
                            }
                            if (!string.IsNullOrEmpty(entry.mask))
                            {
                                replacement.SetTexture("_MetallicGlossMap", Resources.Load<Texture2D>("Models/RoomTextures/" + entry.mask));
                                replacement.SetFloat("_Smoothness", 1);
                                replacement.EnableKeyword("_METALLICSPECGLOSSMAP");
                            }
                            if (entry.emission > 0)
                            {
                                replacement.SetTexture("_EmissionMap", replacement.GetTexture("_BaseMap"));
                                replacement.SetColor("_EmissionColor", Color.white * entry.emission);
                                replacement.EnableKeyword("_EMISSION");
                                replacement.SetFloat("_SpecularHighlights", 0);
                                replacement.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                            }
                        }
                        replacement.SetColor("_BaseColor", color);
                        converted.Add(source, replacement); roomMaterials.Add(replacement);
                    }
                    materials[i] = replacement;
                }
                renderer.sharedMaterials = materials;
                // The distant sky and foliage sit behind the real window opening.
                if (renderer.name == "Room_Sky" || renderer.name == "Room_DistantGreen")
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            return room;
        }

        private void OnDestroy()
        {
            foreach (var material in roomMaterials)
                if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
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
