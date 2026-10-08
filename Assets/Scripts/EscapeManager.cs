using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
    private TextMeshProUGUI escapeHint;
    private Renderer signRenderer;
    public const string EscapeObjective = "Escape via Cold Case Files in Archive A.";
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
            signRenderer = escapeSign.GetComponentInChildren<Renderer>();
            GameObject guide = new GameObject("Escape guidance", typeof(Canvas), typeof(CanvasScaler));
            guide.transform.SetParent(transform, false);
            Canvas canvas = guide.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = guide.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            GameObject label = new GameObject("Archive A exit hint", typeof(RectTransform));
            label.transform.SetParent(guide.transform, false);
            escapeHint = label.AddComponent<TextMeshProUGUI>();
            escapeHint.fontSize = 26f;
            escapeHint.alignment = TextAlignmentOptions.Center;
            escapeHint.color = new Color(1f, 0.85f, 0.25f);
            escapeHint.raycastTarget = false;
            RectTransform rect = escapeHint.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -36f);
            rect.sizeDelta = new Vector2(680f, 100f);
            label.SetActive(IsEscapeUnlocked);
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
        if (escapeHint != null)
        {
            escapeHint.gameObject.SetActive(IsEscapeUnlocked && !transitioning &&
                !PauseManager.IsGamePaused && Time.timeScale > 0f);
            if (IsEscapeUnlocked && escapeSign != null && player != null)
            {
                // Imported mesh pivots may be far from the visible room sign.
                Vector3 target = signRenderer != null ? signRenderer.bounds.center : escapeSign.position;
                Vector3 direction = target - player.position;
                direction.y = 0f;
                Transform view = Camera.main != null ? Camera.main.transform : player;
                Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up);
                float angle = Vector3.SignedAngle(forward, direction, Vector3.up);
                string bearing = Mathf.Abs(angle) > 135f ? "Turn around" :
                    angle > 25f ? "Head right >" : angle < -25f ? "< Head left" : "Straight ahead";
                escapeHint.text = $"EXIT — ARCHIVE A / COLD CASE FILES\n{bearing} · {direction.magnitude:0} m";
            }
        }
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
        // All callers, including EscapeDoor, must satisfy the same proximity gate.
        if (!IsEscapeUnlocked || transitioning || !IsPlayerNearExit() ||
            PauseManager.IsGamePaused || Time.timeScale <= 0f) return false;
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            if (!reportedMissingScene)
                Debug.LogError($"[EscapeManager] Add '{nextSceneName}' to the build scenes before escaping.", this);
            reportedMissingScene = true;
            return false;
        }
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
