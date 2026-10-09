using System;
using CSIVR.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CSIVR.Tools
{
    /// <summary>
    /// Handheld UV scanner. A scan only succeeds when the tool is held and switched on (the XRI Activate
    /// action), the target is within range and the aiming cone, nothing blocks the ray, and those checks hold
    /// continuously for the required time. Any failed check resets progress.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class UVScanner : MonoBehaviour
    {
        [SerializeField] Transform m_ScanOrigin;
        [SerializeField] Light m_Light;
        [SerializeField] AudioSource m_ScanAudio;
        [SerializeField] float m_MaxDistance = 1.5f;
        [SerializeField] float m_ConeHalfAngle = 20f;
        [SerializeField] float m_RequiredSeconds = 2f;
        [Tooltip("Layers the scan ray can hit. Environment and evidence block it; UV targets are what it looks for.")]
        [SerializeField] LayerMask m_RayMask = ~0;

        XRGrabInteractable m_Grab;
        UVTarget[] m_Targets;
        UVTarget m_Lit;
        bool m_On;
        float m_Progress;

        public bool IsOn => m_On;
        public float Progress01 => Mathf.Clamp01(m_Progress / m_RequiredSeconds);

        /// <summary>Raised when the tool is switched on while held (used by the tutorial).</summary>
        public event Action Activated;

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            // Never fall back to "everything": the tool's own collider must not block its ray.
            if (m_RayMask.value == 0)
                m_RayMask = LayerMask.GetMask("Environment", "Evidence", "UVTarget");
            if (m_ScanAudio != null)
            {
                m_ScanAudio.clip = AudioFx.ScanLoop;
                m_ScanAudio.loop = true;
                m_ScanAudio.playOnAwake = false;
            }
            SetLight(false);
        }

        void OnEnable()
        {
            m_Grab.activated.AddListener(OnActivated);
            m_Grab.deactivated.AddListener(OnDeactivated);
            m_Grab.selectExited.AddListener(OnSelectExited);
        }

        void OnDisable()
        {
            m_Grab.activated.RemoveListener(OnActivated);
            m_Grab.deactivated.RemoveListener(OnDeactivated);
            m_Grab.selectExited.RemoveListener(OnSelectExited);
            SwitchOff();
        }

        void Start()
        {
            m_Targets = FindObjectsByType<UVTarget>(FindObjectsSortMode.None);
        }

        void OnActivated(ActivateEventArgs _)
        {
            if (!m_Grab.isSelected) return;
            m_On = true;
            SetLight(true);
            Activated?.Invoke();
            CaseManager.Instance?.CompleteTutorialStep(TutorialStep.Activate);
        }

        void OnDeactivated(DeactivateEventArgs _) => SwitchOff();
        void OnSelectExited(SelectExitEventArgs _) => SwitchOff();

        void SwitchOff()
        {
            m_On = false;
            ResetProgress();
            SetLight(false);
        }

        void Update()
        {
            if (!m_On || !m_Grab.isSelected || m_Targets == null)
            {
                if (m_Lit != null || m_Progress > 0f) ResetProgress();
                return;
            }

            UVTarget valid = null;
            foreach (var t in m_Targets)
            {
                if (t != null && IsValid(t)) { valid = t; break; }
            }

            if (valid == null)
            {
                ResetProgress();
                return;
            }

            if (m_Lit != null && m_Lit != valid) m_Lit.SetIlluminated(false, 0f);
            m_Lit = valid;

            if (valid.Recorded)
            {
                valid.SetIlluminated(true, 1f);
                SetScanSound(true);
                return;
            }

            m_Progress += Time.deltaTime;
            valid.SetIlluminated(true, Progress01);
            SetScanSound(true);

            if (m_Progress >= m_RequiredSeconds)
                Complete(valid);
        }

        bool IsValid(UVTarget target)
        {
            var origin = m_ScanOrigin != null ? m_ScanOrigin : transform;
            Vector3 to = target.AimPoint - origin.position;
            float distance = to.magnitude;
            if (distance > m_MaxDistance || distance < 0.001f) return false;
            if (Vector3.Angle(origin.forward, to) > m_ConeHalfAngle) return false;

            // The first thing the ray hits must be the target itself, so walls and props block the scan.
            if (!Physics.Raycast(origin.position, to / distance, out var hit, distance + 0.05f, m_RayMask, QueryTriggerInteraction.Ignore))
                return false;
            return hit.collider.GetComponentInParent<UVTarget>() == target;
        }

        void Complete(UVTarget target)
        {
            var manager = CaseManager.Instance;
            if (manager != null && manager.RecordEvidence(target.Id))
            {
                AudioFx.PlayAt(AudioFx.Chime, target.transform.position);
                manager.Notify($"{target.Id} recorded: {CaseLibrary.Info(target.Id).Observation}");
                target.SetIlluminated(true, 1f);
            }
            else
            {
                // Not allowed yet (tutorial, wrong case): CaseManager already explained why. Restart the dwell.
                AudioFx.PlayAt(AudioFx.Error, target.transform.position);
                m_Progress = 0f;
            }
        }

        void ResetProgress()
        {
            m_Progress = 0f;
            if (m_Lit != null)
            {
                m_Lit.SetIlluminated(false, 0f);
                m_Lit = null;
            }
            SetScanSound(false);
        }

        void SetLight(bool on)
        {
            if (m_Light != null) m_Light.enabled = on;
        }

        void SetScanSound(bool on)
        {
            if (m_ScanAudio == null) return;
            if (on && !m_ScanAudio.isPlaying) m_ScanAudio.Play();
            else if (!on && m_ScanAudio.isPlaying) m_ScanAudio.Stop();
        }
    }
}
