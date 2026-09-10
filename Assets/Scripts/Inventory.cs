using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Inventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int maxSlots = 5;
    public List<ItemData> items = new List<ItemData>();
    public int selectedIndex = 0;

    [Header("UI Reference")]
    public HotbarUI hotbarUI;

    [Header("Use Item Settings")]
    [Tooltip("Key that uses the currently selected item (F key by default).")]
    public Key useItemKey = Key.F;

    [Tooltip("Also allow Left Mouse Button to use the selected item.")]
    public bool allowLeftClickUse = true;

    private void Start()
    {
        // Clamp selectedIndex in case it was left out of range
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
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            // Hotbar slot selection 1-5
            if (kb.digit1Key.wasPressedThisFrame) SelectSlot(0);
            if (kb.digit2Key.wasPressedThisFrame) SelectSlot(1);
            if (kb.digit3Key.wasPressedThisFrame) SelectSlot(2);
            if (kb.digit4Key.wasPressedThisFrame) SelectSlot(3);
            if (kb.digit5Key.wasPressedThisFrame) SelectSlot(4);

            // Use item: F key or Left Click
            bool pressedUse = kb[useItemKey].wasPressedThisFrame
                || (allowLeftClickUse && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

            if (pressedUse)
            {
                UseSelectedItem();
            }
        }
#else
        // Legacy Input Manager
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);

        bool pressedUse = Input.GetKeyDown(KeyCode.F)
            || (allowLeftClickUse && Input.GetMouseButtonDown(0));

        if (pressedUse)
        {
            UseSelectedItem();
        }
#endif
    }

    // ---------------------------------------------------------------
    // ADD ITEM
    // ---------------------------------------------------------------
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

        // Refresh held item (handles e.g. flashlight in slot 0 on pickup)
        SelectSlot(selectedIndex);
        return true;
    }

    // ---------------------------------------------------------------
    // SELECT SLOT
    // ---------------------------------------------------------------
    public void SelectSlot(int index)
    {
        if (index < 0 || index >= maxSlots) return;

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

    // ---------------------------------------------------------------
    // GET SELECTED ITEM
    // ---------------------------------------------------------------
    public ItemData GetSelectedItem()
    {
        if (selectedIndex >= 0 && selectedIndex < items.Count)
        {
            return items[selectedIndex];
        }
        return null;
    }

    // ---------------------------------------------------------------
    // REMOVE SELECTED ITEM (consumables)
    // ---------------------------------------------------------------
    public void RemoveSelectedItem()
    {
        if (selectedIndex < 0 || selectedIndex >= items.Count) return;

        Debug.Log($"[Inventory] Removing '{items[selectedIndex].itemName}' from slot {selectedIndex}");

        items.RemoveAt(selectedIndex);
        UpdateHotbarUI();

        // Refresh held item so PlayerMovement knows the slot is now empty
        PlayerMovement player = GetComponent<PlayerMovement>();
        if (player == null) player = GetComponentInParent<PlayerMovement>();
        if (player != null)
        {
            player.UpdateHeldItem(GetSelectedItem());
        }
    }

    // ---------------------------------------------------------------
    // USE SELECTED ITEM
    // ---------------------------------------------------------------
    public void UseSelectedItem()
    {
        ItemData selected = GetSelectedItem();
        if (selected == null)
        {
            // Nothing selected — silent return
            return;
        }

        Debug.Log($"[Inventory] Using item: '{selected.itemName}'");

        // ---- Flashlight: PlayerMovement already handles F toggle. Skip here. ----
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm == null) pm = GetComponentInParent<PlayerMovement>();
        if (pm != null && pm.flashlightItemData != null && selected == pm.flashlightItemData)
        {
            // Flashlight toggle is handled in PlayerMovement — do nothing here
            return;
        }

        // ---- Agimat: activate invisibility, then consume ----
        if (selected.itemName == "Agimat")
        {
            if (InvisibilityConsumable.Instance != null)
            {
                InvisibilityConsumable.Instance.UseInvisibility();
                Debug.Log("[Inventory] Agimat used — invisibility activated.");
            }
            else
            {
                Debug.LogWarning("[Inventory] Agimat used but InvisibilityConsumable.Instance is null!");
            }

            RemoveSelectedItem();
            return;
        }

        // ---- Default: no behavior defined ----
        Debug.Log($"[Inventory] '{selected.itemName}' has no use behavior defined.");
    }

    // ---------------------------------------------------------------
    // UPDATE HOTBAR UI
    // ---------------------------------------------------------------
    public void UpdateHotbarUI()
    {
        if (hotbarUI != null)
        {
            hotbarUI.RefreshUI(items, selectedIndex);
        }
    }
}