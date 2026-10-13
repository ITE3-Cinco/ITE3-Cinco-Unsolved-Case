// Purpose: Shows inventory icons, the selected slot and an agimat danger glow.
// Use: Attach to the HUD; assign background and icon images in slot order.
// How it works: Inventory refreshes the icons; frame updates tint the selection and pulse nearby-ghost warnings.

// System.Collections.Generic supplies typed lists, sets and dictionaries for stored data.
using System.Collections.Generic;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity UI supplies canvas widgets such as images, sliders and buttons.
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
    // slotBackgrounds: Drag Slot_0, Slot_1, Slot_2, Slot_3, Slot_4 images here in order. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    [Header("Slot Background Images")]
    [Tooltip("Drag Slot_0, Slot_1, Slot_2, Slot_3, Slot_4 images here in order")]
    public List<Image> slotBackgrounds = new List<Image>();

    // slotIcons: Drag ItemIcon, ItemIcon (1), ItemIcon (2), etc. here in order. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Header("Item Icon Images")]
    [Tooltip("Drag ItemIcon, ItemIcon (1), ItemIcon (2), etc. here in order")]
    public List<Image> slotIcons = new List<Image>();

    // normalColor: tint used for unselected inventory slots. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    [Header("Colors")]
    public Color normalColor = new Color(0.3f, 0.3f, 0.3f, 1f); // Dim grey for unselected slots
    // activeColor: tint used for the selected slot. Assigned in the scene/prefab Inspector; the initializer below
    // is the default.
    public Color activeColor = Color.white;                      // Bright white (#FFFFFF) for selected slot

    // Remember the latest inventory and selected slot so frame updates can tint the right images.
    // displayedItems: latest ItemData list supplied by Inventory.UpdateHotbarUI. RefreshUI sets it from items.
    private List<ItemData> displayedItems;
    // selectedSlot: latest inventory selectedIndex supplied to RefreshUI. RefreshUI sets it from selectedIndex.
    private int selectedSlot;

    /// <summary>After gameplay updates, tint selected slots and pulse agimat icons when a hostile ghost is near.</summary>
    private void LateUpdate()
    {
        // Read GhostNearby from the protection controller registered in InvisibilityConsumable.Awake;
        // warning is false if that controller is missing.
        bool warning = InvisibilityConsumable.Instance != null && InvisibilityConsumable.Instance.GhostNearby;
        // Use Unity gameplay time to make a repeating value from zero to one; pulse controls the warning
        // color blend.
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 7f);
        // Repeat this block while i is below number of entries in slotBackgrounds; start from int i = 0.
        for (int i = 0; i < slotBackgrounds.Count; i++)
        {
            // If slotBackgrounds[i] (entry read from slotBackgrounds) is missing, skip this entry and continue
            // with the next loop iteration.
            if (slotBackgrounds[i] == null) continue;
            // Enable glow only for a valid agimat entry in the Inventory list cached by RefreshUI, and
            // only while the nearby-ghost warning is active.
            bool glow = warning && displayedItems != null && i < displayedItems.Count &&
                displayedItems[i] != null && displayedItems[i].isAgimat;
            // Set slotBackgrounds[i].color to a blend from new Color(0.55f, 0.3f, 0.05f) to new Color(1f, 0.85f,
            // 0.25f) using fraction pulse when glow; otherwise (activeColor when i == selectedSlot; otherwise
            // normalColor).
            slotBackgrounds[i].color = glow
                ? Color.Lerp(new Color(0.55f, 0.3f, 0.05f), new Color(1f, 0.85f, 0.25f), pulse)
                : (i == selectedSlot ? activeColor : normalColor);
            // If i is below number of entries in slotIcons and slotIcons[i] (entry read from slotIcons) exists,
            // run the following block.
            if (i < slotIcons.Count && slotIcons[i] != null)
                // Set slotIcons[i].color to a blend from Color.white to Color.yellow using fraction pulse when
                // glow; otherwise Color.white.
                slotIcons[i].color = glow ? Color.Lerp(Color.white, Color.yellow, pulse) : Color.white;
        }
    }

    /// <summary>Copy the current selection, show occupied icons and clear empty slots.</summary>
    // items is the ordered ItemData list supplied by Inventory.UpdateHotbarUI; the HUD keeps this list reference.
    // selectedIndex is the zero-based slot supplied by Inventory.UpdateHotbarUI, used to choose the highlighted
    // background.
    public void RefreshUI(List<ItemData> items, int selectedIndex)
    {
        // Keep the ItemData list passed by Inventory.UpdateHotbarUI; LateUpdate reads it to identify agimat slots.
        displayedItems = items;
        // Copy the selectedIndex argument from Inventory.UpdateHotbarUI into selectedSlot for later slot
        // highlighting.
        selectedSlot = selectedIndex;
        // 1. Update Icons
        // Repeat this block while i is below number of entries in slotIcons; start from int i = 0.
        for (int i = 0; i < slotIcons.Count; i++)
        {
            // If slotIcons[i] (entry read from slotIcons) is missing, skip this entry and continue with the next
            // loop iteration.
            if (slotIcons[i] == null) continue;

            // If i is below items.Count and items[i] exists and sprite stored in items[i] ItemData asset exists,
            // run the following block.
            if (i < items.Count && items[i] != null && items[i].itemIcon != null)
            {
                // Copy sprite stored in items[i] ItemData asset into slotIcons[i].sprite so this UI Image displays
                // that icon.
                slotIcons[i].sprite = items[i].itemIcon;
                // Enable slotIcons[i] by changing its component enabled flag.
                slotIcons[i].enabled = true;
            }
            else
            {
                // Clear slotIcons[i].sprite to null so it no longer holds a reference.
                slotIcons[i].sprite = null;
                // Disable slotIcons[i] by changing its component enabled flag.
                slotIcons[i].enabled = false;
            }
        }

        // 2. Update Active Slot Highlight Tint
        // Repeat this block while i is below number of entries in slotBackgrounds; start from int i = 0.
        for (int i = 0; i < slotBackgrounds.Count; i++)
        {
            // If slotBackgrounds[i] (entry read from slotBackgrounds) is missing, skip this entry and continue
            // with the next loop iteration.
            if (slotBackgrounds[i] == null) continue;
            // Set slotBackgrounds[i].color to activeColor when (i equals selectedIndex); otherwise normalColor.
            slotBackgrounds[i].color = (i == selectedIndex) ? activeColor : normalColor;
        }
    }
}