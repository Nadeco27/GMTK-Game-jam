using UnityEngine;

/// <summary>
/// Item pickup script updated to implement IInteractable interface for Key-E interaction.
/// Supports both legacy setups and new modular interaction system.
/// </summary>
public class ItemPickup : MonoBehaviour, IInteractable
{
    [Header("Item Pickup Settings")]
    [SerializeField] private ItemData item;
    [SerializeField] private int quantity = 1;

    private static readonly System.Collections.Generic.List<ItemPickup> pickedUpItems = new System.Collections.Generic.List<ItemPickup>();

    private void OnEnable()
    {
        PlayerHealth.OnPlayerDied += RespawnAllItems;
    }

    private void OnDisable()
    {
        PlayerHealth.OnPlayerDied -= RespawnAllItems;
    }

    private static void RespawnAllItems()
    {
        for (int i = pickedUpItems.Count - 1; i >= 0; i--)
        {
            if (pickedUpItems[i] != null)
            {
                pickedUpItems[i].gameObject.SetActive(true);
            }
        }
        pickedUpItems.Clear();
        Debug.Log("[ItemPickup] Respawned all collected items on player death.");
    }

    public bool CanInteract(GameObject interactor) => enabled && gameObject.activeInHierarchy;
    public string InteractionPrompt => string.Empty;

    public void Interact(GameObject interactor)
    {
        Hotbar hotbar = interactor.GetComponent<Hotbar>();
        if (hotbar == null) return;

        bool added = hotbar.AddItem(item, quantity);
        if (!added)
        {
            Debug.Log("Hotbar full — item not picked up.");
            return; // don't notify or disable if it didn't fit
        }

        string itemNameStr = item != null && !string.IsNullOrEmpty(item.itemName) ? item.itemName : gameObject.name;
        NotificationUI.ShowNotification($"Item {itemNameStr} added to backpack.");
        
        // Track item for respawn on player death and disable
        if (!pickedUpItems.Contains(this))
        {
            pickedUpItems.Add(this);
        }
        gameObject.SetActive(false);
    }

    // public Sprite GetItemIcon()
    // {
    //     if (itemIcon != null) return itemIcon;
    //     SpriteRenderer sr = GetComponent<SpriteRenderer>();
    //     if (sr != null) return sr.sprite;
    //     return null;
    // }

    // public bool CanInteract(GameObject interactor)
    // {
    //     return enabled && gameObject.activeInHierarchy;
    // }

    // public void Interact(GameObject interactor)
    // {
    //     Inventory inventory = null;
    //     if (interactor != null)
    //     {
    //         inventory = interactor.GetComponent<Inventory>();
    //     }

    //     if (inventory == null && PlayerController.Instance != null)
    //     {
    //         inventory = PlayerController.Instance.GetComponent<Inventory>();
    //     }

    //     Sprite iconToUse = GetItemIcon();
    //     string nameToUse = string.IsNullOrEmpty(itemName) ? gameObject.name : itemName;

    //     if (inventory != null)
    //     {
    //         inventory.AddItem(nameToUse, iconToUse);
    //     }

    //     if (NotificationUI.Instance != null)
    //     {
    //         NotificationUI.Instance.Show(nameToUse + " Obtained!", iconToUse);
    //     }

    //     Destroy(gameObject);
    // }
}