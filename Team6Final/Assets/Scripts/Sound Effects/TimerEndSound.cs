using UnityEngine;

public class TimerSound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip endClip;
    public float timeRemaining = 30f;

    bool hasPlayed = false;

    void Update()
    {
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
        }
        else if (!hasPlayed)
        {
            hasPlayed = true;
            audioSource.PlayOneShot(endClip);
        }
    }
}