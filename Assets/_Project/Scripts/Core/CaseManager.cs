using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CSIVR.Core
{
    /// <summary>
    /// STUB for Members 3 and 4 to test with. Member 5 owns the real CaseManager (states, submit, restart).
    /// When Member 5's version is merged, DELETE this file (same path = Git will flag the clash) and keep
    /// the same public names: Instance, RecordEvidence, IsRecorded, RecordedCount, OnEvidenceRecorded.
    /// Confirm with Member 5 that the namespace is CSIVR.Core.
    /// </summary>
    public class CaseManager : MonoBehaviour
    {
        public static CaseManager Instance { get; private set; }

        public UnityEvent<string> OnEvidenceRecorded = new UnityEvent<string>();

        readonly HashSet<string> m_Recorded = new HashSet<string>();

        public int RecordedCount => m_Recorded.Count;
        public bool IsRecorded(string id) => m_Recorded.Contains(id);

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Returns true only when a NEW record was added (duplicate prevention lives here).</summary>
        public bool RecordEvidence(string id)
        {
            if (string.IsNullOrEmpty(id) || !m_Recorded.Add(id)) return false;
            OnEvidenceRecorded.Invoke(id);
            return true;
        }
    }
}
