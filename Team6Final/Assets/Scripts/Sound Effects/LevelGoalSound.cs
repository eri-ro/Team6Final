using UnityEngine;

public class LevelGoalSound : MonoBehaviour
{
    public AudioClip goalClip;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AudioSource.PlayClipAtPoint(goalClip, transform.position);
        }
    }
}
