// Purpose: Shows a temporary White Lady at a corner after enough artifacts are collected.
// Use: Attach to a scare controller and assign the ghost, player, camera, corner and wall exit.
// How it works: Artifact progress arms the scare; looking at a nearby clear corner plays it once.

// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;

/// <summary>A visual-only, once-per-run encounter armed by artifact progress.</summary>
public class ArtifactCornerScare : MonoBehaviour
{
    // artifactManager: ArtifactManager supplying artifact progress to this component. Subscribe sets it from the
    // scene ArtifactManager registered in Awake.
    [Header("References")]
    public ArtifactManager artifactManager;
    // ghost: The existing White Lady. A visual copy is used; its patrol is not interrupted. Assigned in the
    // scene/prefab Inspector.
    [Tooltip("The existing White Lady. A visual copy is used; its patrol is not interrupted.")]
    public GhostEnemy ghost;
    // player: player reference used by this script. Start sets it from ghost.playerTransform.
    public Transform player;
    // playerCamera: player Camera used to test whether the scare corner is visible. Start retrieves it from Unity
    // first enabled camera tagged MainCamera. Inspector assignment may supply its initial value.
    public Camera playerCamera;
    // cornerSpawn: Feet position on the corridor floor. Assigned in the scene/prefab Inspector.
    [Tooltip("Feet position on the corridor floor.")]
    public Transform cornerSpawn;
    // wallExit: Place beyond a wall. The apparition deliberately ignores collision and navigation. Assigned in the
    // scene/prefab Inspector.
    [Tooltip("Place beyond a wall. The apparition deliberately ignores collision and navigation.")]
    public Transform wallExit;

    // requiredArtifacts: minimum artifact count that arms the corner scare. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Header("Trigger")]
    [Min(1)] public int requiredArtifacts = 2;
    // activationDistance: maximum player-to-corner distance for the scare to begin. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Min(0.1f)] public float activationDistance = 12f;
    // visibilityMask: Solid geometry used to check that the corner is visible. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Tooltip("Solid geometry used to check that the corner is visible.")]
    public LayerMask visibilityMask = Physics.DefaultRaycastLayers;

    // holdDuration: gameplay seconds the apparition stays still before leaving. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Header("Scare")]
    [Min(0f)] public float holdDuration = 1.2f;
    // exitSpeed: apparition movement distance per gameplay second toward wallExit. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Min(0.1f)] public float exitSpeed = 2.5f;
    // scareAudio: Optional sound played at the apparition when it appears. Assigned in the scene/prefab Inspector.
    [Tooltip("Optional sound played at the apparition when it appears.")]
    public AudioClip scareAudio;
    // audioVolume: volume multiplier for the positional scare sound. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    [Range(0f, 1f)] public float audioVolume = 0.8f;

    // IsArmed can be read by other scripts; only this class can write its backing state.
    public bool IsArmed { get; private set; }
    // HasPlayed can be read by other scripts; only this class can write its backing state.
    public bool HasPlayed { get; private set; }
    // apparition: temporary visual-only ghost GameObject created by the scare coroutine. Updated by PlayScare,
    // ClearApparition.
    private GameObject apparition;
    // subscribedManager: ArtifactManager whose progress event is currently connected. Subscribe sets it from
    // artifactManager.
    private ArtifactManager subscribedManager;

    /// <summary>When enabled, listen for artifact progress.</summary>
    private void OnEnable()
    {
        // Call ArtifactCornerScare.Subscribe; connect to the artifact manager once and check the current
        // collection count.
        Subscribe();
    }

    /// <summary>At scene start, find missing references and stop this component if setup is incomplete.</summary>
    private void Start()
    {
        // Instance and Camera.main may not have been available during OnEnable.
        // Call ArtifactCornerScare.Subscribe; connect to the artifact manager once and check the current
        // collection count.
        Subscribe();
        // If playerCamera is missing, run the following block.
        if (playerCamera == null)
            // Set playerCamera to Unity first enabled camera tagged MainCamera.
            playerCamera = Camera.main;
        // If player is missing and ghost exists, run the following block.
        if (player == null && ghost != null)
            // Set player to ghost.playerTransform.
            player = ghost.playerTransform;

        // If artifactManager == null || ghost == null || player == null or playerCamera == null or cornerSpawn is
        // missing or wallExit is missing, run the following block.
        if (artifactManager == null || ghost == null || player == null ||
            playerCamera == null || cornerSpawn == null || wallExit == null)
        {
            // Report the missing/invalid setup or state as a Unity Console warning.
            Debug.LogWarning("[ArtifactCornerScare] Assign the manager, ghost, player, camera, spawn and exit.", this);
            // Set enabled to false.
            enabled = false;
        }
    }

    /// <summary>Connect to the artifact manager once and check the current collection count.</summary>
    private void Subscribe()
    {
        // If artifactManager is missing, run the following block.
        if (artifactManager == null)
            // Set artifactManager to the scene ArtifactManager registered in Awake.
            artifactManager = ArtifactManager.Instance;
        // Stop this method early when artifactManager is missing or subscribedManager equals artifactManager.
        if (artifactManager == null || subscribedManager == artifactManager)
            // Return now; skip the rest of this method.
            return;

        // Copy artifactManager from this component into subscribedManager.
        subscribedManager = artifactManager;
        // Subscribe OnArtifactCountChanged to subscribedManager.ArtifactCountChanged so the callback runs when
        // that event is raised.
        subscribedManager.ArtifactCountChanged += OnArtifactCountChanged;
        // Call ArtifactCornerScare.OnArtifactCountChanged with unique pickup count stored by subscribedManager
        // ArtifactManager; arm the encounter when the required number of artifacts has been collected.
        OnArtifactCountChanged(subscribedManager.artifactsCollected);
    }

    /// <summary>Arm the encounter when the required number of artifacts has been collected.</summary>
    // count is a int argument supplied by ArtifactCornerScare.Subscribe.
    private void OnArtifactCountChanged(int count)
    {
        // If not (HasPlayed) and count is at least the largest of 1, requiredArtifacts, run the following block.
        if (!HasPlayed && count >= Mathf.Max(1, requiredArtifacts))
            // Set IsArmed to true.
            IsArmed = true;
    }

    /// <summary>Each frame, require an armed scare, a nearby player and a corner visible on screen.</summary>
    private void Update()
    {
        // Stop this method early when !IsArmed || HasPlayed || Time.timeScale <= 0f || PauseManager.IsGamePaused
        // || ghost == null || player == null or playerCamera == null or cornerSpawn is missing or wallExit is
        // missing.
        if (!IsArmed || HasPlayed || Time.timeScale <= 0f || PauseManager.IsGamePaused ||
            ghost == null || player == null || playerCamera == null || cornerSpawn == null || wallExit == null)
            // Return now; skip the rest of this method.
            return;

        // Stop this method early when the distance between player.position and cornerSpawn.position is above
        // activationDistance.
        if (Vector3.Distance(player.position, cornerSpawn.position) > activationDistance)
            // Return now; skip the rest of this method.
            return;

        // Use screen coordinates from 0 to 1 to require the corner inside the central camera view.
        // Store world position of cornerSpawn plus the world-up direction (positive Y) in local sightTarget.
        Vector3 sightTarget = cornerSpawn.position + Vector3.up;
        // Store camera-screen coordinates of sightTarget from playerCamera (X/Y are 0 to 1 inside the view) in
        // local viewport.
        Vector3 viewport = playerCamera.WorldToViewportPoint(sightTarget);
        // Stop this method early when viewport.z <= 0f || viewport.x < 0.1f or viewport.x > 0.9f or viewport.y is
        // below 0.1f or viewport Y component is above 0.9.
        if (viewport.z <= 0f || viewport.x < 0.1f || viewport.x > 0.9f ||
            viewport.y < 0.1f || viewport.y > 0.9f)
            // Return now; skip the rest of this method.
            return;

        // Check the view for solid walls before showing the ghost; triggers do not obstruct this ray.
        // Store sightTarget minus world position of playerCamera.transform in local sightLine.
        Vector3 sightLine = sightTarget - playerCamera.transform.position;
        // Process each hit in all Physics ray hits along the supplied ray, distance and layer mask.
        foreach (RaycastHit hit in Physics.RaycastAll(playerCamera.transform.position,
            sightLine.normalized, sightLine.magnitude, visibilityMask, QueryTriggerInteraction.Ignore))
        {
            // Held items and the player's body must not block their own view.
            // If hit.transform belongs to player Transform hierarchy, skip this entry and continue with the next
            // loop iteration.
            if (hit.transform.IsChildOf(player)) continue;
            // Return now; skip the rest of this method.
            return;
        }

        // Commit the once-per-run state before the coroutine starts, preventing repeated scares.
        // Set HasPlayed to true.
        HasPlayed = true;
        // Set IsArmed to false.
        IsArmed = false;
        // Start coroutine PlayScare(); Unity resumes it after each yield.
        StartCoroutine(PlayScare());
    }

    /// <summary>Over several frames, show a visual copy, play its scream, then move it through the wall.</summary>
    private IEnumerator PlayScare()
    {
        // Copy only animation configuration. The fresh resource supplies a rest
        // pose, and no enemy scripts, colliders or agents enter this hierarchy.
        // Set apparition to a new GameObject named "White Lady Apparition (visual only)".
        apparition = new GameObject("White Lady Apparition (visual only)");
        // Deactivate apparition.
        apparition.SetActive(false);
        // Set apparition.transform.position to world position of cornerSpawn.
        apparition.transform.position = cornerSpawn.position;
        // Store the first GhostAnimation component on ghost in local source.
        GhostAnimation source = ghost.GetComponent<GhostAnimation>();
        // Store a GhostAnimation component attached to apparition in local animation.
        GhostAnimation animation = apparition.AddComponent<GhostAnimation>();
        // If source exists, run the following block.
        if (source != null)
        {
            // Set animation.idleClip to source.idleClip.
            animation.idleClip = source.idleClip;
            // Set animation.walkingClip to source.walkingClip.
            animation.walkingClip = source.walkingClip;
            // Set animation.screamClip to source.screamClip.
            animation.screamClip = source.screamClip;
            // Set animation.blendDuration to source.blendDuration.
            animation.blendDuration = source.blendDuration;
        }
        // Call ArtifactCornerScare.Face with world position of player minus world position of cornerSpawn; turn
        // the apparition toward a direction without tilting it up or down.
        Face(player.position - cornerSpawn.position);
        // Activate apparition.
        apparition.SetActive(true);
        // Keep apparition cached by this component in local visual.
        GameObject visual = apparition;
        // Initialize screamDuration to 0.
        float screamDuration = 0f;
        // Process each animator in all Animator components on or beneath visual (including inactive children).
        foreach (Animator animator in visual.GetComponentsInChildren<Animator>(true))
            // Store the largest of screamDuration, the result of GhostAnimation.PlayScream using animator,
            // ghost.screamParameter in screamDuration.
            screamDuration = Mathf.Max(screamDuration, GhostAnimation.PlayScream(animator, ghost.screamParameter));
        // Keep the full scream alive after the visual disappears into the wall.
        // Call GhostAudio.PlayScare with scareAudio, world position of playerCamera.transform, audioVolume; play a
        // supplied scream at a position, or use the scene ghost scream as a fallback.
        GhostAudio.PlayScare(scareAudio, playerCamera.transform.position, audioVolume);

        // Yield a wait for the largest of holdDuration, screamDuration gameplay seconds (pause stops the timer);
        // Unity resumes the coroutine after it completes.
        yield return new WaitForSeconds(Mathf.Max(holdDuration, screamDuration));

        // If apparition is missing or wallExit is missing, run the following block.
        if (apparition == null || wallExit == null)
        {
            // Call ArtifactCornerScare.ClearApparition; delete the visual copy when its sequence ends or is
            // interrupted.
            ClearApparition();
            // End this coroutine immediately instead of waiting for another frame.
            yield break;
        }
        // Process each animator in all Animator components on or beneath visual (including inactive children).
        foreach (Animator animator in visual.GetComponentsInChildren<Animator>(true))
            // Call ArtifactCornerScare.SetMoving with animator, true; ask the ghost animation helper to switch the
            // apparition between idle and walking.
            SetMoving(animator, true);

        // Store world position of wallExit in local destination.
        Vector3 destination = wallExit.position;
        // Call ArtifactCornerScare.Face with destination minus world position of apparition.transform; turn the
        // apparition toward a direction without tilting it up or down.
        Face(destination - apparition.transform.position);
        // Repeat while squared length of (apparition.transform.position - destination) (avoids a square root) is
        // above 0.0001.
        while ((apparition.transform.position - destination).sqrMagnitude > 0.0001f)
        {
            // Scaled time freezes the sequence with pause and the lose screen.
            // Store world position of apparition.transform moved toward destination by at most Mathf.Max(0.1f,
            // exitSpeed) multiplied by Unity gameplay frame duration in seconds this step in
            // apparition.transform.position.
            apparition.transform.position = Vector3.MoveTowards(apparition.transform.position,
                destination, Mathf.Max(0.1f, exitSpeed) * Time.deltaTime);
            // Pause the coroutine until the next Unity frame so the animation/sequence can render.
            yield return null;
        }
        // Call ArtifactCornerScare.ClearApparition; delete the visual copy when its sequence ends or is
        // interrupted.
        ClearApparition();
    }

    /// <summary>Ask the ghost animation helper to switch the apparition between idle and walking.</summary>
    // animator is a Animator argument supplied by ArtifactCornerScare.PlayScare.
    // moving is a bool argument supplied by ArtifactCornerScare.PlayScare.
    private void SetMoving(Animator animator, bool moving)
    {
        // If ghost exists, call GhostAnimation.SetMoving with animator, ghost.movingParameter, moving; use this
        // custom driver when present, otherwise set a matching Animator bool parameter.
        if (ghost != null) GhostAnimation.SetMoving(animator, ghost.movingParameter, moving);
    }

    /// <summary>Turn the apparition toward a direction without tilting it up or down.</summary>
    // direction is a Vector3 argument supplied by ArtifactCornerScare.PlayScare.
    private void Face(Vector3 direction)
    {
        // Update the vertical Y component direction.y from 0.
        direction.y = 0f;
        // If squared length of direction (avoids a square root) is above 0.001, run the following block.
        if (direction.sqrMagnitude > 0.001f)
            // Store a rotation facing direction direction in apparition.transform.rotation.
            apparition.transform.rotation = Quaternion.LookRotation(direction);
    }

    /// <summary>When disabled, stop listening, stop the sequence and remove the temporary ghost.</summary>
    private void OnDisable()
    {
        // If subscribedManager exists, run the following block.
        if (subscribedManager != null)
            // Unsubscribe OnArtifactCountChanged from subscribedManager.ArtifactCountChanged so the callback stops
            // running when that event is raised.
            subscribedManager.ArtifactCountChanged -= OnArtifactCountChanged;
        // Clear subscribedManager to null so it no longer holds a reference.
        subscribedManager = null;
        // Stop every coroutine running on this component.
        StopAllCoroutines();
        // Call ArtifactCornerScare.ClearApparition; delete the visual copy when its sequence ends or is
        // interrupted.
        ClearApparition();
    }

    /// <summary>Delete the visual copy when its sequence ends or is interrupted.</summary>
    private void ClearApparition()
    {
        // If apparition exists, run the following block.
        if (apparition != null)
            // Schedule apparition for destruction at Unity end-of-frame cleanup.
            Destroy(apparition);
        // Clear apparition to null so it no longer holds a reference.
        apparition = null;
    }

    /// <summary>In the Editor, draw the spawn, exit route and activation range for placement.</summary>
    private void OnDrawGizmosSelected()
    {
        // Stop this method early when cornerSpawn is missing or wallExit is missing.
        if (cornerSpawn == null || wallExit == null)
            // Return now; skip the rest of this method.
            return;
        // Set Gizmos.color to Color.magenta.
        Gizmos.color = Color.magenta;
        // Call Gizmos.DrawWireSphere using world position of cornerSpawn, 0.5.
        Gizmos.DrawWireSphere(cornerSpawn.position, 0.5f);
        // Call Gizmos.DrawLine using world position of cornerSpawn, world position of wallExit.
        Gizmos.DrawLine(cornerSpawn.position, wallExit.position);
        // Call Gizmos.DrawWireSphere using world position of wallExit, 0.3.
        Gizmos.DrawWireSphere(wallExit.position, 0.3f);
        // Set Gizmos.color to Color.yellow.
        Gizmos.color = Color.yellow;
        // Call Gizmos.DrawWireSphere using world position of cornerSpawn, activationDistance.
        Gizmos.DrawWireSphere(cornerSpawn.position, activationDistance);
    }
}
