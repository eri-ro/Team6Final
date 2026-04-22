using System.Collections;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public Transform spawnPoint;       // The player's updated spawn point

    public float checkpointSpinSpeed = 3.0f;
    public float checkpointSpinDuration = 3.0f;

    public int playerFallCount;

    [Header("Sound")]
    public AudioClip checkpointClip;

    void Awake()
    {
        if (spawnPoint == null)
            spawnPoint = transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Checkpoint"))
        {
            // Sets player spawnpoint to the checkpoint
            spawnPoint = other.transform.GetChild(0);
            // If the checkpoint has the compnents, it will speed up the animation then slow it down, and disable the particle effects and show the "CHECKPOINT" UI

            Animator anim = other.gameObject.GetComponent<Animator>();
            if (anim != null)
                StartCoroutine(QuickSpin(checkpointSpinDuration, anim));

            ParticleStopper stopper = other.GetComponent<ParticleStopper>();
            if (stopper != null)
                stopper.stopParticles();

            // Check and see if checkpoint has a valid audioSource, then play the checkpoint sound from it
            if (checkpointClip != null)
            {
                AudioSource src = other.GetComponent<AudioSource>();
                if (src != null)
                    src.PlayOneShot(checkpointClip);
            }

            // Disables the boxcollider so the checkpoint cannot be activated twice
            other.GetComponent<BoxCollider>().enabled = false;
        }
        else if (other.CompareTag("Killplane"))
        {
            transform.position = spawnPoint != null ? spawnPoint.position : transform.position;
            playerFallCount++;
        }
        else if (other.CompareTag("Goal"))
        {
            LevelGoal goal = other.GetComponent<LevelGoal>();
            if (goal != null)
                goal.ChangeFallCount(playerFallCount);
        }
    }

    // Makes the music note spin faster, then slower after (spinTime) seconds
    private IEnumerator QuickSpin(float spinTime, Animator noteSpin)
    {
        noteSpin.speed = checkpointSpinSpeed;
        yield return new WaitForSeconds(spinTime);
        noteSpin.speed = 0.5f;
    }
}
