using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkingSoundPlayer : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip walkingClip;

    void Awake()
    {
        if (audioSource != null)
        {
            audioSource.clip = walkingClip;
            audioSource.loop = true;
        }
    }

    public void PlayWalking()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void StopWalking()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}