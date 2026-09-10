using UnityEngine;

public class Interactable : MonoBehaviour
{
    public string promptMessage = "Press [E] to pick up";

    [Header("Inventory Integration")]
    public ItemData itemData; // Assign Item_Flashlight here in the Inspector

    public void Interact(GameObject playerObject)
    {
        // 1. Find PlayerMovement (handles nested camera or root player object)
        PlayerMovement player = playerObject.GetComponent<PlayerMovement>();
        if (player == null) player = playerObject.GetComponentInParent<PlayerMovement>();
        if (player == null) player = playerObject.GetComponentInChildren<PlayerMovement>();

        if (player != null)
        {
            player.HasFlashlight = true;
        }

        // 2. Find Inventory component on player or its root hierarchy
        Inventory inventory = playerObject.GetComponent<Inventory>();
        if (inventory == null) inventory = playerObject.GetComponentInParent<Inventory>();
        if (inventory == null) inventory = playerObject.GetComponentInChildren<Inventory>();

        if (inventory != null && itemData != null)
        {
            inventory.AddItem(itemData);
        }
        else if (itemData == null)
        {
            Debug.LogWarning("ItemData is unassigned on " + gameObject.name + "! Make sure to drag your ScriptableObject into the Inspector slot.");
        }

        // 3. Show Notification
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowNotification("Flashlight Acquired! Press [F] to toggle", 4.0f);
        }

        // 4. Remove pickup object from the scene
        Destroy(gameObject);
    }
}