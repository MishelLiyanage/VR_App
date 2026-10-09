using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CSIVR.Evidence
{
    /// <summary>
    /// Remembers a loose object's start pose and puts it back if it falls out of the room or the player asks.
    /// Held and already-recorded objects are never reset.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ObjectRecovery : MonoBehaviour
    {
        const float KillHeight = -1f;

        Rigidbody m_Body;
        XRGrabInteractable m_Grab;
        EvidenceItem m_Evidence;
        Vector3 m_HomePosition;
        Quaternion m_HomeRotation;

        void Awake()
        {
            m_Body = GetComponent<Rigidbody>();
            m_Grab = GetComponent<XRGrabInteractable>();
            m_Evidence = GetComponent<EvidenceItem>();
            m_HomePosition = transform.position;
            m_HomeRotation = transform.rotation;
        }

        void Update()
        {
            if (transform.position.y < KillHeight)
                Restore();
        }

        public bool Restore()
        {
            if (m_Grab != null && m_Grab.isSelected) return false;
            if (m_Evidence != null && m_Evidence.Recorded) return false;

            if (!m_Body.isKinematic)
            {
                m_Body.linearVelocity = Vector3.zero;
                m_Body.angularVelocity = Vector3.zero;
            }
            transform.SetPositionAndRotation(m_HomePosition, m_HomeRotation);
            m_Body.position = m_HomePosition;
            m_Body.rotation = m_HomeRotation;
            return true;
        }

        /// <summary>For the "Restore loose items" button. Returns how many objects moved.</summary>
        public static int RestoreAll()
        {
            int count = 0;
            foreach (var r in FindObjectsByType<ObjectRecovery>(FindObjectsSortMode.None))
                if (r.Restore()) count++;
            return count;
        }
    }
}
