using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI _timerText;

    [SerializeField]
    public float _remainingTime;

    [SerializeField]
    string _textBeforeTimer;

    [SerializeField]
    bool _includeMinutes = true;

    [SerializeField]
    bool _started = true;

    [SerializeField]
    string _timerEndText;

    [SerializeField]
    bool _useEndText;

    [SerializeField]
    bool _skipZero = false;

    [Header("Player Settings on completion")]
    [SerializeField]
    [Tooltip("0 = none, 1 = dash, 2 = highjump, 3 = gravityshift")]
    int _abilityValue = 0;

    [SerializeField]
    bool _enablePlayerMovementOnFinish;

    [SerializeField]
    bool _disablePlayerMovementOnFinish;

    [Header("Next Timer")]
    [SerializeField]
    Timer _nextTimer;

    [SerializeField]
    bool _startNextTimer;

    [Header("Trigger Animation After Completion")]
    [SerializeField]
    [Tooltip("Sends a TimerComplete signal to every animator")]
    Animator[] _followupAnimations;

    [Header("Sound")]
    public AudioClip timerEndClip;
    public AudioSource audioSource;

    GameObject _player;
    PauseMenu _pauseMenu;

    // Only refresh TMP when the shown clock digits change (avoids string.Format + GC every frame = micro-stutter).
    int _lastDisplayHash = int.MinValue;

    private void Awake()
    {
        //Looks for Player and PauseMenu objects in hierarchy of scene. Should only be one player and pause menu, and if none it returns error log
        _player = GameObject.FindWithTag("Player");
        _pauseMenu = GameObject.FindWithTag("PauseMenu").GetComponent<PauseMenu>();
        if (_player == null)
        {
            Debug.Log("Please put the Player prefab somewhere in the scene!");
            return;
        }
        if (_pauseMenu == null)
        {
            Debug.Log("Please put the PauseMenu prefab somewhere in the scene!");
            return;
        }
    }
    // Update is called once per frame
    void Update()
    {
        //Only progresses timer if _started is true
        if (_started)
        {
            //time is decreased according to deltaTime, stops if it reaches ~0
            if (_remainingTime > 0)
            {
                _remainingTime -= Time.deltaTime;
            }
            else if (_remainingTime < 0.1)
            {
                _remainingTime = 0;
                _started = false;

                // Disables/enables player movement depending on what is set
                if (_enablePlayerMovementOnFinish)
                {
                    EnablePlayerMovement(_abilityValue);
                }
                else if (_disablePlayerMovementOnFinish)
                {
                    DisablePlayerMovement(!_useEndText);
                }

                //Starts the next timer if linked and _startNextTimer is true
                if (_startNextTimer && _nextTimer != null)
                {
                    _nextTimer.StartTimer();
                    _nextTimer.gameObject.SetActive(true);
                }

                //Displays end text if useEndText is true
                if (_useEndText)
                {
                    if (_timerText != null)
                        _timerText.text = _timerEndText;
                    return;
                }

                // Attempts to play all animations set in Followup Animations
                if (_followupAnimations != null)
                {
                    for (int i = 0; i < _followupAnimations.Length; i++)
                    {
                        if (_followupAnimations[i] != null)
                            _followupAnimations[i].SetTrigger("TimerComplete");
                    }
                }
                // Plays timerEndClip audio if set
                if (audioSource != null && timerEndClip != null)
                {
                    audioSource.PlayOneShot(timerEndClip);
                }
            }

            if (_timerText == null)
                return;

            //Make minutes and seconds into whole numbers
            int minutes = Mathf.FloorToInt(_remainingTime / 60);
            int seconds = Mathf.FloorToInt(_remainingTime % 60);

            //Increases seconds by one if _skipZero is enabled
            if (minutes == 0 && _skipZero)
                seconds++;

            int displayHash;
            if (_includeMinutes)
                displayHash = minutes * 100 + seconds;
            else
                displayHash = seconds;

            if (displayHash == _lastDisplayHash)
                return;

            _lastDisplayHash = displayHash;
            if (_includeMinutes)
                _timerText.text = _textBeforeTimer + string.Format("{0:00}:{1:00}", minutes, seconds);
            else
                _timerText.text = _textBeforeTimer + seconds.ToString();
        }
    }

    public void StartTimer()
    {
        _started = true;
        _lastDisplayHash = int.MinValue;
    }

    public void StopTimer()
    {
        _started = false;
    }

    public void HideTimer()
    {
        gameObject.SetActive(false);
    }
    //Enables the player to move, used after the intro countdown
    public void EnablePlayerMovement(int abilityValue)
    {
        if (_player != null)
        {
            _player.GetComponent<PlayerController>().canMove = true;
        }
        else
        {
            Debug.Log("Connect the player GameObject to the Timer to disable their movement!", this);
        }
        if (_pauseMenu != null)
        {
            _pauseMenu.canPause = true;
        }
    }
    //Stops all player movement and unlocks the mouse, used whenever the player game overs. Also hides the timer if there is no end text
    public void DisablePlayerMovement(bool hideTimer)
    {
        if (_player == null)
        {
            Debug.Log("Connect the player GameObject to the Timer to disable their movement!", this);
            if (hideTimer)
                HideTimer();
            return;
        }

        _player.GetComponent<PlayerMotor>().ClearVelocity();
        _player.GetComponent<PlayerController>().canMove = false;

        if (_pauseMenu != null)
        {
            _pauseMenu.canPause = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (hideTimer)
            HideTimer();
    }
}
