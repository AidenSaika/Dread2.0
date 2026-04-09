using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandRushOut : MonoBehaviour
{
    public Transform player;
    public float triggerRange = 10f;
    public float rushDistance = 5f;
    public float rushSpeed = 10f;
    public Animator handAnimator;

    [Header("Audio")]
    public AudioSource audioSource;  // AudioSource for playing sounds
    public AudioClip rushOutSound;   // Sound for when the hand rushes out

    private bool playerInTriggerZone = false;
    private bool hasRushed = false;
    private Vector3 rushStartPosition;
    private Vector3 rushTargetPosition;

    void Update()
    {
        // Check if the player is within the trigger range
        if (!playerInTriggerZone && Vector3.Distance(player.position, transform.position) <= triggerRange)
        {
            playerInTriggerZone = true;
            StartCoroutine(RushOutAndGrab());
        }
    }

    // Coroutine to handle the rush out and grab animation
    IEnumerator RushOutAndGrab()
    {
        // Play the grab animation immediately when the hand starts rushing
        if (handAnimator != null)
        {
            handAnimator.SetTrigger("Grab");  // Assuming "Grab" is the trigger for the hand's grab animation
        }

        // Play rush out sound
        if (audioSource != null && rushOutSound != null)
        {
            audioSource.PlayOneShot(rushOutSound);
        }

        // Set the rush start and target positions
        rushStartPosition = transform.position;
        rushTargetPosition = rushStartPosition + transform.forward * rushDistance;  // Move forward from the door

        // Rush towards the target position
        float rushProgress = 0;
        while (rushProgress < 1f)
        {
            transform.position = Vector3.Lerp(rushStartPosition, rushTargetPosition, rushProgress);
            rushProgress += Time.deltaTime * rushSpeed / rushDistance;
            yield return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, triggerRange);
    }
}