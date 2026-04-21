using UnityEngine;

public class DashSound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip dashClip;

    DashAbility dash;

    void Start()
    {
        dash = GetComponent<DashAbility>();
    }

    void Update()
    {
        if (dash == null) return;

        // Detect when dash is used (same key as your controller)
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            audioSource.PlayOneShot(dashClip);
        }
    }
}