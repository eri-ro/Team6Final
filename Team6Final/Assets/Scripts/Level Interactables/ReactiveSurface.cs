using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReactiveSurface : MonoBehaviour
{
    static readonly int timeOfLastHitId = Shader.PropertyToID("_TimeOfLastHit");
    Material reactiveMaterial;

    [Tooltip("Make this the same as the index of the reactive surface material")]
    [SerializeField]
    int materialIndex = 1;

    private void Awake()
    {
        reactiveMaterial = GetComponent<MeshRenderer>().materials[materialIndex];
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            reactiveMaterial.SetFloat(timeOfLastHitId, Time.time);
        }
    }
}
