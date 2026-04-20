using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public Transform spawnPoint;       // The player's updated spawn point

    public float checkpointSpinSpeed = 3.0f;
    public float checkpointSpinDuration = 3.0f;

    public int playerFallCount;
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Checkpoint")
        {
            // Sets player spawnpoint to the checkpoint
            spawnPoint = other.transform.GetChild(0);
            // If the checkpoint has the compnents, it will speed up the animation then slow it down, and disable the particle effects and show the "CHECKPOINT" UI
            if (other.gameObject.GetComponent<Animator>() != null)
            {
                Animator noteSpin = other.gameObject.GetComponent<Animator>();
                StartCoroutine(QuickSpin(checkpointSpinDuration, noteSpin));
            }
            if (other.GetComponent<ParticleStopper>() != null)
            {
                other.GetComponent<ParticleStopper>().stopParticles();
            }
            // Disables the boxcollider so the checkpoint cannot be activated twice
            other.GetComponent<BoxCollider>().enabled = false;
        }
        else if (other.tag == "Killplane")
        {
            gameObject.transform.position = spawnPoint.transform.position;
            playerFallCount++;
        }
        else if (other.tag == "Goal")
        {
            other.GetComponent<LevelGoal>().ChangeFallCount(playerFallCount);
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
