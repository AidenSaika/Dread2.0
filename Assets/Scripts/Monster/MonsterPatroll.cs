using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterPatroll : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float speed = 3f;
    public float rotationSpeed = 5f;
    public float waitTime = 2f;
    public Animator animator;

    [Header("Audio")]
    public AudioSource footstepAudioSource; // AudioSource for playing footstep sounds
    public AudioClip footstepSound;         // Footstep sound clip
    public float footstepInterval = 0.5f;   // Time interval between footstep sounds

    private int currentPointIndex = 0;
    private bool isPatrollingForward = true;
    private bool isWaiting = false;
    private Vector3 previousPosition;
    private float movementThreshold = 0.001f;
    private float footstepTimer = 0f;       // Timer for footstep sound intervals

    void Start()
    {
        previousPosition = transform.position;
        footstepAudioSource.clip = footstepSound;  // Assign footstep sound to the AudioSource
    }

    void Update()
    {
        if (!isWaiting)
        {
            Patrol();
        }

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
            if (footstepTimer >= footstepInterval)
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
            footstepAudioSource.Stop();  // Stop footstep sound when not moving
        }

        previousPosition = transform.position;
    }

    void Patrol()
    {
        Transform targetPoint = patrolPoints[currentPointIndex];
        Vector3 targetPosition = new Vector3(targetPoint.position.x, transform.position.y, targetPoint.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            if (currentPointIndex == 0 || currentPointIndex == patrolPoints.Length - 1)
            {
                StartCoroutine(WaitAtPoint());
            }
            else
            {
                UpdatePatrolPoint();
            }
        }
    }

    void UpdatePatrolPoint()
    {
        if (isPatrollingForward)
        {
            currentPointIndex++;
            if (currentPointIndex >= patrolPoints.Length)
            {
                currentPointIndex = patrolPoints.Length - 1;
                isPatrollingForward = false;
            }
        }
        else
        {
            currentPointIndex--;
            if (currentPointIndex < 0)
            {
                currentPointIndex = 0;
                isPatrollingForward = true;
            }
        }
    }

    IEnumerator WaitAtPoint()
    {
        isWaiting = true;
        Debug.Log("Waiting at point " + (currentPointIndex + 1));
        footstepAudioSource.Stop(); // Stop footstep sound when waiting
        yield return new WaitForSeconds(waitTime);
        isWaiting = false;
        UpdatePatrolPoint();
    }
}