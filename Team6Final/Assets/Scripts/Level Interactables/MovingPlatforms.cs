using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatforms : MonoBehaviour
{
    public GameObject[] pathNodes;      // Array of path nodes
    public int nodeIndex = 0;           // Index of the array, starts at index 0
    public float platformSpeed = 3f;    // Movement speed of the platform


    // Start is called before the first frame update
    void Start()
    {
        transform.position = pathNodes[nodeIndex].transform.position;   // Move the platform to the starting node
    }

    private void FixedUpdate()
    {
        MoveToPoint();      // Move platforms at a fixed update speed, to match physics of player
    }

    void MoveToPoint()
    {
        // Change target node when platform gets close to current target
        if(Vector3.Distance(transform.position, pathNodes[nodeIndex].transform.position) < 0.5f)
            nodeIndex = (nodeIndex + 1) % pathNodes.Length;

        //Move to target node
        transform.position = Vector3.MoveTowards(transform.position, pathNodes[nodeIndex].transform.position, platformSpeed * Time.deltaTime);
    }

    // Make it so player moves with platform while standing on it
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
            other.transform.parent = this.gameObject.transform;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
            other.transform.parent = null;
    }
}
