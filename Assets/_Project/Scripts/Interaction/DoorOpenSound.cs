using UnityEngine;

[RequireComponent(typeof(HingeJoint), typeof(AudioSource))]
public class DoorOpenSound : MonoBehaviour
{
    [SerializeField] AudioClip openingClip;
    [SerializeField, Min(0.01f)] float openAngleThreshold = 1f;

    HingeJoint hinge;
    AudioSource audioSource;
    float previousAbsoluteAngle;
    bool hasPlayed;

    void Awake()
    {
        hinge = GetComponent<HingeJoint>();
        audioSource = GetComponent<AudioSource>();

        previousAbsoluteAngle = Mathf.Abs(hinge.angle);
        hasPlayed = previousAbsoluteAngle >= openAngleThreshold;

        if (openingClip == null)
            Debug.LogWarning($"Assign an opening sound to {name}.", this);
    }

    void FixedUpdate()
    {
        float currentAbsoluteAngle = Mathf.Abs(hinge.angle);
        bool opening = currentAbsoluteAngle > previousAbsoluteAngle + 0.02f;

        if (!hasPlayed && opening && currentAbsoluteAngle >= openAngleThreshold)
        {
            if (openingClip != null)
                audioSource.PlayOneShot(openingClip);

            hasPlayed = true;
        }

        if (currentAbsoluteAngle < openAngleThreshold * 0.5f)
            hasPlayed = false;

        previousAbsoluteAngle = currentAbsoluteAngle;
    }
}