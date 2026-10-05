using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRespawn : MonoBehaviour
{
    [Header("Fallback Spawn")]
    public Transform defaultSpawnPoint;

    [Header("Testing Controls")]
    public KeyCode debugRespawnKey = KeyCode.R;
    public float killYDepth = -20f;

    private CharacterController characterController;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();

        // The lobby is the fallback spawn until a checkpoint is reached.
        if (!RespawnPoint.hasReachedCheckpoint && defaultSpawnPoint != null)
        {
            RespawnPoint.activeCheckpointPosition = defaultSpawnPoint.position;
            RespawnPoint.activeCheckpointRotation = defaultSpawnPoint.rotation;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && GetDebugRespawnKeyPressed())
            Respawn();

        if (transform.position.y < killYDepth)
            Respawn();
    }

    private bool GetDebugRespawnKeyPressed()
    {
        if (debugRespawnKey == KeyCode.R)
            return Keyboard.current.rKey.wasPressedThisFrame;

        return false;
    }

    public void Respawn()
    {
        Vector3 targetPosition = RespawnPoint.hasReachedCheckpoint
            ? RespawnPoint.activeCheckpointPosition
            : (defaultSpawnPoint != null ? defaultSpawnPoint.position : transform.position);

        Quaternion targetRotation = RespawnPoint.hasReachedCheckpoint
            ? RespawnPoint.activeCheckpointRotation
            : (defaultSpawnPoint != null ? defaultSpawnPoint.rotation : transform.rotation);

        if (characterController != null)
        {
            characterController.enabled = false;
            transform.SetPositionAndRotation(targetPosition, targetRotation);
            characterController.enabled = true;
        }
        else
        {
            transform.SetPositionAndRotation(targetPosition, targetRotation);
        }

        Debug.Log($"[Respawn] Teleported to: {targetPosition}");
    }
}