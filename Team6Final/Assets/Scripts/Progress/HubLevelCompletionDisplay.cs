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

    [SerializeField] GameObject endingPortal;

    [SerializeField] GameObject instructionText;
    void Start()
    {
        Apply();
    }

    public void Apply()
    {
        if (LevelProgress.Instance == null)
            return;

        LevelProgress p = LevelProgress.Instance;
        SetPair(_orchestraCompleteVisual, _orchestraIncompleteVisual, p.IsComplete(LevelId.Orchestra));
        SetPair(_rockCompleteVisual, _rockIncompleteVisual, p.IsComplete(LevelId.Rock));
        SetPair(_demoCompleteVisual, _demoIncompleteVisual, p.IsComplete(LevelId.Demo));
        SetPair(_edmCompleteVisual, _edmIncompleteVisual, p.IsComplete(LevelId.EDM));

        if (p.IsComplete(LevelId.Rock) || p.IsComplete(LevelId.Orchestra) || p.IsComplete(LevelId.EDM))
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.transform.position = this.transform.position;
                instructionText.SetActive(false);
            }
        }

        if (p.IsComplete(LevelId.Rock) && p.IsComplete(LevelId.Orchestra) && p.IsComplete(LevelId.EDM))
            endingPortal.SetActive(true);
        else
            endingPortal.SetActive(false);
    }

    static void SetPair(GameObject complete, GameObject incomplete, bool levelIsComplete)
    {
        if (complete != null)
        {
            complete.SetActive(levelIsComplete);
        }
        if (incomplete != null)
            incomplete.SetActive(!levelIsComplete);
    }
}
