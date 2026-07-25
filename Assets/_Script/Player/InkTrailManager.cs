using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent Manager (DontDestroyOnLoad) that records and renders ink trails per scene.
/// Preserves player ink trails across scene transitions and manages stroke lifespans across runs.
/// Auto-initializes on Play Mode entry if missing.
/// </summary>
public class InkTrailManager : MonoBehaviour
{
    public static InkTrailManager Instance { get; private set; }

    [Header("Ink Dot Visual Settings")]
    [Tooltip("Masukkan Prefab yang memiliki SpriteRenderer dengan gambar titik/jejak Anda.")]
    [SerializeField] private GameObject dotPrefab;

    [Header("Excluded Scenes")]
    [Tooltip("Names of scenes where ink trails should NOT be created or rendered (e.g. Index, MainMenu).")]
    [SerializeField] private List<string> nonGameplayScenes = new List<string> { "Index", "MainMenu", "Menu", "Title" };

    [Header("Run Tracking")]
    [Tooltip("Current run index. Incremented each time player dies and respawns.")]
    [SerializeField] private int currentRunIndex = 1;

    [Tooltip("Number of runs ink trails will persist before being deleted.")]
    [SerializeField] private int maxRunRetentionCount = 2;

    public int CurrentRunIndex => currentRunIndex;

    // Dictionary mapping scene name -> list of strokes in that scene
    private Dictionary<string, List<InkStroke>> sceneStrokes = new Dictionary<string, List<InkStroke>>();

    private InkStroke currentStroke;
    private GameObject inkContainer;

    /// <summary>
    /// Automatically ensures InkTrailManager exists as soon as Play Mode starts in any scene.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject managerObj = new GameObject("[InkTrailManager]");
            managerObj.AddComponent<InkTrailManager>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // If existing Instance was created without dotPrefab, transfer reference from this instance before destroying duplicate
            if (Instance.dotPrefab == null && this.dotPrefab != null)
            {
                Instance.dotPrefab = this.dotPrefab;
            }
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
        foreach (string name in nonGameplayScenes)
        {
            if (sceneName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. Ignore Additive scene loads (such as UIScene loading additively)
        if (mode == LoadSceneMode.Additive) return;

        // 2. Clear ink renderers when entering a main menu scene
        if (IsNonGameplayScene(scene.name))
        {
            if (inkContainer != null)
            {
                Destroy(inkContainer);
            }
            return;
        }

        // 3. Reset current active stroke when changing gameplay scenes
        currentStroke = null;

        // 4. Re-render all existing strokes for this gameplay scene
        RebuildSceneStrokes(scene.name);
    }

    /// <summary>
    /// Re-renders all stored ink strokes for the given scene.
    /// </summary>
    private void RebuildSceneStrokes(string sceneName)
    {
        if (inkContainer != null)
        {
            Destroy(inkContainer);
        }
        inkContainer = new GameObject("[InkTrailContainer]");

        if (!sceneStrokes.ContainsKey(sceneName))
        {
            sceneStrokes[sceneName] = new List<InkStroke>();
            return;
        }

        foreach (InkStroke stroke in sceneStrokes[sceneName])
        {
            foreach (Vector2 point in stroke.points)
            {
                SpawnDotObject(point);
            }
        }
    }

    /// <summary>
    /// Called when player dies and respawns. Increments the run count
    /// and purges ink strokes older than maxRunRetentionCount.
    /// </summary>
    public void AdvanceToNextRun()
    {
        EndCurrentStroke();
        currentRunIndex++;

        // Keep trails from current run and (maxRunRetentionCount - 1) previous runs
        int minAllowedRunIndex = currentRunIndex - (maxRunRetentionCount - 1);

        // Clean up strokes older than maxRunRetentionCount across all scenes
        foreach (var sceneKey in new List<string>(sceneStrokes.Keys))
        {
            List<InkStroke> strokes = sceneStrokes[sceneKey];
            strokes.RemoveAll(s => s.runIndex < minAllowedRunIndex);
        }

        // Rebuild strokes in active scene
        string activeScene = SceneManager.GetActiveScene().name;
        if (!IsNonGameplayScene(activeScene))
        {
            RebuildSceneStrokes(activeScene);
        }
    }

    private void SpawnDotObject(Vector2 position)
    {
        if (dotPrefab == null) return;
        
        if (inkContainer == null)
        {
            inkContainer = new GameObject("[InkTrailContainer]");
        }

        GameObject dot = Instantiate(dotPrefab, position, Quaternion.identity, inkContainer.transform);
        
        
        dot.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
        
        // Randomize the scale of the dot 
        float randomScale = UnityEngine.Random.Range(0.1f, 0.2f);
        dot.transform.localScale = new Vector3(randomScale, randomScale, 1f);
    }
    /// <summary>
    /// Checks if there is any ink stroke point within a specified radius of the target position in the given scene.
    /// </summary>
    public bool CheckInkNearPosition(string sceneName, Vector2 targetPos, float radius)
    {
        if (!sceneStrokes.ContainsKey(sceneName)) return false;

        float sqrRadius = radius * radius;
        foreach (InkStroke stroke in sceneStrokes[sceneName])
        {
            foreach (Vector2 point in stroke.points)
            {
                // Jika jarak antara titik tinta dan target lebih kecil dari radius
                if ((point - targetPos).sqrMagnitude <= sqrRadius)
                {
                    return true;
                }
            }
        }
        return false;
    }
    /// <summary>
    /// Starts a new ink stroke at the specified world position for the current run.
    /// </summary>
    public void StartNewStroke(Vector2 startPos)
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (IsNonGameplayScene(currentScene)) return;

        if (!sceneStrokes.ContainsKey(currentScene))
        {
            sceneStrokes[currentScene] = new List<InkStroke>();
        }

        currentStroke = new InkStroke(currentRunIndex);
        currentStroke.AddPoint(startPos);
        sceneStrokes[currentScene].Add(currentStroke);

        // Munculkan gambar titik pertama
        SpawnDotObject(startPos);
    }

    /// <summary>
    /// Appends a new point to the currently active stroke.
    /// </summary>
    public void AddPointToCurrentStroke(Vector2 point)
    {
        if (currentStroke == null)
        {
            StartNewStroke(point);
            return;
        }

        currentStroke.AddPoint(point);
        SpawnDotObject(point);
    }

    /// <summary>
    /// Ends the current stroke drawing.
    /// </summary>
    public void EndCurrentStroke()
    {
        currentStroke = null;
    }

    #region Clean Mechanics
    /// <summary>
    /// Clears all ink trails stored for the current active scene.
    /// </summary>
    public void ClearCurrentSceneInk()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (sceneStrokes.ContainsKey(currentScene))
        {
            sceneStrokes[currentScene].Clear();
        }

        RebuildSceneStrokes(currentScene);
    }

    /// <summary>
    /// Resets all ink trails across all scenes and resets run index to 1. Called when starting a new game.
    /// </summary>
    public void ResetAllInk()
    {
        sceneStrokes.Clear();
        currentStroke = null;
        currentRunIndex = 1;
        if (inkContainer != null)
        {
            Destroy(inkContainer);
        }
        Debug.Log("[InkTrailManager] Cleared all ink trails for new game.");
    }
    #endregion
}