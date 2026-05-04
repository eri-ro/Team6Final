using UnityEngine;
using UnityEngine.Serialization;


// Toggles per-level pairs in the hub: for each level, the incomplete object is shown until
// the level is completed in LevelProgress, then it is hidden and the complete object is shown.

public class HubLevelCompletionDisplay : MonoBehaviour
{
    [Header("Complete — show only after this level is marked complete in LevelProgress")]
    [SerializeField] GameObject _orchestraCompleteVisual;
    [SerializeField] GameObject _rockCompleteVisual;
    [SerializeField] GameObject _demoCompleteVisual;
    [SerializeField] GameObject _edmCompleteVisual;

    [Header("Incomplete — show until the level is completed, then hidden")]
    [SerializeField] GameObject _orchestraIncompleteVisual;
    [SerializeField] GameObject _rockIncompleteVisual;
    [SerializeField] GameObject _demoIncompleteVisual;
    [SerializeField] GameObject _edmIncompleteVisual;

    public int levelsCompleted = 0;
    void Start()
    {
        Apply();
    }

    public void Apply()
    {
        if (LevelProgress.Instance == null)
            return;

        LevelProgress p = LevelProgress.Instance;
        levelsCompleted += SetPair(_orchestraCompleteVisual, _orchestraIncompleteVisual, p.IsComplete(LevelId.Orchestra));
        levelsCompleted += SetPair(_rockCompleteVisual, _rockIncompleteVisual, p.IsComplete(LevelId.Rock));
        levelsCompleted += SetPair(_demoCompleteVisual, _demoIncompleteVisual, p.IsComplete(LevelId.Demo));
        levelsCompleted += SetPair(_edmCompleteVisual, _edmIncompleteVisual, p.IsComplete(LevelId.EDM));

        if (levelsCompleted > 0)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.transform.position = this.transform.position;
            }
        }
    }

    static int SetPair(GameObject complete, GameObject incomplete, bool levelIsComplete)
    {
        int result = 0;
        if (complete != null)
        {
            complete.SetActive(levelIsComplete);
            result = 1;
        }
        if (incomplete != null)
            incomplete.SetActive(!levelIsComplete);
        return result;
    }
}
