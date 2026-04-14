using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MonsterWalkToPlayer : MonoBehaviour
{
    public Transform player;
    public float speed = 3f;
    public Animator animator;
    public float rotationSpeed = 5f;
    public Collider interactionZone;

    [Header("Audio")]
    public AudioSource footstepAudioSource; // AudioSource for playing footstep sounds
    public AudioClip footstepSound; // Footstep sound clip
    public float footstepInterval = 0.5f; // Time interval between footstep sounds

    private bool playerInZone = false;
    private Vector3 targetPosition;
    private bool moveToTarget = false;
    private Vector3 previousPosition;
    private float movementThreshold = 0.001f;

    private float footstepTimer = 0f; // Timer for footstep intervals

    void Awake()
    {
        TryResolvePlayer();
    }

    void Start()
    {
        previousPosition = transform.position;

        if (footstepAudioSource != null)
        {
            footstepAudioSource.clip = footstepSound; // Assign the footstep sound to the AudioSource
        }
    }

    void Update()
    {
        if (player == null)
        {
            TryResolvePlayer();
        }

        if (playerInZone && Input.GetKeyDown(KeyCode.E))
        {
            if (player == null)
            {
                return;
            }

            // Set target position to player's position, but keep monster's Y position unchanged
            targetPosition = new Vector3(player.position.x, transform.position.y, player.position.z);
            moveToTarget = true;
            Debug.Log("Monster will move to position: " + targetPosition);
        }

        if (moveToTarget)
        {
            // Ensure the monster only moves on the X and Z axes (Y stays constant)
            Vector3 currentPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            Vector3 targetPositionWithLockedY = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);

            // Move the monster towards the target position with locked Y
            transform.position = Vector3.MoveTowards(currentPosition, targetPositionWithLockedY, speed * Time.deltaTime);

            // Calculate the direction to the target (on the X and Z plane)
            Vector3 directionToTarget = (targetPositionWithLockedY - transform.position).normalized;

            // Ensure the monster only rotates around the Y axis (prevent tilting)
            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);  // Only Y rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Check if the monster reached the target position
            if (Vector3.Distance(transform.position, targetPositionWithLockedY) < 0.1f)
            {
                moveToTarget = false;
                Debug.Log("Monster reached the target position.");
            }
        }

        // Calculate velocity to detect movement
        float velocity = (transform.position - previousPosition).magnitude / Time.deltaTime;
        bool isMoving = velocity > movementThreshold;

        if (isMoving)
        {
            if (!animator.enabled)
            {
                animator.enabled = true;
            }

            // Play footstep sounds at intervals
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= footstepInterval && footstepAudioSource != null)
            {
                footstepAudioSource.Play();  // Play footstep sound
                footstepTimer = 0f;  // Reset timer
            }
        }
        else
        {
            if (animator.enabled)
            {
                animator.enabled = false;
            }

            if (footstepAudioSource != null)
            {
                footstepAudioSource.Stop(); // Stop footstep sounds when not moving
            }
        }

        previousPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            Debug.Log("Player entered interaction zone.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            Debug.Log("Player left interaction zone.");
        }
    }

    private void TryResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerByTag = GameObject.FindGameObjectWithTag("Player");
        if (playerByTag != null)
        {
            player = playerByTag.transform;
            return;
        }

#if UNITY_2023_1_OR_NEWER
        PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
#else
        PlayerMovement movement = FindObjectOfType<PlayerMovement>();
#endif
        if (movement != null)
        {
            player = movement.transform;
        }
    }
}
