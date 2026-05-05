using UnityEngine;

public class SpotlightZone : MonoBehaviour
{
    public Light spotlight;
    public SlidingDoor door;
    public float holdDuration = 3f;
    public GameObject crate;

    public float pulseMinMultiplier = 0.2f;
    public float pulseMaxMultiplier = 3f;
    public float pulseStartSpeed = 1f;
    public float pulseEndSpeed = 8f;

    public float edgMin = 0f;
    public float edgMax = 1f;

    [Header("Audio")]
    public AudioClip scanLoopClip;
    public float scanVolume = 1f;

    public float Progress { get; private set; }
    public bool Triggered { get; private set; }
    public bool CrateInZone { get; private set; }

    float timer;
    [SerializeField] float baseIntensity;
    float baseEdg;
    Material coneMaterial;

    AudioSource scanAudioSource;
    bool scanSoundPlaying;

    void Start()
    {
        if (spotlight != null) baseIntensity = spotlight.intensity;
        Renderer r = GetComponent<Renderer>();
        if (r)
        {
            coneMaterial = r.material;
            baseEdg = coneMaterial.GetFloat("_Edg");
        }

        scanAudioSource = gameObject.AddComponent<AudioSource>();
        scanAudioSource.loop = true;
        scanAudioSource.playOnAwake = false;
        scanAudioSource.spatialBlend = 1f;
        scanAudioSource.clip = scanLoopClip;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == crate) { CrateInZone = true; timer = 0f; }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject == crate) CrateInZone = false;
    }

    void Update()
    {
        bool shouldScan = CrateInZone && !Triggered;

        if (shouldScan && !scanSoundPlaying && scanLoopClip != null)
        {
            scanAudioSource.Play();
            scanSoundPlaying = true;
        }
        else if (!shouldScan && scanSoundPlaying)
        {
            scanAudioSource.Stop();
            scanSoundPlaying = false;
        }

        if (Triggered || spotlight == null || door == null) return;

        if (CrateInZone)
        {
            scanAudioSource.volume = scanVolume;
            timer += Time.deltaTime;

            Progress = Mathf.Clamp01(timer / holdDuration);
            float speed = Mathf.Lerp(pulseStartSpeed, pulseEndSpeed, Progress);
            float pulse = Mathf.Abs(Mathf.Sin(timer * speed));
            spotlight.intensity = Mathf.Lerp(baseIntensity * pulseMinMultiplier, baseIntensity * pulseMaxMultiplier, pulse);
            coneMaterial?.SetFloat("_Edg", Mathf.Lerp(edgMin, edgMax, pulse));
            scanAudioSource.pitch = Mathf.Lerp(1f, 3f, Progress);

            if (timer >= holdDuration)
            {
                Triggered = true;
                spotlight.intensity = baseIntensity;
                coneMaterial?.SetFloat("_Edg", baseEdg);
                door.LockOpen();
            }
        }
        else
        {
            timer = 0f;
            Progress = 0f;
            scanAudioSource.pitch = 1f;
            spotlight.intensity = baseIntensity;
            coneMaterial?.SetFloat("_Edg", baseEdg);
        }
    }
}
