using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterChase : MonoBehaviour
{
    public Transform player;
    public float triggerRange = 10f;
    public float rushDistance = 5f;
    public float rushSpeed = 10f;
    public float chaseSpeed = 4f;
    public float pauseDuration = 1f;
    public Vector3 rushDirection = Vector3.forward;
    public Animator doorAnimator;
    public GameObject hand;

    [Header("Monster Audio")]
    public AudioSource audioSource;
    public AudioClip doorBreakSound;
    public AudioClip monsterScreamSound;

    [Header("Footstep Audio")]
    public AudioSource footstepAudioSource;  // AudioSource for footstep sounds
    public AudioClip footstepSound;          // Footstep sound clip
    public float footstepInterval = 0.5f;    // Time interval between footstep sounds

    [Header("Player Audio")]
    private AudioSource playerBreathingAudio;
    private AudioSource playerHeartbeatAudio;
    private AudioSource playerGeneralAudio;
    public AudioClip breathingSound;
    public AudioClip heartbeatSound;

    private bool playerInTriggerZone = false;
    private bool isChasing = false;
    private Vector3 rushStartPosition;
    private Vector3 rushTargetPosition;
    private float footstepTimer = 0f; // Timer for controlling footstep interval

    void Awake()
    {
        TryResolvePlayer();
    }

    void Start()
    {
        TryResolvePlayer();
        ResolvePlayerAudioSources();
    }

    void Update()
    {
        if (player == null)
        {
            TryResolvePlayer();
            ResolvePlayerAudioSources();
            if (player == null)
            {
                return;
            }
        }

        // Check if the player is within the trigger range
        if (!playerInTriggerZone && Vector3.Distance(player.position, transform.position) <= triggerRange)
        {
            playerInTriggerZone = true;

            // Play door breaking sound immediately upon player entering the trigger range
            if (audioSource != null && doorBreakSound != null)
            {
                audioSource.PlayOneShot(doorBreakSound);
            }

            // Trigger the door breaking animation
            if (doorAnimator != null)
            {
                doorAnimator.SetTrigger("Break");
            }

            // Start the monster's rush out after triggering the door animation
            StartCoroutine(RushOut());

            // Start player's breathing and heartbeat sound
            PlayPlayerSounds();
        }

        // If the monster is chasing the player, move towards the player
        if (isChasing)
        {
            ChasePlayer();

            // Play footstep sounds during chase
            PlayFootstepSound();
        }

        if (hand != null)
        {
            hand.SetActive(true);
        }
    }

    // Coroutine to handle the rush out of the door
    IEnumerator RushOut()
    {
        rushStartPosition = transform.position;
        rushTargetPosition = rushStartPosition + rushDirection.normalized * rushDistance;

        float rushProgress = 0;
        while (rushProgress < 1f)
        {
            transform.position = Vector3.Lerp(rushStartPosition, rushTargetPosition, rushProgress);
            rushProgress += Time.deltaTime * rushSpeed / rushDistance;
            yield return null;
        }

        if (audioSource != null && monsterScreamSound != null)
        {
            audioSource.PlayOneShot(monsterScreamSound);
        }

        yield return new WaitForSeconds(pauseDuration);

        isChasing = true;
    }

    // Method for chasing the player
    void ChasePlayer()
    {
        Vector3 targetPosition = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, chaseSpeed * Time.deltaTime);

        Vector3 directionToPlayer = (targetPosition - transform.position).normalized;
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }
    }

    // Play footstep sound with interval
    private void PlayFootstepSound()
    {
        footstepTimer += Time.deltaTime;
        if (footstepTimer >= footstepInterval && footstepAudioSource != null && footstepSound != null)
        {
            footstepAudioSource.PlayOneShot(footstepSound);
            footstepTimer = 0f;
        }
    }

    private void PlayPlayerSounds()
    {
        ResolvePlayerAudioSources();

        if (playerBreathingAudio != null && breathingSound != null)
        {
            playerBreathingAudio.clip = breathingSound;
            playerBreathingAudio.loop = true;
            playerBreathingAudio.Play();
        }

        if (playerHeartbeatAudio != null && heartbeatSound != null)
        {
            playerHeartbeatAudio.clip = heartbeatSound;
            playerHeartbeatAudio.loop = true;
            playerHeartbeatAudio.Play();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, triggerRange);
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

    private void ResolvePlayerAudioSources()
    {
        if (player == null)
        {
            return;
        }

        AudioSource[] playerAudioSources = player.GetComponents<AudioSource>();
        if (playerAudioSources.Length > 0 && playerGeneralAudio == null)
        {
            playerGeneralAudio = playerAudioSources[0];
        }

        if (playerAudioSources.Length > 1 && playerHeartbeatAudio == null)
        {
            playerHeartbeatAudio = playerAudioSources[1];
        }

        if (playerAudioSources.Length > 2 && playerBreathingAudio == null)
        {
            playerBreathingAudio = playerAudioSources[2];
        }
    }
}
