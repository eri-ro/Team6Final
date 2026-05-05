using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameWin : MonoBehaviour
{
    public void Restart()
    {
        SceneManager.LoadScene("UI_MainMenu");
    }
}
