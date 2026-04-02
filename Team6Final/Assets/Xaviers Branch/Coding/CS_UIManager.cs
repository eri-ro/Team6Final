using UnityEngine;
using TMPro; // TextMeshPro — fancier text than the default Unity UI text

// Handles ALL the UI display stuff: timer, rewind bar, Hz display
public class CS_UIManager : MonoBehaviour
{
    [Header("References")]
    public CS_GameManager  gameManager;   // so we can read timer/rewind state
    public CS_MusicManager musicManager;  // so we can read audio data

    [Header("Timer Text")]
    public TMP_Text timerText;           // the text box that shows the timer
    public Color    normalColor   = Color.white;   // default timer color
    public Color    pausedColor   = Color.yellow;  // color when paused
    public Color    rewindColor   = Color.cyan;    // color while rewinding

    [Header("Rewind Budget Text (optional)")]
    public TMP_Text rewindText;                        // shows rewind time left / cooldown
    public Color    budgetReadyColor  = Color.green;   // rewind is available
    public Color    budgetDrainColor  = Color.cyan;    // currently rewinding
    public Color    cooldownColor     = Color.red;     // cooling down, can't rewind

    [Header("Hz Text")]
    public TMP_Text hzText;              // text that shows current pitch in Hz
    public float    minHz     = 0f;      // lowest pitch we expect to see
    public float    maxHz     = 2000f;   // highest pitch we expect to see
    public float    minScale  = 0.8f;    // smallest the Hz text can get
    public float    maxScale  = 1.6f;    // biggest the Hz text can get
    public float    scaleSmoothing = 6f; // how fast the text scales up/down (higher = snappier)

    private float currentHzScale; // tracks the text's current scale so we can lerp it smoothly

    void Start()
    {
        currentHzScale = minScale; // start at the small size
    }

    void Update()
    {
        // Call all three update functions every frame
        UpdateTimer();
        UpdateRewindBudget();
        UpdateHz();
    }

    // Formats and displays the elapsed game time as MM:SS.ms
    void UpdateTimer()
    {
        if (timerText == null || gameManager == null) return; // safety check

        float t     = gameManager.elapsedTime;            // raw seconds
        int minutes = Mathf.FloorToInt(t / 60f);          // whole minutes
        int seconds = Mathf.FloorToInt(t % 60f);          // remaining seconds
        int ms      = Mathf.FloorToInt((t * 100f) % 100f);// centiseconds (like 00:12.47)

        // Slap it all together in a nice format
        timerText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, ms);

        // Change color based on what state the game is in
        if      (gameManager.IsRewinding) timerText.color = rewindColor;
        else if (gameManager.IsPaused)    timerText.color = pausedColor;
        else                              timerText.color = normalColor;
    }

    // Shows how much rewind time is left, or the cooldown countdown
    void UpdateRewindBudget()
    {
        if (rewindText == null || gameManager == null) return;

        if (gameManager.CooldownLeft > 0f)
        {
            // Rewind just ended — player has to wait before using it again
            rewindText.text  = string.Format("Rewind ready in {0:0.0}s", gameManager.CooldownLeft);
            rewindText.color = cooldownColor;
        }
        else if (gameManager.IsRewinding)
        {
            // Currently rewinding — show how many seconds of budget are left
            rewindText.text  = string.Format("Rewind: {0:0.0}s left", gameManager.RewindBudgetLeft);
            rewindText.color = budgetDrainColor;
        }
        else
        {
            // Rewind is just sitting there ready to go
            rewindText.text  = string.Format("Rewind: {0:0.0}s", gameManager.RewindBudgetLeft);
            rewindText.color = budgetReadyColor;
        }
    }

    // Reads the current pitch and makes the Hz text pulse/grow with it
    void UpdateHz()
    {
        if (hzText == null) return;

        if (musicManager == null || musicManager.analyzer == null)
        {
            hzText.text = "Hz: --"; // no audio data available
            return;
        }

        float hz    = musicManager.analyzer.pitchValue;       // grab the current pitch in Hz
        hzText.text = "Hz: " + Mathf.RoundToInt(hz);         // round it and show it

        // Map the Hz value into a 0–1 range, then remap that to our scale range
        float target   = Mathf.Lerp(minScale, maxScale, Mathf.InverseLerp(minHz, maxHz, hz));

        // Smoothly move toward the target scale instead of snapping
        currentHzScale = Mathf.Lerp(currentHzScale, target, Time.deltaTime * scaleSmoothing);

        // Apply the scale to the text object (uniform scale on all axes)
        hzText.transform.localScale = Vector3.one * currentHzScale;
    }
}