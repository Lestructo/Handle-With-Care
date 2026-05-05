using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public CharacterController controller;
    public float speed = 8f;
    public float gravity = -9.81f;
    public float jumpHeight = 3f;

    [Header("Ladder")]
    public float climbSpeed = 4f;

    [Header("Audio")]
    public AudioClip footStepSound;
    public float footStepDelay = 0.35f;
    public AudioClip climbSound;
    public float footstepVolume = 1f;
    public float climbVolume = 1f;

    private Vector2 moveInput;
    private Vector3 velocity;
    private bool isGrounded;
    public bool OnLadder { get; private set; }
    private float nextFootstep = 0;
    private AudioSource audioSource;
    private AudioSource climbLoopSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        climbLoopSource = gameObject.AddComponent<AudioSource>();
        climbLoopSource.clip = climbSound;
        climbLoopSource.loop = true;
        climbLoopSource.playOnAwake = false;
        climbLoopSource.spatialBlend = 0f;
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;

        if (OnLadder)
        {
            DetachFromLadder();
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        else if (isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void Update()
    {
        isGrounded = controller.isGrounded;

        if (OnLadder)
        {
            HandleLadderMovement();
            return;
        }

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        Vector3 motion = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(motion * speed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        HandleFootsteps(motion);
    }

    private void HandleLadderMovement()
    {
        Vector3 horizontal = transform.right * moveInput.x;
        velocity.y = moveInput.y * climbSpeed;

        controller.Move(horizontal * speed * Time.deltaTime);
        controller.Move(velocity * Time.deltaTime);

        climbLoopSource.volume = climbVolume;
        if (climbSound != null)
        {
            bool moving = moveInput.magnitude > 0.1f;
            if (moving && !climbLoopSource.isPlaying)
                climbLoopSource.Play();
            else if (!moving && climbLoopSource.isPlaying)
                climbLoopSource.Stop();
        }
    }

    public void AttachToLadder()
    {
        OnLadder = true;
        velocity = Vector3.zero;
    }

    public void DetachFromLadder()
    {
        OnLadder = false;
        climbLoopSource.Stop();
    }

    private void HandleFootsteps(Vector3 motion)
    {
        if (audioSource == null || footStepSound == null) return;
        if (motion.magnitude > 0.1f && isGrounded)
        {
            nextFootstep -= Time.deltaTime;
            if (nextFootstep <= 0)
            {
                audioSource.PlayOneShot(footStepSound, footstepVolume);
                nextFootstep = Mathf.Max(footStepDelay, 0.05f);
            }
        }
    }
}
