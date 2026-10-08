using UnityEngine;
using TMPro;

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
    public Transform escapeSign;
    public Collider[] exitWalls;
    [Min(0.1f)] public float exitDistance = 1.8f;
    public string nextSceneName = "Level2";
    public float fadeDuration = 1.5f;
    private bool transitioning;
    private bool reportedMissingScene;
    private TextMeshPro signMarker;
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
        if (escapeSign != null)
        {
            GameObject marker = new GameObject("Escape sign marker");
            marker.transform.SetParent(transform, false);
            signMarker = marker.AddComponent<TextMeshPro>();
            signMarker.text = "ESCAPE\n▼";
            signMarker.fontSize = 3f;
            signMarker.alignment = TextAlignmentOptions.Center;
            signMarker.rectTransform.sizeDelta = new Vector2(4f, 2f);
            signMarker.color = new Color(1f, 0.85f, 0.25f);
            marker.SetActive(IsEscapeUnlocked);
        }
    }

    private void OnArtifactCountChanged(int count) => RefreshEscapeState();

    private void OnDestroy()
    {
        if (artifactManager != null) artifactManager.ArtifactCountChanged -= OnArtifactCountChanged;
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        if (signMarker != null)
        {
            signMarker.gameObject.SetActive(IsEscapeUnlocked);
            signMarker.transform.position = escapeSign.position + Vector3.up * (0.8f + Mathf.Sin(Time.time * 3f) * 0.1f);
            if (Camera.main != null) signMarker.transform.rotation = Camera.main.transform.rotation;
        }
        if (!IsEscapeUnlocked || transitioning || player == null || exitWalls == null ||
            PauseManager.IsGamePaused || Time.timeScale <= 0f) return;
        foreach (Collider wall in exitWalls)
        {
            if (wall == null || !wall.enabled) continue;
            if (Vector3.Distance(player.position, wall.ClosestPoint(player.position)) <= exitDistance)
            {
                TryTransition();
                break;
            }
        }
    }

    private void TryTransition()
    {
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            if (!reportedMissingScene)
                Debug.LogError($"[EscapeManager] Add '{nextSceneName}' to the build scenes before escaping.", this);
            reportedMissingScene = true;
            return;
        }
        transitioning = true;
        // Freeze gameplay while the unscaled fade runs so the ghost cannot catch the player.
        Time.timeScale = 0f;
        foreach (PauseManager pause in FindObjectsByType<PauseManager>(FindObjectsSortMode.None))
            pause.enabled = false;
        ScreenFadeController fade = ScreenFadeController.Instance;
        if (fade == null) fade = gameObject.AddComponent<ScreenFadeController>();
        fade.FadeToScene(nextSceneName, fadeDuration);
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