using UnityEngine;
using UnityEngine.SceneManagement;

// Call from a trigger collider on the hub. Loads the build scene for this level when the player enters.
// Put the same LevelId on the "complete" and "incomplete" objects for that level; only one is visible at a time.
public class HubLevelPortal : MonoBehaviour
{
    [SerializeField] LevelId _destinationLevel;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        string scene = SceneNameFor(_destinationLevel);
        if (string.IsNullOrEmpty(scene))
        {
            Debug.LogWarning($"{nameof(HubLevelPortal)} on {name}: no scene for {_destinationLevel}.", this);
            return;
        }

        SceneManager.LoadScene(scene);
    }

    public static string SceneNameFor(LevelId id)
    {
        return id switch
        {
            LevelId.Orchestra => "Orchestra",
            LevelId.Rock => "Rock",
            LevelId.Demo => "Demo",
            LevelId.EDM => "EDM",
            _ => null
        };
    }
}
