using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class PhysicsSound : MonoBehaviour
{
    public AudioClip impactSound;
    [Range(0f, 20f)] public float minImpactVelocity = 2f;
    public float cooldown = 0.2f;

    AudioSource audioSource;
    float lastPlayTime = -999f;

    void Awake() => audioSource = GetComponent<AudioSource>();

    void OnCollisionEnter(Collision collision)
    {
        if (Time.time - lastPlayTime < cooldown) return;
        float speed = collision.relativeVelocity.magnitude;
        if (speed < minImpactVelocity) return;

        lastPlayTime = Time.time;
        float volume = Mathf.Clamp01(speed / 10f);
        audioSource.PlayOneShot(impactSound, volume);
    }
}
