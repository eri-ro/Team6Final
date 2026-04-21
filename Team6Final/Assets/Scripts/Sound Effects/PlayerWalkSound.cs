using UnityEngine;

public class PlayerWalkSound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip walkClip;

    public float groundCheckDistance = 1.2f;
    public float moveThreshold = 0.1f;

    void Update()
    {
        // Movement input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        bool isMoving = Mathf.Abs(h) > moveThreshold || Mathf.Abs(v) > moveThreshold;

        // Ground check (works with your gravity system)
        Vector3 down = -GravityWorld.Up.normalized;
        bool isGrounded = Physics.Raycast(transform.position, down, groundCheckDistance);

        // Play walking sound
        if (isGrounded && isMoving)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.clip = walkClip;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
        }
    }
}