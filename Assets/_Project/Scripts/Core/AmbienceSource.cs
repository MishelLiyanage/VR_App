using UnityEngine;

namespace CSIVR.Core
{
    /// <summary>Quiet looping room tone attached to a place in the room (the ceiling vent), not to the listener.</summary>
    [RequireComponent(typeof(AudioSource))]
    public class AmbienceSource : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float m_Volume = 0.5f;

        void Start()
        {
            var src = GetComponent<AudioSource>();
            src.clip = AudioFx.Ambience;
            src.loop = true;
            src.volume = m_Volume;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 1.5f;
            src.maxDistance = 9f;
            src.Play();
        }
    }
}
