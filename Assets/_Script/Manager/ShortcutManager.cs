using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent Singleton Manager that tracks unlocked shortcuts across gameplay scenes and player respawns.
/// Shortcuts remain open permanently during a run, but reset when player enters/restarts from the Main Menu.
/// </summary>
public class ShortcutManager : MonoBehaviour
{
    public static ShortcutManager Instance { get; private set; }

    // Set storing unique IDs of unlocked shortcuts in the current game session
    private HashSet<string> unlockedShortcutKeys = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject managerObj = new GameObject("[ShortcutManager]");
            managerObj.AddComponent<ShortcutManager>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private bool IsNonGameplayScene(string sceneName)
    {
        return sceneName.Equals("Index", System.StringComparison.OrdinalIgnoreCase)
            || sceneName.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase)
            || sceneName.Equals("Menu", System.StringComparison.OrdinalIgnoreCase)
            || sceneName.Equals("Title", System.StringComparison.OrdinalIgnoreCase);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;

        // Reset shortcuts when player returns to Main Menu
        if (IsNonGameplayScene(scene.name))
        {
            ResetAllShortcuts();
            return;
        }

        // Apply state for all ItemInfo shortcuts present in the newly loaded scene
        ItemInfo[] itemsInScene = FindObjectsByType<ItemInfo>(FindObjectsSortMode.None);
        foreach (ItemInfo item in itemsInScene)
        {
            if (item != null)
            {
                item.CheckAndApplyPersistentShortcutState();
            }
        }
    }

    /// <summary>
    /// Registers a shortcut as unlocked for the current game session.
    /// </summary>
    public void UnlockShortcut(string shortcutKey)
    {
        if (string.IsNullOrEmpty(shortcutKey)) return;

        if (!unlockedShortcutKeys.Contains(shortcutKey))
        {
            unlockedShortcutKeys.Add(shortcutKey);
            Debug.Log($"[ShortcutManager] Shortcut '{shortcutKey}' unlocked permanently for this run.");
        }
    }

    /// <summary>
    /// Checks whether a shortcut has been unlocked in the current game session.
    /// </summary>
    public bool IsShortcutUnlocked(string shortcutKey)
    {
        if (string.IsNullOrEmpty(shortcutKey)) return false;
        return unlockedShortcutKeys.Contains(shortcutKey);
    }

    /// <summary>
    /// Resets all unlocked shortcuts. Called when starting a new game or entering Main Menu.
    /// </summary>
    public void ResetAllShortcuts()
    {
        unlockedShortcutKeys.Clear();
        Debug.Log("[ShortcutManager] All shortcuts reset for a new game.");
    }
}
