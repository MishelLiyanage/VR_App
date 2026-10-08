using System.Collections;
using CSIVR.Core;
using TMPro;
using UnityEngine;

namespace CSIVR.Core
{
    /// <summary>
    /// A door that stays locked until the given case is solved correctly, then swings open. While locked the
    /// reader glows red and the tape and sign stay up; unlocking turns the reader green, removes the tape and
    /// activates the next room's teleport anchors.
    /// </summary>
    public class RoomDoor : MonoBehaviour
    {
        [SerializeField] int m_UnlockAfterCase;
        [SerializeField] Transform m_Hinge;
        [SerializeField] float m_OpenAngle = -95f;
        [SerializeField] float m_OpenSeconds = 1.3f;
        [SerializeField] Renderer m_Reader;
        [SerializeField] Material m_LockedMaterial;
        [SerializeField] Material m_UnlockedMaterial;
        [SerializeField] TextMeshPro m_Sign;
        [SerializeField] string m_LockedText = "LOCKED\nSubmit your finding";
        [SerializeField] string m_UnlockedText = "DOOR OPEN";
        [SerializeField] GameObject[] m_HideOnUnlock;
        [SerializeField] GameObject[] m_ActivateOnUnlock;
        [SerializeField] AudioSource m_Audio;

        public bool IsOpen { get; private set; }

        void OnEnable()
        {
            if (CaseManager.Instance != null) CaseManager.Instance.CaseSolved += OnCaseSolved;
        }

        void OnDisable()
        {
            if (CaseManager.Instance != null) CaseManager.Instance.CaseSolved -= OnCaseSolved;
        }

        void Start()
        {
            if (m_Reader != null && m_LockedMaterial != null) m_Reader.sharedMaterial = m_LockedMaterial;
            if (m_Sign != null) m_Sign.text = m_LockedText;
            foreach (var go in m_ActivateOnUnlock)
                if (go != null) go.SetActive(false);
        }

        void OnCaseSolved(int caseIndex)
        {
            if (caseIndex == m_UnlockAfterCase) Unlock();
        }

        public void Unlock()
        {
            if (IsOpen) return;
            IsOpen = true;

            if (m_Reader != null && m_UnlockedMaterial != null) m_Reader.sharedMaterial = m_UnlockedMaterial;
            if (m_Sign != null) m_Sign.text = m_UnlockedText;
            foreach (var go in m_HideOnUnlock)
                if (go != null) go.SetActive(false);
            foreach (var go in m_ActivateOnUnlock)
                if (go != null) go.SetActive(true);

            if (m_Audio != null) m_Audio.PlayOneShot(AudioFx.Chime);
            else AudioFx.PlayAt(AudioFx.Chime, transform.position);
            StartCoroutine(Swing());
        }

        IEnumerator Swing()
        {
            if (m_Hinge == null) yield break;
            var start = m_Hinge.localRotation;
            var end = start * Quaternion.Euler(0f, m_OpenAngle, 0f);
            for (float t = 0f; t < m_OpenSeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / m_OpenSeconds);
                m_Hinge.localRotation = Quaternion.Slerp(start, end, k);
                yield return null;
            }
            m_Hinge.localRotation = end;
        }
    }
}
