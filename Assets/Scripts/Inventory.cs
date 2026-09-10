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

    void Start()
    {
        UpdateHotbarUI();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // Select slots 1-5 via number keys
        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectSlot(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectSlot(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectSlot(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectSlot(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectSlot(4);
    }

    public bool AddItem(ItemData item)
    {
        if (items.Count >= maxSlots)
        {
            Debug.Log("Inventory full!");
            return false;
        }

        items.Add(item);
        UpdateHotbarUI(); // <--- THIS TURNS THE IMAGE COMPONENT BACK ON!
        return true;
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= items.Count) return;
        selectedIndex = index;
        UpdateHotbarUI();
    }

    public void UpdateHotbarUI()
    {
        if (hotbarUI != null)
        {
            hotbarUI.RefreshUI(items, selectedIndex);
        }
    }
}