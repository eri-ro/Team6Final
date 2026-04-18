using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelGoal : MonoBehaviour
{
    [Header("Particles")]

    [SerializeField]
    int _finishParticleEmission = 20;

    [SerializeField]
    public ParticleSystem 
        _finishParticleSystem,
        _goalParticleSystem;


    [Header("Cameras")]

    [SerializeField]
    bool _switchCameras = true;

    [SerializeField]
    Camera
        playerCamera,
        endCamera;

    [Header("Animatior")]

    [SerializeField]
    Animator endAnimation;

    private void OnTriggerEnter(Collider other)
    {
        //Checks to see if player entered collider
        if (other.gameObject.tag == "Player")
        {
            //Stops player movement, and unlocks the mouse
            other.GetComponent<PlayerMotor>().ClearVelocity();
            other.GetComponent<PlayerController>().enabled = false;
            other.GetComponent<PlayerMotor>().enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;


            //Does a pulse effect on the goal and stops any further particles from spawning
            _finishParticleSystem.Emit(_finishParticleEmission);
            _goalParticleSystem.Stop();

            //Switches camera to an overhead view of the player if cameras are set and _switchCameras is true
            if (_switchCameras && endCamera != null && playerCamera != null)
            {
                playerCamera.enabled = false;
                endCamera.enabled = true;
            }

            endAnimation.SetTrigger("LevelEnd");
        }
    }

    public void RetryLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
