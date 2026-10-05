using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    public static Vector3 activeCheckpointPosition;
    public static Quaternion activeCheckpointRotation;
    public static bool hasReachedCheckpoint = false;

    [Header("Checkpoint Spawn")]
    [Tooltip("Optional child Transform where the player should respawn after interacting with this checkpoint map. If empty, this object's position and rotation are used.")]
    public Transform checkpointSpawnPoint;

    [Header("Checkpoint Type")]
    [Tooltip("Use only for a scene's starting location. It is not activated by the player and does not count as a discovered checkpoint.")]
    public bool isInitialSpawn = false;

    private void Awake()
    {
        // Initial spawn is only a fallback location. Maps must be interacted with
        // before they become active checkpoints.
        if (isInitialSpawn && !hasReachedCheckpoint)
        {
            SetActivePosition(GetSpawnPosition(), GetSpawnRotation());
        }
    }

    public void ActivateCheckpoint()
    {
        activeCheckpointPosition = GetSpawnPosition();
        activeCheckpointRotation = GetSpawnRotation();
        hasReachedCheckpoint = true;

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

    private static void SetActivePosition(Vector3 position, Quaternion rotation)
    {
        activeCheckpointPosition = position;
        activeCheckpointRotation = rotation;
    }

    public static void ResetCheckpoint()
    {
        activeCheckpointPosition = Vector3.zero;
        activeCheckpointRotation = Quaternion.identity;
        hasReachedCheckpoint = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isInitialSpawn ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(GetSpawnPosition(), 0.6f);
    }
}