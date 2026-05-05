using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerGrab : MonoBehaviour
{
    public float grabRange = 3f;
    public Transform holdPoint;
    public LayerMask grabLayer;
    public float grabDrag = 25f;          // high drag keeps the object from swinging wildly while held
    public float grabForce = 200f;        // spring force pulling the object toward the hold point
    public float scrollSpeed = 25f;
    public float minHoldDistance = 1f;
    public float maxHoldDistance = 3f;

    public float grabAngularDrag = 25f;   // dampens spinning so the object settles quickly

    private GameObject grabbedObject;
    private Rigidbody grabbedRB;
    private float originalDrag;
    private float originalAngularDrag;
    private float currentHoldDistance = 2f;
    private Vector3 grabLocalOffset;


    public float magnetizeRange = 50f;
    public float magnetizeForce = 5f;
    public float magnetizeBrake = 10f;
    public float magnetizeAngularDrag = 5f;
    private Rigidbody magnetizeRB;
    private GameObject magnetizeObject;
    private Vector3 magnetizeLocalOffset;
    private float originalMagnetizeAngularDrag;
    private bool originalMagnetizeUseGravity;
    private bool magnetizeActive;
    private PlayerMovement movement;

    public Color grabHighlightColor = Color.red;
    public Color magnetizeHighlightColor = Color.blue;
    public Color crosshairDefaultColor = Color.white;
    public Image crosshairImage;
    public Light playerLight;
    public float lightColorSpeed = 3f;
    public float glassLightFadeStart = 4f;
    public float glassLightFadeEnd = 2f;
    private Color lightDefaultColor;
    private Color targetLightColor;
    public float lightBaseIntensity = 1f;
    private Highlightable currentHighlight;

    [Header("Tether")]
    public float grabSnapDistance = 4.5f;
    public float magnetizeSnapDistance = 75f;
    public int grabTetherSegments = 2;
    public float grabTetherSag = 0f;
    public float grabTetherWidth = 0.02f;

    public int magnetizeTetherSegments = 30;
    public float magnetizeTetherSag = 0.3f;
    public float magnetizeTetherWidth = 0.05f;
    public float magnetizeWobbleFrequency = 3f;
    public float magnetizeWobbleAmplitude = 0.15f;
    public float magnetizeWobbleSpeed = 5f;

    [Header("Audio")]
    public AudioClip grabLoopClip;
    public AudioClip magnetizeLoopClip;
    public float grabVolume = 1f;
    public float magnetizeVolume = 1f;
    AudioSource grabLoopSource;
    AudioSource magnetizeLoopSource;

    private InputAction interactAction;
    private InputAction magnetizeAction;
    private LineRenderer grabTether;
    private LineRenderer magnetizeTether;


    // Subscribe/unsubscribe in OnEnable/OnDisable rather than Start/OnDestroy
    // so input is correctly paused when the component is disabled (e.g. on ladder).
    void OnEnable()
    {
        var playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput != null)
        {
            interactAction = playerInput.actions.FindAction("Interact");
            if (interactAction != null)
                interactAction.performed += OnInteractPerformed;

            magnetizeAction = playerInput.actions.FindAction("Magnetize");
            if (magnetizeAction != null)
            {
                magnetizeAction.performed += OnMagnetizePerformed;
                magnetizeAction.canceled += OnMagnetizeCanceled;
            }
        }
    }

    void OnDisable()
    {
        if (interactAction != null)
            interactAction.performed -= OnInteractPerformed;

        if (magnetizeAction != null)
        {
            magnetizeAction.performed -= OnMagnetizePerformed;
            magnetizeAction.canceled -= OnMagnetizeCanceled;
        }

        if (grabLoopSource != null) grabLoopSource.Stop();
        if (magnetizeLoopSource != null) magnetizeLoopSource.Stop();
    }

    GrabStamina grabStamina;
    public bool IsGrabbing => grabbedObject != null;
    public bool IsMagnetizing => magnetizeActive;

    void Start()
    {
        movement = GetComponentInParent<PlayerMovement>();
        grabStamina = FindFirstObjectByType<GrabStamina>();

        grabLoopSource = gameObject.AddComponent<AudioSource>();
        grabLoopSource.loop = true;
        grabLoopSource.playOnAwake = false;
        grabLoopSource.spatialBlend = 0f;

        magnetizeLoopSource = gameObject.AddComponent<AudioSource>();
        magnetizeLoopSource.loop = true;
        magnetizeLoopSource.playOnAwake = false;
        magnetizeLoopSource.spatialBlend = 0f;

        grabTether = CreateTether("GrabTether", grabTetherWidth, grabHighlightColor);
        magnetizeTether = CreateTether("MagnetizeTether", magnetizeTetherWidth, magnetizeHighlightColor);

        if (playerLight != null)
        {
            lightDefaultColor = playerLight.color;
            targetLightColor = lightDefaultColor;
            playerLight.intensity = lightBaseIntensity;
        }

    }

    void Update()
    {
        // Check if on ladder
        if (movement != null && movement.OnLadder)
        {
            if (grabbedObject != null) DropObject();
            if (magnetizeActive) StopMagnetize();
            ClearHighlight();
            return;
        }

        // auto-release if an explosion knocks the object beyond the snap threshold
        if (grabbedObject != null && Vector3.Distance(grabbedObject.transform.TransformPoint(grabLocalOffset), holdPoint.position) > grabSnapDistance)
            DropObject();

        if (magnetizeActive && magnetizeObject != null && Vector3.Distance(magnetizeObject.transform.TransformPoint(magnetizeLocalOffset), holdPoint.position) > magnetizeSnapDistance)
            StopMagnetize();

        grabLoopSource.volume = grabVolume;
        magnetizeLoopSource.volume = magnetizeVolume;

        float scroll = Mouse.current.scroll.y.ReadValue();
        if (scroll != 0)
        {
            currentHoldDistance = Mathf.Clamp(currentHoldDistance + (scroll * scrollSpeed * Time.deltaTime), minHoldDistance, maxHoldDistance);
            holdPoint.localPosition = new Vector3(0, 0, currentHoldDistance);
        }

        UpdateHighlight();

        if (playerLight != null)
        {
            playerLight.color = Color.Lerp(playerLight.color, targetLightColor, lightColorSpeed * Time.deltaTime);
            float closestGlass = glassLightFadeStart;
            foreach (Collider col in Physics.OverlapSphere(transform.position, glassLightFadeStart, grabLayer))
            {
                if (col.CompareTag("Glass"))
                {
                    float d = Vector3.Distance(transform.position, col.ClosestPoint(transform.position));
                    if (d < closestGlass) closestGlass = d;
                }
            }
            float targetIntensity = Mathf.Clamp01(Mathf.InverseLerp(glassLightFadeEnd, glassLightFadeStart, closestGlass)) * lightBaseIntensity;
            playerLight.intensity = Mathf.Lerp(playerLight.intensity, targetIntensity, lightColorSpeed * Time.deltaTime);
        }
    }

    void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (PauseMenu.IsPaused) return;
        if (movement != null && movement.OnLadder) return;

        if (grabbedObject == null) TryGrab();
        else DropObject();
    }

    void FixedUpdate()
    {
        if (grabbedObject != null)
            MoveObject();

        if (magnetizeRB != null && magnetizeActive)
            MoveMagnetizeObject();
    }

    void TryGrab()
    {
        if (grabStamina != null && !grabStamina.CanUse) return;
        if (magnetizeActive) StopMagnetize();

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, grabRange, grabLayer))
        {
            if (hit.collider.CompareTag("Glass")) return;
            grabbedObject = hit.collider.gameObject;
            grabbedRB = grabbedObject.GetComponent<Rigidbody>();

            // Store the exact surface point hit, in local space so it follows the object as it rotates
            grabLocalOffset = grabbedObject.transform.InverseTransformPoint(hit.point); // local space so offset follows object as it rotates

            currentHoldDistance = 2f;
            holdPoint.localPosition = new Vector3(0, 0, currentHoldDistance);

            if (grabbedRB != null)
            {
                grabbedRB.linearVelocity = Vector3.zero;
                grabbedRB.angularVelocity = Vector3.zero;

                originalDrag = grabbedRB.linearDamping;
                originalAngularDrag = grabbedRB.angularDamping;
                grabbedRB.linearDamping = grabDrag;
                grabbedRB.angularDamping = grabAngularDrag;

                if (playerLight != null) targetLightColor = grabHighlightColor;
            }

            if (grabLoopClip != null && grabLoopSource != null)
            {
                grabLoopSource.clip = grabLoopClip;
                grabLoopSource.Play();
            }
        }
    }

    public void ForceRelease(GameObject go)
    {
        if (grabbedObject == go) DropObject();
        if (magnetizeObject == go) StopMagnetize();
    }

    public void ForceReleaseAll()
    {
        if (grabbedObject != null) DropObject();
        if (magnetizeActive) StopMagnetize();
    }

    public void ForceReleaseInRadius(Vector3 center, float radius)
    {
        if (grabbedObject != null && Vector3.Distance(grabbedObject.transform.position, center) <= radius)
            DropObject();
        if (magnetizeObject != null && Vector3.Distance(magnetizeObject.transform.position, center) <= radius)
            StopMagnetize();
    }

    void DropObject()
    {
        if (grabbedRB != null)
        {
            grabbedRB.useGravity = true;
            grabbedRB.linearDamping = originalDrag;
            grabbedRB.angularDamping = originalAngularDrag;
            grabbedRB.constraints = RigidbodyConstraints.None;
        }
        grabbedObject = null;
        grabbedRB = null;
        if (playerLight != null) targetLightColor = lightDefaultColor;

        if (grabLoopSource != null) grabLoopSource.Stop();
    }

    // Force is applied at the exact grab point rather than the centre of mass
    // so the object rotates naturally when pulled off-axis.
    void MoveObject()
    {
        Vector3 grabWorldPoint = grabbedObject.transform.TransformPoint(grabLocalOffset);
        grabbedRB.AddForceAtPosition((holdPoint.position - grabWorldPoint) * grabForce, grabWorldPoint);
    }

    void OnMagnetizePerformed(InputAction.CallbackContext ctx)
    {
        if (PauseMenu.IsPaused) return;
        if (movement != null && movement.OnLadder) return;
        if (grabbedObject != null) return;
        if (grabStamina != null && !grabStamina.CanUse) return;

        Ray ray = new Ray(transform.position, transform.forward);

        RaycastHit[] allHits = Physics.RaycastAll(ray, magnetizeRange, grabLayer);
        System.Array.Sort(allHits, (a, b) => a.distance.CompareTo(b.distance));

        RaycastHit hit = default;
        bool found = false;
        foreach (RaycastHit h in allHits)
        {
            if (h.collider.CompareTag("Glass")) continue;
            hit = h;
            found = true;
            break;
        }

        if (found)
        {
            magnetizeObject = hit.collider.gameObject;
            magnetizeRB = magnetizeObject.GetComponent<Rigidbody>();

            if (magnetizeRB != null)
            {
                magnetizeRB.linearVelocity = Vector3.zero;
                magnetizeRB.angularVelocity = Vector3.zero;

                magnetizeLocalOffset = magnetizeObject.transform.InverseTransformPoint(hit.point);
                originalMagnetizeAngularDrag = magnetizeRB.angularDamping;
                magnetizeRB.angularDamping = magnetizeAngularDrag;
                originalMagnetizeUseGravity = magnetizeRB.useGravity;
                magnetizeRB.useGravity = false; // object must float freely while being pulled toward the player

                magnetizeActive = true;
                if (playerLight != null) targetLightColor = magnetizeHighlightColor;

                if (magnetizeLoopClip != null && magnetizeLoopSource != null)
                {
                    magnetizeLoopSource.clip = magnetizeLoopClip;
                    magnetizeLoopSource.Play();
                }
            }
        }
    }

    void OnMagnetizeCanceled(InputAction.CallbackContext ctx)
    {
        StopMagnetize();
    }

    void MoveMagnetizeObject()
    {
        Vector3 grabWorldPoint = magnetizeObject.transform.TransformPoint(magnetizeLocalOffset);
        float distToPlayer = Vector3.Distance(transform.position, grabWorldPoint);

        if (distToPlayer <= currentHoldDistance)
        {
            magnetizeRB.linearVelocity = Vector3.Lerp(magnetizeRB.linearVelocity, Vector3.zero, Time.fixedDeltaTime * magnetizeBrake);
        }
        else
        {
            Vector3 toHold = holdPoint.position - grabWorldPoint;
            magnetizeRB.AddForceAtPosition(toHold.normalized * magnetizeForce, grabWorldPoint);
        }

        // Gentle bob times while magnetized
        magnetizeRB.AddForce(Vector3.up    * Mathf.Sin(Time.time * 2f)  * 1f);
        magnetizeRB.AddForce(Vector3.right * Mathf.Sin(Time.time * 3f)  * 1f);
    }

    void StopMagnetize()
    {
        if (magnetizeRB != null)
        {
            magnetizeRB.angularDamping = originalMagnetizeAngularDrag;
            magnetizeRB.useGravity = true;
        }

        magnetizeActive = false;
        magnetizeObject = null;
        magnetizeRB = null;
        if (playerLight != null && grabbedObject == null) targetLightColor = lightDefaultColor;

        if (magnetizeTether != null) magnetizeTether.enabled = false;

        if (magnetizeLoopSource != null) magnetizeLoopSource.Stop();
    }

    void LateUpdate()
    {
        UpdateGrabTether();
        UpdateMagnetizeTether();
    }

    LineRenderer CreateTether(string name, float width, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.material.color = color;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.useWorldSpace = true;
        lr.enabled = false;
        return lr;
    }

    void UpdateGrabTether()
    {
        if (grabTether == null) return;
        if (grabbedObject == null) { grabTether.enabled = false; return; }

        Vector3 start = holdPoint.position;
        Vector3 end = grabbedObject.transform.TransformPoint(grabLocalOffset);
        float dist = Vector3.Distance(start, end);
        Vector3 control = (start + end) * 0.5f + Vector3.down * dist * grabTetherSag;

        grabTether.material.color = grabHighlightColor;
        grabTether.enabled = true;
        grabTether.positionCount = grabTetherSegments + 1;
        for (int i = 0; i <= grabTetherSegments; i++)
        {
            float t = i / (float)grabTetherSegments;
            float u = 1f - t;
            grabTether.SetPosition(i, u * u * start + 2f * u * t * control + t * t * end); // quadratic bezier
        }
    }

    void UpdateMagnetizeTether()
    {
        if (magnetizeTether == null) return;
        if (!magnetizeActive || magnetizeObject == null) { magnetizeTether.enabled = false; return; }

        Vector3 start = holdPoint.position;
        Vector3 end = magnetizeObject.transform.TransformPoint(magnetizeLocalOffset);
        float dist = Vector3.Distance(start, end);
        Vector3 control = (start + end) * 0.5f + Vector3.down * dist * magnetizeTetherSag;

        magnetizeTether.startWidth = magnetizeTetherWidth;
        magnetizeTether.endWidth = magnetizeTetherWidth;

        // Perpendicular axis for snake wobble
        Vector3 dir = (end - start).normalized;
        // fallback axis in case the tether is perfectly vertical (cross product would be zero)
        Vector3 perp = Vector3.Cross(dir, Vector3.up).normalized;
        if (perp.sqrMagnitude < 0.01f) perp = Vector3.Cross(dir, Vector3.forward).normalized;

        magnetizeTether.material.color = magnetizeHighlightColor;
        magnetizeTether.enabled = true;
        magnetizeTether.positionCount = magnetizeTetherSegments + 1;
        for (int i = 0; i <= magnetizeTetherSegments; i++)
        {
            float t = i / (float)magnetizeTetherSegments;
            float u = 1f - t;
            Vector3 point = u * u * start + 2f * u * t * control + t * t * end;
            float envelope = Mathf.Sin(t * Mathf.PI);
            float wobble = Mathf.Sin(t * magnetizeWobbleFrequency * Mathf.PI * 2f + Time.time * magnetizeWobbleSpeed) * magnetizeWobbleAmplitude * envelope;
            magnetizeTether.SetPosition(i, point + perp * wobble);
        }
    }

    void UpdateHighlight()
    {
        if (grabbedObject != null)
        {
            SetCurrentHighlight(GetHighlightableFromGO(grabbedObject), grabHighlightColor);
            SetCrosshairColor(grabHighlightColor);
            return;
        }

        if (magnetizeActive && magnetizeObject != null)
        {
            SetCurrentHighlight(GetHighlightableFromGO(magnetizeObject), magnetizeHighlightColor);
            SetCrosshairColor(magnetizeHighlightColor);
            return;
        }

        // Red: in grab range (not glass). Blue: magnetize range, passes through glass.
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, grabRange, grabLayer)
            && !hit.collider.CompareTag("Glass"))
        {
            SetCurrentHighlight(GetHighlightable(hit.collider), grabHighlightColor);
            SetCrosshairColor(grabHighlightColor);
            return;
        }

        RaycastHit[] allHits = Physics.RaycastAll(transform.position, transform.forward, magnetizeRange, grabLayer);
        System.Array.Sort(allHits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit h in allHits)
        {
            if (h.collider.CompareTag("Glass")) continue;
            SetCurrentHighlight(GetHighlightable(h.collider), magnetizeHighlightColor);
            SetCrosshairColor(magnetizeHighlightColor);
            return;
        }

        SetCurrentHighlight(null);
        SetCrosshairColor(crosshairDefaultColor);
    }

    void SetCrosshairColor(Color color)
    {
        if (crosshairImage != null) crosshairImage.color = color;
    }

    void SetCurrentHighlight(Highlightable next, Color color = default)
    {
        if (next != currentHighlight)
        {
            if (currentHighlight != null) currentHighlight.SetHighlight(false);
            currentHighlight = next;
        }
        if (currentHighlight != null) currentHighlight.SetHighlight(true, color);
    }

    Highlightable GetHighlightableFromGO(GameObject go)
    {
        Highlightable h = go.GetComponent<Highlightable>();
        if (h == null) h = go.GetComponentInParent<Highlightable>();
        if (h == null) h = go.GetComponentInChildren<Highlightable>();
        return h;
    }

    void ClearHighlight()
    {
        if (currentHighlight != null)
        {
            currentHighlight.SetHighlight(false);
            currentHighlight = null;
        }
    }

    Highlightable GetHighlightable(Collider col)
    {
        Highlightable h = col.GetComponent<Highlightable>();
        if (h == null) h = col.GetComponentInParent<Highlightable>();
        if (h == null) h = col.GetComponentInChildren<Highlightable>();
        return h;
    }
}
