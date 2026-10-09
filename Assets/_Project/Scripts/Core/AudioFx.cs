using System;
using UnityEngine;

namespace CSIVR.Core
{
    /// <summary>
    /// Procedurally generated sounds, so the project ships no third-party audio and needs no attribution for it.
    /// Sources using these clips are 3D (Spatial Blend = 1); Unity's built-in panning, not an HRTF spatializer.
    /// </summary>
    public static class AudioFx
    {
        const int Rate = 44100;
        static AudioClip s_Chime, s_Click, s_Error, s_ScanLoop, s_Ambience;

        public static AudioClip Chime => s_Chime != null ? s_Chime : (s_Chime = Make("chime", 0.7f, t =>
            (Mathf.Sin(2f * Mathf.PI * 880f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * 1320f * t)) * 0.35f * Mathf.Exp(-5f * t) * Mathf.Min(1f, t * 400f)));

        public static AudioClip Click => s_Click != null ? s_Click : (s_Click = Make("click", 0.08f, t =>
            Mathf.Sin(2f * Mathf.PI * 600f * t) * 0.4f * Mathf.Exp(-45f * t)));

        public static AudioClip Error => s_Error != null ? s_Error : (s_Error = Make("error", 0.25f, t =>
            Mathf.Sin(2f * Mathf.PI * 200f * t) * 0.35f * Mathf.Exp(-9f * t)));

        // Whole number of cycles per second so the loop is seamless.
        public static AudioClip ScanLoop => s_ScanLoop != null ? s_ScanLoop : (s_ScanLoop = Make("scan-loop", 1f, t =>
            Mathf.Sin(2f * Mathf.PI * 520f * t) * (0.18f + 0.08f * Mathf.Sin(2f * Mathf.PI * 4f * t))));

        public static AudioClip Ambience
        {
            get
            {
                if (s_Ambience != null) return s_Ambience;
                var rng = new System.Random(7);
                float noise = 0f;
                s_Ambience = Make("ambience", 6f, t =>
                {
                    noise = noise * 0.97f + ((float)rng.NextDouble() * 2f - 1f) * 0.03f;
                    return Mathf.Sin(2f * Mathf.PI * 50f * t) * 0.10f + Mathf.Sin(2f * Mathf.PI * 100f * t) * 0.04f + noise * 0.25f;
                });
                return s_Ambience;
            }
        }

        public static void PlayAt(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip != null)
                AudioSource.PlayClipAtPoint(clip, position, volume);
        }

        /// <summary>Adds a 3D AudioSource with a sensible falloff.</summary>
        public static AudioSource AddSpatialSource(GameObject go, float minDistance = 1f, float maxDistance = 8f)
        {
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            return src;
        }

        static AudioClip Make(string name, float seconds, Func<float, float> sample)
        {
            int count = Mathf.CeilToInt(seconds * Rate);
            var data = new float[count];
            for (int i = 0; i < count; i++)
                data[i] = Mathf.Clamp(sample(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
