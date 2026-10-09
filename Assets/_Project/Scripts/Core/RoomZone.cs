using UnityEngine;

namespace CSIVR.Core
{
    /// <summary>
    /// A volume covering a room. When the player's head first enters it after the previous case was solved,
    /// the case moves on and this room's briefing board comes alive. Polling the head position works for the
    /// XR rig and the desktop rig alike.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class RoomZone : MonoBehaviour
    {
        BoxCollider m_Box;
        bool m_Fired;

        void Awake()
        {
            m_Box = GetComponent<BoxCollider>();
            m_Box.isTrigger = true;
        }

        void Update()
        {
            if (m_Fired) return;
            var manager = CaseManager.Instance;
            var cam = Camera.main;
            if (manager == null || cam == null) return;

            if (!m_Box.bounds.Contains(cam.transform.position)) return;
            if (manager.AdvanceToNextCase())
                m_Fired = true;
        }
    }
}
