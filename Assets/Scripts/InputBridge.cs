using UnityEngine;
using UnityEngine.InputSystem;

public class InputBridge : MonoBehaviour
{
    [Header("Target Script")]
    [Tooltip("Optional. Drag the object that has PlayerRespawn here. If left empty, this object is searched automatically.")]
    [SerializeField] private MonoBehaviour targetRespawnScript;

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.rKey.wasPressedThisFrame)
            return;

        if (targetRespawnScript != null)
        {
            targetRespawnScript.SendMessage("Respawn", SendMessageOptions.DontRequireReceiver);
            Debug.Log("[Input Bridge] Respawn request sent.");
            return;
        }

        PlayerRespawn respawn = FindFirstObjectByType<PlayerRespawn>();

        if (respawn != null)
        {
            respawn.Respawn();
            Debug.Log("[Input Bridge] Found PlayerRespawn automatically and respawned.");
        }
        else
        {
            Debug.LogWarning("[Input Bridge] No PlayerRespawn component was found.");
        }
    }
}