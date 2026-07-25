using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls doors that require a specific key to open when the player bumps into them.
/// Persists opened door state across scene reloads and player death respawns.
/// </summary>
public class KeyDoor : MonoBehaviour
{
    // Persistent static collection of all unlocked door IDs across scenes and deaths
    private static readonly HashSet<string> openedDoorIDs = new HashSet<string>();

    [Header("Key Requirement")]
    [Tooltip("The ScriptableObject item required (Optional if using Required Key Name).")]
    [SerializeField] private ItemData requiredItemData;

    [Tooltip("The exact name of the key required in Inventory/Hotbar (e.g. 'Red Key' or 'Kunci Merah').")]
    [SerializeField] private string requiredKeyName = "Red Key";

    [Header("Door Settings")]
    [Tooltip("Unique Identifier for this door. Auto-generated if left empty.")]
    [SerializeField] private string customDoorID;

    [Tooltip("If true, the entire GameObject will be deactivated when unlocked. If false, only the collider and renderer will be disabled.")]
    [SerializeField] private bool disableGameObjectOnOpen = true;

    [Tooltip("Message to show in NotificationUI when player opens the door.")]
    [SerializeField] private string openSuccessMessage = "Door successfully opened.";

    [Tooltip("Message to show in NotificationUI when player touches the door without the key.")]
    [SerializeField] private string missingKeyMessage = "Key is missing.";

    [Header("Optional Components")]
    [SerializeField] private Collider2D doorCollider;
    [SerializeField] private SpriteRenderer doorRenderer;

    private bool isOpen = false;
    private float nextNotificationTime = 0f;

    private void Awake()
    {
        if (doorCollider == null) doorCollider = GetComponent<Collider2D>();
        if (doorRenderer == null) doorRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // If this door was already opened in a previous run or before scene reload, maintain opened state
        string id = GetDoorID();
        if (openedDoorIDs.Contains(id))
        {
            isOpen = true;
            ApplyOpenState(isInitialLoad: true);
        }
    }

    /// <summary>
    /// Resets all opened key doors. Called when starting a new game from Main Menu.
    /// </summary>
    public static void ResetOpenedDoors()
    {
        openedDoorIDs.Clear();
        Debug.Log("[KeyDoor] Reset all opened doors for a new game.");
    }

    private string GetDoorID()
    {
        if (!string.IsNullOrEmpty(customDoorID)) return customDoorID;
        return $"{gameObject.scene.name}_{gameObject.name}_{transform.position.x:F2}_{transform.position.y:F2}";
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryOpenDoor(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        TryOpenDoor(collider.gameObject);
    }

    /// <summary>
    /// Checks if the object bumping into the door is the player with the required key.
    /// </summary>
    public bool TryOpenDoor(GameObject targetObj)
    {
        if (isOpen) return true;

        // Check if target is Player
        if (!targetObj.CompareTag("Player") && targetObj.GetComponent<PlayerController>() == null)
        {
            return false;
        }

        bool hasKey = CheckPlayerHasKey(targetObj);

        if (hasKey)
        {
            OpenDoor();
            return true;
        }
        else
        {
            // Player bumped into door without key
            if (Time.time >= nextNotificationTime)
            {
                nextNotificationTime = Time.time + 2.0f; // Cooldown to avoid notification spam
                if (!string.IsNullOrEmpty(missingKeyMessage))
                {
                    NotificationUI.ShowNotification(missingKeyMessage);
                }
                Debug.Log($"[KeyDoor] Player bumped into {gameObject.name} without required key: '{requiredKeyName}'");
            }
            return false;
        }
    }

    /// <summary>
    /// Checks Inventory and Hotbar to see if player holds the key (without removing it).
    /// </summary>
    private bool CheckPlayerHasKey(GameObject player)
    {
        // 1. Check Inventory component
        Inventory inventory = player.GetComponent<Inventory>();
        if (inventory != null)
        {
            if (!string.IsNullOrEmpty(requiredKeyName) && inventory.HasItem(requiredKeyName))
            {
                return true;
            }
        }

        // 2. Check Hotbar component
        Hotbar hotbar = player.GetComponent<Hotbar>();
        if (hotbar != null)
        {
            if (requiredItemData != null && hotbar.HasItem(requiredItemData))
            {
                return true;
            }
            if (!string.IsNullOrEmpty(requiredKeyName) && hotbar.HasItem(requiredKeyName))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Unlocks and opens the door, registering it in persistent openedDoorIDs.
    /// </summary>
    public void OpenDoor()
    {
        if (isOpen) return;
        isOpen = true;

        openedDoorIDs.Add(GetDoorID());
        ApplyOpenState(isInitialLoad: false);
    }

    private void ApplyOpenState(bool isInitialLoad)
    {
        if (!isInitialLoad)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("door_open");
            }

            if (!string.IsNullOrEmpty(openSuccessMessage))
            {
                NotificationUI.ShowNotification(openSuccessMessage);
            }
        }

        if (disableGameObjectOnOpen)
        {
            gameObject.SetActive(false);
        }
        else
        {
            if (doorCollider != null) doorCollider.enabled = false;
            if (doorRenderer != null) doorRenderer.enabled = false;
        }

        Debug.Log($"[KeyDoor] Door '{gameObject.name}' (ID: {GetDoorID()}) open state applied. Key remains in inventory.");
    }
}
