using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] Renderer m_LampRenderer;
    [SerializeField, Min(0f)] float m_MinPause = 1.5f;
    [SerializeField, Min(0f)] float m_MaxPause = 4f;
    [SerializeField, Min(1)] int m_MinPulses = 2;
    [SerializeField, Min(1)] int m_MaxPulses = 4;
    [SerializeField, Min(0f)] float m_MinPulseDuration = 0.05f;
    [SerializeField, Min(0f)] float m_MaxPulseDuration = 0.14f;

    Light m_Light;
    float m_OriginalIntensity;
    Color m_OriginalEmissionColor;
    MaterialPropertyBlock m_PropertyBlock;
    Coroutine m_FlickerRoutine;

    void Awake()
    {
        m_Light = GetComponent<Light>();
        m_OriginalIntensity = m_Light.intensity;
        if (m_LampRenderer != null && m_LampRenderer.sharedMaterial.HasProperty(EmissionColorId))
        {
            m_OriginalEmissionColor = m_LampRenderer.sharedMaterial.GetColor(EmissionColorId);
            m_PropertyBlock = new MaterialPropertyBlock();
            m_LampRenderer.GetPropertyBlock(m_PropertyBlock);
        }
    }

    void OnEnable()
    {
        m_FlickerRoutine = StartCoroutine(Flicker());
    }

    void OnDisable()
    {
        if (m_FlickerRoutine != null)
            StopCoroutine(m_FlickerRoutine);
        m_FlickerRoutine = null;

        if (m_Light != null)
            SetLit(true);
    }

    IEnumerator Flicker()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(m_MinPause, m_MaxPause));

            int pulseCount = Random.Range(m_MinPulses, m_MaxPulses + 1);
            for (int i = 0; i < pulseCount; i++)
            {
                SetLit(false);
                yield return new WaitForSeconds(Random.Range(m_MinPulseDuration, m_MaxPulseDuration));
                SetLit(true);
                yield return new WaitForSeconds(Random.Range(m_MinPulseDuration, m_MaxPulseDuration));
            }
        }
    }

    void SetLit(bool lit)
    {
        m_Light.intensity = lit ? m_OriginalIntensity : 0f;
        if (m_PropertyBlock == null)
            return;

        m_PropertyBlock.SetColor(EmissionColorId, lit ? m_OriginalEmissionColor : Color.black);
        m_LampRenderer.SetPropertyBlock(m_PropertyBlock);
    }
}
