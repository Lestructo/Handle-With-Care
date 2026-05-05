using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class RobotEnemy : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform[] waypoints;
    public float moveSpeed = 3f;
    public float waypointReachedDistance = 0.5f;
    public float pathCorrectionStrength = 3f;

    [Header("Detection")]
    public Transform eyeTransform;
    public Transform target;
    public float detectionRange = 15f;
    public float detectionFOV = 90f;
    public float eyeSweepSpeed = 60f;
    public float eyeSweepAngle = 45f;

    [Header("Shooting")]
    public Transform cannonTransform;
    public GameObject orbPrefab;
    public float shootInterval = 2f;
    public float orbSpeed = 10f;
    public float pursuitSpeed = 4f;
    public float pursuitStopDistance = 5f;
    public float pursuitBackUpDistance = 3.5f;
    public float pursuitCorrectionScale = 0.3f;
    public float rotationDamping = 5f;
    public float moveSmoothing = 5f;
    public float searchDuration = 5f;

    [Header("VFX")]
    public ParticleSystem muzzleVFX;

    [Header("Bob")]
    public float bobHeight = 0.15f;
    public float bobSpeed = 2f;

    [Header("Audio")]
    public AudioClip wanderLoopClip;
    public AudioClip pursuitLoopClip;
    public AudioClip shootSound;
    public float wanderVolume = 1f;
    public float shootVolume = 1f;

    private enum State { Wander, Shoot, Search }
    private State state = State.Wander;
    private int currentWaypoint;
    private float shootTimer;
    private float eyeSweepT;
    private Rigidbody rb;
    private float baseY;
    private Vector3 smoothedVelocity;
    private Vector3 smoothedTargetPos;
    private Vector3 lastKnownTargetPos;
    private float searchTimer;
    private Vector3 previousWaypointPos;

    AudioSource wanderAudioSource;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY
                       | RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ;

        var frictionless = new PhysicsMaterial { dynamicFriction = 0f, staticFriction = 0f, frictionCombine = PhysicsMaterialCombine.Minimum };
        GetComponent<CapsuleCollider>().material = frictionless;

        baseY = transform.position.y;
        previousWaypointPos = transform.position;

        wanderAudioSource = gameObject.AddComponent<AudioSource>();
        wanderAudioSource.loop = true;
        wanderAudioSource.playOnAwake = false;
        wanderAudioSource.spatialBlend = 1f;
    }

    void Update()
    {
        bool canSeePlayer = CanSeePlayer();

        if (canSeePlayer && state != State.Shoot)
        {
            state = State.Shoot;
            shootTimer = shootInterval;
            smoothedTargetPos = target.position;
        }
        else if (!canSeePlayer && state == State.Shoot)
        {
            state = State.Search;
            lastKnownTargetPos = smoothedTargetPos;
            searchTimer = searchDuration;
        }

        if (state == State.Search)
        {
            searchTimer -= Time.deltaTime;
            if (searchTimer <= 0f)
            {
                state = State.Wander;
                currentWaypoint = NearestWaypoint();
                int prevIndex = (currentWaypoint - 1 + waypoints.Length) % waypoints.Length;
                previousWaypointPos = waypoints[prevIndex].position;
            }
        }

        wanderAudioSource.volume = wanderVolume;
        UpdateWanderSound();

        if (state == State.Wander)
            DoWander();
        else if (state == State.Shoot)
            DoShoot();
        else
            DoSearch();
    }

    void UpdateWanderSound()
    {
        if (wanderAudioSource == null) return;
        AudioClip desired = state == State.Wander ? wanderLoopClip
                          : state == State.Shoot  ? pursuitLoopClip
                          : null;

        if (desired == null)
        {
            wanderAudioSource.Stop();
            return;
        }

        if (wanderAudioSource.clip != desired)
        {
            wanderAudioSource.clip = desired;
            wanderAudioSource.Play();
        }
        else if (!wanderAudioSource.isPlaying)
        {
            wanderAudioSource.Play();
        }
    }

    bool CanSeePlayer()
    {
        if (target == null) return false;
        Vector3 targetPos = target.position + Vector3.up * 0.5f;
        Vector3 toTarget = targetPos - eyeTransform.position;
        if (toTarget.magnitude > detectionRange) return false;
        Vector3 flatForward = new Vector3(eyeTransform.forward.x, 0, eyeTransform.forward.z);
        Vector3 flatToTarget = new Vector3(toTarget.x, 0, toTarget.z);
        if (Vector3.Angle(flatForward, flatToTarget) > detectionFOV * 0.5f) return false;

        RaycastHit[] hits = Physics.RaycastAll(eyeTransform.position, toTarget.normalized, toTarget.magnitude);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            if (hit.collider.GetComponentInParent<ExplodingOrb>() != null) continue;
            if (hit.transform != target) return false;
        }

        return true;
    }

    void Move(Vector3 dir, float speed)
    {
        Vector3 desiredVelocity = dir.sqrMagnitude > 0.001f ? dir.normalized * speed : Vector3.zero;
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, desiredVelocity, moveSmoothing * Time.deltaTime);
        rb.linearVelocity = new Vector3(smoothedVelocity.x, 0f, smoothedVelocity.z);
    }

    int NearestWaypoint()
    {
        int nearest = 0;
        float minDist = float.MaxValue;
        for (int i = 0; i < waypoints.Length; i++)
        {
            float d = Vector3.SqrMagnitude(waypoints[i].position - transform.position);
            if (d < minDist) { minDist = d; nearest = i; }
        }
        return nearest;
    }

    void AdvanceWaypoint(bool resetMomentum)
    {
        if (resetMomentum)
        {
            smoothedVelocity = Vector3.zero;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
        previousWaypointPos = waypoints[currentWaypoint].position;
        int dir = Random.value < 0.5f ? 1 : -1;
        currentWaypoint = (currentWaypoint + dir + waypoints.Length) % waypoints.Length;
    }

    Vector3 PathCorrection()
    {
        if (waypoints == null || waypoints.Length == 0) return Vector3.zero;
        Vector3 lineDir = waypoints[currentWaypoint].position - previousWaypointPos;
        lineDir.y = 0;
        if (lineDir.sqrMagnitude < 0.001f) return Vector3.zero;
        Vector3 toRobot = new Vector3(transform.position.x - previousWaypointPos.x, 0, transform.position.z - previousWaypointPos.z);
        Vector3 onLine = new Vector3(previousWaypointPos.x, 0, previousWaypointPos.z) + lineDir.normalized * Vector3.Dot(toRobot, lineDir.normalized);
        return (onLine - new Vector3(transform.position.x, 0, transform.position.z)) * pathCorrectionStrength;
    }

    void DoWander()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Vector3 dir = waypoints[currentWaypoint].position - transform.position;
        dir.y = 0;

        Move(dir + PathCorrection(), moveSpeed);

        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 120f * Time.deltaTime);
        }

        if (dir.magnitude <= waypointReachedDistance)
            AdvanceWaypoint(true);

        eyeSweepT += Time.deltaTime * eyeSweepSpeed * Mathf.Deg2Rad;
        eyeTransform.localRotation = Quaternion.Euler(0, Mathf.Sin(eyeSweepT) * eyeSweepAngle, 0);
    }

    void DoSearch()
    {
        Vector3 dir = lastKnownTargetPos - transform.position;
        dir.y = 0;

        if (dir.magnitude > waypointReachedDistance)
        {
            Move(dir, moveSpeed);
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 120f * Time.deltaTime);
            }
        }
        else
        {
            Move(Vector3.zero, 0f);
        }

        eyeSweepT += Time.deltaTime * eyeSweepSpeed * Mathf.Deg2Rad;
        eyeTransform.localRotation = Quaternion.Euler(0, Mathf.Sin(eyeSweepT) * eyeSweepAngle, 0);
    }

    void DoShoot()
    {
        Vector3 toTarget = target.position - eyeTransform.position;
        Vector3 flatDir = new(toTarget.x, 0, toTarget.z);

        if (toTarget.sqrMagnitude > 0.01f)
        {
            smoothedTargetPos = Vector3.Lerp(smoothedTargetPos, target.position, rotationDamping * Time.deltaTime);
            eyeTransform.rotation = Quaternion.RotateTowards(
                eyeTransform.rotation, Quaternion.LookRotation(toTarget), 180f * Time.deltaTime);
            Vector3 flatSmoothed = new(smoothedTargetPos.x - transform.position.x, 0, smoothedTargetPos.z - transform.position.z);
            if (flatSmoothed.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flatSmoothed), 120f * Time.deltaTime);
        }

        Vector3 pursuitCorrection = PathCorrection() * pursuitCorrectionScale;
        if (flatDir.magnitude > pursuitStopDistance)
            Move(flatDir + pursuitCorrection, pursuitSpeed);
        else if (flatDir.magnitude < pursuitBackUpDistance)
            Move(-flatDir, pursuitSpeed);

        if (waypoints != null && waypoints.Length > 0)
        {
            Vector3 toWaypoint = waypoints[currentWaypoint].position - transform.position;
            toWaypoint.y = 0;
            if (toWaypoint.magnitude <= waypointReachedDistance)
                AdvanceWaypoint(false);
        }

        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            FireOrb();
            shootTimer = shootInterval;
        }
    }

    void LateUpdate()
    {
        Vector3 euler = transform.eulerAngles;
        transform.eulerAngles = new Vector3(0f, euler.y, 0f);

        float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, baseY + bobOffset, transform.position.z);
    }

    void FireOrb()
    {
        if (orbPrefab == null || cannonTransform == null) return;

        Vector3 dir = (target.position - cannonTransform.position).normalized;
        GameObject orb = Instantiate(orbPrefab, cannonTransform.position, Quaternion.LookRotation(dir));

        Rigidbody orbRb = orb.GetComponent<Rigidbody>();
        if (orbRb != null)
            orbRb.linearVelocity = dir * orbSpeed;

        if (muzzleVFX != null)
            muzzleVFX.Play();

        if (shootSound != null)
            AudioSource.PlayClipAtPoint(shootSound, cannonTransform.position, shootVolume);
    }
}
