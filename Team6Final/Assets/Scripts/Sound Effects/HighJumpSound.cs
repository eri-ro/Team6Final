using UnityEngine;

public class HighJumpSound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip jumpClip;

    HighJumpAbility jump;

    void Start()
    {
        jump = GetComponent<HighJumpAbility>();
    }

    void Update()
    {
        if (jump == null) return;

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            audioSource.PlayOneShot(jumpClip);
        }
    }
}
