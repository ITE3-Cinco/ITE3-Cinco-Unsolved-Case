using UnityEngine;

public class EscapeManager : MonoBehaviour
{
    public static EscapeManager Instance;

    [Header("Required Collectible Groups")]
    [Tooltip("Assign parent objects containing the items required to unlock the escape door.")]
    public CollectibleGroup[] requiredGroups;

    [Header("Optional Existing Artifact Requirement")]
    public bool requireAllArtifacts = false;

    public bool IsEscapeUnlocked { get; private set; }

    [Header("Corridor Escape")]
    public Transform player;
    public Collider[] exitWalls;
    [Min(0.1f)] public float exitDistance = 1.8f;
    public string nextSceneName = "Lvl-2";
    public float fadeDuration = 1.5f;
    private bool transitioning;
    private bool reportedMissingScene;
    public const string EscapeObjective = "Escape the corridors through Archive A room.";
    private ArtifactManager artifactManager;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        artifactManager = ArtifactManager.Instance;
        if (artifactManager != null) artifactManager.ArtifactCountChanged += OnArtifactCountChanged;
        RefreshEscapeState();
    }

    private void OnArtifactCountChanged(int count) => RefreshEscapeState();

    private void OnDestroy()
    {
        if (artifactManager != null) artifactManager.ArtifactCountChanged -= OnArtifactCountChanged;
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        TryTransition();
    }

    public bool IsPlayerNearExit()
    {
        if (player == null || exitWalls == null) return false;
        foreach (Collider wall in exitWalls)
        {
            if (wall == null || !wall.enabled || !wall.gameObject.activeInHierarchy) continue;
            if (Vector3.Distance(player.position, wall.ClosestPoint(player.position)) <= exitDistance)
                return true;
        }
        return false;
    }

    public bool TryTransition()
    {
        // Unlocking the objective still requires reaching the exit before transitioning.
        if (!IsEscapeUnlocked || transitioning || SceneTransition.IsLoading || !IsPlayerNearExit() ||
            PauseManager.IsGamePaused || Time.timeScale <= 0f) return false;
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            if (!reportedMissingScene)
                Debug.LogError($"[EscapeManager] Add '{nextSceneName}' to the build scenes before escaping.", this);
            reportedMissingScene = true;
            return false;
        }
        CollectionProgress.PrepareForNextLevel(nextSceneName);
        transitioning = true;
        // Freeze gameplay while the unscaled fade runs so the ghost cannot catch the player.
        Time.timeScale = 0f;
        foreach (PauseManager pause in FindObjectsByType<PauseManager>(FindObjectsInactive.Exclude))
            pause.enabled = false;
        ScreenFadeController fade = ScreenFadeController.Instance;
        if (fade == null) fade = gameObject.AddComponent<ScreenFadeController>();
        fade.FadeToScene(nextSceneName, fadeDuration);
        return true;
    }

    public void RefreshEscapeState()
    {
        bool groupsComplete = true;

        if (requiredGroups != null)
        {
            foreach (CollectibleGroup group in requiredGroups)
            {
                if (group != null && !group.IsComplete)
                {
                    groupsComplete = false;
                    break;
                }
            }
        }

        bool artifactsComplete = true;
        if (requireAllArtifacts)
        {
            artifactsComplete = ArtifactManager.Instance != null &&
                                ArtifactManager.Instance.artifactsCollected >= ArtifactManager.Instance.totalArtifacts;
        }

        IsEscapeUnlocked = groupsComplete && artifactsComplete;
        Debug.Log($"[EscapeManager] Escape unlocked: {IsEscapeUnlocked}");
    }

    public bool CanEscape()
    {
        RefreshEscapeState();
        return IsEscapeUnlocked;
    }
}
