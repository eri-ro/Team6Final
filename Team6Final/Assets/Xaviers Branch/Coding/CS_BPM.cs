using UnityEngine;

// Makes any object scale up/down based on the current music pitch
// Just slap this on a GameObject and hook up the analyzer
public class CS_BPM : MonoBehaviour
{
    [Header("References")]
    public CS_MusicAnalyzer analyzer; // where we get our pitch data from

    [Header("Pitch Settings")]
    public float targetPitch = 1000f; // the Hz value that maps to the MAX scale

    [Header("Scale Range")]
    public Vector3 minScale = Vector3.one * 0.8f; // scale when pitch is 0
    public Vector3 maxScale = Vector3.one * 1.4f; // scale when pitch hits targetPitch

    [Header("Smoothing")]
    public float lerpSpeed = 8f; // how fast it responds to pitch changes (higher = snappier)

    public void Start()
    {
        // If we forgot to assign the analyzer, try to find it automatically
        if (analyzer == null)
        {
            analyzer = FindFirstObjectByType<CS_MusicAnalyzer>();
        }
    }

    void Update()
    {
        if (analyzer == null)
            return; // nothing to do if there's no analyzer

        float pitch = analyzer.pitchValue; // get current pitch in Hz

        // Turn pitch into a 0–1 value: 0 at zero Hz, 1 at targetPitch
        float t = Mathf.Clamp01(pitch / targetPitch);

        // Interpolate between min and max scale based on that 0–1 value
        Vector3 targetScale = Vector3.Lerp(minScale, maxScale, t);

        // Smooth the transition so it doesn't jump instantly
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * lerpSpeed
        );
    }
}