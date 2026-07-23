using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles smooth asynchronous scene transitions with screen fade effects.
/// Prevents game freezing when loading scenes in Metroidvania style games.
/// </summary>
public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup faderCanvasGroup;
    [SerializeField] private float fadeDuration = 0.4f;

    private bool isTransitioning;

    private void Awake()
    {
        // Singleton pattern to persist fader across scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Auto-create default Fader UI if not assigned in Inspector
        if (faderCanvasGroup == null)
        {
            CreateDefaultFaderUI();
        }

        faderCanvasGroup.alpha = 0f;
        faderCanvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// Smoothly transitions to a target scene using async loading & fade effect.
    /// </summary>
    public void FadeToScene(string sceneName, LevelConnection connection)
    {
        if (isTransitioning) return;
        StartCoroutine(FadeAndLoadRoutine(sceneName, connection));
    }

    private IEnumerator FadeAndLoadRoutine(string sceneName, LevelConnection connection)
    {
        isTransitioning = true;
        faderCanvasGroup.blocksRaycasts = true;

        // 1. Fade Out (To Black)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            faderCanvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }
        faderCanvasGroup.alpha = 1f;

        // Set the active connection before scene load
        LevelConnection.ActiveConnection = connection;

        // 2. Load Scene Asynchronously in background thread (No freezing!)
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 3. Fade In (From Black)
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            faderCanvasGroup.alpha = Mathf.Clamp01(1f - (timer / fadeDuration));
            yield return null;
        }
        faderCanvasGroup.alpha = 0f;
        faderCanvasGroup.blocksRaycasts = false;
        isTransitioning = false;
    }

    private void CreateDefaultFaderUI()
    {
        GameObject canvasObj = new GameObject("SceneFaderCanvas");
        canvasObj.transform.SetParent(transform);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        faderCanvasGroup = canvasObj.AddComponent<CanvasGroup>();

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        
        Image image = imageObj.AddComponent<Image>();
        image.color = Color.black;

        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
    }
}
