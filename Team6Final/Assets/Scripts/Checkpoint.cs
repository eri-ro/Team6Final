using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public Transform spawnPoint;       // The player's updated spawn point

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Checkpoint")
        {
            spawnPoint = other.transform.GetChild(0);
        }
        else if (other.tag == "Killplane")
        {
            gameObject.transform.position = spawnPoint.transform.position;
        }
    }
}
