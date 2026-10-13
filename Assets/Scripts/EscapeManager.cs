// Purpose: Unlocks the corridor exit after the assigned collection requirements are met.
// Use: Attach to the corridor manager; assign required groups, player and exit wall colliders.
// How it works: Recheck collection progress; reaching an unlocked exit freezes play and loads the next scene.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;

public class EscapeManager : MonoBehaviour
{
    // EscapeManager.Instance is the shared reference set to this component during initialization; other scripts
    // read it to find this manager.
    public static EscapeManager Instance;

    // requiredGroups: Assign parent objects containing the items required to unlock the escape door. Assigned in
    // the scene/prefab Inspector.
    [Header("Required Collectible Groups")]
    [Tooltip("Assign parent objects containing the items required to unlock the escape door.")]
    public CollectibleGroup[] requiredGroups;

    // requireAllArtifacts: whether escape also requires ArtifactManager completion. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Header("Optional Existing Artifact Requirement")]
    public bool requireAllArtifacts = false;

    // IsEscapeUnlocked can be read by other scripts; only this class can write its backing state.
    public bool IsEscapeUnlocked { get; private set; }

    // player: player reference used by this script. Assigned in the scene/prefab Inspector.
    [Header("Corridor Escape")]
    public Transform player;
    // exitWalls: colliders marking the corridor escape location. Assigned in the scene/prefab Inspector.
    public Collider[] exitWalls;
    // exitDistance: maximum distance from the player to an exit collider. Assigned in the scene/prefab Inspector;
    // the initializer below is the default.
    [Min(0.1f)] public float exitDistance = 1.8f;
    // nextSceneName: build-scene name requested after escape or story completion. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    public string nextSceneName = "Lvl-2";
    // fadeDuration: seconds allocated to a fade. Assigned in the scene/prefab Inspector; the initializer below is
    // the default.
    public float fadeDuration = 1.5f;
    // transitioning: guard preventing the level transition from being requested twice. Updated by TryTransition.
    private bool transitioning;
    // reportedMissingScene: reported missing scene. Updated by TryTransition.
    private bool reportedMissingScene;
    // EscapeObjective: escape objective. Fixed script constant; callers cannot change it.
    public const string EscapeObjective = "Escape the corridors through Archive A room.";
    // artifactManager: ArtifactManager supplying artifact progress to this component. Start sets it from the scene
    // ArtifactManager registered in Awake.
    private ArtifactManager artifactManager;

    /// <summary>Register one shared escape manager for this scene.</summary>
    private void Awake()
    {
        // If Instance is missing, run the following block.
        if (Instance == null)
            // Register this component in the class shared Instance field so other scripts can locate it without
            // repeated scene searches.
            Instance = this;
        else
            // Schedule the GameObject that owns this component for destruction at Unity end-of-frame cleanup.
            Destroy(gameObject);
    }

    /// <summary>Listen for artifact changes and calculate the starting escape permission.</summary>
    private void Start()
    {
        // Set artifactManager to the scene ArtifactManager registered in Awake.
        artifactManager = ArtifactManager.Instance;
        // If artifactManager exists, subscribe OnArtifactCountChanged to artifactManager.ArtifactCountChanged so
        // the callback runs when that event is raised.
        if (artifactManager != null) artifactManager.ArtifactCountChanged += OnArtifactCountChanged;
        // Call EscapeManager.RefreshEscapeState; unlock escape only when every required group and optional
        // artifact total is complete.
        RefreshEscapeState();
    }

    /// <summary>Recheck escape requirements whenever artifact progress changes.</summary>
    // count is a int argument supplied by the code, event or Inspector callback invoking OnArtifactCountChanged.
    // Call EscapeManager.RefreshEscapeState; unlock escape only when every required group and optional artifact
    // total is complete.
    private void OnArtifactCountChanged(int count) => RefreshEscapeState();

    /// <summary>Unsubscribe from artifact events and clear the shared manager reference.</summary>
    private void OnDestroy()
    {
        // If artifactManager exists, unsubscribe OnArtifactCountChanged from artifactManager.ArtifactCountChanged
        // so the callback stops running when that event is raised.
        if (artifactManager != null) artifactManager.ArtifactCountChanged -= OnArtifactCountChanged;
        // If Instance equals this, clear this class shared Instance reference so callers cannot reuse the removed
        // component.
        if (Instance == this) Instance = null;
    }

    /// <summary>After normal frame updates, check whether the player can leave now.</summary>
    private void LateUpdate()
    {
        // Call EscapeManager.TryTransition; validate escape conditions and the destination, then begin a single
        // scene transition.
        TryTransition();
    }

    /// <summary>Measure from the player to the nearest point on each active exit collider.</summary>
    public bool IsPlayerNearExit()
    {
        // If player is missing or exitWalls is missing, return false to the method caller.
        if (player == null || exitWalls == null) return false;
        // Process each wall in exitWalls.
        foreach (Collider wall in exitWalls)
        {
            // If wall is missing or not (wall.enabled) or not (wall.gameObject and its parent objects are active),
            // skip this entry and continue with the next loop iteration.
            if (wall == null || !wall.enabled || !wall.gameObject.activeInHierarchy) continue;
            // If the distance between player.position and wall.ClosestPoint(player.position) is at most
            // exitDistance, run the following block.
            if (Vector3.Distance(player.position, wall.ClosestPoint(player.position)) <= exitDistance)
                // Return true to the method caller.
                return true;
        }
        // Return false to the method caller.
        return false;
    }

    /// <summary>Validate escape conditions and the destination, then begin a single scene transition.</summary>
    public bool TryTransition()
    {
        // Unlocking the objective still requires reaching the exit before transitioning.
        // If !IsEscapeUnlocked || transitioning || SceneTransition.IsLoading or !IsPlayerNearExit() or
        // PauseManager.IsGamePaused or Unity gameplay clock multiplier (0 freezes gameplay time) is at most 0, run
        // the following block.
        if (!IsEscapeUnlocked || transitioning || SceneTransition.IsLoading || !IsPlayerNearExit() ||
            PauseManager.IsGamePaused || Time.timeScale <= 0f) return false;
        // If not (Unity can load build-scene nextSceneName), run the following block.
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            // If not (reportedMissingScene), run the following block.
            if (!reportedMissingScene)
                // Report the failed setup or state as a Unity Console error.
                Debug.LogError($"[EscapeManager] Add '{nextSceneName}' to the build scenes before escaping.", this);
            // Set reportedMissingScene to true.
            reportedMissingScene = true;
            // Return false to the method caller.
            return false;
        }
        // Call CollectionProgress.PrepareForNextLevel with nextSceneName; mark the destination that may carry
        // collection history after an escape.
        CollectionProgress.PrepareForNextLevel(nextSceneName);
        // Set transitioning to true.
        transitioning = true;
        // Freeze gameplay while the unscaled fade runs so the ghost cannot catch the player.
        // Set Unity gameplay clock multiplier to zero; scaled movement/timers stop, while code using real time can
        // continue.
        Time.timeScale = 0f;
        // Process each pause in all scene PauseManager components.
        foreach (PauseManager pause in FindObjectsByType<PauseManager>(FindObjectsInactive.Exclude))
            // Disable pause by changing its component enabled flag.
            pause.enabled = false;
        // Store the fade controller registered in Awake in local fade.
        ScreenFadeController fade = ScreenFadeController.Instance;
        // If fade is missing, store a ScreenFadeController component attached to the GameObject that owns this
        // component in fade.
        if (fade == null) fade = gameObject.AddComponent<ScreenFadeController>();
        // Call ScreenFadeController.FadeToScene with nextSceneName, fadeDuration; pass the destination and
        // duration to the common scene transition loader.
        fade.FadeToScene(nextSceneName, fadeDuration);
        // Return true to the method caller.
        return true;
    }

    /// <summary>Unlock escape only when every required group and optional artifact total is complete.</summary>
    public void RefreshEscapeState()
    {
        // First require every configured pickup group to be complete.
        // Initialize groupsComplete to true.
        bool groupsComplete = true;

        // If requiredGroups exists, run the following block.
        if (requiredGroups != null)
        {
            // Process each group in requiredGroups.
            foreach (CollectibleGroup group in requiredGroups)
            {
                // If group exists and not (group pickup completion check from CollectibleGroup), run the following
                // block.
                if (group != null && !group.IsComplete)
                {
                    // Set groupsComplete to false.
                    groupsComplete = false;
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
                }
            }
        }

        // Then apply the optional artifact requirement; both checks must pass to unlock escape.
        // Initialize artifactsComplete to true.
        bool artifactsComplete = true;
        // If requireAllArtifacts, run the following block.
        if (requireAllArtifacts)
        {
            // Set artifactsComplete to the scene ArtifactManager registered in Awake exists and unique pickup
            // count stored by ArtifactManager.Instance ArtifactManager is at least required artifact count
            // configured on ArtifactManager.Instance.
            artifactsComplete = ArtifactManager.Instance != null &&
                                ArtifactManager.Instance.artifactsCollected >= ArtifactManager.Instance.totalArtifacts;
        }

        // Combine the calculated group and artifact results; EscapeManager permits escape only when both are true.
        IsEscapeUnlocked = groupsComplete && artifactsComplete;
        // Write the diagnostic message to the Unity Console; it does not update game UI.
        Debug.Log($"[EscapeManager] Escape unlocked: {IsEscapeUnlocked}");
    }

    /// <summary>Refresh the requirements and return the current escape permission to a caller.</summary>
    public bool CanEscape()
    {
        // Call EscapeManager.RefreshEscapeState; unlock escape only when every required group and optional
        // artifact total is complete.
        RefreshEscapeState();
        // Return IsEscapeUnlocked to the method caller.
        return IsEscapeUnlocked;
    }
}
