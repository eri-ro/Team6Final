using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelWin : MonoBehaviour
{
    public void Restart()
    {
        SceneManager.LoadScene("Hub Area");
    }
}
