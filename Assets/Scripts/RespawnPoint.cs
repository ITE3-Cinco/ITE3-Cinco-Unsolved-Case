using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    public static event System.Action<Vector3> CheckpointSaved;
    public static Vector3 activeCheckpointPosition;
    public static Quaternion activeCheckpointRotation;
    public static bool hasReachedCheckpoint = false;

    private static string checkpointScenePath;
    private static bool retryRequested;

    // Also runs when entering Play Mode with domain reload disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        CheckpointSaved = null;
        ResetCheckpoint();
    }

    // Only an explicit Try Again can carry a checkpoint through a scene reload.
    public static bool BeginRun(string scenePath)
    {
        bool resume = retryRequested && hasReachedCheckpoint && checkpointScenePath == scenePath;
        retryRequested = false;
        if (!resume)
            ResetCheckpoint();
        return resume;
    }

    public static void PrepareForRetry(string scenePath)
    {
        retryRequested = hasReachedCheckpoint && checkpointScenePath == scenePath;
        if (!retryRequested)
            ResetCheckpoint();
    }

    [Header("Checkpoint Spawn")]
    [Tooltip("Optional child Transform where the player should respawn after interacting with this checkpoint map. If empty, this object's position and rotation are used.")]
    public Transform checkpointSpawnPoint;

    [Header("Checkpoint Type")]
    [Tooltip("Use only for a scene's starting location. It is not activated by the player and does not count as a discovered checkpoint.")]
    public bool isInitialSpawn = false;

    public void ActivateCheckpoint()
    {
        if (isInitialSpawn)
            return;

        checkpointScenePath = gameObject.scene.path;
        activeCheckpointPosition = GetSpawnPosition();
        activeCheckpointRotation = GetSpawnRotation();
        hasReachedCheckpoint = true;
        CheckpointSaved?.Invoke(activeCheckpointPosition);

        Debug.Log($"<color=green>[Checkpoint Activated]</color> Map checkpoint '{gameObject.name}' is now active. Respawn location: {activeCheckpointPosition}");

        if (NotificationManager.Instance != null)
            NotificationManager.Instance.ShowNotification("Checkpoint updated.", 2.5f);
    }

    private Vector3 GetSpawnPosition()
    {
        return checkpointSpawnPoint != null
            ? checkpointSpawnPoint.position
            : transform.position;
    }

    private Quaternion GetSpawnRotation()
    {
        return checkpointSpawnPoint != null
            ? checkpointSpawnPoint.rotation
            : transform.rotation;
    }

    public static void ResetCheckpoint()
    {
        activeCheckpointPosition = Vector3.zero;
        activeCheckpointRotation = Quaternion.identity;
        hasReachedCheckpoint = false;
        checkpointScenePath = null;
        retryRequested = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isInitialSpawn ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(GetSpawnPosition(), 0.6f);
    }
}