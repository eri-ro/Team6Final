using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceshipDoorSound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip doorOpenClip;

    private bool hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasPlayed)
        {
            audioSource.PlayOneShot(doorOpenClip);
            hasPlayed = true;
        }
    }
}
