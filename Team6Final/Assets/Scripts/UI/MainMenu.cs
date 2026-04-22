using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("Demo");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
