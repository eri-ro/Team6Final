using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectPlayer : MonoBehaviour
{
    [SerializeField]
    Animator animator;
    

    private void OnTriggerEnter(Collider other)
    {
        //Checks to see if player entered collider
        if (other.CompareTag("Player"))
        {
            // Play animation if animator is not null
            if (animator != null)
            {
                animator.SetTrigger("PlayerEntered");
            }

        }
    }
}
