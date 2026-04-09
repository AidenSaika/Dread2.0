using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;

    public float groundDrag;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Sonar")]
    public GameObject vfxPrefab;
    public Vector3 vfxOffset = new Vector3(0, 1, 0);
    public float sonarIntervals;
    public AudioClip sonarSound;  // Sound for sonar scanning

    [Header("Keybinds")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode sonarKey = KeyCode.E;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    public Transform orientation;

    public float horizontalInput;
    public float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    public MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
    }

    [Header("Audio")]
    public AudioSource audioSource;  // Main AudioSource to play sounds
    public AudioClip walkSound;  // Sound for walking
    public AudioClip sprintSound;  // Sound for sprinting
    public float walkFootstepInterval = 0.5f;  // Time between footsteps when walking
    public float sprintFootstepInterval = 0.3f;  // Time between footsteps when sprinting
    private float footstepTimer = 0f;  // Timer for footstep sounds

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        startYScale = transform.localScale.y;
    }

    private void Update()
    {
        // Ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        MyInput();
        SpeedControl();
        StateHandler();

        if (Input.GetKeyDown(sonarKey))
        {
            SonarScan();
        }

        // Handle drag
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;

        // Play footstep sounds based on movement state
        PlayFootstepSounds();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // Start crouch
        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        // Stop crouch
        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
    }

    private void StateHandler()
    {
        // Mode - Crouching
        if (Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }

        // Mode - Sprinting
        else if (Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }

        // Mode - Walking
        else
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }
    }

    private void MovePlayer()
    {
        // Calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // Limit velocity if needed
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void SonarScan()
    {
        // Play sonar sound
        if (sonarSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(sonarSound);
        }

        // Visual effect for sonar scan
        Vector3 spawnPosition = transform.position + vfxOffset;
        Instantiate(vfxPrefab, spawnPosition, Quaternion.identity);

        StartCoroutine(SpawnAdditionalVFX(spawnPosition));
    }

    private IEnumerator SpawnAdditionalVFX(Vector3 initialPosition)
    {
        yield return new WaitForSeconds(sonarIntervals);

        Vector3 secondPosition = initialPosition;
        Instantiate(vfxPrefab, secondPosition, Quaternion.identity);

        yield return new WaitForSeconds(sonarIntervals);

        Vector3 thirdPosition = initialPosition;
        Instantiate(vfxPrefab, thirdPosition, Quaternion.identity);
    }

    private void PlayFootstepSounds()
    {
        // If player is grounded, moving, and not crouching
        if (grounded && (horizontalInput != 0 || verticalInput != 0) && state != MovementState.crouching)
        {
            footstepTimer -= Time.deltaTime;

            // If the footstep timer has expired, play the appropriate sound
            if (footstepTimer <= 0)
            {
                if (state == MovementState.sprinting && sprintSound != null)
                {
                    audioSource.PlayOneShot(sprintSound);
                    footstepTimer = sprintFootstepInterval;  // Sprinting footstep interval
                }
                else if (state == MovementState.walking && walkSound != null)
                {
                    audioSource.PlayOneShot(walkSound);
                    footstepTimer = walkFootstepInterval;  // Walking footstep interval
                }
            }
        }
        else
        {
            // Reset the timer when the player is not moving or grounded
            footstepTimer = 0f;
        }
    }
}