using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputSettings : MonoBehaviour
{
    [SerializeField]
    Slider sensSlider;
    [SerializeField]
    Toggle 
        xToggle, 
        yToggle;

    PlayerController controller;

    void Awake()
    {
        //Looks for Player object in hierarchy of scene. Should only be one player, and if none it returns error log
        controller = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
        if (controller == null)
        {
            Debug.Log("Please put the Player prefab somewhere in the scene!");
            return;
        }

        //Checks if settings have been set before, initializes them if not
        if (PlayerPrefs.HasKey("Sens") && PlayerPrefs.HasKey("invertX") && PlayerPrefs.HasKey("invertY"))
        {
            sensSlider.value = PlayerPrefs.GetFloat("Sens");
            xToggle.isOn = PlayerPrefs.GetInt("invertX") == 1;
            yToggle.isOn = PlayerPrefs.GetInt("invertY") == 1;
            //Debug.Log("playerPrefs found, adding previous settings");
        }
        else
        {
            InitializeSettings();
            //Debug.Log("No playerPrefs found, initializing settings");
        }
    }

    public void ChangeSensitivity(float newSens)
    {
        if (controller == null)
        {
            Debug.Log("Please put the Player prefab somewhere in the scene!");
            return;
        }
        controller.UpdateCameraSensitivity(newSens);
        PlayerPrefs.SetFloat("Sens", newSens);
    }

    public void ChangeInvertX(bool enable)
    {
        if (controller == null)
        {
            Debug.Log("Please put the Player prefab somewhere in the scene!");
            return;
        }
        controller.InvertCameraX(enable);
        PlayerPrefs.SetInt("invertX", enable ? 1 : 0);
    }

    public void ChangeInvertY(bool enable)
    {
        if (controller == null)
        {
            Debug.Log("Please put the Player prefab somewhere in the scene!");
            return;
        }
        controller.InvertCameraY(enable);
        PlayerPrefs.SetInt("invertY", enable ? 1 : 0);
    }

    void InitializeSettings()
    {
        PlayerPrefs.SetFloat("Sens", 2f);
        PlayerPrefs.SetInt("invertX", 0);
        PlayerPrefs.SetInt("invertY", 1);
    }
}
