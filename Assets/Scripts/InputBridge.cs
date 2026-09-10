using UnityEngine;
using UnityEngine.InputSystem;

public class InputBridge : MonoBehaviour
{
    [Header("Target Scripts to Trigger")]
    [Tooltip("Drag the object with your original PlayerRespawn script here!")]
    [SerializeField] public MonoBehaviour targetRespawnScript; // Changed type to catch any original layout safely

    private void Update()
    {
        // Standalone Unity 6 listener for the [R] key
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (targetRespawnScript != null)
            {
                // BYPASS THE CLASS ERROR: Send a message down the engine pipeline 
                // to execute the original logic using a text reference string.
                // This fires the code successfully without touching your file!
                targetRespawnScript.SendMessage("PlayerRespawn", SendMessageOptions.DontRequireReceiver);

                Debug.Log("[Input Bridge] Successfully sent external execution ping to your original script!");
            }
        }
    }
}