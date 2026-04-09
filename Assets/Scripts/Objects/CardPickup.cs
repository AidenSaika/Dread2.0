using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardPickup : MonoBehaviour
{
    public float pickUpRange = 3f;
    public GameObject card;
    public Transform player;

    void Update()
    {
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
}
