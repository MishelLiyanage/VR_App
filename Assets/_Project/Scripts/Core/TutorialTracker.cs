using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CSIVR.Core
{
    /// <summary>
    /// Advances the tutorial on real actions: moving to another spot, grabbing the practice prop.
    /// (Activating the tool is reported by the UV scanner.) Practice objects never count as evidence.
    /// </summary>
    public class TutorialTracker : MonoBehaviour
    {
        const float MoveDistance = 1.0f;

        [SerializeField] XRGrabInteractable m_PracticeProp;

        Vector3 m_Start;
        bool m_HasStart;

        void OnEnable()
        {
            if (m_PracticeProp != null) m_PracticeProp.selectEntered.AddListener(OnPracticeGrabbed);
        }

        void OnDisable()
        {
            if (m_PracticeProp != null) m_PracticeProp.selectEntered.RemoveListener(OnPracticeGrabbed);
        }

        void OnPracticeGrabbed(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs _)
        {
            CaseManager.Instance?.CompleteTutorialStep(TutorialStep.Grab);
        }

        void Update()
        {
            var manager = CaseManager.Instance;
            if (manager == null || manager.State != CaseState.Tutorial)
            {
                m_HasStart = false;
                return;
            }

            var cam = Camera.main;
            if (cam == null) return;

            var pos = cam.transform.position;
            if (!m_HasStart)
            {
                m_Start = pos;
                m_HasStart = true;
                return;
            }

            var flat = new Vector3(pos.x - m_Start.x, 0f, pos.z - m_Start.z);
            if (flat.magnitude >= MoveDistance)
                manager.CompleteTutorialStep(TutorialStep.Move);
        }
    }
}
