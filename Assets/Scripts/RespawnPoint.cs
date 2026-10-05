using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    public static Vector3 activeCheckpointPosition;
    public static Quaternion activeCheckpointRotation;
    public static bool hasReachedCheckpoint = false;

    [Header("Checkpoint Type")]
    public bool isInitialSpawn = false;

    private void Awake()
    {
        if (isInitialSpawn && !hasReachedCheckpoint)
        {
            activeCheckpointPosition = transform.position;
            activeCheckpointRotation = transform.rotation;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerRespawn>() != null)
        {
            activeCheckpointPosition = transform.position;
            activeCheckpointRotation = transform.rotation;
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