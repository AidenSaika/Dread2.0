using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerTest : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerEnter called with: " + other.name);

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player has entered the trigger zone!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("OnTriggerExit called with: " + other.name);

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player has exited the trigger zone!");
        }
    }
}
