using System.Collections;
using CSIVR.Core;
using CSIVR.Interface;
using TMPro;
using UnityEngine;

namespace CSIVR.Tools
{
    /// <summary>
    /// A fixed fluorescent training mark. The collider stays active so the scanner can detect it;
    /// only the visual is hidden until it is validly illuminated. A small display-only progress panel
    /// above the mark shows the scan. If the mark is a fingerprint, the panel then runs a search against the
    /// simulated police criminal database and shows the match.
    /// </summary>
    public class UVTarget : MonoBehaviour
    {
        [SerializeField] string m_Id = "E04";
        [SerializeField] Renderer m_Mark;
        [Tooltip("Where the progress panel floats, relative to this object. The panel faces local +Z, like the mark.")]
        [SerializeField] Vector3 m_PanelOffset = new Vector3(0f, 0.24f, -0.05f);

        public string Id => m_Id;
        public Vector3 AimPoint => transform.position;
        public bool Recorded => CaseManager.Instance != null && CaseManager.Instance.IsRecorded(m_Id);

        GameObject m_Panel;
        TextMeshProUGUI m_PanelText, m_DetailText;
        RectTransform m_Fill;
        float m_ShownProgress = -1f;
        bool m_ShownRecorded;
        bool m_Pinned;   // true while the database search or its result is on show

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
            if (id != m_Id) return;
            ShowProgress(1f, true);

            var fingerprint = CriminalDatabase.FingerprintFor(m_Id);
            if (fingerprint != null)
                StartCoroutine(RunDatabaseSearch(fingerprint));
        }

        /// <summary>The mark is visible only while validly illuminated; the record itself persists.</summary>
        public void SetIlluminated(bool lit, float progress01)
        {
            if (m_Mark != null) m_Mark.enabled = lit;
            m_Panel.SetActive(lit || m_Pinned);

            // While the database search is showing, the scan readout must not overwrite it.
            if (lit && !m_Pinned) ShowProgress(progress01, Recorded);
        }

        void ShowProgress(float progress01, bool recorded)
        {
            if (m_Pinned) return;
            if (Mathf.Approximately(progress01, m_ShownProgress) && recorded == m_ShownRecorded) return;
            m_ShownProgress = progress01;
            m_ShownRecorded = recorded;

            SetReadout(recorded ? "EVIDENCE RECORDED" : "SCANNING...", recorded ? SpatialUI.TealLight : SpatialUI.Light,
                recorded ? 1f : progress01, "");
        }

        void SetReadout(string title, Color titleColor, float progress01, string detail)
        {
            m_PanelText.text = title;
            m_PanelText.color = titleColor;
            float fill = Mathf.Clamp01(progress01);
            m_Fill.gameObject.SetActive(fill > 0.04f);
            m_Fill.anchorMax = new Vector2(Mathf.Max(fill, 0.04f), 1f);
            m_DetailText.text = detail;
        }

        IEnumerator RunDatabaseSearch(string fingerprintId)
        {
            m_Pinned = true;
            m_Panel.SetActive(true);

            // Let "evidence recorded" be read, then compare the print against each police record in turn.
            SetReadout("EVIDENCE RECORDED", SpatialUI.TealLight, 1f, "");
            yield return new WaitForSeconds(1.2f);

            var records = CriminalDatabase.Records;
            int steps = 10;
            for (int i = 1; i <= steps; i++)
            {
                int checkedCount = Mathf.Min(records.Length, Mathf.CeilToInt(records.Length * i / (float)steps));
                SetReadout("SEARCHING POLICE DATABASE", SpatialUI.Light, i / (float)steps,
                    $"<size=70%>Print {fingerprintId}\nChecked {checkedCount} of {records.Length} records</size>");
                AudioFx.PlayAt(AudioFx.Click, transform.position, 0.5f);
                yield return new WaitForSeconds(0.35f);
            }

            var match = CriminalDatabase.FindByFingerprint(fingerprintId);
            var manager = CaseManager.Instance;
            if (match == null)
            {
                SetReadout("NO MATCH", SpatialUI.Amber, 1f, "<size=70%>Print not in the database</size>");
                manager?.Notify("Fingerprint search: no match in the police database.");
            }
            else
            {
                manager?.ReportCriminal(match);
                AudioFx.PlayAt(AudioFx.Chime, transform.position);
                SetReadout("MATCH FOUND", SpatialUI.TealLight, 1f, $"<b>{match.Name}</b>\n<size=70%>Inmate {match.InmateNumber}</size>");
                manager?.Notify($"Fingerprint match: {match.Name}, inmate {match.InmateNumber}.");
            }

            // Keep the result up for a while, then fall back to the normal behaviour.
            yield return new WaitForSeconds(14f);
            m_Pinned = false;
            m_Panel.SetActive(false);
        }

        void BuildPanel()
        {
            var canvas = SpatialUI.CreateCanvas("Scan Progress", transform, new Vector2(520f, 250f), 0.5f, interactive: false);
            m_Panel = canvas.gameObject;
            canvas.transform.localPosition = m_PanelOffset;

            var card = SpatialUI.CreateImage(canvas.transform, "Card", UISprites.Rounded, SpatialUI.Navy);
            SpatialUI.Stretch(card.rectTransform, 0, 0, 0, 0);

            m_PanelText = SpatialUI.CreateText(canvas.transform, "Label", 40f, TextAlignmentOptions.Left, SpatialUI.Light);
            m_PanelText.fontStyle = FontStyles.Bold;
            m_PanelText.characterSpacing = 3f;
            m_PanelText.textWrappingMode = TextWrappingModes.NoWrap;
            SpatialUI.TopStretch(m_PanelText.rectTransform, 32, 24, 18, 56);

            var trough = SpatialUI.CreateImage(canvas.transform, "Trough", UISprites.Rounded, SpatialUI.NavyLight);
            SpatialUI.TopStretch(trough.rectTransform, 32, 32, 84, 40);

            var fill = SpatialUI.CreateImage(trough.transform, "Fill", UISprites.Rounded, SpatialUI.Teal);
            m_Fill = fill.rectTransform;
            m_Fill.anchorMin = new Vector2(0f, 0f);
            m_Fill.anchorMax = new Vector2(0.04f, 1f);
            m_Fill.offsetMin = m_Fill.offsetMax = Vector2.zero;

            m_DetailText = SpatialUI.CreateText(canvas.transform, "Detail", 36f, TextAlignmentOptions.TopLeft, SpatialUI.Light);
            SpatialUI.TopStretch(m_DetailText.rectTransform, 32, 24, 138, 100);
        }
    }
}
