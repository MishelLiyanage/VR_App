using CSIVR.Core;
using CSIVR.Interface;
using TMPro;
using UnityEngine;

namespace CSIVR.Tools
{
    /// <summary>
    /// A fixed fluorescent training mark. The collider stays active so the scanner can detect it;
    /// only the visual is hidden until it is validly illuminated. A small display-only progress panel
    /// above the mark shows the scan.
    /// </summary>
    public class UVTarget : MonoBehaviour
    {
        [SerializeField] string m_Id = "E04";
        [SerializeField] Renderer m_Mark;
        [Tooltip("Where the progress panel floats, relative to this object. The panel faces local +Z, like the mark.")]
        [SerializeField] Vector3 m_PanelOffset = new Vector3(0f, 0.2f, -0.05f);

        public string Id => m_Id;
        public Vector3 AimPoint => transform.position;
        public bool Recorded => CaseManager.Instance != null && CaseManager.Instance.IsRecorded(m_Id);

        GameObject m_Panel;
        TextMeshProUGUI m_PanelText;
        RectTransform m_Fill;
        float m_ShownProgress = -1f;
        bool m_ShownRecorded;

        void Awake()
        {
            if (m_Mark != null) m_Mark.enabled = false;
            BuildPanel();
            m_Panel.SetActive(false);
        }

        void OnEnable()
        {
            if (CaseManager.Instance != null) CaseManager.Instance.EvidenceRecorded += OnRecorded;
        }

        void OnDisable()
        {
            if (CaseManager.Instance != null) CaseManager.Instance.EvidenceRecorded -= OnRecorded;
        }

        void OnRecorded(string id)
        {
            if (id == m_Id) ShowProgress(1f, true);
        }

        /// <summary>The mark is visible only while validly illuminated; the record itself persists.</summary>
        public void SetIlluminated(bool lit, float progress01)
        {
            if (m_Mark != null) m_Mark.enabled = lit;
            m_Panel.SetActive(lit);
            if (lit) ShowProgress(progress01, Recorded);
        }

        void ShowProgress(float progress01, bool recorded)
        {
            if (Mathf.Approximately(progress01, m_ShownProgress) && recorded == m_ShownRecorded) return;
            m_ShownProgress = progress01;
            m_ShownRecorded = recorded;

            m_PanelText.text = recorded ? "EVIDENCE RECORDED" : "SCANNING...";
            m_PanelText.color = recorded ? SpatialUI.TealLight : SpatialUI.Light;
            float fill = recorded ? 1f : Mathf.Clamp01(progress01);
            m_Fill.gameObject.SetActive(fill > 0.04f);
            m_Fill.anchorMax = new Vector2(Mathf.Max(fill, 0.04f), 1f);
        }

        void BuildPanel()
        {
            var canvas = SpatialUI.CreateCanvas("Scan Progress", transform, new Vector2(520f, 170f), 0.5f, interactive: false);
            m_Panel = canvas.gameObject;
            canvas.transform.localPosition = m_PanelOffset;

            var card = SpatialUI.CreateImage(canvas.transform, "Card", UISprites.Rounded, SpatialUI.Navy);
            SpatialUI.Stretch(card.rectTransform, 0, 0, 0, 0);

            m_PanelText = SpatialUI.CreateText(canvas.transform, "Label", 42f, TextAlignmentOptions.Left, SpatialUI.Light);
            m_PanelText.fontStyle = FontStyles.Bold;
            m_PanelText.characterSpacing = 3f;
            m_PanelText.textWrappingMode = TextWrappingModes.NoWrap;
            SpatialUI.TopStretch(m_PanelText.rectTransform, 32, 24, 18, 60);

            var trough = SpatialUI.CreateImage(canvas.transform, "Trough", UISprites.Rounded, SpatialUI.NavyLight);
            SpatialUI.BottomStretch(trough.rectTransform, 32, 32, 26, 44);

            var fill = SpatialUI.CreateImage(trough.transform, "Fill", UISprites.Rounded, SpatialUI.Teal);
            m_Fill = fill.rectTransform;
            m_Fill.anchorMin = new Vector2(0f, 0f);
            m_Fill.anchorMax = new Vector2(0.04f, 1f);
            m_Fill.offsetMin = m_Fill.offsetMax = Vector2.zero;
        }
    }
}
