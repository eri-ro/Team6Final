using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Ending : MonoBehaviour
{
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void ToMainMenu()
    {
        SceneManager.LoadScene("UI_MainMenu");
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}
