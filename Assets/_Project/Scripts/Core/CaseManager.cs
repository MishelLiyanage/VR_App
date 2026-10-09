using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CSIVR.Core
{
    public enum CaseState { Briefing, Tutorial, Investigating, ReadyToSubmit, Debrief }

    public enum TutorialStep { Move, Grab, Activate }

    /// <summary>
    /// Owns the case state and the set of recorded evidence IDs for the whole run. Rooms are played in order:
    /// each has its own case; a correct finding unlocks the door to the next one. Evidence, the stations and the
    /// scanner only request changes here; the UI, door and audio observe the events.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class CaseManager : MonoBehaviour
    {
        public static CaseManager Instance { get; private set; }

        public int CaseIndex { get; private set; }
        public CaseState State { get; private set; } = CaseState.Briefing;
        public SubmitResult LastResult { get; private set; }

        public CaseDefinition Case => CaseLibrary.Get(CaseIndex);
        public bool IsFinalCase => CaseIndex >= CaseLibrary.Count - 1;

        public event Action StateChanged;
        public event Action TutorialProgressed;
        public event Action<string> EvidenceRecorded;
        public event Action<SubmitResult> FindingSubmitted;
        /// <summary>Raised once per case when its finding is correct (the door to the next room listens).</summary>
        public event Action<int> CaseSolved;
        /// <summary>Short feedback line for the station panels.</summary>
        public event Action<string> Message;

        readonly HashSet<string> m_Recorded = new HashSet<string>();
        readonly HashSet<TutorialStep> m_TutorialDone = new HashSet<TutorialStep>();
        readonly SubmitResult[] m_Solved = new SubmitResult[CaseLibrary.Count];

        public bool IsRecorded(string id) => id != null && m_Recorded.Contains(id);
        public bool IsTutorialDone(TutorialStep step) => m_TutorialDone.Contains(step);
        public int TutorialDoneCount => m_TutorialDone.Count;
        public SubmitResult SolvedResult(int caseIndex) => caseIndex >= 0 && caseIndex < m_Solved.Length ? m_Solved[caseIndex] : null;

        /// <summary>Records belonging to the current case.</summary>
        public int RecordedCount => RecordedIn(CaseIndex);

        public int RecordedIn(int caseIndex)
        {
            int n = 0;
            foreach (var id in CaseLibrary.Get(caseIndex).EvidenceIds)
                if (m_Recorded.Contains(id)) n++;
            return n;
        }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void StartCase()
        {
            if (State != CaseState.Briefing) return;
            // The tutorial only runs in the first room.
            SetState(CaseIndex == 0 ? CaseState.Tutorial : CaseState.Investigating);
        }

        public void SkipTutorial()
        {
            if (State == CaseState.Tutorial)
                SetState(CaseState.Investigating);
        }

        public void CompleteTutorialStep(TutorialStep step)
        {
            if (State != CaseState.Tutorial || !m_TutorialDone.Add(step))
                return;

            TutorialProgressed?.Invoke();
            if (m_TutorialDone.Count >= Enum.GetValues(typeof(TutorialStep)).Length)
                Invoke(nameof(FinishTutorial), 1.2f);
        }

        void FinishTutorial()
        {
            if (State == CaseState.Tutorial)
                SetState(CaseState.Investigating);
        }

        /// <summary>Returns true only when a new record was added.</summary>
        public bool RecordEvidence(string id)
        {
            if (!CaseLibrary.IsValidId(id)) return false;

            if (State != CaseState.Investigating && State != CaseState.ReadyToSubmit)
            {
                Notify("Press Start on the board first.");
                return false;
            }

            if (!Case.Contains(id))
            {
                Notify("That evidence belongs to a different case.");
                return false;
            }

            if (!m_Recorded.Add(id)) return false;

            EvidenceRecorded?.Invoke(id);
            if (State == CaseState.Investigating && RecordedCount >= Case.RequiredRecords)
                SetState(CaseState.ReadyToSubmit);
            return true;
        }

        public List<string> MissingIds()
        {
            var missing = new List<string>();
            foreach (var id in Case.EvidenceIds)
                if (!m_Recorded.Contains(id)) missing.Add(id);
            return missing;
        }

        /// <summary>Always returns an explained result. A blocked submission has Accepted = false and changes nothing.</summary>
        public SubmitResult SubmitFinding(string answerId)
        {
            if (State != CaseState.ReadyToSubmit)
            {
                var names = new List<string>();
                foreach (var id in MissingIds()) names.Add(Case.Info(id).Title);
                return new SubmitResult
                {
                    CaseIndex = CaseIndex,
                    Accepted = false,
                    AnswerLabel = Case.Answer(answerId).Label,
                    Summary = "Not ready to submit.",
                    Explanation = names.Count > 0
                        ? "Still to record: " + string.Join(", ", names) + "."
                        : "Finish the current step first.",
                };
            }

            LastResult = CaseLibrary.Evaluate(CaseIndex, answerId);
            SetState(CaseState.Debrief);
            FindingSubmitted?.Invoke(LastResult);

            if (LastResult.Correct && m_Solved[CaseIndex] == null)
            {
                m_Solved[CaseIndex] = LastResult;
                CaseSolved?.Invoke(CaseIndex);
            }
            return LastResult;
        }

        /// <summary>After a wrong finding: go back and choose again without losing the records.</summary>
        public void RetryFinding()
        {
            if (State == CaseState.Debrief && LastResult != null && !LastResult.Correct)
                SetState(CaseState.ReadyToSubmit);
        }

        /// <summary>Called when the player walks into the next room after solving this one.</summary>
        public bool AdvanceToNextCase()
        {
            if (State != CaseState.Debrief || LastResult == null || !LastResult.Correct || IsFinalCase)
                return false;

            CaseIndex++;
            LastResult = null;
            SetState(CaseState.Briefing);
            return true;
        }

        public void Notify(string message) => Message?.Invoke(message);

        /// <summary>Reloading the single scene resets state, held objects, scanner progress, tutorial, doors and UI.</summary>
        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void SetState(CaseState next)
        {
            State = next;
            StateChanged?.Invoke();
        }
    }
}
