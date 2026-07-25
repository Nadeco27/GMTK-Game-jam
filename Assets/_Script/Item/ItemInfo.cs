using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script attached to Information Items in the game world.
/// When interacted with by the player, it opens an Info Panel showing a centered graphic.
/// Optionally unlocks shortcuts by disabling configured GameObjects (same scene & cross-scene) permanently across scene transitions/respawns.
/// </summary>
public class ItemInfo : MonoBehaviour, IInteractable
{
    [Header("Item Information Settings")]
    [Tooltip("Title or identifier for this information item.")]
    [SerializeField] private string itemName = "Information Note";

    [Tooltip("The sprite graphic to be displayed in the center of the Info Panel.")]
    [SerializeField] private Sprite infoSprite;

    [Tooltip("Custom prompt message displayed when in interaction range.")]
    [SerializeField] private string promptMessage = "Tekan E untuk membaca";

    [Header("Notification Settings")]
    [Tooltip("If true, shows a notification UI message after closing the Info Panel.")]
    [SerializeField] private bool showNotificationOnClose = true;

    [Tooltip("The text to display in the notification UI after closing the Info Panel.")]
    [SerializeField] private string notificationMessage = "Something has changed within the map.";

    [Header("Shortcut Unlock Settings (Same Scene)")]
    [Tooltip("If true, interacting with this info item will permanently open/unlock hidden shortcuts in this run.")]
    [SerializeField] private bool unlockShortcutOnInteract = false;

    [Tooltip("Optional custom unique ID for this shortcut. If left blank, it will automatically generate a key.")]
    [SerializeField] private string customShortcutID = "";

    [Tooltip("List of GameObjects (walls, secret doors, obstacles) in THIS scene to disable when this shortcut is unlocked.")]
    [SerializeField] private List<GameObject> objectsToDisable = new List<GameObject>();

    [Header("Cross-Scene Shortcut Settings")]
    [Tooltip("List of Shortcut IDs in OTHER scenes to unlock/disable when this info item is read.")]
    [SerializeField] private List<string> crossSceneShortcutIDsToDisable = new List<string>();

    public string ItemName => itemName;
    public Sprite InfoSprite => infoSprite;
    public string InteractionPrompt => promptMessage;

    private void Start()
    {
        CheckAndApplyPersistentShortcutState();
    }

    public string GetShortcutKey()
    {
        if (!string.IsNullOrEmpty(customShortcutID))
        {
            return customShortcutID;
        }
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return $"{sceneName}_{gameObject.name}_{itemName}";
    }

    /// <summary>
    /// Checks if this shortcut was previously unlocked in the current game session and applies disabled state.
    /// </summary>
    public void CheckAndApplyPersistentShortcutState()
    {
        if (!unlockShortcutOnInteract || objectsToDisable == null || objectsToDisable.Count == 0) return;

        if (ShortcutManager.Instance != null && ShortcutManager.Instance.IsShortcutUnlocked(GetShortcutKey()))
        {
            ApplyShortcutUnlockedState();
        }
    }

    /// <summary>
    /// Disables all assigned same-scene shortcut GameObjects.
    /// </summary>
    public void ApplyShortcutUnlockedState()
    {
        if (objectsToDisable == null) return;

        foreach (GameObject obj in objectsToDisable)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        return enabled && gameObject.activeInHierarchy;
    }

    public void Interact(GameObject interactor)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("paper_pickup");
        }

        if (InfoPanelUI.Instance != null)
        {
            string notificationText = showNotificationOnClose ? notificationMessage : null;
            InfoPanelUI.Instance.ShowInfo(infoSprite, notificationText);
        }
        else
        {
            Debug.LogWarning($"[ItemInfo] InfoPanelUI instance not found in scene while interacting with '{itemName}'.");
        }

        if (unlockShortcutOnInteract || (crossSceneShortcutIDsToDisable != null && crossSceneShortcutIDsToDisable.Count > 0))
        {
            UnlockShortcut();
        }
    }

    /// <summary>
    /// Unlocks the shortcut, disabling configured same-scene GameObjects & cross-scene shortcut IDs in ShortcutManager.
    /// </summary>
    public void UnlockShortcut()
    {
        ApplyShortcutUnlockedState();

        if (ShortcutManager.Instance != null)
        {
            // Register same-scene shortcut
            if (unlockShortcutOnInteract)
            {
                ShortcutManager.Instance.UnlockShortcut(GetShortcutKey());
            }

            // Register cross-scene shortcut IDs
            if (crossSceneShortcutIDsToDisable != null)
            {
                foreach (string crossID in crossSceneShortcutIDsToDisable)
                {
                    if (!string.IsNullOrEmpty(crossID))
                    {
                        ShortcutManager.Instance.UnlockShortcut(crossID);
                    }
                }
            }
        }
    }
}
