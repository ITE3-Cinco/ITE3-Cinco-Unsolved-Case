// Purpose: Uses an agimat to protect the player and make hostile ghosts retreat briefly.
// Use: Attach to the player with Inventory; optional materials show visual transparency.
// How it works: Nearby ghosts trigger a HUD warning; consuming an agimat starts a timed protection coroutine.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;

public class InvisibilityConsumable : MonoBehaviour
{
    // InvisibilityConsumable.Instance is the shared reference set to this component during initialization; other
    // scripts read it to find this manager.
    public static InvisibilityConsumable Instance;

    // duration: configured effect duration in seconds. Assigned in the scene/prefab Inspector; the initializer
    // below is the default.
    [Header("Invisibility Settings")]
    public float duration = 5f; // Invisibility length in seconds
    // isInvisible: shared protection state checked by ghost detection and catches. Updated by InvisibilityRoutine,
    // OnDestroy.
    public bool isInvisible = false;

    // originalMaterial: material restored when protection ends. Assigned in the scene/prefab Inspector.
    [Header("Visual Effects (Optional)")]
    public Material originalMaterial;
    // invisibleMaterial: optional temporary material used during protection. Assigned in the scene/prefab
    // Inspector.
    public Material invisibleMaterial; // Semi-transparent shader material
    // playerRenderer: optional player Renderer whose material changes during protection. Assigned in the
    // scene/prefab Inspector.
    public Renderer playerRenderer;

    // warningDistance: distance at which a carried agimat warns about hostile ghosts. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Header("Agimat Warning")]
    [Min(1f)] public float warningDistance = 16f;
    // GhostNearby can be read by other scripts; only this class can write its backing state.
    public bool GhostNearby { get; private set; }
    // inventory: Inventory holding player items and selection. Start retrieves it from the first Inventory
    // component on this GameObject.
    private Inventory inventory;
    // Populated in Awake so UseInvisibility() called before Start still works.
    // ghosts: scene GhostEnemy references checked for nearby hostile threats. Awake retrieves it from all scene
    // GhostEnemy components.
    private GhostEnemy[] ghosts = System.Array.Empty<GhostEnemy>();
    // invisibilityCoroutine: running protection sequence handle used to control its timer. Updated by
    // UseInvisibility, InvisibilityRoutine, OnDestroy.
    private Coroutine invisibilityCoroutine;

    /// <summary>Register the shared protection controller and find ghosts before early callers can use it.</summary>
    private void Awake()
    {
        // If Instance exists and Instance differs from this, run the following block.
        if (Instance != null && Instance != this)
        {
            // Schedule the GameObject that owns this component for destruction at Unity end-of-frame cleanup.
            Destroy(gameObject);
            // Return now; skip the rest of this method.
            return;
        }

        // Register this component in the class shared Instance field so other scripts can locate it without
        // repeated scene searches.
        Instance = this;

        // Populate ghosts here so the array is ready before Start runs,
        // ensuring UseInvisibility() works even if called very early.
        // Fix: use FindObjectsByType without the deprecated FindObjectsSortMode parameter.
        // Store all scene GhostEnemy components in ghosts.
        ghosts = FindObjectsByType<GhostEnemy>(FindObjectsInactive.Exclude);
    }

    /// <summary>Find the player inventory and refresh the ghost list after scene initialization.</summary>
    private void Start()
    {
        // Retrieve Inventory from this GameObject; inventory now points to the component storing its items and
        // selected slot.
        inventory = GetComponent<Inventory>();

        // Refresh in Start in case ghosts were added after Awake.
        // Store all scene GhostEnemy components in ghosts.
        ghosts = FindObjectsByType<GhostEnemy>(FindObjectsInactive.Exclude);
    }

    /// <summary>Set the danger warning when an agimat is carried and a hostile ghost is within range.</summary>
    private void Update()
    {
        // Set GhostNearby to false.
        GhostNearby = false;
        // If isInvisible or inventory is missing or not (inventory carried-agimat check from Inventory.HasAgimat),
        // return now; skip the rest of this method.
        if (isInvisible || inventory == null || !inventory.HasAgimat) return;
        // Process each ghost in ghosts.
        foreach (GhostEnemy ghost in ghosts)
        {
            // If ghost != null and ghost.isActiveAndEnabled and ghost.IsHostile and the distance between
            // transform.position and ghost.transform.position is at most warningDistance, run the following block.
            if (ghost != null && ghost.isActiveAndEnabled && ghost.IsHostile &&
                Vector3.Distance(transform.position, ghost.transform.position) <= warningDistance)
            {
                // Set GhostNearby to true.
                GhostNearby = true;
                // Leave this loop or switch case so no later statements in that block run.
                break;
            }
        }
    }

    /// <summary>During active gameplay, consume one agimat and start protection if it is not already active.</summary>
    public bool TryUseAgimat()
    {
        // If isInvisible || Time.timeScale <= 0f or PauseManager.IsGamePaused or inventory is missing or not (the
        // result of Inventory.ConsumeAgimat), run the following block.
        if (isInvisible || Time.timeScale <= 0f || PauseManager.IsGamePaused ||
            inventory == null || !inventory.ConsumeAgimat()) return false;
        // Call InvisibilityConsumable.UseInvisibility; start the timed effect only when the player is currently
        // unprotected.
        UseInvisibility();
        // If the scene NotificationManager registered in Awake exists, run the following block.
        if (NotificationManager.Instance != null)
            // Call NotificationManager.ShowNotification with "Agimat protects you. The ghost retreats.", 3; show a
            // new message and restart its hide timer so it receives its full display time.
            NotificationManager.Instance.ShowNotification("Agimat protects you. The ghost retreats.", 3f);
        // Return true to the method caller.
        return true;
    }

    /// <summary>Start the timed effect only when the player is currently unprotected.</summary>
    public void UseInvisibility()
    {
        // If not (isInvisible), run the following block.
        if (!isInvisible)
        {
            // Store the coroutine handle for InvisibilityRoutine(); Unity resumes it after each yield. in
            // invisibilityCoroutine.
            invisibilityCoroutine = StartCoroutine(InvisibilityRoutine());
        }
    }

    /// <summary>Enable protection, repel ghosts, wait for its duration and restore visibility.</summary>
    private IEnumerator InvisibilityRoutine()
    {
        // Set isInvisible to true.
        isInvisible = true;
        // Set GhostNearby to false.
        GhostNearby = false;
        // Process each ghost in ghosts.
        foreach (GhostEnemy ghost in ghosts)
            // If ghost exists, call ghost.Repel using duration.
            if (ghost != null) ghost.Repel(duration);
        // Write the diagnostic message to the Unity Console; it does not update game UI.
        Debug.Log("<color=purple>[Invisibility Active]</color> Player is now hidden from enemies!");

        // Apply visual transparency if assigned
        // If playerRenderer exists and invisibleMaterial exists, run the following block.
        if (playerRenderer != null && invisibleMaterial != null)
        {
            // Copy invisibleMaterial from this component into playerRenderer.material.
            playerRenderer.material = invisibleMaterial;
        }

        // Wait in gameplay seconds, so pausing also pauses the protection duration.
        // Yield a wait for duration gameplay seconds (pause stops the timer); Unity resumes the coroutine after it
        // completes.
        yield return new WaitForSeconds(duration);

        // Revert transparency
        // If playerRenderer exists and originalMaterial exists, run the following block.
        if (playerRenderer != null && originalMaterial != null)
        {
            // Copy originalMaterial from this component into playerRenderer.material.
            playerRenderer.material = originalMaterial;
        }

        // Set isInvisible to false.
        isInvisible = false;
        // Clear invisibilityCoroutine to null so it no longer holds a reference.
        invisibilityCoroutine = null;
        // Write the diagnostic message to the Unity Console; it does not update game UI.
        Debug.Log("<color=purple>[Invisibility Expired]</color> Enemies can see you again!");
    }

    /// <summary>Clear the shared reference, stop the timer and restore the original material.</summary>
    private void OnDestroy()
    {
        // If Instance equals this, run the following block.
        if (Instance == this)
            // Clear this class shared Instance reference so callers cannot reuse the removed component.
            Instance = null;

        // If invisibilityCoroutine exists, run the following block.
        if (invisibilityCoroutine != null)
        {
            // Stop the coroutine stored in invisibilityCoroutine.
            StopCoroutine(invisibilityCoroutine);
            // Clear invisibilityCoroutine to null so it no longer holds a reference.
            invisibilityCoroutine = null;
        }

        // If playerRenderer exists and originalMaterial exists, run the following block.
        if (playerRenderer != null && originalMaterial != null)
            // Copy originalMaterial from this component into playerRenderer.material.
            playerRenderer.material = originalMaterial;

        // Set isInvisible to false.
        isInvisible = false;
    }
}
