using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    public bool isAgimat;
    public string itemName;
    public Sprite itemIcon;
}