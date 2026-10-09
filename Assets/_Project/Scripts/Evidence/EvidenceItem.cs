using CSIVR.Core;
using UnityEngine;

namespace CSIVR.Evidence
{
    /// <summary>
    /// Metadata for a movable clue. It never awards progress itself; the station asks CaseManager to record it.
    /// </summary>
    public class EvidenceItem : MonoBehaviour
    {
        [SerializeField] string m_Id = "E01";
        [SerializeField] bool m_RequiresUV;

        public string Id => m_Id;
        public bool RequiresUV => m_RequiresUV;
        public string Title => CaseLibrary.Info(m_Id).Title;
        public string Description => CaseLibrary.Info(m_Id).Observation;
        public bool Recorded => CaseManager.Instance != null && CaseManager.Instance.IsRecorded(m_Id);
    }
}
