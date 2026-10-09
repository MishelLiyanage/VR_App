using System.Collections;
using CSIVR.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CSIVR.Evidence
{
    /// <summary>
    /// A tray with a trigger volume. A released item inside it is recorded once; an item still in hand only
    /// gets a "Release to record" prompt. UV evidence is logged by scanning and is rejected here.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EvidenceStation : MonoBehaviour
    {
        [Tooltip("Which case this tray belongs to (0 = first room).")]
        [SerializeField] int m_CaseIndex;
        [Tooltip("Display positions for the first three evidence items of this case, in order.")]
        [SerializeField] Transform[] m_Slots;
        [SerializeField] AudioSource m_Audio;

        const float MessageCooldown = 1.5f;
        float m_NextMessageTime;

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerStay(Collider other)
        {
            var item = other.GetComponentInParent<EvidenceItem>();
            var manager = CaseManager.Instance;
            if (item == null || manager == null || item.Recorded)
                return;

            if (CaseLibrary.CaseOf(item.Id) != m_CaseIndex)
            {
                Say(manager, "This tray belongs to a different case.");
                return;
            }

            if (m_CaseIndex != manager.CaseIndex)
            {
                Say(manager, m_CaseIndex < manager.CaseIndex ? "This case is already closed." : "Solve the earlier case first.");
                return;
            }

            if (item.RequiresUV)
            {
                Say(manager, "This evidence is logged with the UV scanner.");
                return;
            }

            var grab = item.GetComponent<XRGrabInteractable>();
            if (grab != null && grab.isSelected)
            {
                Say(manager, $"Release to record: {item.Title}");
                return;
            }

            if (manager.State != CaseState.Investigating && manager.State != CaseState.ReadyToSubmit)
            {
                Say(manager, manager.State == CaseState.Tutorial ? "Finish the practice steps on the board first." : "Press Start on the board first.");
                return;
            }

            if (manager.RecordEvidence(item.Id))
            {
                Accept(item, grab);
                manager.Notify($"{item.Id} recorded: {CaseLibrary.Info(item.Id).Observation}");

                // The access card is also run through the staff registry, like a fingerprint against a database.
                var cardId = SuspectRegistry.CardIdFor(item.Id);
                if (cardId != null)
                    StartCoroutine(RunRegistryLookup(manager, cardId));
            }
        }

        IEnumerator RunRegistryLookup(CaseManager manager, string cardId)
        {
            // Let the "recorded" note be read first, then show a short search before the result.
            yield return new WaitForSeconds(1.2f);

            const int steps = 8;
            for (int i = 1; i <= steps; i++)
            {
                manager.Notify($"Searching staff card registry...  {i * 100 / steps}%");
                AudioFx.PlayAt(AudioFx.Click, transform.position, 0.5f);
                yield return new WaitForSeconds(0.3f);
            }

            var record = SuspectRegistry.FindByCard(cardId);
            if (record == null)
            {
                manager.Notify($"Card {cardId}: no match in the staff registry.");
                yield break;
            }

            manager.ReportCardHolder(record);
            AudioFx.PlayAt(AudioFx.Chime, transform.position);
            manager.Notify(SuspectRegistry.ResultText(record));
        }

        void Accept(EvidenceItem item, XRGrabInteractable grab)
        {
            // Wait until released (checked by the caller), then lock the item in its display slot.
            var body = item.GetComponent<Rigidbody>();
            if (body != null)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                body.isKinematic = true;
                body.useGravity = false;
            }

            // Disabled so XRI never restores the old kinematic state if someone aims at it again.
            if (grab != null) grab.enabled = false;

            var slot = SlotFor(item.Id);
            if (slot != null)
            {
                item.transform.SetPositionAndRotation(slot.position, slot.rotation);
                if (body != null)
                {
                    body.position = slot.position;
                    body.rotation = slot.rotation;
                }
            }

            if (m_Audio != null) m_Audio.PlayOneShot(AudioFx.Chime);
            else AudioFx.PlayAt(AudioFx.Chime, transform.position);
        }

        Transform SlotFor(string id)
        {
            if (m_Slots == null) return null;
            int index = CaseLibrary.Get(m_CaseIndex).IndexOf(id);
            return index >= 0 && index < m_Slots.Length ? m_Slots[index] : null;
        }

        void Say(CaseManager manager, string text)
        {
            if (Time.time < m_NextMessageTime) return;
            m_NextMessageTime = Time.time + MessageCooldown;
            manager.Notify(text);
        }
    }
}
