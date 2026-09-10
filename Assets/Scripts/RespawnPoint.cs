using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    // Static shared checkpoint across all instances
    public static Vector3 activeCheckpointPosition;
    public static bool hasReachedCheckpoint = false;

    [Header("Checkpoint Type")]
    public bool isInitialSpawn = false;

    private void Awake()
    {
        // Set starting point if marked as initial spawn and no checkpoint reached yet
        if (isInitialSpawn && !hasReachedCheckpoint)
        {
            activeCheckpointPosition = transform.position;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verify it's the player entering the trigger
        if (other.CompareTag("Player") || other.GetComponent<PlayerRespawn>() != null)
        {
            activeCheckpointPosition = transform.position;
            hasReachedCheckpoint = true;
            Debug.Log($"<color=green>[Checkpoint Activated]</color> New respawn location: {transform.position}");
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isInitialSpawn ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.6f);
    }
}