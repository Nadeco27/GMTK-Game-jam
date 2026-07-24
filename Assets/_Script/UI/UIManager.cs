using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent UI Manager that automatically ensures the dedicated UI Scene is loaded additively
/// alongside gameplay scenes, while excluding non-gameplay scenes like Main Menu.
/// </summary>
public class UIManager : MonoBehaviour
{
    private static UIManager _instance;

    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("UI Scene Settings")]
    [Tooltip("The exact name of the dedicated UI scene added in Build Settings.")]
    [SerializeField] private string uiSceneName = "UIScene";

    [Header("Excluded Scenes")]
    [Tooltip("Names of scenes where the gameplay UI should NOT be loaded (e.g. Index, MainMenu).")]
    [SerializeField] private List<string> nonGameplayScenes = new List<string> { "Index", "MainMenu", "Menu", "Title", "StartMenu" };

    /// <summary>
    /// Automatically runs when Play Mode starts, ensuring UIManager exists.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject managerObj = new GameObject("[UIManager]");
            managerObj.AddComponent<UIManager>();
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }

        EnsureUISceneState();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureUISceneState();
    }

    private bool IsNonGameplayScene(string sceneName)
    {
        foreach (string name in nonGameplayScenes)
        {
            if (sceneName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks the current active scene and loads/unloads the gameplay UIScene accordingly.
    /// </summary>
    public void EnsureUISceneState()
    {
        if (string.IsNullOrEmpty(uiSceneName)) return;

        string currentScene = SceneManager.GetActiveScene().name;

        // If we are currently in Main Menu or non-gameplay scene, unload UIScene if loaded
        if (IsNonGameplayScene(currentScene))
        {
            UnloadUISceneIfLoaded();
            return;
        }

        // Check if UIScene is already loaded in the hierarchy
        bool isLoaded = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name.Equals(uiSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                isLoaded = true;
                break;
            }
        }

        if (!isLoaded)
        {
            if (Application.CanStreamedLevelBeLoaded(uiSceneName))
            {
                SceneManager.LoadSceneAsync(uiSceneName, LoadSceneMode.Additive);
            }
            else
            {
                Debug.LogWarning($"[UIManager] Cannot load '{uiSceneName}' additively because it is not added to Build Settings!");
            }
        }
    }

    private void UnloadUISceneIfLoaded()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name.Equals(uiSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                SceneManager.UnloadSceneAsync(uiSceneName);
                break;
            }
        }
    }
}
