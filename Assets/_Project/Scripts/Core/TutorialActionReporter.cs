using UnityEngine;

namespace CSIVR.Core
{
    /// <summary>UnityEvent bridge for XR teleport, grab-release and activate events.</summary>
    public sealed class TutorialActionReporter : MonoBehaviour
    {
        public void ReportTeleport() => Report(TutorialAction.Teleport);
        public void ReportGrabRelease() => Report(TutorialAction.GrabRelease);
        public void ReportToolActivation() => Report(TutorialAction.ActivateTool);

        public void Report(TutorialAction action)
        {
            if (CaseManager.Instance != null)
                CaseManager.Instance.ReportTutorialAction(action);
        }
    }
}
