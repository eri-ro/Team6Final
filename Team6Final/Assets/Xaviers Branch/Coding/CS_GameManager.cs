using System.Collections.Generic;
using UnityEngine;

// Handles the game timer, pausing, and the rewind mechanic
public class CS_GameManager : MonoBehaviour
{
    [Header("References")]
    public CS_GravityCharacterController player;

    [Header("Rewind Settings")]
    public float recordInterval = 0.05f; // how often (in seconds) we save a snapshot of the player
    public float rewindBudget   = 5f;    // max seconds worth of rewind you're allowed
    public float rewindCooldown = 10f;   // how long you have to wait after rewinding before it refills

    // Public read-only state so UI and other scripts can check these without changing them
    public float elapsedTime      { get; private set; } = 0f;
    public bool  IsRewinding      { get; private set; } = false;
    public bool  IsPaused         { get; private set; } = false;
    public bool  IsRunning        { get; private set; } = false;
    public float RewindBudgetLeft { get; private set; }
    public float CooldownLeft     { get; private set; } // how many seconds until rewind refills

    // Snapshot struct — stores just enough info to restore the player to a past state
    private struct PlayerSnapshot
    {
        public Vector3    position;
        public Quaternion rotation;
        public Vector3    localUp; // gravity direction at that moment
    }

    private List<PlayerSnapshot> history     = new List<PlayerSnapshot>(); // all recorded snapshots
    private float                recordTimer = 0f;   // counts up to recordInterval
    private float                rewindAccum = 0f;   // tracks our position in the history during rewind
    private Rigidbody            playerRb;

    private bool onCooldown = false; // true while we're waiting for rewind to refill

    void Start()
    {
        RewindBudgetLeft = rewindBudget;
        CooldownLeft     = 0f;

        if (player != null)
            playerRb = player.GetComponent<Rigidbody>();

        StartTimer();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O)) ResetTimer();
        if (Input.GetKeyDown(KeyCode.P)) TogglePause();

        // Rewind only works if: game is running, not paused, not on cooldown, budget remains, and Z is held
        bool wantsRewind = IsRunning
                        && !IsPaused
                        && !onCooldown
                        && RewindBudgetLeft > 0f
                        && Input.GetKey(KeyCode.Z);

        if (wantsRewind  && !IsRewinding) BeginRewind();
        if (!wantsRewind &&  IsRewinding) EndRewind();

        IsRewinding = wantsRewind;

        // Normal forward-time logic
        if (IsRunning && !IsRewinding)
        {
            if (!IsPaused)
                elapsedTime += Time.deltaTime; // tick the timer up

            // Count down the cooldown and refill budget when it hits zero
            if (onCooldown)
            {
                CooldownLeft -= Time.deltaTime;
                if (CooldownLeft <= 0f)
                {
                    CooldownLeft     = 0f;
                    RewindBudgetLeft = rewindBudget; // full refill
                    onCooldown       = false;
                }
            }

            // Record a snapshot on the interval
            recordTimer += Time.deltaTime;
            if (recordTimer >= recordInterval)
            {
                recordTimer -= recordInterval;
                RecordSnapshot();
            }
        }

        // Rewind logic — drains budget and moves the player backward through history
        if (IsRewinding)
        {
            StepRewind();

            RewindBudgetLeft -= Time.deltaTime; // 1 second of budget per real second

            if (RewindBudgetLeft <= 0f)
            {
                // Ran out of budget — force stop the rewind and start cooldown
                RewindBudgetLeft = 0f;
                EndRewind();
                IsRewinding  = false;
                onCooldown   = true;
                CooldownLeft = rewindCooldown;
            }
        }
    }

    // Starts the timer fresh from zero and clears all history
    public void StartTimer()
    {
        elapsedTime      = 0f;
        IsRunning        = true;
        IsPaused         = false;
        IsRewinding      = false;
        recordTimer      = 0f;
        RewindBudgetLeft = rewindBudget;
        CooldownLeft     = 0f;
        onCooldown       = false;
        history.Clear();
        RecordSnapshot(); // save initial position immediately
        Debug.Log("[GameManager] Timer started.");
    }

    public void StopTimer()
    {
        IsRunning = false;
        Debug.Log("[GameManager] Timer stopped.");
    }

    // Resets everything back to default without restarting
    public void ResetTimer()
    {
        elapsedTime      = 0f;
        IsRunning        = false;
        IsPaused         = false;
        IsRewinding      = false;
        recordTimer      = 0f;
        RewindBudgetLeft = rewindBudget;
        CooldownLeft     = 0f;
        onCooldown       = false;
        history.Clear();

        if (playerRb != null)
            playerRb.isKinematic = false; // make sure physics is re-enabled

        Debug.Log("[GameManager] Timer reset.");
    }

    public void TogglePause()
    {
        if (!IsRunning) return;

        // Can't pause while rewinding — end the rewind first
        if (!IsPaused && IsRewinding)
        {
            EndRewind();
            IsRewinding = false;
        }

        IsPaused = !IsPaused;
        Debug.Log("[GameManager] Timer " + (IsPaused ? "paused." : "resumed."));
    }

    // Saves the player's current position, rotation, and gravity direction
    void RecordSnapshot()
    {
        if (player == null) return;
        history.Add(new PlayerSnapshot
        {
            position = player.transform.position,
            rotation = player.transform.rotation,
            localUp  = player.localUp
        });
    }

    // Called once when rewind starts — freeze physics so we can manually move the player
    void BeginRewind()
    {
        if (playerRb != null)
        {
            playerRb.velocity        = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.isKinematic     = true; // kinematic = no physics forces affect it
        }
        rewindAccum = history.Count - 1f; // start at the most recent snapshot
    }

    // Called every frame while rewinding — moves the player back through history
    void StepRewind()
    {
        if (history.Count == 0 || player == null) return;

        // Step backward through history at the same rate we recorded it
        // (1/recordInterval = snapshots per second, so stepping that many per second = real-time rewind)
        rewindAccum -= (1f / recordInterval) * Time.deltaTime;
        rewindAccum  = Mathf.Max(rewindAccum, 0f); // don't go before the beginning

        // Grab the snapshot at our current position in history
        int idx = Mathf.Clamp(Mathf.RoundToInt(rewindAccum), 0, history.Count - 1);
        PlayerSnapshot snap = history[idx];

        // Teleport the player to the saved state
        player.transform.position = snap.position;
        player.transform.rotation = snap.rotation;
        player.localUp            = snap.localUp;

        // Rewind the timer display proportionally too
        float progress = (float)idx / Mathf.Max(history.Count - 1, 1);
        elapsedTime    = progress * elapsedTime;

        // Trim history that's now in the "future" so you can't re-rewind past where you stopped
        if (idx < history.Count - 1)
            history.RemoveRange(idx + 1, history.Count - idx - 1);
    }

    // Called when the player releases Z or runs out of budget
    void EndRewind()
    {
        if (playerRb != null)
            playerRb.isKinematic = false; // re-enable physics

        // Start the cooldown if we actually used any budget
        if (!onCooldown && RewindBudgetLeft < rewindBudget)
        {
            onCooldown   = true;
            CooldownLeft = rewindCooldown;
        }
    }

    // Draws a yellow line in the scene view showing the player's recorded path
    void OnDrawGizmos()
    {
        if (history == null || history.Count < 2) return;
        Gizmos.color = Color.yellow;
        for (int i = 1; i < history.Count; i++)
            Gizmos.DrawLine(history[i - 1].position, history[i].position);
    }
}