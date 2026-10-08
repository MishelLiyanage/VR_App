using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CSIVR.Evidence
{
    /// <summary>Tints an interactable while it is hovered, so the player can tell it can be grabbed.</summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class InteractableHighlight : MonoBehaviour
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int Color_ = Shader.PropertyToID("_Color");

        [SerializeField] Color m_HoverTint = new Color(1f, 0.9f, 0.3f);
        [SerializeField, Range(0f, 1f)] float m_Strength = 0.45f;

        XRBaseInteractable m_Interactable;
        Renderer[] m_Renderers;
        readonly List<Color> m_Original = new List<Color>();
        MaterialPropertyBlock m_Block;

        void Awake()
        {
            m_Interactable = GetComponent<XRBaseInteractable>();
            m_Renderers = GetComponentsInChildren<Renderer>();
            m_Block = new MaterialPropertyBlock();
            foreach (var r in m_Renderers)
            {
                var m = r.sharedMaterial;
                m_Original.Add(m != null && m.HasProperty(BaseColor) ? m.GetColor(BaseColor) : Color.white);
            }
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
            Apply(false);
        }

        void OnHoverEntered(UnityEngine.XR.Interaction.Toolkit.HoverEnterEventArgs _) => Apply(true);
        void OnHoverExited(UnityEngine.XR.Interaction.Toolkit.HoverExitEventArgs _) => Apply(m_Interactable.isHovered);

        void Apply(bool on)
        {
            for (int i = 0; i < m_Renderers.Length; i++)
            {
                if (m_Renderers[i] == null) continue;
                if (!on)
                {
                    m_Renderers[i].SetPropertyBlock(null);
                    continue;
                }
                var tinted = Color.Lerp(m_Original[i], m_HoverTint, m_Strength);
                m_Renderers[i].GetPropertyBlock(m_Block);
                m_Block.SetColor(BaseColor, tinted);
                m_Block.SetColor(Color_, tinted);
                m_Renderers[i].SetPropertyBlock(m_Block);
            }
        }
    }
}
