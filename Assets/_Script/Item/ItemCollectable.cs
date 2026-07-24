using UnityEngine;

/// <summary>
/// Script attached to Collectable Items in the game world.
/// When interacted with by pressing the interact key in range, it adds the item & sprite to Inventory,
/// triggers a bottom-right notification UI, and destroys the item from the map.
/// </summary>
public class ItemCollectable : MonoBehaviour, IInteractable
{
    [Header("Item Collectable Settings")]
    [Tooltip("Name of the item added to inventory and displayed in notification.")]
    [SerializeField] private string itemName = "Flashlight";

    [Tooltip("Sprite icon for this item (displayed in Notification UI & Inventory). If left null, uses SpriteRenderer's sprite.")]
    [SerializeField] private Sprite itemIcon;

    [Tooltip("Custom prompt message displayed when in interaction range.")]
    [SerializeField] private string promptMessage = "Tekan E untuk mengambil";

    public string ItemName => itemName;
    public Sprite ItemIcon => GetItemIcon();
    public string InteractionPrompt => promptMessage;

    public Sprite GetItemIcon()
    {
        if (itemIcon != null) return itemIcon;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) return sr.sprite;
        return null;
    }

    public bool CanInteract(GameObject interactor)
    {
        return enabled && gameObject.activeInHierarchy;
    }

    public void Interact(GameObject interactor)
    {
        Inventory inventory = null;
        if (interactor != null)
        {
            inventory = interactor.GetComponent<Inventory>();
        }

        if (inventory == null && PlayerController.Instance != null)
        {
            inventory = PlayerController.Instance.GetComponent<Inventory>();
        }

        Sprite iconToUse = GetItemIcon();
        string nameToUse = string.IsNullOrEmpty(itemName) ? gameObject.name : itemName;

        if (inventory != null)
        {
            inventory.AddItem(nameToUse, iconToUse);
        }

        if (NotificationUI.Instance != null)
        {
            NotificationUI.Instance.Show(nameToUse + " Obtained!", iconToUse);
        }
        else
        {
            Debug.Log($"[Notification] {nameToUse} Obtained!");
        }

        Destroy(gameObject);
    }
}
