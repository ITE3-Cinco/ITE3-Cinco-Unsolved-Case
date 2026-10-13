// Purpose: Stores collected ItemData assets and manages the selected hotbar slot.
// Use: Attach to the player and assign HotbarUI; number keys 1 to 5 select slots.
// How it works: Pickups add items; slot changes refresh the HUD and update the held flashlight.

// System.Collections.Generic supplies typed lists, sets and dictionaries for stored data.
using System.Collections.Generic;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;

public class Inventory : MonoBehaviour
{
    // maxSlots: maximum inventory capacity. Assigned in the scene/prefab Inspector; the initializer below is the
    // default.
    [Header("Inventory Settings")]
    // The list stores item assets in slot order; selectedIndex is zero-based, so key 1 selects index 0.
    public int maxSlots = 5;
    // items: ordered ItemData references stored by Inventory. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    public List<ItemData> items = new List<ItemData>();
    // selectedIndex: zero-based selected inventory slot. SelectSlot sets it from index.
    public int selectedIndex = 0;

    // hotbarUI: HotbarUI receiving items and selectedIndex for display. Assigned in the scene/prefab Inspector.
    [Header("UI Reference")]
    public HotbarUI hotbarUI;

    /// <summary>Draw the starting inventory when the player enters the scene.</summary>
    private void Start()
    {
        // Call Inventory.UpdateHotbarUI; send the inventory contents and selected index to the assigned HUD.
        UpdateHotbarUI();
    }

    /// <summary>While not loading, read number-key presses using the configured Unity input system.</summary>
    private void Update()
    {
        // If SceneTransition.IsLoading, return now; skip the rest of this method.
        if (SceneTransition.IsLoading) return;
        // Hotbar Slot Selection (1-5)
#if ENABLE_INPUT_SYSTEM
        // If UnityEngine.InputSystem.Keyboard.current exists, run the following block.
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            // Store UnityEngine.InputSystem.Keyboard.current in local kb.
            var kb = UnityEngine.InputSystem.Keyboard.current;
            // If number 1 key on kb was newly pressed this frame, call Inventory.SelectSlot with 0; validate a
            // slot, highlight it and tell PlayerMovement which item is selected.
            if (kb.digit1Key.wasPressedThisFrame) SelectSlot(0);
            // If number 2 key on kb was newly pressed this frame, call Inventory.SelectSlot with 1; validate a
            // slot, highlight it and tell PlayerMovement which item is selected.
            if (kb.digit2Key.wasPressedThisFrame) SelectSlot(1);
            // If number 3 key on kb was newly pressed this frame, call Inventory.SelectSlot with 2; validate a
            // slot, highlight it and tell PlayerMovement which item is selected.
            if (kb.digit3Key.wasPressedThisFrame) SelectSlot(2);
            // If number 4 key on kb was newly pressed this frame, call Inventory.SelectSlot with 3; validate a
            // slot, highlight it and tell PlayerMovement which item is selected.
            if (kb.digit4Key.wasPressedThisFrame) SelectSlot(3);
            // If number 5 key on kb was newly pressed this frame, call Inventory.SelectSlot with 4; validate a
            // slot, highlight it and tell PlayerMovement which item is selected.
            if (kb.digit5Key.wasPressedThisFrame) SelectSlot(4);
        }
#else
        // If key KeyCode.Alpha1 was newly pressed this frame, call Inventory.SelectSlot with 0; validate a slot,
        // highlight it and tell PlayerMovement which item is selected.
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        // If key KeyCode.Alpha2 was newly pressed this frame, call Inventory.SelectSlot with 1; validate a slot,
        // highlight it and tell PlayerMovement which item is selected.
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        // If key KeyCode.Alpha3 was newly pressed this frame, call Inventory.SelectSlot with 2; validate a slot,
        // highlight it and tell PlayerMovement which item is selected.
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        // If key KeyCode.Alpha4 was newly pressed this frame, call Inventory.SelectSlot with 3; validate a slot,
        // highlight it and tell PlayerMovement which item is selected.
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        // If key KeyCode.Alpha5 was newly pressed this frame, call Inventory.SelectSlot with 4; validate a slot,
        // highlight it and tell PlayerMovement which item is selected.
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);
#endif
    }

    /// <summary>Reject a full inventory; otherwise store the item, refresh the HUD and update the held item.</summary>
    // item is an ItemData asset supplied by a pickup, flashlight helper or Level3Flow; the list stores the
    // reference, not a clone.
    public bool AddItem(ItemData item)
    {
        // If number of entries in items is at least maxSlots, run the following block.
        if (items.Count >= maxSlots)
        {
            // Write the diagnostic message to the Unity Console; it does not update game UI.
            Debug.Log("Inventory Full!");
            // Return false to the method caller.
            return false;
        }

        // Store the caller-supplied ItemData reference in the next inventory slot; this keeps its asset data, not
        // the world pickup object.
        items.Add(item);
        // Call Inventory.UpdateHotbarUI; send the inventory contents and selected index to the assigned HUD.
        UpdateHotbarUI();

        // Check held item after adding
        // Call Inventory.SelectSlot with selectedIndex; validate a slot, highlight it and tell PlayerMovement
        // which item is selected.
        SelectSlot(selectedIndex);
        // Return true to the method caller.
        return true;
    }

    /// <summary>Validate a slot, highlight it and tell PlayerMovement which item is selected.</summary>
    // index comes from number-key handling or another inventory caller; valid slot numbers run from zero to
    // maxSlots minus one.
    public void SelectSlot(int index)
    {
        // If index is below 0 or index is at least maxSlots, return now; skip the rest of this method.
        if (index < 0 || index >= maxSlots) return;

        // Store the requested zero-based index from the number-key handler or other caller as the current
        // inventory selection.
        selectedIndex = index;
        // Call Inventory.UpdateHotbarUI; send the inventory contents and selected index to the assigned HUD.
        UpdateHotbarUI();

        // Notify PlayerMovement to enable/disable handheld objects (e.g. flashlight)
        // Store the first PlayerMovement component on this GameObject in local player.
        PlayerMovement player = GetComponent<PlayerMovement>();
        // If player is missing, store the first PlayerMovement component on or above this GameObject in player.
        if (player == null) player = GetComponentInParent<PlayerMovement>();
        // If player exists, run the following block.
        if (player != null)
        {
            // Pass Inventory.GetSelectedItem result to PlayerMovement.UpdateHeldItem; it hides or shows the held
            // flashlight for this slot.
            player.UpdateHeldItem(GetSelectedItem());
        }
    }

    /// <summary>Return the item in the selected slot, or null when that slot is empty.</summary>
    public ItemData GetSelectedItem()
    {
        // Read items[selectedIndex] only if the selected slot contains an entry; this avoids indexing
        // beyond the current list.
        if (selectedIndex >= 0 && selectedIndex < items.Count)
        {
            // Return items[selectedIndex] (entry read from items) to the method caller.
            return items[selectedIndex];
        }
        // Return no reference (null) to the method caller.
        return null; // Selected slot is currently empty
    }

    // Defense may use any carried agimat, even when another slot is selected.
    // HasAgimat is computed when read: return whether items has an entry matching item => item != null &&
    // item.isAgimat.
    public bool HasAgimat => items.Exists(item => item != null && item.isAgimat);

    /// <summary>Remove one agimat, adjust the selected slot and refresh the display.</summary>
    public bool ConsumeAgimat()
    {
        // Search Inventory items for the first non-null ItemData with isAgimat enabled; index receives its
        // zero-based position or -1 if absent.
        int index = items.FindIndex(item => item != null && item.isAgimat);
        // If index is below 0, return false to the method caller.
        if (index < 0) return false;
        // Remove entry at zero-based index index from items.
        items.RemoveAt(index);
        // Keep the old selection within the shortened item list, using slot zero when empty; SelectSlot
        // then refreshes the hotbar and held flashlight.
        SelectSlot(Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, items.Count - 1)));
        // Return true to the method caller.
        return true;
    }

    /// <summary>Send the inventory contents and selected index to the assigned HUD.</summary>
    public void UpdateHotbarUI()
    {
        // If hotbarUI exists, run the following block.
        if (hotbarUI != null)
        {
            // Send this Inventory item list and selectedIndex to HotbarUI.RefreshUI, which copies icons and
            // highlights the selected slot.
            hotbarUI.RefreshUI(items, selectedIndex);
        }
    }
}