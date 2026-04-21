using UnityEngine;
 
public class AbilitySoundController : MonoBehaviour
{
    public AudioSource audioSource;

    public AudioClip dashClip;
    public AudioClip gravityClip;
    public AudioClip highJumpClip;

    public void PlayAbilitySound(PlayerController.AbilityState ability)
    {
        switch (ability)
        {
            case PlayerController.AbilityState.Dash:
                PlayClip(dashClip);
                break;
            case PlayerController.AbilityState.HighJump:
                PlayClip(highJumpClip);
                break;
            case PlayerController.AbilityState.GravityShift:
                PlayClip(gravityClip);
                break;
            default:
                break;
        }
    }

    void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}