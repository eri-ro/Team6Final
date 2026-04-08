using UnityEngine;

public class CS_MusicManager : MonoBehaviour
{
    public SongData[] songs;
    public AudioSource audioSource;
    public CS_GravityCharacterController player;
    public CS_MusicAnalyzer analyzer;

    public float blendTime = 1f;

    private int currentIndex = 0;
    public SongData currentSong;
    private SongData targetSong;
    private float blendTimer = 0f;

    private float baseWalk;
    private float baseJump;
    private Vector3 baseScale;

    private float beatTimer = 0f;
    [HideInInspector] public bool isBeat = false;

    void Start()
    {
        baseWalk  = player.walkSpeed;
        baseJump  = player.jumpForce;
        baseScale = player.transform.localScale;

        if (songs.Length > 0)
            PlaySong(0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) SwitchSong(-1);
        if (Input.GetKeyDown(KeyCode.E)) SwitchSong(1);

        isBeat = false;
        if (currentSong != null && currentSong.bpm > 0f)
        {
            float beatInterval = 60f / currentSong.bpm;
            beatTimer += Time.deltaTime;
            if (beatTimer >= beatInterval)
            {
                beatTimer -= beatInterval;
                isBeat = true;
            }
        }

        if (targetSong != null)
        {
            blendTimer += Time.deltaTime;
            float t = Mathf.Clamp01(blendTimer / blendTime);
            ApplySongStats(t);

            if (t >= 1f)
            {
                currentSong = targetSong;
                targetSong = null;
            }
        }
        else if (currentSong != null)
        {
            ApplySongStats(1f);
        }
    }

    void SwitchSong(int dir)
    {
        int nextIndex = (currentIndex + dir + songs.Length) % songs.Length;
        PlaySong(nextIndex);
    }

    void PlaySong(int index)
    {
        currentIndex = index;
        targetSong   = songs[index];
        currentSong  = targetSong;
        blendTimer   = 0f;
        beatTimer    = 0f;

        if (audioSource != null && targetSong.clip != null)
        {
            audioSource.clip = targetSong.clip;
            audioSource.Play();
        }
    }

    void ApplySongStats(float t)
    {
        float   walk  = baseWalk;
        float   jump  = baseJump;
        Vector3 scale = baseScale;

        SongData song = targetSong ?? currentSong;
        if (song != null)
        {
            foreach (var boost in song.boosts)
            {
                float pitchNow   = analyzer.pitchValue;
                float multiplier = boost.multiplier;

                if (boost.reactiveToPitch)
                {
                    switch (boost.pitchDirection)
                    {
                        case PitchDirection.AboveTarget:
                            // Lerp 1 → multiplier across 0 → targetPitch.
                            // Once pitch surpasses targetPitch, hold at multiplier max.
                            multiplier = Mathf.Lerp(1f, boost.multiplier,
                                Mathf.Clamp01(pitchNow / boost.targetPitch));
                            break;

                        case PitchDirection.BelowTarget:
                            // Lerp multiplier → 1 across 0 → targetPitch.
                            // Low pitch = full multiplier, high pitch = held at 1 (never below base).
                            multiplier = Mathf.Lerp(boost.multiplier, 1f,
                                Mathf.Clamp01(pitchNow / boost.targetPitch));
                            break;

                        case PitchDirection.Any:
                        default:
                            multiplier = boost.targetPitch > 0f
                                ? boost.multiplier * Mathf.Clamp01(pitchNow / boost.targetPitch)
                                : boost.multiplier;
                            break;
                    }
                }

                if (boost.reactiveToVolume)
                    multiplier *= Mathf.Clamp01(analyzer.rmsValue * 10f);

                switch (boost.stat)
                {
                    case StatType.WalkSpeed:   walk  = baseWalk  * multiplier; break;
                    case StatType.JumpForce:   jump  = baseJump  * multiplier; break;
                    case StatType.PlayerScale: scale = baseScale * multiplier; break;
                }
            }
        }

        player.walkSpeed            = Mathf.Lerp(player.walkSpeed,             walk,  t);
        player.jumpForce            = Mathf.Lerp(player.jumpForce,             jump,  t);
        player.transform.localScale = Vector3.Lerp(player.transform.localScale, scale, t);
    }
}