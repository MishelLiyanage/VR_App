using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace CSIVR.Input
{
    public enum RigMode
    {
        /// <summary>Editor: XR Interaction Simulator. Player build: Desktop (keyboard and mouse).</summary>
        Auto,
        EditorSimulator,
        Desktop,
        Headset,
    }

    /// <summary>
    /// Chooses one input/pose driver at startup so the XR rig, the desktop rig and the
    /// simulator never run at the same time and only one AudioListener stays active.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class ModeBootstrap : MonoBehaviour
    {
        [SerializeField] RigMode m_Mode = RigMode.Auto;
        [SerializeField] GameObject m_XRRig;
        [SerializeField] GameObject m_DesktopRig;

        public static RigMode ActiveMode { get; private set; }

        void Awake()
        {
            ActiveMode = Resolve(m_Mode);

            bool useXR = ActiveMode != RigMode.Desktop;
            if (m_XRRig != null)
                m_XRRig.SetActive(useXR);
            if (m_DesktopRig != null)
                m_DesktopRig.SetActive(!useXR);

            // The simulator auto-loads in the Editor; it must not run alongside the desktop rig,
            // and a headset build does not need it.
            if (ActiveMode == RigMode.Desktop || ActiveMode == RigMode.Headset)
            {
                var sim = FindAnyObjectByType<XRInteractionSimulator>(FindObjectsInactive.Include);
                if (sim != null)
                    Destroy(sim.gameObject);
            }

            // "Initialize XR on Startup" is off so the Desktop build runs without a headset.
            // Headset mode therefore starts the XR loader itself.
            if (ActiveMode == RigMode.Headset)
                StartCoroutine(InitializeXR());

            Debug.Log($"[ModeBootstrap] Active mode: {ActiveMode}");
        }

        static IEnumerator InitializeXR()
        {
            var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (manager == null)
                yield break;

            if (manager.activeLoader == null)
                yield return manager.InitializeLoader();

            if (manager.activeLoader != null)
                manager.StartSubsystems();
            else
                Debug.LogWarning("[ModeBootstrap] Headset mode requested but no XR loader could start.");
        }

        void OnDestroy()
        {
            if (ActiveMode == RigMode.Headset && XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
            {
                var manager = XRGeneralSettings.Instance.Manager;
                if (manager.isInitializationComplete)
                {
                    manager.StopSubsystems();
                    manager.DeinitializeLoader();
                }
            }
        }

        static RigMode Resolve(RigMode requested)
        {
            if (requested != RigMode.Auto)
                return requested;
#if UNITY_EDITOR
            return RigMode.EditorSimulator;
#else
            return RigMode.Desktop;
#endif
        }
    }
}
