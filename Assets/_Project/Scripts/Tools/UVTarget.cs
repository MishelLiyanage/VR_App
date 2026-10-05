using UnityEngine;

namespace CSIVR.Tools
{
    /// <summary>
    /// Member 4. Fixed fluorescent training mark (E04). Needs a NON-trigger collider on the "UVTarget" layer.
    /// Keep this object active - only the mark renderer is hidden - otherwise the scanner cannot see it.
    /// </summary>
    public class UVTarget : MonoBehaviour
    {
        [SerializeField] string m_EvidenceId = "E04";
        [SerializeField] Renderer m_Mark;
        [SerializeField] Transform m_AimPoint;

        public string EvidenceId => m_EvidenceId;
        public Transform AimPoint => m_AimPoint != null ? m_AimPoint : transform;

        void Awake() => SetRevealed(false);

        public void SetRevealed(bool revealed)
        {
            if (m_Mark != null) m_Mark.enabled = revealed;
        }
    }
}
