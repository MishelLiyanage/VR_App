using CSIVR.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CSIVR.Tools
{
    /// <summary>
    /// Member 4. Handheld UV scanner. Uses XRI's Activate action, so the VR trigger, the simulator and the
    /// desktop left mouse button (DesktopRig) all work without any key code in here.
    /// Valid scan = held + on + in range + inside cone + first ray hit is the target, for m_DwellSeconds.
    /// Any failed check resets progress.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class UVScanner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform m_ScanOrigin;       // lamp tip, +Z = beam direction
        [SerializeField] Light m_UVLight;
        [SerializeField] AudioSource m_ScanLoop;
        [SerializeField] AudioSource m_DoneSound;
        [SerializeField] GameObject m_ProgressRoot;    // shown only while scanning
        [SerializeField] Transform m_ProgressFill;     // scaled along X

        [Header("Validity rules (tunable)")]
        [SerializeField] float m_MaxDistance = 1.5f;
        [SerializeField] float m_ConeHalfAngle = 20f;
        [SerializeField] float m_DwellSeconds = 2f;
        [Tooltip("Environment + Evidence + UVTarget. Leave Default out so the tool and hands never block the ray.")]
        [SerializeField] LayerMask m_ObstructionMask = ~0;

        [Header("Events")]
        public UnityEvent<float> OnProgress = new UnityEvent<float>(); // 0..1
        public UnityEvent OnCompleted = new UnityEvent();

        [Header("Debug (read-only at runtime)")]
        [SerializeField] string m_Status = "Off";

        XRGrabInteractable m_Grab;
        UVTarget[] m_Targets;
        UVTarget m_Revealed;
        bool m_On;
        float m_Progress;
        Vector3 m_FillBaseScale;

        public string Status => m_Status;

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            if (GetComponent<HoverHighlight>() == null) gameObject.AddComponent<HoverHighlight>();
            m_Targets = FindObjectsByType<UVTarget>(FindObjectsSortMode.None);
            if (m_ProgressFill != null) m_FillBaseScale = m_ProgressFill.localScale;
            SetLamp(false);
            SetBar(0f);
        }

        void OnEnable()
        {
            m_Grab.activated.AddListener(OnActivated);
            m_Grab.deactivated.AddListener(OnDeactivated);
            m_Grab.selectExited.AddListener(OnReleased);
        }

        void OnDisable()
        {
            m_Grab.activated.RemoveListener(OnActivated);
            m_Grab.deactivated.RemoveListener(OnDeactivated);
            m_Grab.selectExited.RemoveListener(OnReleased);
        }

        void OnActivated(ActivateEventArgs _) { m_On = true; SetLamp(true); }
        void OnDeactivated(DeactivateEventArgs _) => TurnOff();
        void OnReleased(SelectExitEventArgs _) => TurnOff();

        void TurnOff()
        {
            m_On = false;
            SetLamp(false);
            Fail("Off");
        }

        void Update()
        {
            if (!m_On) return;
            if (!m_Grab.isSelected) { Fail("Not held"); return; }

            var cm = CaseManager.Instance;
            if (cm == null) { Fail("No CaseManager"); return; }

            var target = FindValidTarget(out string why);
            if (target == null) { Fail(why); return; }

            // Valid illumination: show the mark while the light is on it.
            if (m_Revealed != target) { ResetReveal(); m_Revealed = target; }
            target.SetRevealed(true);

            if (cm.IsRecorded(target.EvidenceId)) { m_Status = "Recorded"; return; }

            m_Status = "Scanning";
            m_Progress += Time.deltaTime;
            float t = Mathf.Clamp01(m_Progress / m_DwellSeconds);
            SetBar(t);
            OnProgress.Invoke(t);

            if (m_Progress >= m_DwellSeconds && cm.RecordEvidence(target.EvidenceId))
            {
                if (m_DoneSound != null) m_DoneSound.Play();
                OnCompleted.Invoke();
                m_Status = "Recorded";
            }
        }

        // distance -> cone angle -> first ray hit must be the target
        UVTarget FindValidTarget(out string why)
        {
            why = "No target";
            Vector3 origin = m_ScanOrigin.position;

            foreach (var t in m_Targets)
            {
                Vector3 to = t.AimPoint.position - origin;
                float dist = to.magnitude;
                if (dist < 0.001f) continue;

                if (dist > m_MaxDistance) { why = "Too far"; continue; }
                if (Vector3.Angle(m_ScanOrigin.forward, to) > m_ConeHalfAngle) { why = "Wrong angle"; continue; }

                if (Physics.Raycast(origin, to / dist, out RaycastHit hit, dist + 0.05f,
                                    m_ObstructionMask, QueryTriggerInteraction.Ignore)
                    && hit.collider.GetComponentInParent<UVTarget>() == t)
                    return t;

                why = "Blocked";
            }
            return null;
        }

        void Fail(string reason)
        {
            m_Status = reason;
            if (m_Progress > 0f) { m_Progress = 0f; OnProgress.Invoke(0f); }
            SetBar(0f);
            ResetReveal();
        }

        void ResetReveal()
        {
            if (m_Revealed != null) { m_Revealed.SetRevealed(false); m_Revealed = null; }
        }

        void SetLamp(bool on)
        {
            if (m_UVLight != null) m_UVLight.enabled = on;
            if (m_ScanLoop == null) return;
            if (on && !m_ScanLoop.isPlaying) m_ScanLoop.Play();
            else if (!on) m_ScanLoop.Stop();
        }

        // Left-anchored progress bar: the fill grows from its left end.
        void SetBar(float t)
        {
            if (m_ProgressRoot != null) m_ProgressRoot.SetActive(t > 0f);
            if (m_ProgressFill == null) return;
            float w = m_FillBaseScale.x;
            m_ProgressFill.localScale = new Vector3(Mathf.Max(0.0001f, w * t), m_FillBaseScale.y, m_FillBaseScale.z);
            var p = m_ProgressFill.localPosition;
            p.x = -w * 0.5f + w * t * 0.5f;
            m_ProgressFill.localPosition = p;
        }

        void OnDrawGizmosSelected()
        {
            if (m_ScanOrigin == null) return;
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(m_ScanOrigin.position, m_ScanOrigin.forward * m_MaxDistance);
        }
    }
}
