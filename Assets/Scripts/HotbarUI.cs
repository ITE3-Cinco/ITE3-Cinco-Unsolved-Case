using NUnit.Framework.Interfaces;
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
            // 1. Update Icon Sprites
            if (i < items.Count && items[i] != null && items[i].itemIcon != null)
            {
                slotIcons[i].sprite = items[i].itemIcon;
                slotIcons[i].enabled = true; // Show icon image
            }
            else
            {
                slotIcons[i].sprite = null;
                slotIcons[i].enabled = false; // Hide icon image if empty
            }

            // 2. Update Slot Highlight Tint
            if (i < slotBackgrounds.Count && slotBackgrounds[i] != null)
            {
                slotBackgrounds[i].color = (i == selectedIndex) ? selectedColor : normalColor;
            }
        }
    }
}