using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakableDoor : MonoBehaviour
{
    private Animator animator;
    private bool isBroken = false;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void BreakDoor()
    {
        if (!isBroken)
        {
            Debug.Log("Door is being broken!");
            animator.SetTrigger("Break");
            isBroken = true;
        }
    }

    void Update()
    {
        if (isBroken && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1 && !animator.IsInTransition(0))
        {
            animator.speed = 0;  // Stop the animation once it has completed
        }
    }
}
