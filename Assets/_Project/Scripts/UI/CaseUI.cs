using CSIVR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CSIVR.UI
{
    /// <summary>World-space briefing, progress, finding, debrief and credits presentation.</summary>
    public sealed class CaseUI : MonoBehaviour
    {
        [SerializeField] CaseManager m_Case;
        [SerializeField] Text m_Title;
        [SerializeField] Text m_Status;
        [SerializeField] Text m_EvidenceLog;
        [SerializeField] GameObject m_StartControls;
        [SerializeField] GameObject m_FindingControls;
        [SerializeField] GameObject m_DebriefControls;
        [SerializeField] GameObject m_HelpPanel;
        [SerializeField] GameObject m_CreditsPanel;

        void OnEnable()
        {
            if (m_Case == null) m_Case = CaseManager.Instance;
            if (m_Case != null) m_Case.CaseChanged.AddListener(Refresh);
            Refresh();
        }

        void OnDisable()
        {
            if (m_Case != null) m_Case.CaseChanged.RemoveListener(Refresh);
        }

        public void Refresh()
        {
            if (m_Case == null) return;
            m_Title.text = TitleFor(m_Case.State);
            m_Status.text = m_Case.State == CaseState.Debrief ? m_Case.DebriefMessage : m_Case.StatusMessage;
            m_EvidenceLog.text = $"Evidence {m_Case.RecordedCount}/{m_Case.RequiredEvidenceCount}\n{m_Case.EvidenceLogText()}";
            m_StartControls.SetActive(m_Case.State == CaseState.Briefing);
            m_FindingControls.SetActive(m_Case.State == CaseState.ReadyToSubmit);
            m_DebriefControls.SetActive(m_Case.State == CaseState.Debrief);
        }

        public void ToggleCredits()
        {
            if (m_CreditsPanel != null) m_CreditsPanel.SetActive(!m_CreditsPanel.activeSelf);
        }

        public void ToggleHelp()
        {
            if (m_HelpPanel != null) m_HelpPanel.SetActive(!m_HelpPanel.activeSelf);
        }

        static string TitleFor(CaseState state)
        {
            switch (state)
            {
                case CaseState.Briefing: return "THE MISSING PROTOTYPE";
                case CaseState.Tutorial: return "FIELD TUTORIAL";
                case CaseState.Investigating: return "INVESTIGATION";
                case CaseState.ReadyToSubmit: return "SUBMIT A FINDING";
                default: return "CASE DEBRIEF";
            }
        }
    }
}
