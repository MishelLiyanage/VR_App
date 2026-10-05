using System.IO;
using CSIVR.Core;
using CSIVR.Evidence;
using CSIVR.Tools;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace CSIVR.EditorTools
{
    /// <summary>
    /// Members 3 and 4. Menu: CSI VR > Evidence and UV.
    ///  1) Create Prefabs      -> Assets/_Project/Prefabs/Evidence and /Tools
    ///  2) Place Test Set      -> drops them into the OPEN scene at positions matching MainSceneBuilder's room.
    /// Do this in a copy of Main (Scenes/Workbenches/...), NOT in Main.unity (Member 1 rebuilds Main).
    /// </summary>
    public static class EvidenceUVBuilder
    {
        const string Root = "Assets/_Project";
        const string EvidenceDir = Root + "/Prefabs/Evidence";
        const string ToolsDir = Root + "/Prefabs/Tools";

        // ---------- menu ----------

        [MenuItem("CSI VR/Evidence and UV/1 Create Prefabs")]
        public static void CreatePrefabs()
        {
            EnsureFolder(EvidenceDir);
            EnsureFolder(ToolsDir);
            EnsureFolder(Root + "/Materials");

            BuildEvidence("Evidence_E01_AccessCard", "E01", "Access card",
                "Card identifier B-17. Finding a card does not identify the offender.",
                new Vector3(0.086f, 0.012f, 0.054f), new Color(0.2f, 0.4f, 0.8f));
            BuildEvidence("Evidence_E02_AccessLog", "E02", "Printed access log",
                "Fictional log: successful B-17 access at 19:42.",
                new Vector3(0.15f, 0.012f, 0.21f), new Color(0.92f, 0.92f, 0.86f));
            BuildEvidence("Evidence_E03_PackingLabel", "E03", "Torn packing label",
                "Matches the prototype container; supports what was moved.",
                new Vector3(0.10f, 0.012f, 0.06f), new Color(0.85f, 0.72f, 0.5f));

            BuildStation();
            BuildUVTool();
            BuildUVTarget();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EvidenceUVBuilder] Prefabs created in " + Root + "/Prefabs");
        }

        [MenuItem("CSI VR/Evidence and UV/2 Place Test Set In Open Scene")]
        public static void PlaceTestSet()
        {
            var evidenceParent = FindParent("Evidence");
            var toolsParent = FindParent("Tools");
            var systems = FindParent("Systems");

            if (Object.FindAnyObjectByType<CaseManager>() == null)
            {
                var cm = new GameObject("Case Manager (stub - Member 5 replaces)", typeof(CaseManager));
                if (systems != null) cm.transform.SetParent(systems);
            }

            // Placeholder ledge so E03 has somewhere to sit (the placeholder shelf is a solid block).
            var ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ledge.name = "Shelf Ledge (placeholder)";
            ledge.transform.position = new Vector3(-2.2f, 1.1f, 2.9f);
            ledge.transform.localScale = new Vector3(0.9f, 0.04f, 0.3f);
            SetLayer(ledge, "Environment");
            Undo.RegisterCreatedObjectUndo(ledge, "Place ledge");

            Place(EvidenceDir + "/Evidence_E01_AccessCard.prefab", evidenceParent, new Vector3(1.7f, 0.02f, -2.6f), 0f);
            Place(EvidenceDir + "/Evidence_E02_AccessLog.prefab", evidenceParent, new Vector3(2.6f, 0.77f, -2.0f), 0f);
            Place(EvidenceDir + "/Evidence_E03_PackingLabel.prefab", evidenceParent, new Vector3(-2.2f, 1.14f, 2.9f), 0f);
            Place(EvidenceDir + "/Evidence Station.prefab", evidenceParent, new Vector3(3.3f, 0.9f, 0f), 90f);
            Place(ToolsDir + "/UV Tool.prefab", toolsParent, new Vector3(2.6f, 0.79f, -2.6f), 0f);
            Place(ToolsDir + "/UV Target E04.prefab", toolsParent, new Vector3(2.2f, 1.0f, 2.87f), 0f);

            var zone = new GameObject("Recovery Zone", typeof(BoxCollider), typeof(ObjectRecovery));
            zone.transform.position = new Vector3(0f, -2f, 0f);
            var box = zone.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(30f, 1f, 30f);
            if (systems != null) zone.transform.SetParent(systems);
            Undo.RegisterCreatedObjectUndo(zone, "Place recovery zone");

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[EvidenceUVBuilder] Test set placed. Save the scene (File > Save).");
        }

        // ---------- evidence ----------

        static void BuildEvidence(string file, string id, string title, string desc, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = file;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Evidence_" + id, color);
            SetLayer(go, "Evidence");

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var grab = go.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;

            var item = go.AddComponent<EvidenceItem>();
            Set(item, "m_EvidenceId", p => p.stringValue = id);
            Set(item, "m_Title", p => p.stringValue = title);
            Set(item, "m_Description", p => p.stringValue = desc);
            Set(item, "m_RequiresUV", p => p.boolValue = false);

            Save(go, $"{EvidenceDir}/{file}.prefab");
        }

        // ---------- station ----------
        // Root origin = top of the table. Place with yaw 90 so the label faces the player at the station anchor.

        static void BuildStation()
        {
            var root = new GameObject("Evidence Station");

            var trayMat = MakeMaterial("StationTray", new Color(0.55f, 0.58f, 0.62f));
            var tray = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tray.name = "Tray";
            tray.transform.SetParent(root.transform, false);
            tray.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            tray.transform.localScale = new Vector3(0.7f, 0.04f, 0.45f);
            tray.GetComponent<Renderer>().sharedMaterial = trayMat;
            SetLayer(tray, "Environment");

            // Trigger + EvidenceStation must be on the SAME object.
            var zone = new GameObject("Record Zone", typeof(BoxCollider), typeof(EvidenceStation), typeof(AudioSource));
            zone.transform.SetParent(root.transform, false);
            zone.transform.localPosition = new Vector3(0f, 0.14f, 0f);
            var box = zone.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(0.66f, 0.2f, 0.41f);
            var audio = zone.GetComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;

            var slots = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var s = new GameObject("Slot " + (i + 1)).transform;
                s.SetParent(root.transform, false);
                s.localPosition = new Vector3(-0.22f + 0.22f * i, 0.055f, 0f);
                slots[i] = s;
            }

            var labelGO = new GameObject("Station Label", typeof(TextMeshPro));
            labelGO.transform.SetParent(root.transform, false);
            labelGO.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            var label = labelGO.GetComponent<TextMeshPro>();
            label.text = "Evidence station";
            label.fontSize = 1.5f; // tune in the Inspector if too big/small
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.rectTransform.sizeDelta = new Vector2(1f, 0.4f);

            var station = zone.GetComponent<EvidenceStation>();
            Set(station, "m_Label", p => p.objectReferenceValue = label);
            Set(station, "m_ConfirmSound", p => p.objectReferenceValue = audio);
            Set(station, "m_DisplaySlots", p =>
            {
                p.arraySize = slots.Length;
                for (int i = 0; i < slots.Length; i++)
                    p.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            });

            BuildRestoreButton(root.transform);
            Save(root, EvidenceDir + "/Evidence Station.prefab");
        }

        static void BuildRestoreButton(Transform parent)
        {
            var canvasGO = new GameObject("Restore Canvas", typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster));
            canvasGO.transform.SetParent(parent, false);
            canvasGO.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rt = canvasGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300f, 80f);
            rt.localScale = Vector3.one * 0.001f;
            rt.localPosition = new Vector3(0f, 0.3f, 0.3f);

            var btnGO = new GameObject("Restore Button", typeof(Image), typeof(Button));
            btnGO.transform.SetParent(canvasGO.transform, false);
            Stretch(btnGO.GetComponent<RectTransform>());
            btnGO.GetComponent<Image>().color = new Color(0.1f, 0.25f, 0.4f);

            var txtGO = new GameObject("Text", typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(btnGO.transform, false);
            Stretch(txtGO.GetComponent<RectTransform>());
            var txt = txtGO.GetComponent<TextMeshProUGUI>();
            txt.text = "Restore loose items";
            txt.fontSize = 28f;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;

            var recovery = canvasGO.AddComponent<ObjectRecovery>();
            UnityEventTools.AddPersistentListener(btnGO.GetComponent<Button>().onClick, recovery.RestoreLooseItems);
        }

        // ---------- UV tool + target ----------

        static void BuildUVTool()
        {
            var root = new GameObject("UV Tool");

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0f, 0.04f);
            col.size = new Vector3(0.05f, 0.05f, 0.2f);

            var toolMat = MakeMaterial("UVToolBody", new Color(0.15f, 0.15f, 0.18f));
            Visual(PrimitiveType.Cube, "Handle", root.transform, Vector3.zero, new Vector3(0.03f, 0.03f, 0.12f), Vector3.zero, toolMat);
            Visual(PrimitiveType.Cylinder, "Lamp", root.transform, new Vector3(0f, 0f, 0.09f), new Vector3(0.05f, 0.03f, 0.05f),
                   new Vector3(90f, 0f, 0f), MakeMaterial("UVToolLamp", new Color(0.5f, 0.3f, 0.9f)));

            var origin = new GameObject("ScanOrigin").transform;
            origin.SetParent(root.transform, false);
            origin.localPosition = new Vector3(0f, 0f, 0.125f);

            var lightGO = new GameObject("UV Light", typeof(Light));
            lightGO.transform.SetParent(origin, false);
            var light = lightGO.GetComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = 40f;           // = 2 x cone half-angle (20)
            light.range = 2f;
            light.intensity = 3f;
            light.color = new Color(0.6f, 0.35f, 1f);
            light.enabled = false;

            // Grip point. Keep identity rotation so the beam points where the hand/ray points (also on desktop).
            var attach = new GameObject("Attach").transform;
            attach.SetParent(root.transform, false);
            attach.localPosition = new Vector3(0f, 0f, -0.02f);

            var grab = root.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.attachTransform = attach;

            var loop = AddAudio(root.transform, "Scan Loop", true);
            var done = AddAudio(root.transform, "Done Sound", false);

            var bar = new GameObject("Progress Bar").transform;
            bar.SetParent(root.transform, false);
            bar.localPosition = new Vector3(0f, 0.05f, 0f);
            var back = Visual(PrimitiveType.Cube, "Back", bar, Vector3.zero, new Vector3(0.084f, 0.012f, 0.004f), Vector3.zero,
                              MakeMaterial("UVBarBack", new Color(0.1f, 0.1f, 0.1f)));
            var fill = Visual(PrimitiveType.Cube, "Fill", bar, new Vector3(0f, 0f, -0.003f), new Vector3(0.08f, 0.008f, 0.004f), Vector3.zero,
                              MakeMaterial("UVBarFill", new Color(0.2f, 1f, 0.4f)));
            bar.gameObject.SetActive(false);

            var scanner = root.AddComponent<UVScanner>();
            Set(scanner, "m_ScanOrigin", p => p.objectReferenceValue = origin);
            Set(scanner, "m_UVLight", p => p.objectReferenceValue = light);
            Set(scanner, "m_ScanLoop", p => p.objectReferenceValue = loop);
            Set(scanner, "m_DoneSound", p => p.objectReferenceValue = done);
            Set(scanner, "m_ProgressRoot", p => p.objectReferenceValue = bar.gameObject);
            Set(scanner, "m_ProgressFill", p => p.objectReferenceValue = fill.transform);
            Set(scanner, "m_ObstructionMask", p => p.intValue = LayerMask.GetMask("Environment", "Evidence", "UVTarget"));

            Save(root, ToolsDir + "/UV Tool.prefab");
        }

        static void BuildUVTarget()
        {
            var root = new GameObject("UV Target E04");
            SetLayer(root, "UVTarget");
            var col = root.AddComponent<BoxCollider>();   // NON-trigger on purpose: the scanner ray must hit it
            col.size = new Vector3(0.12f, 0.06f, 0.02f);

            // Quad faces -Z, so the player (on the -Z side of the cabinet) sees it. Offset avoids z-fighting.
            var markMat = MakeMaterial("UVMark", new Color(0.6f, 0.4f, 1f), "Universal Render Pipeline/Unlit");
            var mark = Visual(PrimitiveType.Quad, "Mark", root.transform, new Vector3(0f, 0f, -0.012f), new Vector3(0.1f, 0.05f, 1f), Vector3.zero, markMat);

            var target = root.AddComponent<UVTarget>();
            Set(target, "m_EvidenceId", p => p.stringValue = "E04");
            Set(target, "m_Mark", p => p.objectReferenceValue = mark.GetComponent<Renderer>());
            Set(target, "m_AimPoint", p => p.objectReferenceValue = root.transform);

            Save(root, ToolsDir + "/UV Target E04.prefab");
        }

        // ---------- helpers ----------

        static GameObject Visual(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Vector3 euler, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>()); // visuals must not block rays or physics
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static AudioSource AddAudio(Transform parent, string name, bool loop)
        {
            var go = new GameObject(name, typeof(AudioSource));
            go.transform.SetParent(parent, false);
            var a = go.GetComponent<AudioSource>();
            a.playOnAwake = false;
            a.loop = loop;
            a.spatialBlend = 1f;
            a.maxDistance = 6f;
            return a;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void Set(Object target, string property, System.Action<SerializedProperty> apply)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(property);
            if (p == null)
            {
                Debug.LogError($"[EvidenceUVBuilder] Property '{property}' not found on {target.GetType().Name}");
                return;
            }
            apply(p);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetLayer(GameObject go, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogWarning($"[EvidenceUVBuilder] Layer '{layerName}' missing. Run CSI VR > Build Main Scene first (Member 1).");
                return;
            }
            go.layer = layer;
        }

        static void Save(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static Transform FindParent(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.transform : null;
        }

        static void Place(string prefabPath, Transform parent, Vector3 position, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("[EvidenceUVBuilder] Missing prefab " + prefabPath + " - run 'Create Prefabs' first.");
                return;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent != null) go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            Undo.RegisterCreatedObjectUndo(go, "Place " + prefab.name);
        }

        static Material MakeMaterial(string name, Color color, string shader = "Universal Render Pipeline/Lit")
        {
            var path = $"{Root}/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            mat = new Material(Shader.Find(shader) ?? Shader.Find("Standard")) { color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
