using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

/// <summary>
/// Singleton UI Manager in UIScene for the Level Exit / Victory Panel.
/// Enhanced with fun celebratory DOTween animations (Elastic Pop-In, Title Pulse, Icon Wobble & Button Punch).
/// Automatically handles locking player movement & interaction, showing the exit UI panel,
/// and transitioning smoothly to the Main Menu scene via SceneFader.
/// </summary>
public class LevelExitUI : MonoBehaviour
{
    private static LevelExitUI _instance;

    public static LevelExitUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<LevelExitUI>(FindObjectsInactive.Include);
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("UI References")]
    [Tooltip("The main container GameObject for the exit UI panel.")]
    public GameObject panel;

    [Tooltip("The Button inside the exit panel that transitions to Main Menu.")]
    public Button returnToMainMenuButton;

    [Header("Scene Transition Settings")]
    [Tooltip("Default target scene name for main menu.")]
    [SerializeField] private string defaultMainMenuSceneName = "MainMenu";

    [Header("DOTween Festive Animation Settings")]
    [Tooltip("Enable DOTween celebratory animations for victory exit panel.")]
    [SerializeField] private bool useDOTween = true;

    [Tooltip("Duration of the pop-up panel entrance animation.")]
    [SerializeField] private float popAnimDuration = 0.45f;

    [Header("Optional Festive Elements for Animation")]
    [Tooltip("Optional: Title or Victory Text component (e.g. 'STAGE CLEAR!').")]
    [SerializeField] private RectTransform titleTextTransform;

    [Tooltip("Optional: Decorative Icon / Trophy / Star graphic.")]
    [SerializeField] private RectTransform celebrationIconTransform;

    private string currentTargetSceneName = "MainMenu";
    private CanvasGroup panelCanvasGroup;
    private Tween titlePulseTween;
    private Tween iconWobbleTween;
    private Tween buttonPulseTween;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            if (_instance.panel == null && this.panel != null)
            {
                Destroy(_instance);
                _instance = this;
                if (transform.parent == null) DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            _instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }

        AutoLinkReferences();
        SetupButtonListener();

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        KillAllTweens();
    }

    private void AutoLinkReferences()
    {
        if (panel == null)
        {
            panel = gameObject;
        }

        if (returnToMainMenuButton == null)
        {
            returnToMainMenuButton = GetComponentInChildren<Button>(true);
        }

        if (panel != null && panelCanvasGroup == null)
        {
            panelCanvasGroup = panel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = panel.AddComponent<CanvasGroup>();
            }
        }
    }

    private void SetupButtonListener()
    {
        if (returnToMainMenuButton != null)
        {
            returnToMainMenuButton.onClick.RemoveAllListeners();
            returnToMainMenuButton.onClick.AddListener(OnReturnToMainMenuClicked);
        }
    }

    private void KillAllTweens()
    {
        titlePulseTween?.Kill();
        iconWobbleTween?.Kill();
        buttonPulseTween?.Kill();

        if (panel != null) panel.transform.DOKill();
        if (titleTextTransform != null) titleTextTransform.DOKill();
        if (celebrationIconTransform != null) celebrationIconTransform.DOKill();
        if (returnToMainMenuButton != null) returnToMainMenuButton.transform.DOKill();
        if (panelCanvasGroup != null) panelCanvasGroup.DOKill();
    }

    /// <summary>
    /// Static helper method to display the Level Exit UI from any trigger in any scene.
    /// </summary>
    public static void ShowExit(string mainMenuSceneName = "MainMenu")
    {
        if (Instance != null)
        {
            Instance.ShowExitPanel(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning($"[LevelExitUI] Instance not found! Ensure LevelExitUI is placed on a panel in UIScene.");
        }
    }

    /// <summary>
    /// Locks player movement and interaction, then displays the exit UI panel with DOTween animations.
    /// </summary>
    public void ShowExitPanel(string targetSceneName = null)
    {
        currentTargetSceneName = !string.IsNullOrEmpty(targetSceneName) ? targetSceneName : defaultMainMenuSceneName;

        AutoLinkReferences();
        SetupButtonListener();

        // 1. Lock player movement & stop velocity
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.enabled = false;
            Rigidbody2D rb = PlayerController.Instance.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        // 2. Lock player interactions
        if (PlayerInteractor.Instance != null)
        {
            PlayerInteractor.Instance.SetInteractionEnabled(false);
        }

        // 3. Display Exit Panel with Fun Celebratory Animation
        if (panel != null)
        {
            panel.SetActive(true);

            if (useDOTween)
            {
                KillAllTweens();

                // Initial hidden state for entrance animation
                panel.transform.localScale = Vector3.zero;
                if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;

                // Elastic Pop-In Panel Entrance
                panel.transform.DOScale(Vector3.one, popAnimDuration).SetEase(Ease.OutBack);
                if (panelCanvasGroup != null) panelCanvasGroup.DOFade(1f, popAnimDuration * 0.6f);

                // Title Text Punch Scale & Continuous Pulse Loop
                if (titleTextTransform != null)
                {
                    titleTextTransform.localScale = Vector3.one;
                    titleTextTransform.DOPunchScale(new Vector3(0.2f, 0.2f, 0f), 0.5f, 5, 0.5f)
                        .OnComplete(() =>
                        {
                            titlePulseTween = titleTextTransform.DOScale(1.06f, 0.7f)
                                .SetLoops(-1, LoopType.Yoyo)
                                .SetEase(Ease.InOutSine);
                        });
                }

                // Celebration Icon Wobble Loop Animation
                if (celebrationIconTransform != null)
                {
                    celebrationIconTransform.localScale = Vector3.zero;
                    celebrationIconTransform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetDelay(0.15f)
                        .OnComplete(() =>
                        {
                            iconWobbleTween = celebrationIconTransform.DORotate(new Vector3(0f, 0f, 10f), 0.6f)
                                .SetLoops(-1, LoopType.Yoyo)
                                .SetEase(Ease.InOutSine);
                        });
                }

                // Return Button Pop-In Entrance & Gentle Pulse Loop
                if (returnToMainMenuButton != null)
                {
                    RectTransform btnRect = returnToMainMenuButton.GetComponent<RectTransform>();
                    if (btnRect != null)
                    {
                        btnRect.localScale = Vector3.zero;
                        btnRect.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetDelay(0.25f)
                            .OnComplete(() =>
                            {
                                buttonPulseTween = btnRect.DOScale(1.05f, 0.6f)
                                    .SetLoops(-1, LoopType.Yoyo)
                                    .SetEase(Ease.InOutSine);
                            });
                    }
                }
            }
            else
            {
                panel.transform.localScale = Vector3.one;
                if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;
            }
        }
    }

    /// <summary>
    /// Button click listener: Punch feedback and smooth fade load to Main Menu.
    /// </summary>
    public void OnReturnToMainMenuClicked()
    {
        // Interactive punch scale feedback on button click
        if (returnToMainMenuButton != null && useDOTween)
        {
            returnToMainMenuButton.transform.DOPunchScale(new Vector3(-0.15f, -0.15f, 0f), 0.2f, 5, 0.5f);
        }

        KillAllTweens();

        // Re-enable player interaction for future runs
        if (PlayerInteractor.Instance != null)
        {
            PlayerInteractor.Instance.SetInteractionEnabled(true);
        }

        if (panel != null)
        {
            panel.SetActive(false);
        }

        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeToScene(currentTargetSceneName, null);
        }
        else
        {
            SceneManager.LoadSceneAsync(currentTargetSceneName);
        }
    }
}
