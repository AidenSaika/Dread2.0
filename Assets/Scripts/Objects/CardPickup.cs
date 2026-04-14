using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardPickup : MonoBehaviour
{
    public float pickUpRange = 3f;
    public GameObject card;
    public Transform player;

    void Awake()
    {
        TryResolveReferences();
    }

    void Update()
    {
        if (card == null)
        {
            card = gameObject;
        }

        if (player == null)
        {
            TryResolvePlayer();
        }

        if (player == null || card == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            float distanceToCard = Vector3.Distance(player.position, card.transform.position);
            if (distanceToCard <= pickUpRange)
            {
                PickUp();
            }
        }
    }

    // pick up the card
    void PickUp()
    {
        Debug.Log("Card picked up!");
        Destroy(card);
    }

    private void OnDrawGizmosSelected()
    {
        if (card != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(card.transform.position, pickUpRange);
        }
    }

    private void TryResolveReferences()
    {
        if (card == null)
        {
            card = gameObject;
        }

        TryResolvePlayer();
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
