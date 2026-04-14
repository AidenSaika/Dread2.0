using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GateSlide : MonoBehaviour
{
    public Camera playerCamera;
    public float interactionDistance = 3f;
    public GameObject door;
    public GameObject button;
    public float doorSlideDistanceX = 0f;
    public float doorSlideDistanceY = 0f;
    public float doorSlideDistanceZ = -5f;
    public float doorSlideSpeed = 2f;
    public LayerMask buttonLayerMask;
    public GameObject Monster;

    [Header("Audio")]
    public AudioSource audioSource;  // AudioSource for playing sounds
    public AudioClip doorSlideSound; // Sound for sliding door
    public AudioClip alarmSound;     // Sound for alarm

    private bool isButtonPressed = false;
    private Vector3 originalDoorPosition;
    private Vector3 targetDoorPosition;
    private bool doorOpening = false;

    void Awake()
    {
        TryResolvePlayerCamera();
    }

    void Start()
    {
        if (door == null)
        {
            return;
        }

        // Store the initial door position
        originalDoorPosition = door.transform.position;
        // Set the target position to the right of the original position
        targetDoorPosition = originalDoorPosition + new Vector3(doorSlideDistanceX, doorSlideDistanceY, doorSlideDistanceZ);
    }

    void Update()
    {
        if (playerCamera == null)
        {
            TryResolvePlayerCamera();
        }

        // Detect if the player is looking at the button and pressing 'F'
        if (CheckIfPlayerLookingAtButton() && Input.GetKeyDown(KeyCode.F) && !isButtonPressed)
        {
            isButtonPressed = true;
            doorOpening = true;
            // Play door slide sound
            if (audioSource != null && doorSlideSound != null)
            {
                audioSource.PlayOneShot(doorSlideSound);
            }
            // Debug.Log("Button pressed, door will open");
            if (Monster != null)
            {
                Monster.SetActive(true);
            }

            // Start the alarm sound with a delay and set it to loop
            StartCoroutine(PlayAlarmSoundWithDelay(3f));  // 3 seconds delay for the alarm
        }

        // If the door is opening, move it to the target position
        if (doorOpening && door != null)
        {
            door.transform.position = Vector3.Lerp(door.transform.position, targetDoorPosition, Time.deltaTime * doorSlideSpeed);
            // Check if the door has reached the target position
            if (Vector3.Distance(door.transform.position, targetDoorPosition) < 0.01f)
            {
                doorOpening = false; // Stop moving the door once it reaches the target
            }
        }
    }

    // Method to check if the player is looking at the button using raycast
    bool CheckIfPlayerLookingAtButton()
    {
        if (playerCamera == null || button == null)
        {
            return false;
        }

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0)); // Center of the screen
        RaycastHit hit;

        // Perform raycast and check if the ray hits the button
        if (Physics.Raycast(ray, out hit, interactionDistance, buttonLayerMask))
        {
            // Check if the hit object is the button
            if (hit.transform.gameObject == button)
            {
                Debug.Log("Player is looking at the button");
                return true;
            }
        }

        return false;
    }

    private void TryResolvePlayerCamera()
    {
        if (playerCamera != null)
        {
            return;
        }

        if (Camera.main != null)
        {
            playerCamera = Camera.main;
            return;
        }

#if UNITY_2023_1_OR_NEWER
        playerCamera = FindFirstObjectByType<Camera>();
#else
        playerCamera = FindObjectOfType<Camera>();
#endif
    }

    // Coroutine to play the alarm sound after a delay
    IEnumerator PlayAlarmSoundWithDelay(float delay)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delay);

        // Play alarm sound and set it to loop
        if (audioSource != null && alarmSound != null)
        {
            audioSource.clip = alarmSound;
            audioSource.loop = true;  // Enable looping
            audioSource.Play();
        }
    }
}
