using UnityEngine;

/// <summary>
/// Item pickup script updated to implement IInteractable interface for Key-E interaction.
/// Supports both legacy setups and new modular interaction system.
/// </summary>
public class ItemPickup : MonoBehaviour, IInteractable
{
    [Header("Legacy Item Pickup Settings")]
    [SerializeField] private ItemData item;
    [SerializeField] private int quantity = 1;
    
    // public string itemName = "Flashlight";
    // public Sprite itemIcon;
    public string promptMessage = "Tekan E untuk mengambil";

    public bool CanInteract(GameObject interactor) => enabled && gameObject.activeInHierarchy;
    public string InteractionPrompt => promptMessage;

    public void Interact(GameObject interactor)
    {
        
        Hotbar hotbar = interactor.GetComponent<Hotbar>();
        if (hotbar == null) return;

        bool added = hotbar.AddItem(item, quantity);
        if (!added)
        {
            Debug.Log("Hotbar full — item not picked up.");
            return; // don't notify or destroy if it didn't fit
        }

        NotificationUI.Instance?.Show(item.itemName + " Obtained!", item.icon);
        Destroy(gameObject);
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