using System.Collections.Generic;
using CSIVR.Core;
using CSIVR.Evidence;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Object = UnityEngine.Object;

namespace CSIVR.EditorTools
{
    /// <summary>
    /// Geometry and dressing for both rooms. 1 unit = 1 m. Room 1 (research office) is z in [-3.5, 3.5];
    /// Room 2 (security office) is z in [-10.6, -3.6], joined by a doorway at x in [0, 1] in the wall at z = -3.55.
    /// Yaw values are the direction a viewer looks (0 = +Z, 90 = +X, 180 = -Z, 270 = -X).
    /// </summary>
    public static partial class MainSceneBuilder
    {
        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent);
            return t;
        }

        // ---------- shell ----------

        static void BuildShell(Transform env)
        {
            var shell = Group(env, "Shell");
            Solid("Floor Room 1", shell, new Vector3(0f, -0.05f, 0f), new Vector3(8f, 0.1f, 7f), mFloor);
            Solid("Floor Room 2", shell, new Vector3(0f, -0.05f, -7.1f), new Vector3(8f, 0.1f, 7.3f), mFloor);

            Solid("Wall North", shell, new Vector3(0f, 1.5f, 3.55f), new Vector3(8.2f, 3f, 0.1f), mWall);
            Solid("Wall South", shell, new Vector3(0f, 1.5f, -10.65f), new Vector3(8.2f, 3f, 0.1f), mWall);
            Solid("Wall East", shell, new Vector3(4.05f, 1.5f, -3.55f), new Vector3(0.1f, 3f, 14.3f), mWall);
            Solid("Wall West", shell, new Vector3(-4.05f, 1.5f, -3.55f), new Vector3(0.1f, 3f, 14.3f), mWall);

            // Dividing wall with a doorway at x in [0, 1], 2.1 m high.
            Solid("Divider Left", shell, new Vector3(-2.05f, 1.5f, -3.55f), new Vector3(4.1f, 3f, 0.1f), mWall);
            Solid("Divider Right", shell, new Vector3(2.55f, 1.5f, -3.55f), new Vector3(3.1f, 3f, 0.1f), mWall);
            Solid("Divider Lintel", shell, new Vector3(0.5f, 2.55f, -3.55f), new Vector3(1.0f, 0.9f, 0.1f), mWall);

            var ceiling = Solid("Ceiling", shell, new Vector3(0f, 3.05f, -3.55f), new Vector3(8.2f, 0.1f, 14.4f), mCeiling);
            ceiling.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------- door ----------

        static void BuildDoor(Transform env, List<GameObject> room2Anchors)
        {
            var root = new GameObject("Room Door", typeof(RoomDoor));
            root.transform.SetParent(env);
            root.transform.position = new Vector3(0.5f, 0f, -3.55f);

            Solid("Door Jamb Left", root.transform, new Vector3(0f, 1.05f, -3.55f), new Vector3(0.06f, 2.1f, 0.16f), mMetal);
            Solid("Door Jamb Right", root.transform, new Vector3(1.0f, 1.05f, -3.55f), new Vector3(0.06f, 2.1f, 0.16f), mMetal);

            var hinge = new GameObject("Door Hinge").transform;
            hinge.SetParent(root.transform);
            hinge.position = new Vector3(0.97f, 0f, -3.55f);

            var slab = Solid("Door Slab", hinge, new Vector3(0.5f, 1.05f, -3.55f), new Vector3(0.94f, 2.1f, 0.05f), mDarkWood);
            Deco("Door Window", slab.transform, new Vector3(0.5f, 1.55f, -3.55f), new Vector3(0.4f, 0.7f, 0.06f), mGlassDoor);
            Deco("Door Handle", slab.transform, new Vector3(0.12f, 1.0f, -3.55f), new Vector3(0.12f, 0.03f, 0.11f), mMetal);

            // Card reader on the room 1 side of the wall: red while locked, green once open.
            Deco("Reader Plate", root.transform, new Vector3(1.25f, 1.25f, -3.485f), new Vector3(0.16f, 0.26f, 0.02f), mDarkMetal);
            var reader = Deco("Card Reader", root.transform, new Vector3(1.25f, 1.28f, -3.47f), new Vector3(0.1f, 0.14f, 0.03f), mReaderOff);
            Deco("Card Slot", root.transform, new Vector3(1.25f, 1.17f, -3.468f), new Vector3(0.09f, 0.012f, 0.03f), mDarkMetal);

            var sign = Text3D(root.transform, "Door Sign", new Vector3(0.5f, 2.5f, -3.44f), 180f, "LOCKED\nSubmit your finding", 1.0f,
                new Color(1f, 0.45f, 0.4f), new Vector2(1.3f, 0.4f));

            // Crime scene tape across the doorway; removed when the door unlocks.
            var tapes = new List<GameObject>
            {
                Tape(root.transform, new Vector3(0.5f, 1.55f, -3.44f), 9f),
                Tape(root.transform, new Vector3(0.5f, 1.2f, -3.43f), -9f),
            };

            var audio = AudioFx.AddSpatialSource(root, 1f, 8f);

            var so = new SerializedObject(root.GetComponent<RoomDoor>());
            so.FindProperty("m_UnlockAfterCase").intValue = 0;
            so.FindProperty("m_Hinge").objectReferenceValue = hinge;
            so.FindProperty("m_Reader").objectReferenceValue = reader.GetComponent<Renderer>();
            so.FindProperty("m_LockedMaterial").objectReferenceValue = mReaderOff;
            so.FindProperty("m_UnlockedMaterial").objectReferenceValue = mReaderOn;
            so.FindProperty("m_Sign").objectReferenceValue = sign;
            so.FindProperty("m_Audio").objectReferenceValue = audio;
            SetObjectArray(so.FindProperty("m_HideOnUnlock"), tapes);
            SetObjectArray(so.FindProperty("m_ActivateOnUnlock"), room2Anchors);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetObjectArray(SerializedProperty array, List<GameObject> items)
        {
            array.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        // Tape strip readable from room 1 (viewer looks -Z, yaw 180).
        static GameObject Tape(Transform parent, Vector3 pos, float roll)
        {
            var root = new GameObject("Crime Tape").transform;
            root.SetParent(parent);
            root.SetPositionAndRotation(pos, Quaternion.Euler(0f, 180f, roll));

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Strip";
            quad.transform.SetParent(root, false);
            quad.transform.localScale = new Vector3(1.7f, 0.11f, 1f);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.GetComponent<Renderer>().sharedMaterial = mTape;

            var textGo = new GameObject("Text", typeof(TextMeshPro));
            textGo.transform.SetParent(root, false);
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.004f);
            var tmp = textGo.GetComponent<TextMeshPro>();
            tmp.text = "CRIME SCENE   DO NOT CROSS";
            tmp.fontSize = 0.55f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.05f, 0.05f, 0.05f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = new Vector2(1.5f, 0.06f);
            return root.gameObject;
        }

        // ---------- room 1: research office ----------

        static GameObject BuildRoom1(Transform room, Transform anchors, Transform evidence, Transform tools, Transform ui)
        {
            var f = Group(room, "Furniture");
            var d = Group(room, "Decor");

            // Teleport anchors face their task.
            Anchor("Anchor Briefing", anchors, new Vector3(-1.6f, 0f, -1.0f), 270f);
            Anchor("Anchor Desk", anchors, new Vector3(1.2f, 0f, -2.0f), 90f);
            Anchor("Anchor Shelf", anchors, new Vector3(-2.2f, 0f, 1.9f), 0f);
            Anchor("Anchor Cabinet", anchors, new Vector3(2.2f, 0f, 1.9f), 0f);
            Anchor("Anchor Evidence Station", anchors, new Vector3(1.9f, 0f, 0.0f), 90f);
            Anchor("Anchor Door", anchors, new Vector3(0.5f, 0f, -2.5f), 180f);

            // Wall panelling.
            Wainscot(d, "Wainscot North", new Vector3(0f, 0.5f, 3.485f), 8f, true, -1f);
            Wainscot(d, "Wainscot West", new Vector3(-3.985f, 0.5f, 0f), 7f, false, 1f);
            Wainscot(d, "Wainscot East", new Vector3(3.985f, 0.5f, 0f), 7f, false, -1f);
            Wainscot(d, "Wainscot South Left", new Vector3(-2.0f, 0.5f, -3.485f), 4f, true, 1f);
            Wainscot(d, "Wainscot South Right", new Vector3(2.5f, 0.5f, -3.485f), 3f, true, 1f);

            Deco("Rug", d, new Vector3(0f, 0.006f, 0f), new Vector3(3f, 0.012f, 2f), mRug);
            Deco("Rug Inner", d, new Vector3(0f, 0.012f, 0f), new Vector3(2.6f, 0.004f, 1.6f), mWall);
            CeilingLamps(d, new[] { -2f, 2f }, new[] { -1.5f, 1.5f });

            // Desk, chair and desk dressing. E02 lies on the desktop.
            Solid("Desk Top", f, new Vector3(2.6f, 0.725f, -2.0f), new Vector3(0.8f, 0.05f, 1.6f), mWood);
            Solid("Desk Panel Left", f, new Vector3(2.6f, 0.35f, -2.75f), new Vector3(0.7f, 0.7f, 0.05f), mWood);
            Solid("Desk Panel Right", f, new Vector3(2.6f, 0.35f, -1.25f), new Vector3(0.7f, 0.7f, 0.05f), mWood);
            Solid("Desk Back", f, new Vector3(3.0f, 0.4f, -2.0f), new Vector3(0.03f, 0.6f, 1.5f), mWood);
            Solid("Chair Seat", f, new Vector3(1.9f, 0.45f, -2.0f), new Vector3(0.48f, 0.07f, 0.48f), mChairFabric);
            Solid("Chair Back", f, new Vector3(1.66f, 0.76f, -2.0f), new Vector3(0.07f, 0.52f, 0.46f), mChairFabric);
            Cyl("Chair Post", f, new Vector3(1.9f, 0.22f, -2.0f), new Vector3(0.06f, 0.2f, 0.06f), mDarkMetal);
            Cyl("Chair Base", f, new Vector3(1.9f, 0.04f, -2.0f), new Vector3(0.52f, 0.025f, 0.52f), mDarkMetal);
            Monitor(f, new Vector3(2.85f, 0.75f, -2.45f), 90f, mScreen, "CASE FILES\nOFFICE 01", 0.7f);
            Deco("Keyboard", f, new Vector3(2.5f, 0.757f, -2.45f), new Vector3(0.14f, 0.014f, 0.40f), mDarkMetal);
            Cyl("Mug", f, new Vector3(2.45f, 0.80f, -1.5f), new Vector3(0.08f, 0.05f, 0.08f), mWhite);
            DeskLamp(f, new Vector3(2.85f, 0.75f, -1.55f), 90f);

            // Side table: E01 lies on it.
            Solid("Side Table", f, new Vector3(2.6f, 0.3f, -0.8f), new Vector3(0.5f, 0.6f, 0.5f), mWood);

            // Open bookcase on the north wall: E03 lies on the middle board (top at 1.02 m).
            Solid("Shelf Side Left", f, new Vector3(-3.18f, 1.0f, 3.25f), new Vector3(0.04f, 2.0f, 0.4f), mDarkWood);
            Solid("Shelf Side Right", f, new Vector3(-1.22f, 1.0f, 3.25f), new Vector3(0.04f, 2.0f, 0.4f), mDarkWood);
            Solid("Shelf Back", f, new Vector3(-2.2f, 1.0f, 3.435f), new Vector3(2.0f, 2.0f, 0.03f), mDarkWood);
            foreach (var y in new[] { 0.5f, 1.0f, 1.5f, 1.98f })
                Solid($"Shelf Board {y:0.00}", f, new Vector3(-2.2f, y, 3.25f), new Vector3(1.96f, 0.04f, 0.38f), mWood);
            Books(f, new Vector3(-2.9f, 0.52f, 3.25f), 7);
            Books(f, new Vector3(-1.9f, 1.52f, 3.25f), 5);
            Cardboard(f, new Vector3(-2.6f, 1.67f, 3.25f), new Vector3(0.38f, 0.30f, 0.28f), 6f);
            Cardboard(f, new Vector3(-2.7f, 2.15f, 3.25f), new Vector3(0.42f, 0.32f, 0.30f), -8f);

            // Cabinet on the north wall; the scanner target sits on its right handle.
            Solid("Cabinet", f, new Vector3(2.2f, 0.9f, 3.2f), new Vector3(1.2f, 1.8f, 0.6f), mMetal);
            Deco("Cabinet Door Gap", f, new Vector3(2.2f, 0.9f, 2.896f), new Vector3(0.012f, 1.7f, 0.01f), mDarkMetal);
            Deco("Cabinet Handle Left", f, new Vector3(2.1f, 1.0f, 2.87f), new Vector3(0.03f, 0.30f, 0.04f), mDarkMetal);
            Deco("Cabinet Handle Right", f, new Vector3(2.3f, 1.0f, 2.87f), new Vector3(0.03f, 0.30f, 0.04f), mDarkMetal);
            Deco("Cabinet Lock", f, new Vector3(2.2f, 1.0f, 2.897f), new Vector3(0.05f, 0.08f, 0.01f), mLampGlow);
            BuildCabinetTarget(room, "E04", new Vector3(2.3f, 1.0f, 2.885f), 0f, new Vector2(0.14f, 0.32f));

            Cardboard(f, new Vector3(-3.5f, 0.25f, 2.7f), new Vector3(0.5f, 0.5f, 0.5f), 12f);
            Cardboard(f, new Vector3(-3.45f, 0.7f, 2.72f), new Vector3(0.4f, 0.4f, 0.4f), -6f);
            Cardboard(f, new Vector3(-3.4f, 0.2f, 1.9f), new Vector3(0.45f, 0.4f, 0.35f), 20f);
            Plant(d, new Vector3(3.55f, 0f, 3.05f), 1.3f);
            Plant(d, new Vector3(-3.5f, 0f, -3.0f), 1.1f);
            Plant(d, new Vector3(3.55f, 0f, -3.1f), 1.0f);

            // Wall dressing: clock, posters, window with a city view, extinguisher.
            Cyl("Wall Clock", d, new Vector3(0f, 2.35f, 3.47f), new Vector3(0.42f, 0.015f, 0.42f), mWhite, false, new Vector3(90f, 0f, 0f));
            Deco("Clock Hand Hour", d, new Vector3(0f, 2.38f, 3.455f), new Vector3(0.02f, 0.10f, 0.006f), mDarkMetal);
            Deco("Clock Hand Minute", d, new Vector3(0.05f, 2.35f, 3.455f), new Vector3(0.14f, 0.015f, 0.006f), mDarkMetal);
            Poster(d, new Vector3(-0.2f, 1.7f, 3.47f), 0f, new Vector2(0.7f, 0.9f), "SAFETY\nFIRST", mPosterTeal, 1.3f);
            Poster(d, new Vector3(-3.965f, 1.7f, 1.9f), 270f, new Vector2(0.8f, 1.0f), "LAB\nRULES", mPosterAmber, 1.3f);
            Cyl("Extinguisher", d, new Vector3(-0.45f, 0.3f, -3.35f), new Vector3(0.13f, 0.28f, 0.13f), mExtinguisher);

            BuildWindow(d, new Vector3(3.98f, 1.6f, 1.9f));

            // Briefing board: backing, marker ledge. The canvas hangs 3 cm in front of it.
            Solid("Briefing Board", d, new Vector3(-3.95f, 1.7f, -1.0f), new Vector3(0.05f, 1.45f, 2.6f), mBoard);
            Deco("Marker Ledge", d, new Vector3(-3.9f, 0.95f, -1.0f), new Vector3(0.1f, 0.03f, 2.2f), mMetal);

            // Tool table with the practice cube and the UV scanner.
            Solid("Practice Table", f, new Vector3(-2.6f, 0.45f, -2.5f), new Vector3(0.9f, 0.9f, 0.6f), mDarkMetal);
            Deco("Practice Table Top", f, new Vector3(-2.6f, 0.91f, -2.5f), new Vector3(0.92f, 0.02f, 0.62f), mTray);
            Text3D(f, "Practice Label", new Vector3(-2.6f, 0.923f, -2.72f), 180f, "TRAINING AREA", 0.7f, new Color(0.6f, 0.9f, 0.9f),
                new Vector2(0.5f, 0.1f), FontStyles.Bold, TextAlignmentOptions.Center, true);

            var practiceCube = BuildGrabbable("Practice Cube", new Vector3(0.1f, 0.1f, 0.1f), mPractice);
            practiceCube.transform.SetParent(tools);
            practiceCube.transform.position = new Vector3(-2.4f, 0.97f, -2.5f);
            BuildScanner(tools, new Vector3(-2.75f, 0.96f, -2.5f), 90f);

            // Evidence: station tray plus the three movable clues.
            BuildStation(f, evidence, 0, new Vector3(3.3f, 0f, 0f), 90f, new[] { "ACCESS CARD", "ACCESS LOG", "PACKING LABEL" });
            var card = MakeEvidencePrefab(new EvidenceSpec
            {
                Id = "E01", PrefabName = "Evidence_E01_AccessCard", Size = new Vector3(0.12f, 0.012f, 0.08f), Mat = mCard,
                Text = "B-17", FontSize = 0.7f, TextColor = Color.white, Stripe = true,
            });
            var log = MakeEvidencePrefab(new EvidenceSpec
            {
                Id = "E02", PrefabName = "Evidence_E02_AccessLog", Size = new Vector3(0.21f, 0.012f, 0.30f), Mat = mPaper,
                Text = "ACCESS LOG\nB-17  OK\n19:42", FontSize = 0.7f, TextColor = new Color(0.1f, 0.1f, 0.15f),
            });
            var label = MakeEvidencePrefab(new EvidenceSpec
            {
                Id = "E03", PrefabName = "Evidence_E03_PackingLabel", Size = new Vector3(0.14f, 0.012f, 0.07f), Mat = mLabel,
                Text = "PROTOTYPE\nCONTAINER", FontSize = 0.5f, TextColor = new Color(0.25f, 0.12f, 0.05f),
            });
            Place(card, evidence, new Vector3(2.6f, 0.606f, -0.8f), 20f);   // beside the desk
            Place(log, evidence, new Vector3(2.6f, 0.756f, -2.0f), -10f);   // on the desktop
            Place(label, evidence, new Vector3(-2.2f, 1.026f, 3.1f), 160f); // on the shelf

            BuildCaseUi(ui, 0, new Vector3(-3.90f, 1.7f, -1.0f), 270f, new Vector3(3.3f, 1.62f, 0f), 90f);
            return practiceCube;
        }

        static void Books(Transform parent, Vector3 start, int count)
        {
            var colors = new[]
            {
                new Color(0.75f, 0.2f, 0.2f), new Color(0.2f, 0.45f, 0.7f), new Color(0.9f, 0.7f, 0.2f),
                new Color(0.25f, 0.55f, 0.35f), new Color(0.55f, 0.3f, 0.6f),
            };
            float x = start.x;
            for (int i = 0; i < count; i++)
            {
                float w = 0.035f + 0.012f * (i % 3);
                float h = 0.22f + 0.03f * (i % 4);
                var mat = MakeMaterial("Book" + (i % colors.Length), colors[i % colors.Length]);
                Deco("Book", parent, new Vector3(x + w * 0.5f, start.y + h * 0.5f, start.z), new Vector3(w, h, 0.24f), mat);
                x += w + 0.004f;
            }
        }

        static void BuildWindow(Transform parent, Vector3 center)
        {
            Deco("Window Glass", parent, center, new Vector3(0.03f, 1.1f, 1.5f), mWindow);
            Deco("Window Frame Top", parent, center + new Vector3(-0.01f, 0.58f, 0f), new Vector3(0.05f, 0.06f, 1.62f), mTrim);
            Deco("Window Frame Bottom", parent, center + new Vector3(-0.01f, -0.58f, 0f), new Vector3(0.05f, 0.06f, 1.62f), mTrim);
            Deco("Window Frame Front", parent, center + new Vector3(-0.01f, 0f, -0.78f), new Vector3(0.05f, 1.2f, 0.06f), mTrim);
            Deco("Window Frame Back", parent, center + new Vector3(-0.01f, 0f, 0.78f), new Vector3(0.05f, 1.2f, 0.06f), mTrim);
            Deco("Window Mullion", parent, center + new Vector3(-0.01f, 0f, 0f), new Vector3(0.05f, 1.1f, 0.04f), mTrim);
            Deco("Window Sill", parent, center + new Vector3(-0.06f, -0.63f, 0f), new Vector3(0.16f, 0.04f, 1.7f), mTrim);
        }

        // ---------- room 2: security office ----------

        static List<GameObject> BuildRoom2(Transform room, Transform anchors, Transform evidence, Transform tools, Transform ui)
        {
            var f = Group(room, "Furniture");
            var d = Group(room, "Decor");

            // Anchors stay inactive until the door opens (RoomDoor activates them).
            var a = new List<GameObject>
            {
                Anchor("Anchor R2 Entry", anchors, new Vector3(0.5f, 0f, -5.0f), 180f),
                Anchor("Anchor R2 Briefing", anchors, new Vector3(-1.6f, 0f, -5.0f), 270f),
                Anchor("Anchor R2 Console", anchors, new Vector3(-2.4f, 0f, -8.6f), 180f),
                Anchor("Anchor R2 Side Table", anchors, new Vector3(0.8f, 0f, -8.6f), 180f),
                Anchor("Anchor R2 Shelf", anchors, new Vector3(1.9f, 0f, -7.4f), 90f),
                Anchor("Anchor R2 Power Panel", anchors, new Vector3(2.8f, 0f, -9.3f), 180f),
                Anchor("Anchor R2 Evidence Station", anchors, new Vector3(1.9f, 0f, -5.4f), 90f),
            };

            Wainscot(d, "Wainscot West", new Vector3(-3.985f, 0.5f, -7.1f), 7f, false, 1f);
            Wainscot(d, "Wainscot East", new Vector3(3.985f, 0.5f, -7.1f), 7f, false, -1f);
            Wainscot(d, "Wainscot South", new Vector3(0f, 0.5f, -10.585f), 8f, true, 1f);
            Wainscot(d, "Wainscot North Left", new Vector3(-2.0f, 0.5f, -3.615f), 4f, true, -1f);
            Wainscot(d, "Wainscot North Right", new Vector3(2.5f, 0.5f, -3.615f), 3f, true, -1f);

            Deco("Rug", d, new Vector3(0f, 0.006f, -7.1f), new Vector3(3f, 0.012f, 2.4f), mRug2);
            Deco("Rug Inner", d, new Vector3(0f, 0.012f, -7.1f), new Vector3(2.6f, 0.004f, 2.0f), mWall);
            CeilingLamps(d, new[] { -2f, 2f }, new[] { -5.2f, -8.8f });

            // Console desk against the south wall: E05 lies on it.
            Solid("Console Top", f, new Vector3(-2.4f, 0.725f, -10.1f), new Vector3(1.8f, 0.05f, 0.7f), mDarkWood);
            Solid("Console Panel Left", f, new Vector3(-3.275f, 0.35f, -10.1f), new Vector3(0.05f, 0.7f, 0.65f), mDarkWood);
            Solid("Console Panel Right", f, new Vector3(-1.525f, 0.35f, -10.1f), new Vector3(0.05f, 0.7f, 0.65f), mDarkWood);
            Solid("Console Back", f, new Vector3(-2.4f, 0.4f, -10.42f), new Vector3(1.7f, 0.6f, 0.03f), mDarkWood);
            Monitor(f, new Vector3(-3.0f, 0.75f, -10.2f), 180f, mScreen, "CAM 1\nOK", 0.8f);
            Monitor(f, new Vector3(-1.85f, 0.75f, -10.2f), 180f, mScreen, "CAM 2\nOK", 0.8f);
            Deco("Keyboard", f, new Vector3(-2.4f, 0.757f, -9.85f), new Vector3(0.40f, 0.014f, 0.14f), mDarkMetal);
            Cyl("Mug", f, new Vector3(-1.6f, 0.80f, -9.9f), new Vector3(0.08f, 0.05f, 0.08f), mWhite);
            Solid("Chair Seat", f, new Vector3(-2.4f, 0.45f, -9.2f), new Vector3(0.48f, 0.07f, 0.48f), mChairFabric);
            Solid("Chair Back", f, new Vector3(-2.4f, 0.76f, -8.97f), new Vector3(0.46f, 0.52f, 0.07f), mChairFabric);
            Cyl("Chair Post", f, new Vector3(-2.4f, 0.22f, -9.2f), new Vector3(0.06f, 0.2f, 0.06f), mDarkMetal);
            Cyl("Chair Base", f, new Vector3(-2.4f, 0.04f, -9.2f), new Vector3(0.52f, 0.025f, 0.52f), mDarkMetal);

            // Side table: E06 lies on it.
            Solid("Side Table", f, new Vector3(0.8f, 0.3f, -9.9f), new Vector3(0.5f, 0.6f, 0.5f), mDarkWood);

            // Open shelf on the east wall: E07 lies on the middle board.
            Solid("Shelf Side A", f, new Vector3(3.8f, 1.0f, -8.38f), new Vector3(0.4f, 2.0f, 0.04f), mDarkMetal);
            Solid("Shelf Side B", f, new Vector3(3.8f, 1.0f, -6.42f), new Vector3(0.4f, 2.0f, 0.04f), mDarkMetal);
            Solid("Shelf Back", f, new Vector3(3.985f, 1.0f, -7.4f), new Vector3(0.03f, 2.0f, 2.0f), mDarkMetal);
            foreach (var y in new[] { 0.5f, 1.0f, 1.5f, 1.98f })
                Solid($"Shelf Board {y:0.00}", f, new Vector3(3.8f, y, -7.4f), new Vector3(0.38f, 0.04f, 1.96f), mMetal);
            Cardboard(f, new Vector3(3.8f, 0.7f, -7.9f), new Vector3(0.30f, 0.30f, 0.34f), 90f);
            Cardboard(f, new Vector3(3.8f, 1.67f, -6.9f), new Vector3(0.30f, 0.30f, 0.38f), 90f);
            Cardboard(f, new Vector3(3.5f, 0.22f, -8.9f), new Vector3(0.5f, 0.44f, 0.5f), 15f);

            // Camera power panel on the south wall: three switches, the scanner target is on CAM-3.
            Solid("Power Panel", f, new Vector3(2.7f, 1.4f, -10.56f), new Vector3(0.7f, 0.8f, 0.08f), mMetal);
            for (int i = 0; i < 3; i++)
            {
                float x = 2.5f + 0.2f * i;
                Deco($"Switch Base {i + 1}", f, new Vector3(x, 1.45f, -10.5f), new Vector3(0.1f, 0.14f, 0.05f), mDarkMetal);
                Deco($"Switch Lever {i + 1}", f, new Vector3(x, 1.47f, -10.47f), new Vector3(0.025f, 0.07f, 0.03f), i == 2 ? mWhite : mTrim);
                Text3D(f, $"Switch Label {i + 1}", new Vector3(x, 1.33f, -10.515f), 180f, $"CAM-{i + 1}", 0.55f, Color.white,
                    new Vector2(0.12f, 0.05f));
            }
            BuildCabinetTarget(room, "E08", new Vector3(2.9f, 1.45f, -10.49f), 180f, new Vector2(0.12f, 0.16f));

            // Server rack and wall camera dressing.
            Solid("Server Rack", f, new Vector3(3.45f, 1.0f, -9.8f), new Vector3(0.7f, 2.0f, 0.8f), mDarkMetal);
            for (int r = 0; r < 5; r++)
            {
                Deco("Rack Unit", d, new Vector3(3.09f, 0.55f + 0.28f * r, -9.8f), new Vector3(0.012f, 0.18f, 0.7f), mMetal);
                Deco("LED", d, new Vector3(3.085f, 0.58f + 0.28f * r, -9.6f), new Vector3(0.01f, 0.025f, 0.04f), r == 3 ? mLedRed : mLedGreen);
                Deco("LED", d, new Vector3(3.085f, 0.58f + 0.28f * r, -9.5f), new Vector3(0.01f, 0.025f, 0.04f), mLedGreen);
            }
            Deco("Camera 3 Body", d, new Vector3(3.8f, 2.55f, -4.3f), new Vector3(0.28f, 0.14f, 0.14f), mDarkMetal);
            Cyl("Camera 3 Lens", d, new Vector3(3.62f, 2.55f, -4.3f), new Vector3(0.1f, 0.03f, 0.1f), mScreenDark, false, new Vector3(0f, 0f, 90f));
            Deco("Camera 3 LED", d, new Vector3(3.72f, 2.63f, -4.3f), new Vector3(0.02f, 0.02f, 0.02f), mScreenDark);
            Text3D(d, "Camera 3 Tag", new Vector3(3.95f, 2.3f, -4.3f), 90f, "CAM 3", 0.8f, new Color(0.8f, 0.9f, 1f), new Vector2(0.3f, 0.1f));

            // Monitor wall on the west wall: camera 3 shows NO SIGNAL.
            Deco("Monitor Wall Panel", d, new Vector3(-3.975f, 1.8f, -8.6f), new Vector3(0.04f, 0.72f, 2.95f), mDarkMetal);
            Deco("Wall Screen 1", d, new Vector3(-3.95f, 1.8f, -7.7f), new Vector3(0.03f, 0.55f, 0.85f), mScreen);
            Deco("Wall Screen 2", d, new Vector3(-3.95f, 1.8f, -8.6f), new Vector3(0.03f, 0.55f, 0.85f), mScreen);
            Deco("Wall Screen 3", d, new Vector3(-3.95f, 1.8f, -9.5f), new Vector3(0.03f, 0.55f, 0.85f), mScreenRed);
            Text3D(d, "Screen Text 1", new Vector3(-3.93f, 1.8f, -7.7f), 270f, "CAM 1\nONLINE", 1.1f, Color.white, new Vector2(0.8f, 0.5f));
            Text3D(d, "Screen Text 2", new Vector3(-3.93f, 1.8f, -8.6f), 270f, "CAM 2\nONLINE", 1.1f, Color.white, new Vector2(0.8f, 0.5f));
            Text3D(d, "Screen Text 3", new Vector3(-3.93f, 1.8f, -9.5f), 270f, "CAM 3\nNO SIGNAL", 1.1f, Color.white, new Vector2(0.8f, 0.5f));

            Poster(d, new Vector3(-2.4f, 2.0f, -10.57f), 180f, new Vector2(1.4f, 0.4f), "SECURITY OFFICE", mPosterNavy, 1.6f);
            Poster(d, new Vector3(-0.5f, 1.7f, -10.57f), 180f, new Vector2(0.55f, 0.75f), "ACCESS\nRESTRICTED", mPosterAmber, 1.1f);
            Cyl("Extinguisher", d, new Vector3(-3.7f, 0.3f, -4.2f), new Vector3(0.13f, 0.28f, 0.13f), mExtinguisher);
            Plant(d, new Vector3(3.55f, 0f, -3.95f), 1.2f);
            Plant(d, new Vector3(-3.6f, 0f, -10.3f), 1.0f);
            Plant(d, new Vector3(-0.2f, 0f, -10.3f), 0.9f);

            // Briefing board.
            Solid("Briefing Board", d, new Vector3(-3.95f, 1.7f, -5.0f), new Vector3(0.05f, 1.45f, 2.6f), mBoard);
            Deco("Marker Ledge", d, new Vector3(-3.9f, 0.95f, -5.0f), new Vector3(0.1f, 0.03f, 2.2f), mMetal);

            // Tool table with a second UV scanner.
            Solid("Tool Table", f, new Vector3(-2.6f, 0.45f, -7.0f), new Vector3(0.9f, 0.9f, 0.6f), mDarkMetal);
            Deco("Tool Table Top", f, new Vector3(-2.6f, 0.91f, -7.0f), new Vector3(0.92f, 0.02f, 0.62f), mTray);
            Text3D(f, "Tool Label", new Vector3(-2.6f, 0.923f, -7.22f), 180f, "UV SCANNER", 0.7f, new Color(0.6f, 0.9f, 0.9f),
                new Vector2(0.5f, 0.1f), FontStyles.Bold, TextAlignmentOptions.Center, true);
            BuildScanner(tools, new Vector3(-2.75f, 0.96f, -7.0f), 90f);

            // Evidence station and the three movable clues.
            BuildStation(f, evidence, 1, new Vector3(3.3f, 0f, -5.4f), 90f, new[] { "TICKET", "STATUS REPORT", "CABLE TAG" });
            var ticket = MakeEvidencePrefab(new EvidenceSpec
            {
                Id = "E05", PrefabName = "Evidence_E05_MaintenanceTicket", Size = new Vector3(0.22f, 0.014f, 0.30f), Mat = mLabel,
                Text = "MAINTENANCE\nTICKET\n#4471", FontSize = 0.6f, TextColor = new Color(0.25f, 0.12f, 0.05f),
            });
            var report = MakeEvidencePrefab(new EvidenceSpec
            {
                Id = "E06", PrefabName = "Evidence_E06_StatusReport", Size = new Vector3(0.21f, 0.012f, 0.30f), Mat = mPaper,
                Text = "CAM 3 STATUS\nPOWER LOSS\n19:40", FontSize = 0.6f, TextColor = new Color(0.1f, 0.1f, 0.15f),
            });
            var tag = MakeEvidencePrefab(new EvidenceSpec
            {
                Id = "E07", PrefabName = "Evidence_E07_CableTag", Size = new Vector3(0.12f, 0.012f, 0.07f), Mat = mTag,
                Text = "CAM-3\nPOWER", FontSize = 0.5f, TextColor = Color.white,
            });
            Place(ticket, evidence, new Vector3(-2.4f, 0.756f, -10.0f), 10f);   // on the console desk
            Place(report, evidence, new Vector3(0.8f, 0.606f, -9.9f), -15f);    // on the side table
            Place(tag, evidence, new Vector3(3.78f, 1.026f, -7.4f), 90f);       // on the shelf

            BuildCaseUi(ui, 1, new Vector3(-3.90f, 1.7f, -5.0f), 270f, new Vector3(3.3f, 1.62f, -5.4f), 90f);

            // Entering this room (after the door opens) starts case 2.
            var zone = new GameObject("Room 2 Zone", typeof(BoxCollider), typeof(RoomZone));
            zone.transform.SetParent(room);
            zone.transform.position = new Vector3(0f, 1.5f, -7.4f);
            zone.GetComponent<BoxCollider>().size = new Vector3(7.6f, 3f, 6.2f);
            return a;
        }
    }
}
