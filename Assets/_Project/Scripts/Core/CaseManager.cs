using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace CSIVR.Core
{
    public enum CaseState { Briefing, Tutorial, Investigating, ReadyToSubmit, Debrief }
    public enum TutorialAction { Teleport, GrabRelease, ActivateTool }

    [Serializable]
    public struct EvidenceDefinition
    {
        public string id;
        public string title;
        [TextArea] public string observation;
    }

    public readonly struct FindingResult
    {
        public readonly bool Accepted;
        public readonly bool Correct;
        public readonly string Explanation;

        public FindingResult(bool accepted, bool correct, string explanation)
        {
            Accepted = accepted;
            Correct = correct;
            Explanation = explanation;
        }
    }

    /// <summary>Single source of truth for the Missing Prototype case and its end-to-end flow.</summary>
    [DisallowMultipleComponent]
    public sealed class CaseManager : MonoBehaviour
    {
        public const string CorrectFindingId = "card-door";

        [SerializeField] EvidenceDefinition[] m_Evidence =
        {
            new EvidenceDefinition { id = "E01", title = "Access Card", observation = "Card B-17 was found beside the desk." },
            new EvidenceDefinition { id = "E02", title = "Access Log", observation = "The log records successful B-17 access at 19:42." },
            new EvidenceDefinition { id = "E03", title = "Packing Label", observation = "The torn label matches the missing prototype container." },
            new EvidenceDefinition { id = "E04", title = "UV Transfer Mark", observation = "A training transfer mark was recorded on the cabinet handle." },
        };
        [SerializeField] UnityEvent m_CaseChanged = new UnityEvent();

        readonly HashSet<string> m_Recorded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<TutorialAction> m_TutorialActions = new HashSet<TutorialAction>();
        CaseState m_State = CaseState.Briefing;
        string m_Status = "Review the briefing, then select Start Investigation.";
        string m_Debrief = string.Empty;

        public static CaseManager Instance { get; private set; }
        public CaseState State => m_State;
        public int RecordedCount => m_Recorded.Count;
        public int RequiredEvidenceCount => m_Evidence.Length;
        public string StatusMessage => m_Status;
        public string DebriefMessage => m_Debrief;
        public IReadOnlyCollection<string> RecordedEvidenceIds => m_Recorded;
        public UnityEvent CaseChanged => m_CaseChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[CaseManager] Duplicate manager disabled.", this);
                enabled = false;
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void BeginCase()
        {
            if (m_State != CaseState.Briefing) return;
            m_State = CaseState.Tutorial;
            m_Status = TutorialInstruction();
            NotifyChanged();
        }

        /// <summary>Called by real locomotion/interaction events, never by a tutorial Next button.</summary>
        public void ReportTutorialAction(TutorialAction action)
        {
            if (m_State != CaseState.Tutorial || !m_TutorialActions.Add(action)) return;

            if (m_TutorialActions.Count == Enum.GetValues(typeof(TutorialAction)).Length)
            {
                m_State = CaseState.Investigating;
                m_Status = "Tutorial complete. Record the four clues at the evidence station and with the UV scanner.";
            }
            else
            {
                m_Status = TutorialInstruction();
            }
            NotifyChanged();
        }

        public bool RecordEvidence(string id)
        {
            if (m_State != CaseState.Investigating && m_State != CaseState.ReadyToSubmit)
                return false;

            var evidence = FindEvidence(id);
            if (!evidence.HasValue)
            {
                Debug.LogWarning($"[CaseManager] Rejected unknown evidence ID '{id}'.", this);
                return false;
            }
            if (!m_Recorded.Add(evidence.Value.id))
            {
                m_Status = $"{evidence.Value.id} is already recorded; progress was not duplicated.";
                NotifyChanged();
                return false;
            }

            m_Status = $"Recorded {evidence.Value.id}: {evidence.Value.observation}";
            if (m_Recorded.Count == m_Evidence.Length)
            {
                m_State = CaseState.ReadyToSubmit;
                m_Status += " All records are complete. Submit the supported entry route.";
            }
            NotifyChanged();
            return true;
        }

        public FindingResult SubmitFinding(string answerId)
        {
            if (m_State != CaseState.ReadyToSubmit)
            {
                var missing = MissingEvidenceText();
                m_Status = string.IsNullOrEmpty(missing)
                    ? "Complete the investigation before submitting a finding."
                    : $"Cannot submit yet. Missing: {missing}.";
                NotifyChanged();
                return new FindingResult(false, false, m_Status);
            }

            bool correct = string.Equals(answerId, CorrectFindingId, StringComparison.OrdinalIgnoreCase);
            m_Debrief = correct
                ? "Supported finding: card-controlled door. Card B-17 and the 19:42 access log support that route; the packing label and transfer mark support removal of the prototype. The evidence does not identify an offender."
                : "This route is not best supported by the recorded clues. The B-17 card and 19:42 access record support the card-controlled door. The evidence still does not identify an offender.";
            m_State = CaseState.Debrief;
            m_Status = correct ? "Finding supported." : "Finding needs revision.";
            NotifyChanged();
            return new FindingResult(true, correct, m_Debrief);
        }

        public void SubmitCardDoor() => SubmitFinding(CorrectFindingId);
        public void SubmitForcedWindow() => SubmitFinding("forced-window");
        public void SubmitInsufficientEvidence() => SubmitFinding("insufficient");

        public void RestartCase()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public bool IsRecorded(string id) => !string.IsNullOrWhiteSpace(id) && m_Recorded.Contains(id);

        public string EvidenceLogText()
        {
            var lines = new List<string>();
            foreach (var item in m_Evidence)
                lines.Add(m_Recorded.Contains(item.id) ? $"[RECORDED] {item.id} - {item.title}" : $"[MISSING] {item.id} - {item.title}");
            return string.Join("\n", lines);
        }

        string TutorialInstruction()
        {
            if (!m_TutorialActions.Contains(TutorialAction.Teleport)) return "Tutorial 1/3: teleport once to a station.";
            if (!m_TutorialActions.Contains(TutorialAction.GrabRelease)) return "Tutorial 2/3: grab and release the practice object.";
            return "Tutorial 3/3: activate the held practice tool.";
        }

        EvidenceDefinition? FindEvidence(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            foreach (var item in m_Evidence)
                if (string.Equals(item.id, id.Trim(), StringComparison.OrdinalIgnoreCase)) return item;
            return null;
        }

        string MissingEvidenceText()
        {
            var missing = new List<string>();
            foreach (var item in m_Evidence)
                if (!m_Recorded.Contains(item.id)) missing.Add(item.id);
            return string.Join(", ", missing);
        }

        void NotifyChanged()
        {
            m_CaseChanged.Invoke();
            Debug.Log($"[CaseManager] {m_State}: {m_Status}", this);
        }
    }
}
