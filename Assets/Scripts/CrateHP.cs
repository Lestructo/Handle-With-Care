using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CrateHP : MonoBehaviour
{
    public int maxHP = 10;
    public float velocityDamageThreshold = 4f;
    public float damageCooldown = 1f;
    public float pulseStartSpeed = 2f;
    public float pulseEndSpeed = 20f;
    public float crateDeathDuration = 1.5f;

    public Image[] hpBlocks;

    [Header("Audio")]
    public AudioClip damageSound;
    public AudioClip breakSound;
    public AudioClip pulseSound;
    public float damageVolume = 1f;
    public float breakVolume = 1f;
    public float pulseVolume = 1f;

    public bool Invulnerable { get; set; }

    int currentHP;
    float lastDamageTime = -999f;
    Coroutine pulseCoroutine;
    Coroutine outlinePulseCoroutine;
    Highlightable highlightable;
    Renderer crateRenderer;
    PlayerGrab playerGrab;
    AudioSource pulseAudioSource;

    void Start()
    {
        currentHP = maxHP;
        highlightable = GetComponent<Highlightable>();
        crateRenderer = GetComponentInChildren<Renderer>();
        playerGrab = FindFirstObjectByType<PlayerGrab>();

        pulseAudioSource = gameObject.AddComponent<AudioSource>();
        pulseAudioSource.playOnAwake = false;
        pulseAudioSource.spatialBlend = 1f;

        UpdateBlocks();
        UpdateCracks();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude >= velocityDamageThreshold)
            TakeDamage(1);
    }

    public void TakeDamage(int amount)
    {
        if (Invulnerable) return;
        if (Time.time - lastDamageTime < damageCooldown) return;
        lastDamageTime = Time.time;

        currentHP = Mathf.Max(0, currentHP - amount);
        playerGrab?.ForceRelease(gameObject);
        UpdateBlocks();
        UpdateCracks();

        if (currentHP <= 0)
        {
            Vector3 crateCenter = crateRenderer != null ? crateRenderer.bounds.center : transform.position;
            if (breakSound != null) AudioSource.PlayClipAtPoint(breakSound, crateCenter, breakVolume);
            OnDestroyed();
        }
        else
        {
            Vector3 crateCenter = crateRenderer != null ? crateRenderer.bounds.center : transform.position;
            if (damageSound != null) AudioSource.PlayClipAtPoint(damageSound, crateCenter, damageVolume);
            PulseRed();
            StartOutlinePulse();
        }
    }

    void UpdateBlocks()
    {
        if (hpBlocks == null) return;
        for (int i = 0; i < hpBlocks.Length; i++)
        {
            if (hpBlocks[i] == null) continue;
            hpBlocks[i].color = i < currentHP ? Color.white : Color.clear;
        }
    }

    void PulseRed()
    {
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(DoPulse());
    }

    IEnumerator DoPulse()
    {
        float t = 0f;
        while (t < damageCooldown)
        {
            t += Time.deltaTime;
            float frac = t / damageCooldown;
            Color blockColor = Color.Lerp(Color.red, Color.white, frac);
            if (hpBlocks != null)
                for (int i = 0; i < currentHP && i < hpBlocks.Length; i++)
                    if (hpBlocks[i] != null) hpBlocks[i].color = blockColor;
            crateRenderer?.material.SetColor("_CrackColor", Color.Lerp(Color.red, Color.white, frac));
            yield return null;
        }
        UpdateBlocks();
        crateRenderer?.material.SetColor("_CrackColor", Color.white);
    }

    void StartOutlinePulse()
    {
        if (highlightable == null) return;
        if (outlinePulseCoroutine != null) StopCoroutine(outlinePulseCoroutine);
        outlinePulseCoroutine = StartCoroutine(DoOutlinePulse());
    }

    IEnumerator DoOutlinePulse()
    {
        float elapsed = 0f;

        if (pulseSound != null)
        {
            pulseAudioSource.clip = pulseSound;
            pulseAudioSource.loop = true;
            pulseAudioSource.pitch = 1f;
            pulseAudioSource.volume = pulseVolume;
            pulseAudioSource.Play();
        }

        while (elapsed < damageCooldown)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / damageCooldown;
            float speed = Mathf.Lerp(pulseStartSpeed, pulseEndSpeed, t);
            float brightness = Mathf.Abs(Mathf.Sin(elapsed * speed));
            highlightable.SetHighlight(true, new Color(brightness, brightness, brightness, 1f));

            if (pulseSound != null)
                pulseAudioSource.pitch = Mathf.Lerp(1f, 3f, t);

            yield return null;
        }

        pulseAudioSource.Stop();
        pulseAudioSource.loop = false;
        highlightable.SetHighlight(false);
    }

    void UpdateCracks()
    {
        float damageAmount = currentHP <= 0 ? 0.999f : (1f - (float)currentHP / maxHP) * 0.9f;
        crateRenderer?.material.SetFloat("_DamageAmount", damageAmount);
    }

    void OnDestroyed()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;
        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;
        StartCoroutine(CrateDeath());
    }

    IEnumerator CrateDeath()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / crateDeathDuration;
            crateRenderer?.material.SetFloat("_WhiteOut", Mathf.Clamp01(t));
            yield return null;
        }
        FindFirstObjectByType<GameOver>()?.Trigger();
    }
}
