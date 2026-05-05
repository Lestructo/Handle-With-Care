using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    public Transform targetObject;
    public float proximityRange = 5f;
    public Transform closeObject;
    public float closeRange = 5f;
    public float slideSpeed = 2f;
    public float slideHeight = 3f;

    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public float openVolume = 1f;
    public float closeVolume = 1f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Vector3 doorCenter;
    private bool locked;
    private bool wasMoving;
    private bool wasOpening;
    private bool progressOverride;

    void Start()
    {
        closedPosition = transform.localPosition;
        openPosition = closedPosition + Vector3.up * slideHeight;
        Collider col = GetComponent<Collider>();
        doorCenter = col != null ? col.bounds.center : transform.position;
    }

    public void LockOpen()
    {
        locked = true;
        progressOverride = false;
    }

    public void SetOpenProgress(float t)
    {
        progressOverride = true;
        transform.localPosition = Vector3.Lerp(closedPosition, openPosition, Mathf.Clamp01(t));
    }

    void Update()
    {
        if (progressOverride) return;

        bool inRange;
        if (closeObject != null)
            inRange = Vector3.Distance(doorCenter, closeObject.position) > closeRange;
        else
            inRange = locked || (targetObject != null && Vector3.Distance(doorCenter, targetObject.position) <= proximityRange);

        Vector3 target = inRange ? openPosition : closedPosition;
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, slideSpeed * Time.deltaTime);

        bool isMoving = Vector3.Distance(transform.localPosition, target) > 0.01f;

        if (isMoving && !wasMoving)
        {
            AudioClip clip = inRange ? openSound : closeSound;
            float vol = inRange ? openVolume : closeVolume;
            if (clip != null) AudioSource.PlayClipAtPoint(clip, doorCenter, vol);
        }

        wasMoving = isMoving;
        wasOpening = inRange;
    }
}
