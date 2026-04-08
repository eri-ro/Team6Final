using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DemoFunction : MonoBehaviour
{
    [SerializeField]
    public BasicPlayerController player;

    [SerializeField]
    public TMP_Text abilityInfo;

    int abilityIndex = 0;
    //Quits the game if escape is pressed
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            switch (abilityIndex)
            {
                case 0:
                    abilityInfo.text = "Current Ability: Dash";
                    abilityIndex++;
                    break;
                case 1:
                    abilityInfo.text = "Current Ability: High Jump";
                    abilityIndex++;
                    break;
                case 2:
                    abilityInfo.text = "Current Ability: Gravity Shift";
                    abilityIndex++;
                    break;
                case 3:
                    abilityInfo.text = "Current Ability: None";
                    abilityIndex = 0;
                    break;
            }
        }
    }
}
