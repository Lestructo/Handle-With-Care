using UnityEngine;
using UnityEngine.InputSystem;

public class MouseComponent : MonoBehaviour
{
    [Header("Settings")]
    public float mouseSensitivity = 25f;
    public float sensitivityScale = 0.01f; // extra divisor because raw mouse deltas are very large
    public Transform playerBody;
    
    private float xRotation = 0f;
    private Vector2 lookInput;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // This is called by the Player Input component when the mouse moves
    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    void Update()
    {
        // poll direct mouse delta for diagnostics (bypassing PlayerInput)
        Vector2 rawDelta = Vector2.zero;
        if (Mouse.current != null)
        {
            rawDelta = Mouse.current.delta.ReadValue();
        }

        // if PlayerInput didn't fire, fall back to raw delta
        if (lookInput == Vector2.zero && rawDelta != Vector2.zero)
        {
            lookInput = rawDelta;
        }

        // Calculate rotation using the input we received (apply an extra scale to reduce raw strength)
        float mouseX = lookInput.x * mouseSensitivity * sensitivityScale;
        float mouseY = lookInput.y * mouseSensitivity * sensitivityScale;

        // Vertical Rotation
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply Rotations
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);

        // Reset input after applying so we don't accumulate
        lookInput = Vector2.zero;
    }
}