// Purpose: Defines the name, HUD icon and agimat flag of an inventory item.
// Use: Create an asset through Inventory > Item Data, then assign it to a pickup.
// How it works: Inventory stores this asset as item data; it does not represent the visible world object.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    // True marks a consumable defense item; the name and sprite identify it in inventory and HUD.
    // isAgimat: whether this ItemData is an agimat defense consumable. Set on the ItemData asset in the Inspector;
    // pickups and Inventory keep a reference to that asset.
    public bool isAgimat;
    // itemName: display name stored in this ItemData asset. Set on the ItemData asset in the Inspector; pickups
    // and Inventory keep a reference to that asset.
    public string itemName;
    // itemIcon: sprite shown for this ItemData in the hotbar. Set on the ItemData asset in the Inspector; pickups
    // and Inventory keep a reference to that asset.
    public Sprite itemIcon;
}