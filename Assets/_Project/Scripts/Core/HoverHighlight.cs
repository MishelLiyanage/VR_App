using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CSIVR.Core
{
    /// <summary>
    /// Tints an interactable while any interactor hovers it (VR ray/near, simulator and the desktop ray).
    /// Added automatically by EvidenceItem and UVScanner, so existing prefabs need no changes.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class HoverHighlight : MonoBehaviour
    {
        [SerializeField] Color m_HoverColor = new Color(1f, 0.9f, 0.2f);

        XRBaseInteractable m_Interactable;
        MeshRenderer[] m_Renderers;
        MaterialPropertyBlock m_Block;
        int m_HoverCount;

        void Awake()
        {
            m_Interactable = GetComponent<XRBaseInteractable>();
            m_Renderers = GetComponentsInChildren<MeshRenderer>();
            m_Block = new MaterialPropertyBlock();
        }

        void OnEnable()
        {
            m_Interactable.hoverEntered.AddListener(OnHoverEntered);
            m_Interactable.hoverExited.AddListener(OnHoverExited);
        }

        void OnDisable()
        {
            m_Interactable.hoverEntered.RemoveListener(OnHoverEntered);
            m_Interactable.hoverExited.RemoveListener(OnHoverExited);
            m_HoverCount = 0;
            Apply(false);
        }

        void OnHoverEntered(HoverEnterEventArgs _) { m_HoverCount++; Apply(true); }

        void OnHoverExited(HoverExitEventArgs _)
        {
            m_HoverCount = Mathf.Max(0, m_HoverCount - 1);
            if (m_HoverCount == 0) Apply(false);
        }

        void Apply(bool on)
        {
            foreach (var r in m_Renderers)
            {
                if (r == null) continue;
                if (!on) { r.SetPropertyBlock(null); continue; }
                r.GetPropertyBlock(m_Block);
                m_Block.SetColor("_BaseColor", m_HoverColor); // URP Lit
                m_Block.SetColor("_Color", m_HoverColor);     // built-in fallback
                r.SetPropertyBlock(m_Block);
            }
        }
    }
}
