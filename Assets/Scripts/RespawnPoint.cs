// Purpose: Stores the active map checkpoint pose and its collection snapshot.
// Use: Attach to checkpoint maps; optionally assign a child spawn transform.
// How it works: Interactable activates a map; static state carries it through an explicit same-scene retry.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    // CheckpointSaved lets other components subscribe a callback; Invoke later supplies its event arguments to
    // those listeners.
    public static event System.Action<Vector3> CheckpointSaved;
    // activeCheckpointPosition: shared checkpoint world position. Shared across instances; scene reload alone does
    // not reset static state.
    public static Vector3 activeCheckpointPosition;
    // activeCheckpointRotation: shared checkpoint rotation. Shared across instances; scene reload alone does not
    // reset static state.
    public static Quaternion activeCheckpointRotation;
    // hasReachedCheckpoint: shared flag set when a map is activated. Shared across instances; scene reload alone
    // does not reset static state.
    public static bool hasReachedCheckpoint = false;

    // Static checkpoint state survives scene reloads, so keep the scene identity and explicit retry request.
    // checkpointScenePath: scene path to which saved checkpoint data belongs. Shared across instances; scene
    // reload alone does not reset static state.
    private static string checkpointScenePath;
    // retryRequested: whether Try Again explicitly requested checkpoint reuse. Shared across instances; scene
    // reload alone does not reset static state.
    private static bool retryRequested;

    // Also runs when entering Play Mode with domain reload disabled.
    /// <summary>Clear static checkpoint events and progress when a new play session starts.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        // Clear CheckpointSaved to null so it no longer holds a reference.
        CheckpointSaved = null;
        // Call RespawnPoint.ResetCheckpoint; clear both collection history and the saved spawn for a fresh run.
        ResetCheckpoint();
    }

    // Only an explicit Try Again can carry a checkpoint through a scene reload.
    /// <summary>Allow only a matching requested retry to reuse checkpoint state on scene entry.</summary>
    // scenePath is a string argument supplied by PlayerRespawn.Awake.
    public static bool BeginRun(string scenePath)
    {
        // Store retryRequested and checkpointScenePath equals scenePath in local resume.
        bool resume = retryRequested && checkpointScenePath == scenePath;
        // Set retryRequested to false.
        retryRequested = false;
        // Begin collection handling first: ResetCheckpoint also clears session history.
        // CollectionProgress.BeginRun result for scenePath, resume
        CollectionProgress.BeginRun(scenePath, resume);
        // If not (resume), run the following block.
        if (!resume)
            // Call RespawnPoint.ClearSpawn; reset static spawn and retry values to their defaults.
            ClearSpawn();
        // Copy scenePath (argument from the caller) into checkpointScenePath.
        checkpointScenePath = scenePath;
        // Return resume to the method caller.
        return resume;
    }

    /// <summary>Mark an explicit retry for the saved scene, clearing unrelated checkpoint history.</summary>
    // scenePath is a string argument supplied by LoseSceneController.TryAgain.
    public static void PrepareForRetry(string scenePath)
    {
        // Recalculate retryRequested from checkpointScenePath equals scenePath.
        retryRequested = checkpointScenePath == scenePath;
        // If not (retryRequested), run the following block.
        if (!retryRequested)
            // Call RespawnPoint.ResetCheckpoint; clear both collection history and the saved spawn for a fresh
            // run.
            ResetCheckpoint();
    }

    // checkpointSpawnPoint: Optional child Transform where the player should respawn after interacting with this
    // checkpoint map. If empty, this object's position and rotation are used. Assigned in the scene/prefab
    // Inspector.
    [Header("Checkpoint Spawn")]
    [Tooltip("Optional child Transform where the player should respawn after interacting with this checkpoint map. If empty, this object's position and rotation are used.")]
    public Transform checkpointSpawnPoint;

    // isInitialSpawn: Use only for a scene's starting location. It is not activated by the player and does not
    // count as a discovered checkpoint. Assigned in the scene/prefab Inspector; the initializer below is the
    // default.
    [Header("Checkpoint Type")]
    [Tooltip("Use only for a scene's starting location. It is not activated by the player and does not count as a discovered checkpoint.")]
    public bool isInitialSpawn = false;

    /// <summary>Save this map spawn and current progress, then notify the ghost and HUD.</summary>
    public void ActivateCheckpoint()
    {
        // Stop this method early when isInitialSpawn.
        if (isInitialSpawn)
            // Return now; skip the rest of this method.
            return;

        // Set checkpointScenePath to the asset path of this GameObject scene.
        checkpointScenePath = gameObject.scene.path;
        // Read this map assigned checkpointSpawnPoint world position, or its own Transform position, and save it
        // in shared checkpoint state.
        activeCheckpointPosition = GetSpawnPosition();
        // Read this map assigned checkpointSpawnPoint rotation, or its own Transform rotation, and save it for
        // retry/respawn.
        activeCheckpointRotation = GetSpawnRotation();
        // Set hasReachedCheckpoint to true.
        hasReachedCheckpoint = true;
        // Save inventory and collection before notifying listeners of the new safe spawn.
        // Pass this object owning scene to CollectionProgress.Save; it copies collected IDs, inventory items and
        // selection into a checkpoint snapshot.
        CollectionProgress.Save(gameObject.scene);
        // Pass the saved world spawn to checkpoint listeners such as GhostEnemy.UpdateCheckpointPatrol; ?. allows
        // zero listeners.
        CheckpointSaved?.Invoke(activeCheckpointPosition);

        // Write the diagnostic message to the Unity Console; it does not update game UI.
        Debug.Log($"<color=green>[Checkpoint Activated]</color> Map checkpoint '{gameObject.name}' is now active. Respawn location: {activeCheckpointPosition}");

        // If the scene NotificationManager registered in Awake exists, run the following block.
        if (NotificationManager.Instance != null)
            // Call NotificationManager.ShowNotification with formatted text inserting {, 2.5; show a new message
            // and restart its hide timer so it receives its full display time.
            NotificationManager.Instance.ShowNotification($"Checkpoint updated. {CollectionProgress.TotalCollected} items collected.", 2.5f);
    }

    /// <summary>Use the assigned child spawn position, or this map object position as a fallback.</summary>
    private Vector3 GetSpawnPosition()
    {
        // Return world position of checkpointSpawnPoint when checkpointSpawnPoint exists; otherwise world position
        // of transform to the method caller.
        return checkpointSpawnPoint != null
            ? checkpointSpawnPoint.position
            : transform.position;
    }

    /// <summary>Use the assigned child spawn rotation, or this map object rotation as a fallback.</summary>
    private Quaternion GetSpawnRotation()
    {
        // Return world rotation of checkpointSpawnPoint when checkpointSpawnPoint exists; otherwise world rotation
        // of transform to the method caller.
        return checkpointSpawnPoint != null
            ? checkpointSpawnPoint.rotation
            : transform.rotation;
    }

    /// <summary>Clear both collection history and the saved spawn for a fresh run.</summary>
    public static void ResetCheckpoint()
    {
        // Call CollectionProgress.Reset; clear collection and checkpoint history when starting a fresh session.
        CollectionProgress.Reset();
        // Call RespawnPoint.ClearSpawn; reset static spawn and retry values to their defaults.
        ClearSpawn();
    }

    /// <summary>Reset static spawn and retry values to their defaults.</summary>
    private static void ClearSpawn()
    {
        // Set activeCheckpointPosition to the zero vector (no movement or offset).
        activeCheckpointPosition = Vector3.zero;
        // Set activeCheckpointRotation to the identity rotation (no rotation).
        activeCheckpointRotation = Quaternion.identity;
        // Set hasReachedCheckpoint to false.
        hasReachedCheckpoint = false;
        // Clear checkpointScenePath to null so it no longer holds a reference.
        checkpointScenePath = null;
        // Set retryRequested to false.
        retryRequested = false;
    }

    /// <summary>Draw the spawn marker in the Editor, using a different color for the initial spawn.</summary>
    private void OnDrawGizmos()
    {
        // Set Gizmos.color to Color.green when isInitialSpawn; otherwise Color.cyan.
        Gizmos.color = isInitialSpawn ? Color.green : Color.cyan;
        // Call Gizmos.DrawWireSphere using the result of RespawnPoint.GetSpawnPosition, 0.6.
        Gizmos.DrawWireSphere(GetSpawnPosition(), 0.6f);
    }
}