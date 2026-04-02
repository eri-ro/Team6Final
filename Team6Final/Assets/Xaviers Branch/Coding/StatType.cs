using UnityEngine;

// These are basically labels we can pick from a dropdown in Unity
// StatType lets us choose WHAT we want to boost (speed, jump, or size)
public enum StatType { WalkSpeed, JumpForce, PlayerScale }

// This one's for pitch — do we care if pitch is high, low, or whatever
public enum PitchDirection
{
    Any,         // just always apply the boost, don't check pitch at all
    AboveTarget, // only kick in when the pitch goes ABOVE our target Hz
    BelowTarget  // only kick in when pitch is LOW (but never makes it worse than base)
}

// This is one "boost rule" — basically a little bundle of settings
// that says "when THIS happens to the audio, do THAT to the player"
[System.Serializable] // makes it show up nicely in the Unity inspector
public class SongStatBoost
{
    public StatType stat;          // which stat are we changing (speed/jump/scale)
    public float multiplier = 1f;  // how much to multiply it by (1 = no change, 2 = double, etc.)

    public bool reactiveToVolume;  // if true, the boost scales with how loud the audio is

    [Tooltip("Should this boost respond to pitch direction?")]
    public bool reactiveToPitch;   // if true, we check pitch before applying the boost

    [Tooltip("Only used when reactiveToPitch is true.")]
    public PitchDirection pitchDirection = PitchDirection.Any; // which direction matters

    [Tooltip("The Hz threshold used for pitch direction checks.")]
    public float targetPitch; // the frequency we compare against (in Hz)
}

// ScriptableObject means we can make this as an asset file in the project
// Like a little data container for a song's info
[CreateAssetMenu(fileName = "NewSongData", menuName = "Music/SongData")]
public class SongData : ScriptableObject
{
    public AudioClip clip;          // the actual audio file
    public float bpm;               // beats per minute — used to time beat pulses
    public SongStatBoost[] boosts;  // all the boost rules attached to this song
}