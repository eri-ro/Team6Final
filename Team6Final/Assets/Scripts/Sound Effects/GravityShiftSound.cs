using UnityEngine;

public class GravityShiftSound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip gravityClip;

    PlayerGravityShift gravityShift;

    void Start()
    {
        gravityShift = GetComponent<PlayerGravityShift>();
    }

    void Update()
    {
        if (gravityShift == null) return;

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            audioSource.PlayOneShot(gravityClip);
        }
    }
}