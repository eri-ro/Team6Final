using UnityEngine;

public enum LevelId
{
    None = 0,
    Orchestra = 1,
    Rock = 2,
    Demo = 3,
    EDM = 4
}

// Tracks which levels are complete.
public class LevelProgress : MonoBehaviour
{
    public static LevelProgress Instance { get; private set; }

    [SerializeField]
    bool _orchestraComplete;
    [SerializeField]
    bool _rockComplete;
    [SerializeField]
    bool _demoComplete;
    [SerializeField]
    bool _edmComplete;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject(nameof(LevelProgress));
        go.AddComponent<LevelProgress>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool IsComplete(LevelId id)
    {
        return id switch
        {
            LevelId.Orchestra => _orchestraComplete,
            LevelId.Rock => _rockComplete,
            LevelId.Demo => _demoComplete,
            LevelId.EDM => _edmComplete,
            _ => false
        };
    }

    public void MarkComplete(LevelId id)
    {
        if (id == LevelId.None)
            return;

        switch (id)
        {
            case LevelId.Orchestra:
                _orchestraComplete = true;
                break;
            case LevelId.Rock:
                _rockComplete = true;
                break;
            case LevelId.Demo:
                _demoComplete = true;
                break;
            case LevelId.EDM:
                _edmComplete = true;
                break;
        }
    }
}
