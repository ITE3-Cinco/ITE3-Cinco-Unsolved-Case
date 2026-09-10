using UnityEngine;

public class Interactable : MonoBehaviour
{
    public string promptMessage = "Press [E] to pick up";

    [Header("Inventory Integration")]
    public ItemData itemData; // Assign ItemData for flashlight, keys, artifacts, etc.

    [Header("Artifact Settings")]
    public bool isArtifact = false; // Tick true for artifacts (increments counter & objective)

    [Header("Objective Integration")]
    [Tooltip("Optional: Drag the ObjectiveItem for the flashlight task here. If left empty, the script will find it automatically.")]
    public ObjectiveItem flashlightObjective;

    public void Interact(GameObject playerObject)
    {
        Debug.Log($"[Interactable] Interact called on '{gameObject.name}'. isArtifact={isArtifact}");

        // ---------------------------------------------------------------
        // 1. GET PLAYER & INVENTORY REFERENCES
        // ---------------------------------------------------------------
        PlayerMovement player = playerObject.GetComponent<PlayerMovement>();
        if (player == null) player = playerObject.GetComponentInParent<PlayerMovement>();
        if (player == null) player = playerObject.GetComponentInChildren<PlayerMovement>();

        Inventory inventory = playerObject.GetComponent<Inventory>();
        if (inventory == null) inventory = playerObject.GetComponentInParent<Inventory>();
        if (inventory == null) inventory = playerObject.GetComponentInChildren<Inventory>();

        // ---------------------------------------------------------------
        // 2. ADD TO INVENTORY HOTBAR
        // ---------------------------------------------------------------
        bool added = false;
        if (inventory != null && itemData != null)
        {
            added = inventory.AddItem(itemData);
            Debug.Log($"[Interactable] Tried to add '{(itemData != null ? itemData.name : "NULL")}' to inventory. Success: {added}");
        }
        else if (itemData == null)
        {
            Debug.LogWarning($"[Interactable] ItemData is unassigned on '{gameObject.name}'. It will not appear in the hotbar.");
        }

        // If inventory is full, stop here so the 3D item isn't consumed/destroyed
        if (!added && itemData != null)
        {
            Debug.LogWarning("[Interactable] Could not add item to inventory (inventory full). Pickup cancelled.");
            return;
        }

        // ---------------------------------------------------------------
        // 3. ARTIFACT COUNTER (Runs if inventory add succeeded or itemData is null)
        // ---------------------------------------------------------------
        if (isArtifact)
        {
            if (ArtifactManager.Instance != null)
            {
                ArtifactManager.Instance.CollectArtifact();
            }
            else
            {
                Debug.LogWarning("[Interactable] ArtifactManager.Instance is null! Add an ArtifactManager to the scene.");
            }
        }

        // ---------------------------------------------------------------
        // 4. FLASHLIGHT PICKUP & OBJECTIVE LOGIC
        // ---------------------------------------------------------------
        bool isFlashlightPickup = (player != null && itemData != null && itemData == player.flashlightItemData);
        if (isFlashlightPickup)
        {
            player.HasFlashlight = true;

            ObjectiveItem obj = flashlightObjective;
            if (obj == null)
            {
                // If the flashlightObjective field is not assigned, try to find it in the scene automatically
                obj = FindAnyObjectByType<ObjectiveItem>();
            }

            if (obj != null)
            {
                obj.CompleteFlashlightTask();
                Debug.Log($"[Interactable] Notified ObjectiveItem '{obj.gameObject.name}' -> flashlight task complete.");
            }
            else
            {
                Debug.LogWarning("[Interactable] Flashlight picked up, but no ObjectiveItem was found to cross out.");
            }
        }

        // ---------------------------------------------------------------
        // 5. NOTIFICATIONS
        // ---------------------------------------------------------------
        if (NotificationManager.Instance != null)
        {
            if (isFlashlightPickup)
            {
                NotificationManager.Instance.ShowNotification("Flashlight Acquired! Press [F] to toggle", 4.0f);
            }
            else if (itemData != null)
            {
                NotificationManager.Instance.ShowNotification("Acquired an artifact!", 3.0f);
            }
        }

        // ---------------------------------------------------------------
        // 6. REMOVE 3D WORLD OBJECT
        // ---------------------------------------------------------------
        Destroy(gameObject);
    }
}