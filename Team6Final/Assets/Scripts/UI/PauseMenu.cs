using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

// Attach to the root Canvas (or any always-active object)
public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;

    public GameObject pauseFirstButton;
    //public GameObject settingsFirstButton;
    public GameObject settingsClosedButton;


    public static bool isPaused;

    [Tooltip("Background music on an AudioSource")]
    public AudioSource musicPlayer;

    // Player movement scripts that take input
    private PlayerController playerController;
    private PlayerMotor playerMotor;

    void Awake()
    {
        if (pauseMenu == null)
        {
            Transform t = transform.Find("PauseMenu");
            if (t != null)
                pauseMenu = t.gameObject;
        }

        if (pauseMenu == null)
            Debug.LogError("PauseMenu: assign pause panel or add a child named PauseMenu.", this);

        playerController = GameObject.Find("Player").GetComponent<PlayerController>();
        playerMotor = GameObject.Find("Player").GetComponent<PlayerMotor>();
    }

    void Start()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
    }

    void Update()
    {
        if (pauseMenu == null)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Joystick1Button7))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        // Set the paused bools in each script to the same here
        playerController.paused = isPaused;
        playerMotor.paused = isPaused;
    }

    public void PauseGame()
    {
        if (pauseMenu == null)
            return;
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (musicPlayer != null)
            musicPlayer.Pause();

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(pauseFirstButton);
    }

    public void ResumeGame()
    {
        if (pauseMenu == null)
            return;
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (musicPlayer != null)
            musicPlayer.Play();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToHub()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("Hub Area");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("UI_MainMenu");
    }
}
