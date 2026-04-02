using UnityEngine;

// Grabs audio data every frame and calculates volume (RMS) and pitch
[RequireComponent(typeof(AudioSource))] // needs an AudioSource on the same GameObject
public class CS_MusicAnalyzer : MonoBehaviour
{
    public AudioSource audioSource;  // the thing actually playing the music
    public int sampleSize = 1024;    // how many audio samples to grab at once (power of 2 = good)

    [HideInInspector] public float rmsValue;   // current volume (0 = silent, higher = louder)
    [HideInInspector] public float pitchValue; // current dominant frequency in Hz

    private float[] samples;  // raw waveform data buffer
    private float[] spectrum; // frequency data buffer (for pitch detection)

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>(); // grab it automatically if not assigned

        // Initialize both arrays to the right size
        samples  = new float[sampleSize];
        spectrum = new float[sampleSize];
    }

    void Update()
    {
        AnalyzeAudio(); // run the analysis every single frame
    }

    void AnalyzeAudio()
    {
        // Fill the samples array with recent raw audio output data
        audioSource.GetOutputData(samples, 0);

        // RMS (Root Mean Square) = a way to measure "loudness" mathematically
        // Square each sample, average them, then square root the result
        float sum = 0f;
        for (int i = 0; i < sampleSize; i++)
            sum += samples[i] * samples[i]; // square each sample and add it up
        rmsValue = Mathf.Sqrt(sum / sampleSize); // sqrt of the average = RMS volume

        // Get frequency spectrum data using BlackmanHarris window (reduces audio artifacts)
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        // Find which frequency bin has the most energy — that's our dominant pitch
        float maxV = 0f;
        int   maxN = 0;
        for (int i = 0; i < sampleSize; i++)
        {
            if (spectrum[i] > maxV)
            {
                maxV = spectrum[i]; // new loudest bin found
                maxN = i;           // remember which bin it was
            }
        }

        // Convert the bin index into an actual Hz frequency value
        // (output sample rate / 2 = max representable frequency, divide by bin count to get Hz per bin)
        pitchValue = maxN * (AudioSettings.outputSampleRate / 2f) / sampleSize;
    }
}