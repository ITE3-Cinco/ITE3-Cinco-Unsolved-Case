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
        UpdateHotbarUI();
    }

    private void Update()
    {
        if (SceneTransition.IsLoading) return;
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
        }
#else
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);
#endif
    }

    public bool AddItem(ItemData item)
    {
        if (items.Count >= maxSlots)
        {
            Debug.Log("Inventory Full!");
            return false;
        }

        items.Add(item);
        UpdateHotbarUI();

        // Check held item after adding
        SelectSlot(selectedIndex);
        return true;
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= maxSlots) return;

        selectedIndex = index;
        UpdateHotbarUI();

        // Notify PlayerMovement to enable/disable handheld objects (e.g. flashlight)
        PlayerMovement player = GetComponent<PlayerMovement>();
        if (player == null) player = GetComponentInParent<PlayerMovement>();
        if (player != null)
        {
            player.UpdateHeldItem(GetSelectedItem());
        }
    }

    public ItemData GetSelectedItem()
    {
        if (selectedIndex >= 0 && selectedIndex < items.Count)
        {
            return items[selectedIndex];
        }
        return null; // Selected slot is currently empty
    }

    public bool HasAgimat => items.Exists(item => item != null && item.isAgimat);

    public bool ConsumeAgimat()
    {
        int index = items.FindIndex(item => item != null && item.isAgimat);
        if (index < 0) return false;
        items.RemoveAt(index);
        SelectSlot(Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, items.Count - 1)));
        return true;
    }

    public void UpdateHotbarUI()
    {
        if (hotbarUI != null)
        {
            hotbarUI.RefreshUI(items, selectedIndex);
        }
    }
}