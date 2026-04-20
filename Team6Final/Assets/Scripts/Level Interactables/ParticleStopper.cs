using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleStopper : MonoBehaviour
{
    [SerializeField]
    ParticleSystem[] particleSystems;

    [SerializeField]
    GameObject endText;

    [SerializeField]
    float endTextTime = 3;

    public void stopParticles()
    {
        if (particleSystems.Length > 0)
        {
            for (int i = 0; i < particleSystems.Length; i++)
            {
                particleSystems[i].Stop();
            }
            StartCoroutine(QuickText(endTextTime));
        }

    }

    private IEnumerator QuickText(float textTime)
    {
        endText.SetActive(true);
        yield return new WaitForSeconds(textTime);
        endText.SetActive(false);
    }
}
