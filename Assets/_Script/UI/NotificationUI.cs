using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// General action text-only Notification UI manager using TextMeshPro (TMP_Text).
/// Allows modular 1-line calls from anywhere in the codebase (e.g. NotificationUI.ShowNotification("Message")).
/// </summary>
public class NotificationUI : MonoBehaviour
{
    private static NotificationUI _instance;

    public static NotificationUI Instance
    {
        get
        {
            if (_instance == null || _instance.gameObject == null)
            {
                _instance = FindFirstObjectByType<NotificationUI>(FindObjectsInactive.Include);
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("UI References")]
    [Tooltip("The panel container for the notification.")]
    public GameObject panel;

    [Tooltip("TextMeshPro text component displaying the notification message.")]
    public TMP_Text textNotification;

    [Header("Notification Settings")]
    [Tooltip("Duration in seconds the notification stays on screen.")]
    [SerializeField] private float displayDuration = 2.5f;

    private Coroutine currentDisplayRoutine;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // If duplicate NotificationUI exists in scene, destroy the duplicate GameObject
            // to prevent duplicate Canvas panels with static default text from remaining on screen.
            Destroy(gameObject);
            return;
        }

        _instance = this;
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }

        AutoLinkReferences();

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void AutoLinkReferences()
    {
        if (panel == null)
        {
            panel = gameObject;
        }

        if (textNotification == null)
        {
            textNotification = GetComponentInChildren<TMP_Text>(true);
        }

        // Hide any existing image icon components as NotificationUI is text-only
        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
        {
            if (panel != null && img.gameObject != panel)
            {
                img.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Static 1-line modular call to display any text notification in the game.
    /// Example: NotificationUI.ShowNotification("Item Red Key added to backpack.");
    /// </summary>
    public static void ShowNotification(string message)
    {
        if (Instance != null)
        {
            Instance.Show(message);
        }
        else
        {
            Debug.Log($"[NotificationUI] {message}");
        }
    }

    /// <summary>
    /// Displays text-only action notification for the configured duration.
    /// Overload maintained for backwards compatibility.
    /// </summary>
    public void Show(string message, Sprite icon = null)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        AutoLinkReferences();

        if (currentDisplayRoutine != null)
        {
            StopCoroutine(currentDisplayRoutine);
        }

        currentDisplayRoutine = StartCoroutine(ShowRoutine(message));
    }

    private IEnumerator ShowRoutine(string message)
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }

        // 1. Primary update on assigned or auto-found TextMeshPro component
        if (textNotification != null)
        {
            textNotification.text = message;
            textNotification.SetText(message);
            textNotification.gameObject.SetActive(true);
            textNotification.ForceMeshUpdate();
        }

        // 2. Fallback update across all child TMP_Text components inside panel
        GameObject container = panel != null ? panel : gameObject;
        TMP_Text[] tmps = container.GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in tmps)
        {
            if (tmp != null)
            {
                tmp.text = message;
                tmp.SetText(message);
                tmp.gameObject.SetActive(true);
                tmp.ForceMeshUpdate();
            }
        }

        yield return new WaitForSeconds(displayDuration);

        if (panel != null)
        {
            panel.SetActive(false);
        }

        currentDisplayRoutine = null;
    }
}