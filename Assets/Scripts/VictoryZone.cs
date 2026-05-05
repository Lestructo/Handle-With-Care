using UnityEngine;
using UnityEngine.UI;

public class VictoryZone : MonoBehaviour
{
    public SpotlightZone spotlightZone;
    public SlidingDoor door;
    public VictoryScreen victoryScreen;

    [Header("Lift")]
    public float liftSpeed = 3f;

    [Header("Door")]
    public float doorCloseSpeed = 0.5f;
    public float doorOpenDelay = 1f;

    [Header("Fade")]
    public Image fadeOverlay;
    public float fadeDuration = 2f;

    [Header("Audio")]
    public AudioClip victoryMusicClip;
    public float musicVolume = 1f;
    public float musicFadeInDuration = 2f;
    public float musicFadeOutDuration = 2f;

    Rigidbody crateRb;
    CrateHP crateHP;
    AudioSource musicAudioSource;
    bool victoryStarted;
    float fadeTimer;
    float currentDoorProgress;
    float musicFade;
    float zoneEntryTime = -1f;

    void Start()
    {
        if (spotlightZone != null && spotlightZone.crate != null)
        {
            crateRb = spotlightZone.crate.GetComponent<Rigidbody>();
            crateHP = spotlightZone.crate.GetComponent<CrateHP>();
        }

        musicAudioSource = gameObject.AddComponent<AudioSource>();
        musicAudioSource.loop = true;
        musicAudioSource.playOnAwake = false;
        musicAudioSource.spatialBlend = 0f;
        musicAudioSource.volume = 0f;
        musicAudioSource.clip = victoryMusicClip;

        if (fadeOverlay != null)
        {
            fadeOverlay.color = new Color(1f, 1f, 1f, 0f);
            fadeOverlay.raycastTarget = false;
        }
    }

    void FixedUpdate()
    {
        if (spotlightZone == null || crateRb == null) return;
        if (spotlightZone.CrateInZone && !spotlightZone.Triggered)
            crateRb.AddForce(Vector3.up * (Physics.gravity.magnitude + liftSpeed), ForceMode.Acceleration);
    }

    void Update()
    {
        if (spotlightZone == null) return;

        if (!spotlightZone.Triggered)
        {
            if (crateHP != null) crateHP.Invulnerable = spotlightZone.CrateInZone;

            // Door
            if (door != null)
            {
                if (spotlightZone.CrateInZone)
                {
                    float delayedProgress = Mathf.Max(0f, spotlightZone.Progress - doorOpenDelay / spotlightZone.holdDuration);
                    currentDoorProgress = Mathf.Clamp01(delayedProgress / 1.5f);
                }
                else
                    currentDoorProgress = Mathf.MoveTowards(currentDoorProgress, 0f, doorCloseSpeed * Time.deltaTime);
                door.SetOpenProgress(currentDoorProgress);
            }

            // Screen fade: starts at 25% progress, reaches full white at 100%
            if (fadeOverlay != null)
            {
                float p = spotlightZone.Progress;
                float alpha = spotlightZone.CrateInZone
                    ? p * p * p * p
                    : 0f;
                fadeOverlay.color = new Color(1f, 1f, 1f, alpha);
            }

            // Music fade in/out
            if (spotlightZone.CrateInZone)
            {
                if (zoneEntryTime < 0f)
                {
                    zoneEntryTime = Time.time;
                    if (victoryMusicClip != null) musicAudioSource.Play();
                }
                float elapsed = Time.time - zoneEntryTime;
                float fadeT = Mathf.Clamp01(elapsed / Mathf.Max(musicFadeInDuration, 0.01f));
                musicFade = fadeT * fadeT * fadeT;
            }
            else
            {
                if (zoneEntryTime >= 0f) zoneEntryTime = -1f;
                musicFade = Mathf.MoveTowards(musicFade, 0f, Time.deltaTime / Mathf.Max(musicFadeOutDuration, 0.01f));
                if (musicFade <= 0f && musicAudioSource.isPlaying) musicAudioSource.Stop();
            }
            musicAudioSource.volume = musicFade * musicVolume;

            return;
        }

        // Victory sequence
        if (!victoryStarted)
        {
            victoryStarted = true;
            if (crateHP != null) crateHP.Invulnerable = true;
            GameTimer.Instance?.StopTimer();
            if (victoryMusicClip != null && !musicAudioSource.isPlaying)
                musicAudioSource.Play();
        }

        fadeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(fadeTimer / fadeDuration);

        // Transition overlay from white to black, covering all HUD elements
        if (fadeOverlay != null)
        {
            float grey = 1f - t;
            fadeOverlay.color = new Color(grey, grey, grey, 1f);
        }

        AudioListener.volume = Mathf.Lerp(1f, 0f, t);

        if (fadeTimer >= fadeDuration)
        {
            victoryScreen?.Trigger();
            enabled = false;
        }
    }
}
