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

    [Header("Ink Line Visual Settings")]
    [SerializeField] private float lineWidth = 0.35f;
    [SerializeField] private Color inkColor = Color.black;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = -1;

    [Header("Run Tracking")]
    [Tooltip("Current run index. Incremented each time player dies and respawns.")]
    [SerializeField] private int currentRunIndex = 1;

    [Tooltip("Number of runs ink trails will persist before being deleted.")]
    [SerializeField] private int maxRunRetentionCount = 2;

    public int CurrentRunIndex => currentRunIndex;

    // Dictionary mapping scene name -> list of strokes in that scene
    private Dictionary<string, List<InkStroke>> sceneStrokes = new Dictionary<string, List<InkStroke>>();

    private List<LineRenderer> activeLineRenderers = new List<LineRenderer>();
    private InkStroke currentStroke;
    private LineRenderer currentLineRenderer;
    private Material inkMaterial;
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
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateDefaultMaterial();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void CreateDefaultMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader != null)
        {
            inkMaterial = new Material(shader);
            inkMaterial.color = inkColor;
        }
    }

    private bool IsNonGameplayScene(string sceneName)
    {
        return sceneName.Equals("UIScene", System.StringComparison.OrdinalIgnoreCase)
            || sceneName.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase)
            || sceneName.Equals("Menu", System.StringComparison.OrdinalIgnoreCase)
            || sceneName.Equals("Title", System.StringComparison.OrdinalIgnoreCase);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Don't process non-gameplay scenes (like MainMenu or UIScene)
        if (IsNonGameplayScene(scene.name))
        {
            if (inkContainer != null)
            {
                Destroy(inkContainer);
            }
            return;
        }

        // Reset current active stroke when changing scenes
        currentStroke = null;
        currentLineRenderer = null;
        activeLineRenderers.Clear();

        // Re-render all existing strokes for this scene
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
        activeLineRenderers.Clear();

        if (!sceneStrokes.ContainsKey(sceneName))
        {
            sceneStrokes[sceneName] = new List<InkStroke>();
            return;
        }

        List<InkStroke> strokes = sceneStrokes[sceneName];
        foreach (InkStroke stroke in strokes)
        {
            if (stroke.points.Count >= 2)
            {
                LineRenderer lr = CreateLineRendererObject();
                lr.positionCount = stroke.points.Count;
                for (int i = 0; i < stroke.points.Count; i++)
                {
                    lr.SetPosition(i, new Vector3(stroke.points[i].x, stroke.points[i].y, 0f));
                }
                activeLineRenderers.Add(lr);
            }
        }
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

        currentLineRenderer = CreateLineRendererObject();
        currentLineRenderer.positionCount = 1;
        currentLineRenderer.SetPosition(0, new Vector3(startPos.x, startPos.y, 0f));
        
        activeLineRenderers.Add(currentLineRenderer);
    }

    /// <summary>
    /// Appends a new point to the currently active stroke.
    /// </summary>
    public void AddPointToCurrentStroke(Vector2 point)
    {
        if (currentStroke == null || currentLineRenderer == null)
        {
            StartNewStroke(point);
            return;
        }

        currentStroke.AddPoint(point);
        int index = currentLineRenderer.positionCount;
        currentLineRenderer.positionCount = index + 1;
        currentLineRenderer.SetPosition(index, new Vector3(point.x, point.y, 0f));
    }

    /// <summary>
    /// Ends the current stroke drawing.
    /// </summary>
    public void EndCurrentStroke()
    {
        currentStroke = null;
        currentLineRenderer = null;
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

    private LineRenderer CreateLineRendererObject()
    {
        if (inkContainer == null)
        {
            inkContainer = new GameObject("[InkTrailContainer]");
        }

        GameObject strokeObj = new GameObject("InkStroke");
        strokeObj.transform.SetParent(inkContainer.transform);

        LineRenderer lr = strokeObj.AddComponent<LineRenderer>();
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.startColor = inkColor;
        lr.endColor = inkColor;
        lr.material = inkMaterial;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 4;
        lr.useWorldSpace = true;
        lr.sortingLayerName = sortingLayerName;
        lr.sortingOrder = sortingOrder;

        return lr;
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
    #endregion
}
