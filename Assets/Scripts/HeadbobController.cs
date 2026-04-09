using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadbobController : MonoBehaviour
{
    [Header("Head Bob Settings")]
    public float walkBobSpeed = 10f;     
    public float walkBobAmount = 0.05f;  
    public float runBobSpeed = 14f;      
    public float runBobAmount = 0.1f;   
    public float crouchBobSpeed = 6f;    
    public float crouchBobAmount = 0.03f;

    private float defaultYPos = 0f;     
    private float timer;               

    private Transform camTransform;       
    private PlayerMovement playerMovement;

    void Start()
    {
        camTransform = GetComponent<Transform>();
        playerMovement = FindObjectOfType<PlayerMovement>();

        defaultYPos = camTransform.localPosition.y;
    }

    void Update()
    {
        HandleHeadBob();
    }

    void HandleHeadBob()
    {
        // Check if the player is moving (either walking, sprinting, or crouching)
        if (Mathf.Abs(playerMovement.horizontalInput) > 0.1f || Mathf.Abs(playerMovement.verticalInput) > 0.1f)
        {
            float bobSpeed = 0f;
            float bobAmount = 0f;

            // Determine the bob speed and amount based on player's movement state
            if (playerMovement.state == PlayerMovement.MovementState.sprinting)
            {
                bobSpeed = runBobSpeed;
                bobAmount = runBobAmount;
            }
            else if (playerMovement.state == PlayerMovement.MovementState.crouching)
            {
                bobSpeed = crouchBobSpeed;
                bobAmount = crouchBobAmount;
            }
            else
            {
                bobSpeed = walkBobSpeed;
                bobAmount = walkBobAmount;
            }

            // Increment timer based on movement speed
            timer += Time.deltaTime * bobSpeed;

            // Apply Sin wave to Y position for bobbing effect
            camTransform.localPosition = new Vector3(
                camTransform.localPosition.x,
                defaultYPos + Mathf.Sin(timer) * bobAmount,
                camTransform.localPosition.z
            );
        }
        else
        {
            // Reset timer and Y position when the player is not moving
            timer = 0;
            camTransform.localPosition = new Vector3(camTransform.localPosition.x, defaultYPos, camTransform.localPosition.z);
        }
    }
}
