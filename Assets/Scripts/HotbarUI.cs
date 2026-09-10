using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
    [Header("Slot Icon References")]
    [Tooltip("Drag ItemIcon, ItemIcon (1), ItemIcon (2), etc. here in order")]
    public List<Image> slotIcons = new List<Image>();

    [Header("Slot Background References")]
    [Tooltip("Drag Slot_0, Slot_1, Slot_2, etc. here to control selection highlights")]
    public List<Image> slotBackgrounds = new List<Image>();

    [Header("Selection Visuals")]
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(1f, 0.9f, 0.5f, 1f); // Warm tint for active slot

    public void RefreshUI(List<ItemData> items, int selectedIndex)
    {
        for (int i = 0; i < slotIcons.Count; i++)
        {
            if (slotIcons[i] == null) continue;

            // 1. Update Icon Sprites and Toggle Image Component
            if (i < items.Count && items[i] != null && items[i].itemIcon != null)
            {
                slotIcons[i].sprite = items[i].itemIcon;
                slotIcons[i].color = Color.white; // Ensure full opacity
                slotIcons[i].enabled = true;      // Re-enables the disabled Image checkbox in Inspector
            }
            else
            {
                slotIcons[i].sprite = null;
                slotIcons[i].enabled = false;     // Hides the white box when slot is empty
            }

            // 2. Update Active Slot Highlight Tint
            if (i < slotBackgrounds.Count && slotBackgrounds[i] != null)
            {
                slotBackgrounds[i].color = (i == selectedIndex) ? selectedColor : normalColor;
            }
        }
    }
}