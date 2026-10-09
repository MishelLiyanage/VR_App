using CSIVR.Core;
using CSIVR.Evidence;
using CSIVR.Input;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CSIVR.Interface
{
    // Shows a small details card for the evidence item the player is holding, floating in the upper right of the view.
    public class EvidenceInspector : MonoBehaviour
    {
        [Tooltip("Card position relative to the camera in desktop mode (metres): right, up, forward.")]
        [SerializeField] Vector3 m_DesktopOffset = new Vector3(0.80f, 0.40f, 1.30f);
        [Tooltip("Card position relative to the camera in headset mode. Kept closer to the centre for comfort.")]
        [SerializeField] Vector3 m_HeadsetOffset = new Vector3(0.40f, 0.22f, 1.25f);
        [SerializeField] float m_Follow = 7f;

        GameObject m_Panel;
        TextMeshProUGUI m_IdText, m_TitleText, m_BodyText, m_StatusText;
        EvidenceItem m_Current;
        bool m_Placed;

        void Start()
        {
            BuildPanel();
            m_Panel.SetActive(false);

            foreach (var item in FindObjectsByType<EvidenceItem>(FindObjectsSortMode.None))
            {
                var grab = item.GetComponent<XRGrabInteractable>();
                if (grab == null) continue;

                var captured = item;
                grab.selectEntered.AddListener(_ => Show(captured));
                grab.selectExited.AddListener(_ => Hide(captured));
            }
        }

        void Show(EvidenceItem item)
        {
            m_Current = item;
            var info = CaseLibrary.Info(item.Id);
            bool recorded = item.Recorded;

            m_IdText.text = item.Id;
            m_TitleText.text = info.Title;
            m_BodyText.text = string.IsNullOrEmpty(info.Description) ? info.Observation : info.Description;
            m_StatusText.text = recorded
                ? "<color=#5be0d1>Recorded at the evidence station</color>"
                : "<color=#9fb3c8>Not recorded yet. Bring it to the tray.</color>";

            m_Placed = false;
            m_Panel.SetActive(true);
        }

        void Hide(EvidenceItem item)
        {
            if (m_Current != item) return;
            m_Current = null;
            m_Panel.SetActive(false);
        }

        void LateUpdate()
        {
            if (m_Current == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            var offset = ModeBootstrap.ActiveMode == RigMode.Desktop ? m_DesktopOffset : m_HeadsetOffset;
            var target = cam.transform.TransformPoint(offset);
            var t = m_Panel.transform;

            // Ease toward the target so the card trails the view slightly instead of being glued to it.
            t.position = m_Placed ? Vector3.Lerp(t.position, target, 1f - Mathf.Exp(-m_Follow * Time.deltaTime)) : target;
            m_Placed = true;

            // Canvases read correctly when the viewer looks along their +Z, so face away from the camera.
            var away = t.position - cam.transform.position;
            if (away.sqrMagnitude > 0.0001f)
                t.rotation = Quaternion.LookRotation(away, Vector3.up);
        }

        void BuildPanel()
        {
            // Display only: no ray raycaster, so rays pass straight through the card.
            var canvas = SpatialUI.CreateCanvas("Evidence Details", transform, new Vector2(560f, 320f), 0.56f, interactive: false);
            m_Panel = canvas.gameObject;
            var root = canvas.transform;

            var card = SpatialUI.CreateImage(root, "Card", UISprites.Rounded, SpatialUI.Navy);
            SpatialUI.Stretch(card.rectTransform, 0, 0, 0, 0);

            var chip = SpatialUI.CreateImage(root, "Chip", UISprites.Rounded, SpatialUI.Teal);
            SpatialUI.TopLeft(chip.rectTransform, 28, 24, 112, 48);
            m_IdText = SpatialUI.CreateText(chip.transform, "Text", 28f, TextAlignmentOptions.Center, Color.white);
            m_IdText.fontStyle = FontStyles.Bold;
            SpatialUI.Stretch(m_IdText.rectTransform, 0, 0, 0, 0);

            m_TitleText = SpatialUI.CreateText(root, "Title", 36f, TextAlignmentOptions.Left, SpatialUI.Light);
            m_TitleText.fontStyle = FontStyles.Bold;
            m_TitleText.textWrappingMode = TextWrappingModes.NoWrap;
            m_TitleText.overflowMode = TextOverflowModes.Ellipsis;
            SpatialUI.TopStretch(m_TitleText.rectTransform, 158, 24, 22, 52);

            var divider = SpatialUI.CreateImage(root, "Divider", null, SpatialUI.NavyLight, false);
            SpatialUI.TopStretch(divider.rectTransform, 28, 28, 88, 3);

            m_BodyText = SpatialUI.CreateText(root, "Body", 29f, TextAlignmentOptions.TopLeft, SpatialUI.Light);
            SpatialUI.Stretch(m_BodyText.rectTransform, 28, 28, 104, 66);

            m_StatusText = SpatialUI.CreateText(root, "Status", 24f, TextAlignmentOptions.Left, SpatialUI.Light);
            SpatialUI.BottomStretch(m_StatusText.rectTransform, 28, 28, 18, 38);
        }
    }
}