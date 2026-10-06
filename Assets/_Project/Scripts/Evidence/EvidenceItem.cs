using CSIVR.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CSIVR.Evidence
{
    /// <summary>
    /// Member 3. Data + physical state of one movable clue (E01-E03).
    /// It never awards case progress itself - EvidenceStation asks CaseManager to do that.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
    public class EvidenceItem : MonoBehaviour
    {
        [SerializeField] string m_EvidenceId = "E01";
        [SerializeField] string m_Title = "Access card";
        [SerializeField, TextArea] string m_Description = "";
        [SerializeField] bool m_RequiresUV;

        public string EvidenceId => m_EvidenceId;
        public string Title => m_Title;
        public string Description => m_Description;
        public bool RequiresUV => m_RequiresUV;
        public bool Recorded { get; private set; }
        public bool IsHeld => m_Grab != null && m_Grab.isSelected;

        Rigidbody m_Body;
        XRGrabInteractable m_Grab;
        Vector3 m_HomePosition;
        Quaternion m_HomeRotation;

        void Awake()
        {
            m_Body = GetComponent<Rigidbody>();
            m_Grab = GetComponent<XRGrabInteractable>();
            if (GetComponent<HoverHighlight>() == null) gameObject.AddComponent<HoverHighlight>();
            m_HomePosition = transform.position;
            m_HomeRotation = transform.rotation;
        }

        /// <summary>Called by EvidenceStation only AFTER the item has been released.</summary>
        public void MarkRecorded(Transform displaySlot)
        {
            Recorded = true;
            StopMotion();
            m_Body.isKinematic = true;
            if (displaySlot != null)
                transform.SetPositionAndRotation(displaySlot.position, displaySlot.rotation);
            m_Grab.enabled = false; // locked on the tray
        }

        /// <summary>Used by ObjectRecovery. Never resets held or recorded items.</summary>
        public void ResetToHome()
        {
            if (Recorded || IsHeld) return;
            StopMotion();
            transform.SetPositionAndRotation(m_HomePosition, m_HomeRotation);
            Physics.SyncTransforms();
        }

        void StopMotion()
        {
            if (m_Body.isKinematic) return;
            m_Body.linearVelocity = Vector3.zero;
            m_Body.angularVelocity = Vector3.zero;
        }
    }
}
