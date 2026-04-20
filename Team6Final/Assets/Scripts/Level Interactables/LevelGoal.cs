using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

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
        _playerCamera,
        _endCamera;

    [Header("Animatior")]

    [SerializeField]
    Animator _endAnimation;

    [Header("Timer and Fallcount")]

    [SerializeField]
    Timer _levelTimer;

    [SerializeField]
    TextMeshProUGUI _timeTakenText;

    [SerializeField]
    TextMeshProUGUI _fallCountText;

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

            //Stops the level timer and hides it
            _levelTimer.StopTimer();
            _levelTimer.HideTimer();

            //Does a pulse effect on the goal and stops any further particles from spawning
            _finishParticleSystem.Emit(_finishParticleEmission);
            _goalParticleSystem.Stop();

            //Switches camera to an overhead view of the player if cameras are set and _switchCameras is true
            if (_switchCameras && _endCamera != null && _playerCamera != null)
            {
                _playerCamera.enabled = false;
                _endCamera.enabled = true;
            }

            //Gets the remaining time
            float remainingTime = _levelTimer._remainingTime;
            
            //Make minutes and seconds into whole numbers
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);

            //Changes text to show remaining time
            _timeTakenText.text = "Time Remaining: " + string.Format("{0:00}:{1:00}", minutes, seconds);

            

            _endAnimation.SetTrigger("LevelEnd");
        }
    }

    public void RetryLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ChangeFallCount(int falls)
    {
        //Changes text to show fall count
        _fallCountText.text = "Falls: " + falls.ToString();
    }
}
