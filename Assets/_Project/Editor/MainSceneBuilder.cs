using System.IO;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.UI;
using CSIVR.Input;

namespace CSIVR.EditorTools
{
    /// <summary>
    /// Member 1: builds Assets/_Project/Scenes/Main.unity - XR rig, desktop rig, mode bootstrap, locomotion
    /// defaults, teleport anchors, room shell and the integration parents other members drop prefabs into.
    /// Re-running rebuilds the scene from scratch.
    /// </summary>
    public static class MainSceneBuilder
    {
        const string ProjectRoot = "Assets/_Project";
        const string ScenePath = ProjectRoot + "/Scenes/Main.unity";
        const string StarterAssets = "Assets/Samples/XR Interaction Toolkit/3.6.1/Starter Assets";

        const string EnvironmentLayer = "Environment";
        const string EvidenceLayer = "Evidence";
        const string UVTargetLayer = "UVTarget";

        const float SnapTurnDegrees = 30f;

        [MenuItem("CSI VR/Build Main Scene")]
        public static void Build()
        {
            EnsureFolders();
            EnsureLayers();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var floorMat = MakeMaterial("RoomFloor", new Color(0.32f, 0.33f, 0.35f));
            var wallMat = MakeMaterial("RoomWall", new Color(0.78f, 0.78f, 0.74f));
            var propMat = MakeMaterial("PlaceholderProp", new Color(0.45f, 0.36f, 0.27f));
            var boardMat = MakeMaterial("PlaceholderBoard", new Color(0.1f, 0.2f, 0.3f));
            var rayMat = MakeMaterial("DesktopRay", new Color(0.35f, 0.8f, 1f));

            BuildLighting();
            var systems = new GameObject("Systems").transform;
            var rigs = new GameObject("Rigs").transform;
            var environment = new GameObject("Environment").transform;
            var anchors = new GameObject("Teleport Anchors").transform;
            new GameObject("Evidence");
            new GameObject("Tools");
            new GameObject("UI");

            BuildRoom(environment, floorMat, wallMat, propMat, boardMat);
            BuildAnchors(anchors);

            var interactionManager = new GameObject("XR Interaction Manager", typeof(XRInteractionManager));
            interactionManager.transform.SetParent(systems);
            BuildEventSystem(systems);

            var xrRig = BuildXRRig(rigs);
            var desktopRig = BuildDesktopRig(rigs, rayMat);
            BuildModeBootstrap(systems, xrRig, desktopRig);

            ConfigureXRManagement();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[MainSceneBuilder] Built {ScenePath}");
        }

        // ---------- rigs ----------

        static GameObject BuildXRRig(Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{StarterAssets}/Prefabs/XR Origin (XR Rig).prefab");
            if (prefab == null)
                throw new FileNotFoundException("Starter Assets XR Origin prefab not found. Import XRI > Samples > Starter Assets.");

            var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rig.name = "XR Origin (XR Rig)";
            rig.transform.SetParent(parent);
            rig.transform.SetPositionAndRotation(StartPosition, StartRotation);

            var origin = rig.GetComponent<XROrigin>();
            if (origin != null)
                origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            ApplyComfortLocomotion(rig);
            return rig;
        }

        // Teleport + snap turn only. Continuous move/turn and grab-move stay disabled for comfort.
        static void ApplyComfortLocomotion(GameObject rig)
        {
            foreach (var mb in rig.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                var typeName = mb.GetType().Name;
                if (typeName.Contains("ContinuousMove") || typeName.Contains("ContinuousTurn") ||
                    typeName.Contains("DynamicMove") || typeName.Contains("GrabMove"))
                {
                    mb.enabled = false;
                }
            }

            foreach (var snap in rig.GetComponentsInChildren<SnapTurnProvider>(true))
            {
                snap.enabled = true;
                var so = new SerializedObject(snap);
                var amount = so.FindProperty("m_TurnAmount");
                if (amount != null) amount.floatValue = SnapTurnDegrees;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static GameObject BuildDesktopRig(Transform parent, Material rayMat)
        {
            var root = new GameObject("Desktop Rig", typeof(CharacterController), typeof(DesktopRig));
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(StartPosition, StartRotation);

            var cc = root.GetComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.25f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.15f;
            cc.skinWidth = 0.02f;

            var camGO = new GameObject("Desktop Camera", typeof(Camera), typeof(AudioListener));
            camGO.tag = "MainCamera";
            camGO.transform.SetParent(root.transform, false);
            camGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var cam = camGO.GetComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;

            var hand = new GameObject("Desktop Hand", typeof(LineRenderer), typeof(XRRayInteractor), typeof(XRInteractorLineVisual));
            hand.transform.SetParent(camGO.transform, false);
            var line = hand.GetComponent<LineRenderer>();
            line.sharedMaterial = rayMat;
            line.widthMultiplier = 0.004f;
            line.numCapVertices = 4;
            var ray = hand.GetComponent<XRRayInteractor>();
            ray.maxRaycastDistance = 4f;
            ray.enableUIInteraction = true;

            var attach = new GameObject("Hand Attach").transform;
            attach.SetParent(hand.transform, false);
            attach.localPosition = new Vector3(0f, -0.15f, 0.8f);
            ray.attachTransform = attach;

            var rigSO = new SerializedObject(root.GetComponent<DesktopRig>());
            rigSO.FindProperty("m_Camera").objectReferenceValue = cam;
            rigSO.FindProperty("m_Hand").objectReferenceValue = ray;
            rigSO.FindProperty("m_HandAttach").objectReferenceValue = attach;
            rigSO.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        static void BuildModeBootstrap(Transform parent, GameObject xrRig, GameObject desktopRig)
        {
            var go = new GameObject("Mode Bootstrap", typeof(ModeBootstrap));
            go.transform.SetParent(parent);
            var so = new SerializedObject(go.GetComponent<ModeBootstrap>());
            so.FindProperty("m_XRRig").objectReferenceValue = xrRig;
            so.FindProperty("m_DesktopRig").objectReferenceValue = desktopRig;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildEventSystem(Transform parent)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
            go.transform.SetParent(parent);

            // The sample preset wires the XR UI input actions; applying it avoids a module with no input.
            var preset = AssetDatabase.LoadAssetAtPath<Preset>($"{StarterAssets}/Presets/XRI Default XR UI Input Module.preset");
            var module = go.GetComponent<XRUIInputModule>();
            if (preset != null)
                preset.ApplyTo(module);
            else
                Debug.LogWarning("[MainSceneBuilder] XR UI Input Module preset not found; assign UI input actions manually.");
        }

        // ---------- environment ----------

        static readonly Vector3 StartPosition = new Vector3(-1.6f, 0f, -1.0f);
        static readonly Quaternion StartRotation = Quaternion.Euler(0f, 270f, 0f);

        static void BuildLighting()
        {
            var lightGO = new GameObject("Directional Light", typeof(Light));
            var l = lightGO.GetComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.0f;
            l.shadows = LightShadows.Soft;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // 8 m x 7 m room, 3 m ceiling, 1 unit = 1 m. Member 2 replaces the placeholders with real visuals.
        static void BuildRoom(Transform parent, Material floorMat, Material wallMat, Material propMat, Material boardMat)
        {
            int env = LayerMask.NameToLayer(EnvironmentLayer);

            Box("Floor", parent, new Vector3(0f, -0.05f, 0f), new Vector3(8f, 0.1f, 7f), floorMat, env);
            Box("Wall North", parent, new Vector3(0f, 1.5f, 3.55f), new Vector3(8.2f, 3f, 0.1f), wallMat, env);
            Box("Wall South", parent, new Vector3(0f, 1.5f, -3.55f), new Vector3(8.2f, 3f, 0.1f), wallMat, env);
            Box("Wall East", parent, new Vector3(4.05f, 1.5f, 0f), new Vector3(0.1f, 3f, 7f), wallMat, env);
            Box("Wall West", parent, new Vector3(-4.05f, 1.5f, 0f), new Vector3(0.1f, 3f, 7f), wallMat, env);
            Box("Ceiling", parent, new Vector3(0f, 3.05f, 0f), new Vector3(8.2f, 0.1f, 7.2f), wallMat, env);

            var props = new GameObject("Placeholders (Member 2 replaces)").transform;
            props.SetParent(parent);
            Box("Desk", props, new Vector3(2.6f, 0.375f, -2.0f), new Vector3(0.8f, 0.75f, 1.6f), propMat, env);
            Box("Shelf", props, new Vector3(-2.2f, 1.0f, 3.25f), new Vector3(2.0f, 2.0f, 0.4f), propMat, env);
            Box("Cabinet", props, new Vector3(2.2f, 0.9f, 3.2f), new Vector3(1.2f, 1.8f, 0.6f), propMat, env);
            Box("Evidence Station Table", props, new Vector3(3.3f, 0.45f, 0.0f), new Vector3(0.7f, 0.9f, 1.0f), propMat, env);
            Box("Briefing Board", props, new Vector3(-3.95f, 1.7f, -1.0f), new Vector3(0.05f, 1.2f, 2.4f), boardMat, env);
        }

        static void BuildAnchors(Transform parent)
        {
            // Anchors face their task. Yaw is the direction the player looks after teleporting.
            Anchor("Anchor Briefing", parent, new Vector3(-1.6f, 0f, -1.0f), 270f);
            Anchor("Anchor Desk", parent, new Vector3(1.2f, 0f, -2.0f), 90f);
            Anchor("Anchor Shelf", parent, new Vector3(-2.2f, 0f, 1.6f), 0f);
            Anchor("Anchor Cabinet", parent, new Vector3(2.2f, 0f, 1.6f), 0f);
            Anchor("Anchor Evidence Station", parent, new Vector3(1.9f, 0f, 0.0f), 90f);
        }

        static void Anchor(string name, Transform parent, Vector3 position, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{StarterAssets}/DemoAssets/Prefabs/Teleport/Teleport Anchor.prefab");
            if (prefab == null)
            {
                Debug.LogWarning("[MainSceneBuilder] Teleport Anchor prefab not found.");
                return;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        }

        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, int layer)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (layer >= 0) go.layer = layer;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
            return go;
        }

        // ---------- project plumbing ----------

        static void ConfigureXRManagement()
        {
            // Headset-free Windows export: XR is initialised by ModeBootstrap only in Headset mode.
            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (settings != null)
            {
                settings.InitManagerOnStart = false;
                EditorUtility.SetDirty(settings);
            }
            else
            {
                Debug.LogWarning("[MainSceneBuilder] No Standalone XR settings found. Turn off 'Initialize XR on Startup' in XR Plug-in Management.");
            }
        }

        static void EnsureFolders()
        {
            foreach (var sub in new[] { "Scenes", "Scenes/Workbenches", "Materials", "Prefabs", "Scripts", "Audio", "Data", "Input" })
            {
                var path = $"{ProjectRoot}/{sub}";
                if (!AssetDatabase.IsValidFolder(path))
                {
                    var parent = Path.GetDirectoryName(path).Replace('\\', '/');
                    AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
                }
            }
        }

        static void EnsureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            foreach (var name in new[] { EnvironmentLayer, EvidenceLayer, UVTargetLayer })
            {
                bool exists = false;
                for (int i = 0; i < layers.arraySize; i++)
                    if (layers.GetArrayElementAtIndex(i).stringValue == name) { exists = true; break; }
                if (exists) continue;

                for (int i = 8; i < layers.arraySize; i++)
                {
                    var element = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(element.stringValue))
                    {
                        element.stringValue = name;
                        break;
                    }
                }
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        static Material MakeMaterial(string name, Color color)
        {
            var path = $"{ProjectRoot}/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader) { color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
