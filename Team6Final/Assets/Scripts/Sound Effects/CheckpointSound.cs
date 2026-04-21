using UnityEngine;

public class CheckpointSound : MonoBehaviour
{
    public AudioClip checkpointClip;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AudioSource.PlayClipAtPoint(checkpointClip, transform.position);
        }
    }
}