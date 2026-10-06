using UnityEngine;

namespace CSIVR.Evidence
{
    /// <summary>
    /// Member 3. Two uses:
    /// 1) On an object with a big trigger collider below the floor: items that fall out return home.
    /// 2) On any object: RestoreLooseItems() is wired to the "Restore loose items" button.
    /// Held and recorded items are never touched (EvidenceItem.ResetToHome checks).
    /// </summary>
    public class ObjectRecovery : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            var item = other.GetComponentInParent<EvidenceItem>();
            if (item != null) item.ResetToHome();
        }

        public void RestoreLooseItems()
        {
            foreach (var item in FindObjectsByType<EvidenceItem>(FindObjectsSortMode.None))
                item.ResetToHome();
        }
    }
}
