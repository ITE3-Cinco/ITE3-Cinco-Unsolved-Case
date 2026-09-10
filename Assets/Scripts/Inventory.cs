using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int maxSlots = 5;
    public List<ItemData> items = new List<ItemData>();
    public int selectedIndex = 0;

    [Header("UI Reference")]
    public HotbarUI hotbarUI;

    private void Start()
    {
        // Clamp the selected index in case the Inspector has a bad value
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, maxSlots - 1));

        UpdateHotbarUI();

        // Sync held item on start (e.g., if the flashlight is already in the hotbar)
        PlayerMovement player = GetComponent<PlayerMovement>();
        if (player == null) player = GetComponentInParent<PlayerMovement>();
        if (player != null)
        {
            player.UpdateHeldItem(GetSelectedItem());
        }
    }

    private void Update()
    {
        // Hotbar Slot Selection (1-5)
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb.digit1Key.wasPressedThisFrame) SelectSlot(0);
            if (kb.digit2Key.wasPressedThisFrame) SelectSlot(1);
            if (kb.digit3Key.wasPressedThisFrame) SelectSlot(2);
            if (kb.digit4Key.wasPressedThisFrame) SelectSlot(3);
            if (kb.digit5Key.wasPressedThisFrame) SelectSlot(4);

            // Press 'F' or Left Click to use/consume the selected item
            if (kb.fKey.wasPressedThisFrame || UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                UseSelectedItem();
            }
        }
#else
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);

        // Press 'F' or Left Click to use/consume selected item
        if (Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(0))
        {
            UseSelectedItem();
        }
#endif
    }

    /// <summary>
    /// Uses or consumes the item currently selected in the active hotbar slot.
    /// </summary>
    public void UseSelectedItem()
    {
        ItemData currentItem = GetSelectedItem();
        if (currentItem == null) return;

        // Check if item is the Agimat (Invisibility Consumable)
        if (currentItem.itemName == "Agimat")
        {
            if (InvisibilityConsumable.Instance != null)
            {
                InvisibilityConsumable.Instance.UseInvisibility();
                RemoveSelectedItem(); // Removes Agimat from List<ItemData> & refreshes hotbar UI
            }
            else
            {
                Debug.LogWarning("[Inventory] InvisibilityConsumable instance not found in scene!");
            }
        }
    }

    /// <summary>
    /// Adds an item to the first open slot. Returns false if the inventory is full.
    /// </summary>
    public bool AddItem(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("[Inventory] AddItem called with null item.");
            return false;
        }

        if (items.Count >= maxSlots)
        {
            Debug.Log("[Inventory] Inventory Full!");
            return false;
        }

        items.Add(item);
        UpdateHotbarUI();

        // Notify PlayerMovement so handheld items refresh
        SelectSlot(selectedIndex);
        return true;
    }

    /// <summary>
    /// Changes the currently selected hotbar slot and refreshes held-item state.
    /// </summary>
    public void SelectSlot(int index)
    {
        if (index < 0 || index >= maxSlots) return;
        if (index == selectedIndex && items.Count == 0) return; // no-op on empty start

        selectedIndex = index;
        UpdateHotbarUI();

        // Notify PlayerMovement to enable/disable handheld objects (e.g., flashlight)
        PlayerMovement player = GetComponent<PlayerMovement>();
        if (player == null) player = GetComponentInParent<PlayerMovement>();
        if (player != null)
        {
            player.UpdateHeldItem(GetSelectedItem());
        }
    }

    /// <summary>
    /// Returns the item in the currently selected slot, or null if empty.
    /// </summary>
    public ItemData GetSelectedItem()
    {
        if (selectedIndex >= 0 && selectedIndex < items.Count)
        {
            return items[selectedIndex];
        }
        return null;
    }

    /// <summary>
    /// Removes the item in the currently selected slot (e.g., after consumption).
    /// </summary>
    public void RemoveSelectedItem()
    {
        if (selectedIndex < 0 || selectedIndex >= items.Count) return;

        items.RemoveAt(selectedIndex);
        UpdateHotbarUI();

        PlayerMovement player = GetComponent<PlayerMovement>();
        if (player == null) player = GetComponentInParent<PlayerMovement>();
        if (player != null)
        {
            player.UpdateHeldItem(GetSelectedItem());
        }
    }

    public void UpdateHotbarUI()
    {
        if (hotbarUI != null)
        {
            hotbarUI.RefreshUI(items, selectedIndex);
        }
    }
}