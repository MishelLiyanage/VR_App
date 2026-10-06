using System.Collections.Generic;
using CSIVR.Core;
using TMPro;
using UnityEngine;

namespace CSIVR.Evidence
{
    /// <summary>
    /// Member 3. Lives on the object that owns the trigger collider ("Record Zone").
    /// A released, eligible item inside the zone is recorded exactly once.
    /// </summary>
    public class EvidenceStation : MonoBehaviour
    {
        [SerializeField] Transform[] m_DisplaySlots;
        [SerializeField] TMP_Text m_Label;
        [SerializeField] AudioSource m_ConfirmSound;
        [SerializeField] int m_TotalEvidence = 4;

        readonly HashSet<EvidenceItem> m_Inside = new HashSet<EvidenceItem>();
        int m_NextSlot;
        bool m_Prompted;
        string m_Message = "Place evidence on the tray";

        void Start()
        {
            if (CaseManager.Instance != null)
                CaseManager.Instance.OnEvidenceRecorded.AddListener(OnRecorded);
            Refresh();
        }

        void OnDestroy()
        {
            if (CaseManager.Instance != null)
                CaseManager.Instance.OnEvidenceRecorded.RemoveListener(OnRecorded);
        }

        void OnTriggerEnter(Collider other)
        {
            var item = other.GetComponentInParent<EvidenceItem>();
            if (item != null && !item.RequiresUV && !item.Recorded)
                m_Inside.Add(item);
        }

        void OnTriggerExit(Collider other)
        {
            var item = other.GetComponentInParent<EvidenceItem>();
            if (item != null) m_Inside.Remove(item);
        }

        void Update()
        {
            if (m_Inside.Count == 0) { m_Prompted = false; return; }

            bool anyHeld = false;
            foreach (var item in new List<EvidenceItem>(m_Inside)) // copy: the set changes below
            {
                if (item == null || item.Recorded) { m_Inside.Remove(item); continue; }
                if (item.IsHeld) { anyHeld = true; continue; }   // wait for release
                TryAccept(item);
                m_Inside.Remove(item);
            }

            if (anyHeld && !m_Prompted) { m_Message = "Release to record"; Refresh(); }
            m_Prompted = anyHeld;
        }

        void TryAccept(EvidenceItem item)
        {
            var cm = CaseManager.Instance;
            if (cm == null) return;

            if (!cm.RecordEvidence(item.EvidenceId))
            {
                m_Message = item.Title + " is already recorded";
                Refresh();
                return;
            }

            Transform slot = (m_DisplaySlots != null && m_NextSlot < m_DisplaySlots.Length)
                ? m_DisplaySlots[m_NextSlot++] : null;
            item.MarkRecorded(slot);

            if (m_ConfirmSound != null) m_ConfirmSound.Play();
            m_Message = "Recorded: " + item.Title;
            Refresh();
        }

        void OnRecorded(string id)
        {
            m_Message = "Record logged: " + id;
            Refresh();
        }

        void Refresh()
        {
            if (m_Label == null) return;
            int count = CaseManager.Instance != null ? CaseManager.Instance.RecordedCount : 0;
            m_Label.text = $"{m_Message}\n{count}/{m_TotalEvidence} recorded";
        }
    }
}
