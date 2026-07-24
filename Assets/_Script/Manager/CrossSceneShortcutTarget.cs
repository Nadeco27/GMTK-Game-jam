using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attached to obstacle/shortcut GameObjects in any scene that should be disabled
/// when a shortcut ID is unlocked from an Info Item (sticky note) in another scene.
/// </summary>
public class CrossSceneShortcutTarget : MonoBehaviour
{
    [Header("Shortcut Identifier")]
    [Tooltip("Unique ID matching the shortcut ID specified in ItemInfo in another scene.")]
    [SerializeField] private string shortcutID = "";

    [Header("Target Objects")]
    [Tooltip("Optional list of GameObjects to disable when unlocked. If empty, disables this GameObject.")]
    [SerializeField] private List<GameObject> targetObjectsToDisable = new List<GameObject>();

    public string ShortcutID => shortcutID;

    private void Start()
    {
        CheckAndApplyState();
    }

    /// <summary>
    /// Checks if this shortcut ID was unlocked in ShortcutManager and applies the disabled state.
    /// </summary>
    public void CheckAndApplyState()
    {
        if (string.IsNullOrEmpty(shortcutID)) return;

        if (ShortcutManager.Instance != null && ShortcutManager.Instance.IsShortcutUnlocked(shortcutID))
        {
            ApplyUnlockedState();
        }
    }

    /// <summary>
    /// Disables the target GameObjects (or this GameObject if no specific targets are listed).
    /// </summary>
    public void ApplyUnlockedState()
    {
        if (targetObjectsToDisable != null && targetObjectsToDisable.Count > 0)
        {
            foreach (GameObject obj in targetObjectsToDisable)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
