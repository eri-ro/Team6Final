using UnityEngine;
using UnityEngine.SceneManagement;

// Hook the End Screen "Return to Hub" button here: On Click() → LoadHub.
public class ReturnToHub : MonoBehaviour
{
    string _hubSceneName = "Hub Area";

    // Assign this to the button's OnClick() event
    public void LoadHub()
    {
        Time.timeScale = 1f;
        PauseMenu.isPaused = false;
        if (!string.IsNullOrEmpty(_hubSceneName))
            SceneManager.LoadScene(_hubSceneName);
    }
}
