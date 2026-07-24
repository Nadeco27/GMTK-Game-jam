using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventoryItemData
{
    public string itemName;
    public Sprite itemIcon;

    public InventoryItemData(string name, Sprite icon = null)
    {
        itemName = name;
        itemIcon = icon;
    }
}

public class Inventory : MonoBehaviour
{
    public List<string> items = new List<string>();
    public List<InventoryItemData> itemDataList = new List<InventoryItemData>();

    public void AddItem(string itemName)
    {
        AddItem(itemName, null);
    }

    public void AddItem(string itemName, Sprite icon)
    {
        items.Add(itemName);
        itemDataList.Add(new InventoryItemData(itemName, icon));
        Debug.Log(itemName + " Item Added To Inventory!");
    }
}