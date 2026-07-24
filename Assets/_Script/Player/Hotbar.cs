using UnityEngine;
using System;

[System.Serializable]
public class HotbarSlot
{
    public ItemData item;
    public int quantity;
}

public class Hotbar : MonoBehaviour
{
    public static Hotbar Instance { get; private set; }
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

    public HotbarSlot[] slots = new HotbarSlot[5]; // 5 slots in the hotbar
    public event Action OnHotbarChanged;
    public GameObject hotbarUI; // Reference to the hotbar UI GameObject

    public bool AddItem(ItemData item, int amount = 1)
    {
        // try to stack onto existing slot
        if (item.isStackable)
        {
            foreach (var slot in slots)
            {
                if (slot.item == item && slot.quantity < item.maxStackSize)
                {
                    slot.quantity += amount;
                    OnHotbarChanged?.Invoke();
                    return true;
                }
            }
        }
        // else find first empty slot
        foreach (var slot in slots)
        {
            if (slot.item == null)
            {
                slot.item = item;
                slot.quantity = amount;
                OnHotbarChanged?.Invoke();
                return true;
            }
        }
        return false; // hotbar full
    }
}