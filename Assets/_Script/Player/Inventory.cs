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
    public static Inventory Instance { get; private set; }

    public List<string> items = new List<string>();
    public List<InventoryItemData> itemDataList = new List<InventoryItemData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

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

    public bool HasItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return false;
        return items.Exists(item => string.Equals(item, itemName, System.StringComparison.OrdinalIgnoreCase));
    }

    public void Clear()
    {
        items.Clear();
        itemDataList.Clear();
        Debug.Log("Inventory cleared on player death.");
    }
}