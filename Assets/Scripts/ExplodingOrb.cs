using System.Collections;
using UnityEngine;

public class ExplodingOrb : MonoBehaviour
{
    public float explosionRadius = 4f;
    public float explosionForce = 600f;
    public float upwardModifier = 0.5f;
    public float lifetime = 8f;
    public ParticleSystem contactVFX;
    public Light explosionLight;
    public float lightFadeDuration = 0.4f;
    public float vfxFadeDuration = 1f;

    [Header("Audio")]
    public AudioClip explosionSound;
    public float explosionVolume = 1f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        Explode(collision.gameObject);
    }

    void Explode(GameObject hitObject)
    {
        // release the player's grab/magnetize before applying forces so the object flies free
        PlayerGrab grab = FindFirstObjectByType<PlayerGrab>();
        if (grab != null)
        {
            grab.ForceRelease(hitObject);
            grab.ForceReleaseInRadius(transform.position, explosionRadius);
        }

        // apply radial impulse to every rigidbody in range, skipping the robot
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.GetComponentInParent<RobotEnemy>() != null) continue;
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardModifier, ForceMode.Impulse);
            hit.GetComponent<CrateHP>()?.TakeDamage(1);
        }

        if (explosionSound != null)
        {
            Collider col = GetComponent<Collider>();
            Vector3 center = col != null ? col.bounds.center : transform.position;
            AudioSource.PlayClipAtPoint(explosionSound, center, explosionVolume);
        }

        if (contactVFX != null)
        {
            // detach VFX before destroying so the particle effect can finish playing
            contactVFX.transform.SetParent(null);
            contactVFX.Play();
            Destroy(contactVFX.gameObject, vfxFadeDuration);
        }

        if (explosionLight != null)
        {
            // detach light so it survives the orb being destroyed, then fade it out
            explosionLight.transform.SetParent(null);
            GameObject lightObj = explosionLight.gameObject;
            IEnumerator Fade()
            {
                float start = explosionLight.intensity;
                float t = 0f;
                while (t < lightFadeDuration)
                {
                    t += Time.deltaTime;
                    explosionLight.intensity = Mathf.Lerp(start, 0f, t / lightFadeDuration);
                    yield return null;
                }
                Destroy(lightObj);
            }
            lightObj.AddComponent<CoroutineRunner>().Run(Fade());
        }

        Destroy(gameObject);
    }
}
