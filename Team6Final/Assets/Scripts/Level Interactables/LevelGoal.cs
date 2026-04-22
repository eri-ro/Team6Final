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

    [Header("Animator")]

    [SerializeField]
    Animator _endAnimation;

    [Header("Timer and Fallcount")]

    [SerializeField]
    Timer _levelTimer;

    [SerializeField]
    TextMeshProUGUI _timeTakenText;

    [SerializeField]
    TextMeshProUGUI _fallCountText;

    [Header("Sound")]
    [SerializeField] AudioClip _goalClip;

    private void OnTriggerEnter(Collider other)
    {
        //Checks to see if player entered collider
        if (other.CompareTag("Player"))
        {
            // Play goal sound if player has audiosource and goalclip is set
            if (_goalClip != null)
            {
                AudioSource src = other.GetComponent<AudioSource>();
                if (src != null)
                    src.PlayOneShot(_goalClip);
            }

            //Stops player movement, and unlocks the mouse
            other.GetComponent<PlayerMotor>().ClearVelocity();
            other.GetComponent<PlayerController>().enabled = false;
            other.GetComponent<PlayerMotor>().enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            //Stops the level timer and hides it
            if (_levelTimer != null)
            {
                _levelTimer.StopTimer();
                _levelTimer.HideTimer();
            }

            //Does a pulse effect on the goal and stops any further particles from spawning
            if (_finishParticleSystem != null)
                _finishParticleSystem.Emit(_finishParticleEmission);
            if (_goalParticleSystem != null)
                _goalParticleSystem.Stop();

            //Switches camera to an overhead view of the player if cameras are set and _switchCameras is true
            if (_switchCameras && _endCamera != null && _playerCamera != null)
            {
                _playerCamera.enabled = false;
                _endCamera.enabled = true;
            }

            //Gets the remaining time
            if (_levelTimer != null && _timeTakenText != null)
            {
                float remainingTime = _levelTimer._remainingTime;
                int minutes = Mathf.FloorToInt(remainingTime / 60);
                int seconds = Mathf.FloorToInt(remainingTime % 60);
                _timeTakenText.text = "Time Remaining: " + string.Format("{0:00}:{1:00}", minutes, seconds);
            }

            if (_endAnimation != null)
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
        if (_fallCountText != null)
            _fallCountText.text = "Falls: " + falls.ToString();
    }
}
