using System;
using System.IO;
using CSIVR.Core;
using CSIVR.Evidence;
using CSIVR.Interface;
using CSIVR.Tools;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Object = UnityEngine.Object;

namespace CSIVR.EditorTools
{
    /// <summary>
    /// Shared content for both rooms: materials and generated textures, lighting, evidence prefabs,
    /// evidence stations, UV scanners, case systems and the world-space UI. Room geometry is in MainSceneRooms.cs.
    /// </summary>
    public static partial class MainSceneBuilder
    {
        static int EnvLayer => LayerMask.NameToLayer(EnvironmentLayer);
        static int EvLayer => LayerMask.NameToLayer(EvidenceLayer);
        static int UvLayer => LayerMask.NameToLayer(UVTargetLayer);

        // Materials shared by the room builders.
        static Material mFloor, mWall, mWainscot, mTrim, mCeiling, mWood, mDarkWood, mMetal, mDarkMetal, mBoard, mRug, mRug2,
            mCard, mPaper, mTag, mLabel, mTray, mTraySlot, mScanner, mPractice, mLampPanel, mReaderOff, mReaderOn, mWindow,
            mMark, mLampGlow, mTape, mCardboard, mPlant, mPot, mScreen, mScreenDark, mScreenRed, mLedGreen, mLedRed, mChairFabric,
            mExtinguisher, mPosterTeal, mPosterAmber, mPosterNavy, mGlassDoor, mWhite;

        static void BuildWorld(Transform systems)
        {
            MakeWorldMaterials();
            BuildLighting();

            var environment = new GameObject("Environment").transform;
            var anchors = new GameObject("Teleport Anchors").transform;
            var evidence = new GameObject("Evidence").transform;
            var tools = new GameObject("Tools").transform;
            var ui = new GameObject("UI").transform;

            BuildShell(environment);
            var room1 = new GameObject("Room 1 - Research Office").transform;
            room1.SetParent(environment);
            var room2 = new GameObject("Room 2 - Security Office").transform;
            room2.SetParent(environment);

            GameObject practiceCube = BuildRoom1(room1, anchors, evidence, tools, ui);
            var room2Anchors = BuildRoom2(room2, anchors, evidence, tools, ui);
            BuildDoor(environment, room2Anchors);

            BuildAmbience(environment);
            BuildCaseSystems(systems, practiceCube);
        }

        // ---------- materials and textures ----------

        static void MakeWorldMaterials()
        {
            var tiles = MakeTexture("FloorTiles", 128, 128, true, (x, y) =>
            {
                bool grout = x < 2 || y < 2;
                bool alt = ((x / 64) + (y / 64)) % 2 == 0;
                if (grout) return new Color(0.46f, 0.48f, 0.51f);
                return alt ? new Color(0.74f, 0.76f, 0.79f) : new Color(0.70f, 0.72f, 0.76f);
            });
            mFloor = MakeTextured("RoomFloor", Color.white, tiles, new Vector2(16f, 14f), 0f);

            var skyline = MakeTexture("Skyline", 256, 192, false, MakeSkylinePixel);
            mWindow = MakeTextured("WindowSkyline", Color.white, skyline, Vector2.one, 1.1f);

            var tape = MakeTexture("CrimeTape", 256, 64, true, (x, y) =>
            {
                bool edge = y < 12 || y >= 52;
                if (!edge) return new Color(1f, 0.82f, 0.1f);
                return ((x + y) / 14) % 2 == 0 ? new Color(0.08f, 0.08f, 0.08f) : new Color(1f, 0.82f, 0.1f);
            });
            mTape = MakeTextured("CrimeTape", Color.white, tape, Vector2.one, 0f);

            mWall = MakeMaterial("RoomWall", new Color(0.88f, 0.87f, 0.82f));
            mWainscot = MakeMaterial("Wainscot", new Color(0.30f, 0.38f, 0.48f));
            mTrim = MakeMaterial("Trim", new Color(0.93f, 0.93f, 0.92f));
            mCeiling = MakeMaterial("Ceiling", new Color(0.92f, 0.92f, 0.9f));
            mWood = MakeMaterial("Wood", new Color(0.62f, 0.42f, 0.24f));
            mDarkWood = MakeMaterial("DarkWood", new Color(0.30f, 0.20f, 0.13f));
            mMetal = MakeMaterial("Metal", new Color(0.50f, 0.54f, 0.60f), 0.6f, 0.55f);
            mDarkMetal = MakeMaterial("DarkMetal", new Color(0.16f, 0.18f, 0.22f), 0.5f, 0.45f);
            mBoard = MakeMaterial("BriefingBoard", new Color(0.18f, 0.20f, 0.24f));
            mRug = MakeMaterial("Rug", new Color(0.18f, 0.32f, 0.45f));
            mRug2 = MakeMaterial("Rug2", new Color(0.45f, 0.20f, 0.18f));
            mCard = MakeMaterial("AccessCard", new Color(0.20f, 0.45f, 0.85f));
            mPaper = MakeMaterial("Paper", new Color(0.97f, 0.97f, 0.93f));
            mTag = MakeMaterial("CableTag", new Color(0.95f, 0.55f, 0.15f));
            mLabel = MakeMaterial("PackingLabel", new Color(0.90f, 0.80f, 0.55f));
            mTray = MakeMaterial("StationTray", new Color(0.10f, 0.20f, 0.28f), 0.3f, 0.5f);
            mTraySlot = MakeMaterial("StationSlot", new Color(0.04f, 0.09f, 0.14f));
            mScanner = MakeMaterial("ScannerBody", new Color(0.15f, 0.15f, 0.20f), 0.4f, 0.5f);
            mPractice = MakeMaterial("PracticeProp", new Color(0.90f, 0.50f, 0.20f));
            mLampGlow = MakeEmissive("ScannerLamp", new Color(0.45f, 0.20f, 0.80f), 1.5f);
            mLampPanel = MakeEmissive("CeilingLight", new Color(1f, 0.97f, 0.9f), 2f);
            mReaderOff = MakeEmissive("ReaderLocked", new Color(1f, 0.15f, 0.12f), 2.5f);
            mReaderOn = MakeEmissive("ReaderOpen", new Color(0.2f, 1f, 0.35f), 2.5f);
            mMark = MakeEmissive("UVMark", new Color(0.55f, 0.30f, 1f), 3f);
            mCardboard = MakeMaterial("Cardboard", new Color(0.72f, 0.55f, 0.35f));
            mPlant = MakeMaterial("PlantLeaf", new Color(0.18f, 0.55f, 0.25f));
            mPot = MakeMaterial("PlantPot", new Color(0.88f, 0.86f, 0.82f));
            mScreen = MakeEmissive("ScreenOn", new Color(0.25f, 0.55f, 0.75f), 1.1f);
            mScreenDark = MakeMaterial("ScreenOff", new Color(0.04f, 0.05f, 0.07f), 0.3f, 0.6f);
            mScreenRed = MakeEmissive("ScreenAlert", new Color(0.55f, 0.08f, 0.08f), 1.0f);
            mLedGreen = MakeEmissive("LedGreen", new Color(0.2f, 1f, 0.35f), 2.5f);
            mLedRed = MakeEmissive("LedRed", new Color(1f, 0.15f, 0.12f), 2.5f);
            mChairFabric = MakeMaterial("ChairFabric", new Color(0.12f, 0.30f, 0.62f));
            mExtinguisher = MakeMaterial("Extinguisher", new Color(0.78f, 0.10f, 0.10f), 0.2f, 0.6f);
            mPosterTeal = MakeMaterial("PosterTeal", new Color(0.13f, 0.62f, 0.60f));
            mPosterAmber = MakeMaterial("PosterAmber", new Color(0.92f, 0.62f, 0.15f));
            mPosterNavy = MakeMaterial("PosterNavy", new Color(0.12f, 0.22f, 0.40f));
            mGlassDoor = MakeEmissive("DoorGlass", new Color(0.55f, 0.75f, 0.9f), 0.6f);
            mWhite = MakeMaterial("White", new Color(0.95f, 0.95f, 0.95f));
        }

        static Color MakeSkylinePixel(int x, int y)
        {
            float t = y / 191f;
            var sky = Color.Lerp(new Color(0.80f, 0.90f, 1f), new Color(0.32f, 0.58f, 0.88f), t);
            // Building silhouettes with lit windows.
            int[] heights = { 70, 110, 55, 95, 130, 60, 100, 80, 120, 65 };
            int bw = 26;
            int index = Mathf.Min(x / bw, heights.Length - 1);
            if (y < heights[index])
            {
                bool window = (x % bw) % 8 > 2 && (y % 12) > 4 && (x % bw) > 3 && (x % bw) < bw - 3;
                return window ? new Color(1f, 0.9f, 0.55f) : new Color(0.22f, 0.28f, 0.38f);
            }
            return sky;
        }

        static Texture2D MakeTexture(string name, int w, int h, bool repeat, Func<int, int, Color> pixel)
        {
            string folder = ProjectRoot + "/Textures";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(ProjectRoot, "Textures");

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = pixel(x, y);
            tex.SetPixels(px);
            tex.Apply();

            string path = $"{folder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material MakeTextured(string name, Color tint, Texture2D texture, Vector2 tiling, float emission)
        {
            var mat = MakeMaterial(name, tint);
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", texture);
                mat.SetTextureScale("_BaseMap", tiling);
            }
            mat.mainTexture = texture;
            mat.mainTextureScale = tiling;
            if (emission > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetTexture("_EmissionMap", texture);
                mat.SetTextureScale("_EmissionMap", tiling);
                mat.SetColor("_EmissionColor", Color.white * emission);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material MakeEmissive(string name, Color color, float intensity)
        {
            var mat = MakeMaterial(name, color);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void BuildLighting()
        {
            // The ceiling would block the sun, so rooms are lit by flat ambient plus ceiling lamps.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.50f, 0.52f, 0.56f);

            var sun = new GameObject("Directional Light", typeof(Light));
            var l = sun.GetComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 0.3f;
            l.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // ---------- small prop helpers ----------

        static GameObject Deco(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, float yaw = 0f)
        {
            var go = Box(name, parent, pos, size, mat, 0);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Solid(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, float yaw = 0f)
        {
            var go = Box(name, parent, pos, size, mat, EnvLayer);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        static GameObject Cyl(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collider = false, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = size;
            if (euler.HasValue) go.transform.rotation = Quaternion.Euler(euler.Value);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (collider) go.layer = Mathf.Max(0, EnvLayer);
            else Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Ball(string name, Transform parent, Vector3 pos, float diameter, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * diameter;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>Text readable by a viewer who looks along yaw (0 = +Z, 90 = +X, 180 = -Z, 270 = -X).</summary>
        static TextMeshPro Text3D(Transform parent, string name, Vector3 pos, float yaw, string text, float fontSize, Color color,
            Vector2 size, FontStyles style = FontStyles.Bold, TextAlignmentOptions align = TextAlignmentOptions.Center, bool flat = false)
        {
            var go = new GameObject(name, typeof(TextMeshPro));
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.rotation = flat ? Quaternion.Euler(90f, yaw, 0f) : Quaternion.Euler(0f, yaw, 0f);
            var tmp = go.GetComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.rectTransform.sizeDelta = size;
            return tmp;
        }

        static void Plant(Transform parent, Vector3 floorPos, float scale = 1f)
        {
            var p = new GameObject("Plant").transform;
            p.SetParent(parent);
            p.position = floorPos;
            Cyl("Pot", p, floorPos + new Vector3(0f, 0.15f * scale, 0f), new Vector3(0.30f, 0.15f, 0.30f) * scale, mPot, true);
            Cyl("Soil", p, floorPos + new Vector3(0f, 0.29f * scale, 0f), new Vector3(0.26f, 0.01f, 0.26f) * scale, mDarkWood);
            Ball("Leaf 1", p, floorPos + new Vector3(0f, 0.55f * scale, 0f), 0.34f * scale, mPlant);
            Ball("Leaf 2", p, floorPos + new Vector3(0.12f * scale, 0.42f * scale, 0.06f * scale), 0.24f * scale, mPlant);
            Ball("Leaf 3", p, floorPos + new Vector3(-0.11f * scale, 0.46f * scale, -0.05f * scale), 0.26f * scale, mPlant);
            Ball("Leaf 4", p, floorPos + new Vector3(0.02f * scale, 0.74f * scale, -0.02f * scale), 0.22f * scale, mPlant);
        }

        // yaw = direction the screen faces toward the viewer (viewer looks along yaw).
        static void Monitor(Transform parent, Vector3 deskPos, float yaw, Material screen, string label = null, float labelSize = 0.7f)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var m = new GameObject("Monitor").transform;
            m.SetParent(parent);
            m.SetPositionAndRotation(deskPos, rot);

            Deco("Base", m, deskPos + rot * new Vector3(0f, 0.01f, 0f), new Vector3(0.22f, 0.02f, 0.16f), mDarkMetal, yaw);
            Deco("Stand", m, deskPos + rot * new Vector3(0f, 0.11f, 0f), new Vector3(0.04f, 0.2f, 0.04f), mDarkMetal, yaw);
            Deco("Frame", m, deskPos + rot * new Vector3(0f, 0.30f, 0f), new Vector3(0.50f, 0.30f, 0.03f), mDarkMetal, yaw);
            Deco("Screen", m, deskPos + rot * new Vector3(0f, 0.30f, -0.017f), new Vector3(0.46f, 0.26f, 0.004f), screen, yaw);
            if (label != null)
                Text3D(m, "Screen Text", deskPos + rot * new Vector3(0f, 0.30f, -0.021f), yaw, label, labelSize,
                    new Color(0.85f, 0.95f, 1f), new Vector2(0.44f, 0.24f));
        }

        static void DeskLamp(Transform parent, Vector3 deskPos, float yaw)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var l = new GameObject("Desk Lamp").transform;
            l.SetParent(parent);
            l.SetPositionAndRotation(deskPos, rot);
            Cyl("Base", l, deskPos + new Vector3(0f, 0.01f, 0f), new Vector3(0.14f, 0.01f, 0.14f), mDarkMetal);
            Cyl("Arm", l, deskPos + new Vector3(0f, 0.17f, 0f), new Vector3(0.02f, 0.16f, 0.02f), mDarkMetal);
            Cyl("Shade", l, deskPos + rot * new Vector3(0f, 0.34f, 0.04f), new Vector3(0.16f, 0.07f, 0.16f), mDarkMetal, false, new Vector3(20f, yaw, 0f));
            var glow = new GameObject("Lamp Light", typeof(Light));
            glow.transform.SetParent(l);
            glow.transform.position = deskPos + rot * new Vector3(0f, 0.30f, 0.06f);
            var light = glow.GetComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.85f, 0.6f);
            light.range = 2.2f;
            light.intensity = 0.9f;
            light.shadows = LightShadows.None;
        }

        static void Cardboard(Transform parent, Vector3 pos, Vector3 size, float yaw)
        {
            var c = Solid("Cardboard Box", parent, pos, size, mCardboard, yaw);
            Deco("Tape", c.transform, pos + new Vector3(0f, size.y * 0.5f + 0.001f, 0f), new Vector3(size.x * 0.12f, 0.003f, size.z * 1.01f), mTrim, yaw);
        }

        static void Poster(Transform parent, Vector3 pos, float yaw, Vector2 size, string title, Material frameMat, float fontSize = 1.1f)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var p = new GameObject("Poster").transform;
            p.SetParent(parent);
            p.SetPositionAndRotation(pos, rot);
            Deco("Frame", p, pos, new Vector3(size.x + 0.05f, size.y + 0.05f, 0.025f), mDarkMetal, yaw);
            Deco("Face", p, pos + rot * new Vector3(0f, 0f, -0.014f), new Vector3(size.x, size.y, 0.006f), frameMat, yaw);
            Text3D(p, "Title", pos + rot * new Vector3(0f, 0f, -0.02f), yaw, title, fontSize, Color.white,
                new Vector2(size.x * 0.9f, size.y * 0.8f));
        }

        static void CeilingLamps(Transform parent, float[] xs, float[] zs)
        {
            foreach (var x in xs)
            {
                foreach (var z in zs)
                {
                    Deco("Ceiling Fixture Rim", parent, new Vector3(x, 2.992f, z), new Vector3(1.08f, 0.02f, 0.48f), mTrim);
                    Deco("Ceiling Lamp", parent, new Vector3(x, 2.978f, z), new Vector3(1.0f, 0.02f, 0.4f), mLampPanel);
                    var lg = new GameObject("Lamp Light", typeof(Light));
                    lg.transform.SetParent(parent);
                    lg.transform.position = new Vector3(x, 2.7f, z);
                    var light = lg.GetComponent<Light>();
                    light.type = LightType.Point;
                    light.range = 7f;
                    light.intensity = 1.1f;
                    light.shadows = LightShadows.None;
                }
            }
        }

        // Dark lower band and a light rail along a wall. 'along' selects X or Z; viewer side is given by 'inward'.
        static void Wainscot(Transform parent, string name, Vector3 center, float length, bool alongX, float inward)
        {
            var size = alongX ? new Vector3(length, 1.0f, 0.03f) : new Vector3(0.03f, 1.0f, length);
            var rail = alongX ? new Vector3(length, 0.05f, 0.05f) : new Vector3(0.05f, 0.05f, length);
            Deco(name, parent, center, size, mWainscot);
            var railPos = center + new Vector3(0f, 0.525f, 0f);
            if (alongX) railPos.z += inward * 0.01f; else railPos.x += inward * 0.01f;
            Deco(name + " Rail", parent, railPos, rail, mTrim);
        }

        // ---------- evidence ----------

        struct EvidenceSpec
        {
            public string Id, PrefabName, Text;
            public Vector3 Size;
            public Material Mat;
            public float FontSize;
            public Color TextColor;
            public bool Stripe;
        }

        static GameObject MakeEvidencePrefab(EvidenceSpec spec)
        {
            var root = BuildGrabbable(spec.PrefabName, spec.Size, spec.Mat);
            float top = spec.Size.y * 0.5f + 0.001f;

            if (spec.Stripe)
            {
                var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "Badge Stripe";
                stripe.transform.SetParent(root.transform, false);
                stripe.transform.localPosition = new Vector3(0f, top, spec.Size.z * 0.28f);
                stripe.transform.localScale = new Vector3(spec.Size.x * 0.92f, 0.002f, spec.Size.z * 0.18f);
                Object.DestroyImmediate(stripe.GetComponent<Collider>());
                stripe.GetComponent<Renderer>().sharedMaterial = mWhite;
            }

            if (!string.IsNullOrEmpty(spec.Text))
            {
                var t = new GameObject("Text", typeof(TextMeshPro));
                t.transform.SetParent(root.transform, false);
                t.transform.localPosition = new Vector3(0f, top + 0.002f, spec.Stripe ? -spec.Size.z * 0.12f : 0f);
                t.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var tmp = t.GetComponent<TextMeshPro>();
                tmp.text = spec.Text;
                tmp.fontSize = spec.FontSize;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = spec.TextColor;
                tmp.textWrappingMode = TextWrappingModes.Normal;
                tmp.rectTransform.sizeDelta = new Vector2(spec.Size.x * 0.92f, spec.Size.z * 0.8f);
            }

            var item = root.AddComponent<EvidenceItem>();
            var so = new SerializedObject(item);
            so.FindProperty("m_Id").stringValue = spec.Id;
            so.ApplyModifiedPropertiesWithoutUndo();
            SetLayerRecursively(root, EvLayer);

            var path = $"{ProjectRoot}/Prefabs/Evidence/{spec.PrefabName}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // Root = collider + Rigidbody + XR Grab Interactable; the mesh is a child so scale never distorts text.
        static GameObject BuildGrabbable(string name, Vector3 size, Material mat)
        {
            var root = new GameObject(name);
            var col = root.AddComponent<BoxCollider>();
            col.size = size;

            var body = root.AddComponent<Rigidbody>();
            body.mass = 0.1f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vis.name = "Visual";
            vis.transform.SetParent(root.transform, false);
            vis.transform.localScale = size;
            Object.DestroyImmediate(vis.GetComponent<Collider>());
            vis.GetComponent<Renderer>().sharedMaterial = mat;

            var grab = root.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = false;

            root.AddComponent<ObjectRecovery>();
            root.AddComponent<InteractableHighlight>();
            return root;
        }

        static GameObject Place(GameObject prefab, Transform parent, Vector3 position, float yaw)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            return go;
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            if (layer < 0) return;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }

        // ---------- station ----------

        /// <summary>
        /// Tray table for one room. The player stands on the -X side looking along +X (viewerYaw = 90); the tray's
        /// long axis runs along Z. Layout is in world axes. tablePos is the table's floor position.
        /// </summary>
        static GameObject BuildStation(Transform parent, Transform evidenceRoot, int caseIndex, Vector3 tablePos, float viewerYaw, string[] slotLabels)
        {
            Solid("Station Table", parent, tablePos + new Vector3(0f, 0.45f, 0f), new Vector3(0.7f, 0.9f, 1.0f), mDarkMetal);
            Solid("Station Tray", parent, tablePos + new Vector3(0f, 0.905f, 0f), new Vector3(0.5f, 0.01f, 0.94f), mTray);

            // Recessed-looking slots with labels, like an evidence tray.
            for (int i = 0; i < 3; i++)
            {
                var local = new Vector3(0f, 0.911f, -0.3f + 0.3f * i);
                Deco("Slot " + (i + 1), parent, tablePos + local, new Vector3(0.40f, 0.004f, 0.28f), mTraySlot);
                Text3D(parent, "Slot Label " + (i + 1), tablePos + local + new Vector3(-0.215f, 0.004f, 0f), viewerYaw,
                    slotLabels[i], 0.55f, new Color(0.6f, 0.85f, 0.85f), new Vector2(0.28f, 0.12f), FontStyles.Bold, TextAlignmentOptions.Center, true);
            }
            // Glowing rim so the tray reads as the place to put things.
            Deco("Rim Left", parent, tablePos + new Vector3(-0.255f, 0.912f, 0f), new Vector3(0.012f, 0.006f, 0.94f), mReaderOn);
            Deco("Rim Right", parent, tablePos + new Vector3(0.255f, 0.912f, 0f), new Vector3(0.012f, 0.006f, 0.94f), mReaderOn);

            var station = new GameObject("Evidence Station", typeof(BoxCollider), typeof(EvidenceStation));
            station.transform.SetParent(evidenceRoot);
            station.transform.position = tablePos + new Vector3(0f, 1.05f, 0f);
            var trigger = station.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(0.5f, 0.3f, 0.9f);

            var audio = AudioFx.AddSpatialSource(station, 1f, 7f);
            audio.volume = 0.8f;

            var slots = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var slot = new GameObject($"Slot {i + 1}").transform;
                slot.SetParent(station.transform);
                // The log is long, so its slot is turned a quarter turn to fit the tray.
                slot.SetPositionAndRotation(tablePos + new Vector3(0f, 0.917f, -0.3f + 0.3f * i), Quaternion.Euler(0f, i == 1 ? 90f : 0f, 0f));
                slots[i] = slot;
            }

            var so = new SerializedObject(station.GetComponent<EvidenceStation>());
            so.FindProperty("m_CaseIndex").intValue = caseIndex;
            var slotsProp = so.FindProperty("m_Slots");
            slotsProp.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            so.FindProperty("m_Audio").objectReferenceValue = audio;
            so.ApplyModifiedPropertiesWithoutUndo();
            return station;
        }

        // ---------- UV scanner ----------

        static GameObject BuildCabinetTarget(Transform parent, string id, Vector3 pos, float yaw, Vector2 markSize)
        {
            var target = new GameObject("UV Target " + id, typeof(BoxCollider), typeof(UVTarget));
            target.transform.SetParent(parent);
            target.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            target.layer = Mathf.Max(0, UvLayer);
            var col = target.GetComponent<BoxCollider>();
            col.size = new Vector3(markSize.x, markSize.y, 0.04f);

            // Visual sits slightly off the surface to avoid flicker; hidden until validly lit.
            var mark = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mark.name = "Mark";
            mark.transform.SetParent(target.transform, false);
            mark.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            mark.transform.localScale = new Vector3(markSize.x, markSize.y, 1f);
            Object.DestroyImmediate(mark.GetComponent<Collider>());
            var markRenderer = mark.GetComponent<Renderer>();
            markRenderer.sharedMaterial = mMark;
            markRenderer.enabled = false;

            var so = new SerializedObject(target.GetComponent<UVTarget>());
            so.FindProperty("m_Id").stringValue = id;
            so.FindProperty("m_Mark").objectReferenceValue = markRenderer;
            so.ApplyModifiedPropertiesWithoutUndo();
            return target;
        }

        static GameObject BuildScanner(Transform parent, Vector3 pos, float yaw)
        {
            var root = BuildGrabbableTool("UV Scanner");
            var path = $"{ProjectRoot}/Prefabs/Tools/UVScanner.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return Place(prefab, parent, pos, yaw);
        }

        // Handle along +Z, lamp at the tip, ScanOrigin and violet spotlight at the tip.
        static GameObject BuildGrabbableTool(string name)
        {
            var root = new GameObject(name);
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.07f, 0.07f, 0.24f);
            col.center = new Vector3(0f, 0f, 0.04f);

            var body = root.AddComponent<Rigidbody>();
            body.mass = 0.3f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Handle";
            handle.transform.SetParent(root.transform, false);
            handle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            handle.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            handle.transform.localScale = new Vector3(0.04f, 0.07f, 0.04f);
            Object.DestroyImmediate(handle.GetComponent<Collider>());
            handle.GetComponent<Renderer>().sharedMaterial = mScanner;

            var lamp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lamp.name = "Lamp";
            lamp.transform.SetParent(root.transform, false);
            lamp.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            lamp.transform.localPosition = new Vector3(0f, 0f, 0.11f);
            lamp.transform.localScale = new Vector3(0.07f, 0.04f, 0.07f);
            Object.DestroyImmediate(lamp.GetComponent<Collider>());
            lamp.GetComponent<Renderer>().sharedMaterial = mLampGlow;

            var grip = new GameObject("Grip").transform;
            grip.SetParent(root.transform, false);
            grip.localPosition = new Vector3(0f, 0f, -0.03f);

            var origin = new GameObject("ScanOrigin").transform;
            origin.SetParent(root.transform, false);
            origin.localPosition = new Vector3(0f, 0f, 0.16f);

            var lightGo = new GameObject("UV Light", typeof(Light));
            lightGo.transform.SetParent(origin, false);
            var light = lightGo.GetComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(0.55f, 0.30f, 1f);
            light.spotAngle = 40f;
            light.range = 2.5f;
            light.intensity = 6f;
            light.shadows = LightShadows.None;
            light.enabled = false;

            var audio = AudioFx.AddSpatialSource(root, 0.5f, 6f);
            audio.volume = 0.35f;

            var grab = root.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = false;
            grab.attachTransform = grip;

            var scanner = root.AddComponent<UVScanner>();
            var so = new SerializedObject(scanner);
            so.FindProperty("m_ScanOrigin").objectReferenceValue = origin;
            so.FindProperty("m_Light").objectReferenceValue = light;
            so.FindProperty("m_ScanAudio").objectReferenceValue = audio;
            var bits = so.FindProperty("m_RayMask.m_Bits");
            if (bits != null)
                bits.intValue = LayerMask.GetMask(EnvironmentLayer, EvidenceLayer, UVTargetLayer);
            so.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<ObjectRecovery>();
            root.AddComponent<InteractableHighlight>();
            return root;
        }

        // ---------- case systems, door, ambience, UI ----------

        static void BuildCaseSystems(Transform systems, GameObject practiceProp)
        {
            var go = new GameObject("Case System", typeof(CaseManager), typeof(TutorialTracker));
            go.transform.SetParent(systems);
            var so = new SerializedObject(go.GetComponent<TutorialTracker>());
            so.FindProperty("m_PracticeProp").objectReferenceValue = practiceProp.GetComponent<XRGrabInteractable>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildAmbience(Transform parent)
        {
            var go = new GameObject("Room Ambience", typeof(AudioSource), typeof(AmbienceSource));
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(2.0f, 2.85f, 2.2f);
            var go2 = new GameObject("Room Ambience 2", typeof(AudioSource), typeof(AmbienceSource));
            go2.transform.SetParent(parent);
            go2.transform.position = new Vector3(-2.0f, 2.85f, -8.0f);
        }

        /// <param name="boardPos">World position of the board face (canvas plane).</param>
        /// <param name="boardYaw">Direction the reader looks at the board.</param>
        static void BuildCaseUi(Transform ui, int caseIndex, Vector3 boardPos, float boardYaw, Vector3 stationPanelPos, float stationYaw)
        {
            // Canvas fronts face the way the viewer looks, so the anchor yaw is the viewer's look direction.
            var board = new GameObject($"Board Anchor {caseIndex + 1}").transform;
            board.SetParent(ui);
            board.SetPositionAndRotation(boardPos, Quaternion.Euler(0f, boardYaw, 0f));

            var stationAnchor = new GameObject($"Station Panel Anchor {caseIndex + 1}").transform;
            stationAnchor.SetParent(ui);
            stationAnchor.SetPositionAndRotation(stationPanelPos, Quaternion.Euler(0f, stationYaw, 0f));

            var go = new GameObject($"Case UI {caseIndex + 1}", typeof(CaseUI));
            go.transform.SetParent(ui);
            var so = new SerializedObject(go.GetComponent<CaseUI>());
            so.FindProperty("m_CaseIndex").intValue = caseIndex;
            so.FindProperty("m_BoardAnchor").objectReferenceValue = board;
            so.FindProperty("m_StationAnchor").objectReferenceValue = stationAnchor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
