using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
    [Header("Slot Background Images")]
    [Tooltip("Drag Slot_0, Slot_1, Slot_2, Slot_3, Slot_4 images here in order")]
    public List<Image> slotBackgrounds = new List<Image>();

    [Header("Item Icon Images")]
    [Tooltip("Drag ItemIcon, ItemIcon (1), ItemIcon (2), etc. here in order")]
    public List<Image> slotIcons = new List<Image>();

    [Header("Colors")]
    public Color normalColor = new Color(0.3f, 0.3f, 0.3f, 1f); // Dim grey for unselected slots
    public Color activeColor = Color.white;                      // Bright white (#FFFFFF) for selected slot

    public void RefreshUI(List<ItemData> items, int selectedIndex)
    {
        // 1. Update Icons
        for (int i = 0; i < slotIcons.Count; i++)
        {
            if (slotIcons[i] == null) continue;

            if (i < items.Count && items[i] != null && items[i].itemIcon != null)
            {
                slotIcons[i].sprite = items[i].itemIcon;
                slotIcons[i].enabled = true;
            }
            else
            {
                slotIcons[i].sprite = null;
                slotIcons[i].enabled = false;
            }
        }

        // 2. Update Active Slot Highlight Tint
        for (int i = 0; i < slotBackgrounds.Count; i++)
        {
            if (slotBackgrounds[i] == null) continue;
            slotBackgrounds[i].color = (i == selectedIndex) ? activeColor : normalColor;
        }
    }
}