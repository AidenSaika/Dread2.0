using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactionDistance = 5f;
    public GameObject door;
    public float doorOpenAngle = -100f;
    public float doorOpenSpeed = 2f;
    public LayerMask doorLayerMask;

    [Header("Audio")]
    public AudioSource audioSource;  // AudioSource for playing sound
    public AudioClip doorOpenSound;  // Sound for door opening
    public AudioClip doorCloseSound; // Sound for door closing

    private bool isDoorOpen = false;
    private Quaternion originalRotation;
    private Quaternion targetRotation;

    void Start()
    {
        // Store the initial door rotation
        originalRotation = door.transform.rotation;
    }

    void Update()
    {
        // Detect if the player is looking at the door and pressing 'F'
        if (CheckIfPlayerLookingAtDoor() && Input.GetKeyDown(KeyCode.F))
        {
            ToggleDoor();
        }

        // rotate the door to its target angle
        if (isDoorOpen)
        {
            door.transform.rotation = Quaternion.Slerp(door.transform.rotation, targetRotation, Time.deltaTime * doorOpenSpeed);
        }
        else
        {
            door.transform.rotation = Quaternion.Slerp(door.transform.rotation, originalRotation, Time.deltaTime * doorOpenSpeed);
        }
    }

    // Method to check if the player is looking at the door using raycast
    bool CheckIfPlayerLookingAtDoor()
    {
        // Cast a ray from the player's camera forward
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0)); // Center of the screen
        RaycastHit hit;

        // Perform raycast and check if the ray hits something
        if (Physics.Raycast(ray, out hit, interactionDistance, doorLayerMask))
        {
            // Check if the hit object is the door
            if (hit.transform.gameObject == door)
            {
                return true;
            }
        }

        return false;
    }

    // Toggle the door between open and closed states, and play the corresponding sound
    void ToggleDoor()
    {
        if (isDoorOpen)
        {
            isDoorOpen = false;
        }
        else
        {
            isDoorOpen = true;
            if (audioSource != null && doorOpenSound != null)
            {
                audioSource.PlayOneShot(doorOpenSound);
            }
            targetRotation = Quaternion.Euler(door.transform.eulerAngles.x, door.transform.eulerAngles.y + doorOpenAngle, door.transform.eulerAngles.z);
        }
    }
}