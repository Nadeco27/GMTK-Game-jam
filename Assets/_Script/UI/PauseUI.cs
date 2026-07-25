using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Singleton Pause UI Manager that handles pausing the game via Escape key.
/// Features a dark overlay background, 3 festive DOTween animated buttons (Resume, Settings, Main Menu),
/// player control & interaction locking, and auto-generated default UI fallback.
/// </summary>
public class PauseUI : MonoBehaviour
{
    private static PauseUI _instance;

    public static PauseUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<PauseUI>(FindObjectsInactive.Include);
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("UI References")]
    [Tooltip("The main container GameObject for the pause UI panel.")]
    [SerializeField] private GameObject pausePanel;

    [Tooltip("Full-screen dark background overlay image.")]
    [SerializeField] private Image darkBackgroundOverlay;

    [Tooltip("Title Text component (e.g. 'PAUSED').")]
    [SerializeField] private Transform pauseTitleTransform;

    [Tooltip("Optional panel content container box for pop animation (auto-detected if unassigned).")]
    [SerializeField] private Transform pauseContentContainer;

    [Header("Button References")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Settings Panel Reference")]
    [Tooltip("Optional direct reference to Settings Panel GameObject.")]
    [SerializeField] private GameObject settingsPanel;

    [Tooltip("Optional direct reference to Close Settings Button.")]
    [SerializeField] private Button closeSettingsButton;

    [Header("Main Menu Confirmation Panel Reference")]
    [Tooltip("Confirmation panel shown when player clicks Main Menu button.")]
    [SerializeField] private GameObject confirmMainMenuPanel;

    [Tooltip("Proceed Button on confirmation panel (Returns to Main Menu).")]
    [SerializeField] private Button proceedButton;

    [Tooltip("Cancel Button on confirmation panel (Closes confirmation dialog).")]
    [SerializeField] private Button cancelButton;

    [Header("Scene Transition Settings")]
    [Tooltip("Target scene name for returning to Main Menu.")]
    [SerializeField] private string mainMenuSceneName = "Index";

    [Tooltip("List of non-gameplay scene names where pausing is disabled (e.g. Index, MainMenu).")]
    [SerializeField] private List<string> nonPauseScenes = new List<string> { "Index", "MainMenu" };

    [Header("Animation Settings")]
    [Tooltip("Enable celebratory DOTween pop-up entrance and exit animations.")]
    [SerializeField] private bool useDOTweenAnimations = true;

    [SerializeField] private float popAnimDuration = 0.4f;

    public bool IsPaused { get; private set; }

    private CanvasGroup panelCanvasGroup;
    private Tween activeEntranceTween;
    private bool isTransitioning = false;
    private float targetOverlayAlpha = -1f;
    private Coroutine transitionTimeoutCoroutine;

    private void SetTransitioning(bool state, float timeout = 0.5f)
    {
        isTransitioning = state;

        if (transitionTimeoutCoroutine != null)
        {
            StopCoroutine(transitionTimeoutCoroutine);
            transitionTimeoutCoroutine = null;
        }

        if (state && gameObject.activeInHierarchy)
        {
            transitionTimeoutCoroutine = StartCoroutine(TransitionTimeoutRoutine(timeout));
        }
    }

    private IEnumerator TransitionTimeoutRoutine(float timeout)
    {
        yield return new WaitForSecondsRealtime(timeout);
        isTransitioning = false;
        transitionTimeoutCoroutine = null;
    }

    private void CacheDarkBackgroundAlpha()
    {
        if (darkBackgroundOverlay != null && targetOverlayAlpha < 0f)
        {
            float a = darkBackgroundOverlay.color.a;
            targetOverlayAlpha = a > 0.05f ? a : 0.75f;
        }
    }
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // If duplicate instance wakes up from a newly loaded scene (e.g. UIScene loaded additively),
            // copy fresh Inspector references to persistent _instance and destroy duplicate instance!
            _instance.CopyReferencesFrom(this);
            Destroy(gameObject);
            return;
        }

        _instance = this;
        SetTransitioning(false);
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }

        AutoLinkReferences();
        SetupButtonListeners();

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (confirmMainMenuPanel != null)
        {
            confirmMainMenuPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindReferencesIfMissing();
    }

    /// <summary>
    /// Copies fresh serialized UI references from a newly loaded scene component into the persistent Singleton instance.
    /// </summary>
    public void CopyReferencesFrom(PauseUI source)
    {
        if (source == null) return;
        SetTransitioning(false);

        this.pausePanel = source.pausePanel;
        this.darkBackgroundOverlay = source.darkBackgroundOverlay;
        this.pauseTitleTransform = source.pauseTitleTransform;
        this.pauseContentContainer = source.pauseContentContainer;
        this.resumeButton = source.resumeButton;
        this.settingsButton = source.settingsButton;
        this.mainMenuButton = source.mainMenuButton;
        this.settingsPanel = source.settingsPanel;
        this.closeSettingsButton = source.closeSettingsButton;
        this.confirmMainMenuPanel = source.confirmMainMenuPanel;
        this.proceedButton = source.proceedButton;
        this.cancelButton = source.cancelButton;

        AutoLinkReferences();
        SetupButtonListeners();

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (confirmMainMenuPanel != null)
        {
            confirmMainMenuPanel.SetActive(false);
        }
    }

    private void RebindReferencesIfMissing()
    {
        if (pausePanel == null || resumeButton == null)
        {
            PauseUI sceneInstance = FindFirstObjectByType<PauseUI>(FindObjectsInactive.Include);
            if (sceneInstance != null && sceneInstance != this)
            {
                CopyReferencesFrom(sceneInstance);
                Destroy(sceneInstance.gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        SetTransitioning(false);
        activeEntranceTween?.Kill();
        if (pausePanel != null) pausePanel.transform.DOKill();
    }

    private void AutoLinkReferences()
    {
        if (pausePanel == null) pausePanel = gameObject;

        if (panelCanvasGroup == null && pausePanel != null)
        {
            panelCanvasGroup = pausePanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null) panelCanvasGroup = pausePanel.AddComponent<CanvasGroup>();
        }

        if (darkBackgroundOverlay == null && pausePanel != null)
        {
            darkBackgroundOverlay = pausePanel.GetComponentInChildren<Image>(true);
        }

        if (pauseContentContainer == null && pauseTitleTransform != null && pauseTitleTransform.parent != null)
        {
            if (darkBackgroundOverlay == null || pauseTitleTransform.parent != darkBackgroundOverlay.transform)
            {
                pauseContentContainer = pauseTitleTransform.parent;
            }
        }

        AutoAttachButtonJuice();
    }

    private void AutoAttachButtonJuice()
    {
        if (pausePanel == null) return;

        Button[] buttons = pausePanel.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            if (btn != null && btn.GetComponent<UIButtonJuice>() == null)
            {
                btn.gameObject.AddComponent<UIButtonJuice>();
            }
        }
    }

    private void SetupButtonListeners()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OpenSettingsFromPause);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OpenMainMenuConfirmation);
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.RemoveAllListeners();
            closeSettingsButton.onClick.AddListener(CloseSettingsAndReturnToPause);
        }

        if (proceedButton != null)
        {
            proceedButton.onClick.RemoveAllListeners();
            proceedButton.onClick.AddListener(ConfirmReturnToMainMenu);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(CancelReturnToMainMenu);
        }
    }

    private void Update()
    {
        // 1. Disable pausing in non-gameplay scenes like Main Menu
        string currentScene = SceneManager.GetActiveScene().name;
        if (IsNonPauseScene(currentScene)) return;

        // 2. Ignore Escape inputs while transitioning panels (anti-spam protection)
        if (isTransitioning) return;

        // 3. Listen for Escape Key
        if (IsEscapeKeyPressed())
        {
            // Do not trigger pause if InfoPanelUI is open (let InfoPanel close first)
            if (InfoPanelUI.Instance != null && InfoPanelUI.Instance.IsPanelActive)
            {
                return;
            }

            if (IsPaused)
            {
                // Priority 1: Close Main Menu Confirmation panel first if active
                if (IsConfirmationOpen())
                {
                    CancelReturnToMainMenu();
                    return;
                }

                // Priority 2: Close Settings panel first if active
                if (IsSettingsOpen())
                {
                    CloseSettingsAndReturnToPause();
                    return;
                }

                // Priority 3: Close Pause UI and resume game
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    private bool IsSettingsOpen()
    {
        if (settingsPanel != null && settingsPanel.activeInHierarchy)
        {
            return true;
        }

        SettingsUI settingsUI = FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
        if (settingsUI != null && settingsUI.IsSettingsOpen)
        {
            return true;
        }

        return false;
    }

    private bool IsConfirmationOpen()
    {
        return confirmMainMenuPanel != null && confirmMainMenuPanel.activeInHierarchy;
    }

    private bool IsNonPauseScene(string sceneName)
    {
        foreach (string nonPause in nonPauseScenes)
        {
            if (sceneName.Equals(nonPause, System.StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private bool IsEscapeKeyPressed()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape)) return true;
#endif

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
#endif

        return false;
    }

    /// <summary>
    /// Pauses the game, locks player movement & interactions, and displays the Pause Panel with DOTween animations.
    /// Dark background overlay uses pure fade-in animation, while title and buttons use pop entrance.
    /// </summary>
    public void PauseGame()
    {
        if (IsPaused || isTransitioning) return;

        RebindReferencesIfMissing();

        IsPaused = true;
        SetTransitioning(true, popAnimDuration + 0.35f);
        Time.timeScale = 0f;

        // Lock player controller movement and velocity
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.enabled = false;
            Rigidbody2D rb = PlayerController.Instance.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        // Lock player interactions
        if (PlayerInteractor.Instance != null)
        {
            PlayerInteractor.Instance.SetInteractionEnabled(false);
        }

        AutoLinkReferences();
        SetupButtonListeners();

        if (pausePanel != null)
        {
            pausePanel.transform.DOKill();
            pausePanel.SetActive(true);
            pausePanel.transform.localScale = Vector3.one;

            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;

            // 1. Dark background overlay: pure fade-in (no pop / scale effect)
            if (darkBackgroundOverlay != null)
            {
                CacheDarkBackgroundAlpha();
                darkBackgroundOverlay.DOKill();
                darkBackgroundOverlay.transform.localScale = Vector3.one;

                Color c = darkBackgroundOverlay.color;
                c.a = 0f;
                darkBackgroundOverlay.color = c;
                darkBackgroundOverlay.DOFade(targetOverlayAlpha, popAnimDuration).SetUpdate(true);
            }

            if (useDOTweenAnimations)
            {
                activeEntranceTween?.Kill();

                // 2. Pop-In for Main Pause Panel Content Container Box
                if (pauseContentContainer != null && pauseContentContainer != pausePanel.transform && (darkBackgroundOverlay == null || pauseContentContainer != darkBackgroundOverlay.transform))
                {
                    pauseContentContainer.DOKill();
                    pauseContentContainer.localScale = Vector3.zero;
                    pauseContentContainer.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);
                }

                // 3. Staggered Pop-In for Title and Buttons
                if (pauseTitleTransform != null && pauseTitleTransform != pauseContentContainer)
                {
                    pauseTitleTransform.DOKill();
                    pauseTitleTransform.localScale = Vector3.zero;
                    pauseTitleTransform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetDelay(0.08f).SetUpdate(true);
                }

                if (resumeButton != null)
                {
                    resumeButton.transform.DOKill();
                    resumeButton.transform.localScale = Vector3.zero;
                    resumeButton.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetDelay(0.12f).SetUpdate(true);
                }

                if (settingsButton != null)
                {
                    settingsButton.transform.DOKill();
                    settingsButton.transform.localScale = Vector3.zero;
                    settingsButton.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetDelay(0.20f).SetUpdate(true);
                }

                if (mainMenuButton != null)
                {
                    mainMenuButton.transform.DOKill();
                    mainMenuButton.transform.localScale = Vector3.zero;
                    activeEntranceTween = mainMenuButton.transform.DOScale(Vector3.one, 0.35f)
                        .SetEase(Ease.OutBack)
                        .SetDelay(0.28f)
                        .SetUpdate(true);
                }

                DOVirtual.DelayedCall(0.65f, () => SetTransitioning(false)).SetUpdate(true);
            }
            else
            {
                if (pauseContentContainer != null) pauseContentContainer.localScale = Vector3.one;
                if (pauseTitleTransform != null) pauseTitleTransform.localScale = Vector3.one;
                if (resumeButton != null) resumeButton.transform.localScale = Vector3.one;
                if (settingsButton != null) settingsButton.transform.localScale = Vector3.one;
                if (mainMenuButton != null) mainMenuButton.transform.localScale = Vector3.one;
                if (darkBackgroundOverlay != null)
                {
                    Color c = darkBackgroundOverlay.color;
                    c.a = targetOverlayAlpha;
                    darkBackgroundOverlay.color = c;
                }
                SetTransitioning(false);
            }
        }
        else
        {
            SetTransitioning(false);
        }
    }

    /// <summary>
    /// Resumes the game, unfreezes time, and re-enables player controls.
    /// Dark background overlay uses pure fade-out animation.
    /// </summary>
    public void ResumeGame()
    {
        if (!IsPaused || isTransitioning) return;

        SetTransitioning(true, 0.35f);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        if (confirmMainMenuPanel != null)
        {
            confirmMainMenuPanel.transform.DOKill();
            confirmMainMenuPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.transform.DOKill();
            settingsPanel.SetActive(false);
        }

        if (pausePanel != null && useDOTweenAnimations)
        {
            activeEntranceTween?.Kill();

            // 1. Dark background overlay: pure fade-out (alpha 0, scale stays Vector3.one)
            if (darkBackgroundOverlay != null)
            {
                darkBackgroundOverlay.DOKill();
                darkBackgroundOverlay.transform.localScale = Vector3.one;
                darkBackgroundOverlay.DOFade(0f, 0.22f).SetUpdate(true);
            }

            // 2. Scale down pause panel content container box
            if (pauseContentContainer != null && pauseContentContainer != pausePanel.transform && (darkBackgroundOverlay == null || pauseContentContainer != darkBackgroundOverlay.transform))
            {
                pauseContentContainer.DOKill();
                pauseContentContainer.DOScale(Vector3.zero, 0.22f).SetEase(Ease.InBack).SetUpdate(true);
            }

            // 3. Scale down UI title and buttons
            if (pauseTitleTransform != null && pauseTitleTransform != pauseContentContainer)
            {
                pauseTitleTransform.DOKill();
                pauseTitleTransform.DOScale(Vector3.zero, 0.22f).SetEase(Ease.InBack).SetUpdate(true);
            }

            if (resumeButton != null)
            {
                resumeButton.transform.DOKill();
                resumeButton.transform.DOScale(Vector3.zero, 0.22f).SetEase(Ease.InBack).SetUpdate(true);
            }

            if (settingsButton != null)
            {
                settingsButton.transform.DOKill();
                settingsButton.transform.DOScale(Vector3.zero, 0.22f).SetEase(Ease.InBack).SetUpdate(true);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.transform.DOKill();
                mainMenuButton.transform.DOScale(Vector3.zero, 0.22f).SetEase(Ease.InBack).SetUpdate(true);
            }

            DOVirtual.DelayedCall(0.24f, () => FinishResumeGame()).SetUpdate(true);
        }
        else
        {
            FinishResumeGame();
        }
    }

    private void FinishResumeGame()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            pausePanel.transform.localScale = Vector3.one;
        }

        if (pauseContentContainer != null) pauseContentContainer.localScale = Vector3.one;
        if (pauseTitleTransform != null) pauseTitleTransform.localScale = Vector3.one;
        if (resumeButton != null) resumeButton.transform.localScale = Vector3.one;
        if (settingsButton != null) settingsButton.transform.localScale = Vector3.one;
        if (mainMenuButton != null) mainMenuButton.transform.localScale = Vector3.one;

        UnfreezeAndUnlockPlayer();
        SetTransitioning(false);
    }

    private void UnfreezeAndUnlockPlayer()
    {
        Time.timeScale = 1f;
        IsPaused = false;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.enabled = true;
        }

        if (PlayerInteractor.Instance != null)
        {
            PlayerInteractor.Instance.SetInteractionEnabled(true);
        }
    }

    /// <summary>
    /// Opens Settings Panel without disabling Pause Panel.
    /// </summary>
    public void OpenSettingsFromPause()
    {
        if (isTransitioning) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        // Keep pausePanel active so child settingsPanel can display properly
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        // Search for settings panel in scene if unassigned
        if (settingsPanel == null)
        {
            SettingsUI settingsUI = FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
            if (settingsUI != null)
            {
                settingsPanel = settingsUI.gameObject;
            }
        }

        if (settingsPanel != null)
        {
            SettingsUI settingsUI = settingsPanel.GetComponent<SettingsUI>();
            if (settingsUI != null)
            {
                settingsUI.EnsurePanelActive();
            }

            SetTransitioning(true, 0.45f);
            settingsPanel.transform.DOKill();
            settingsPanel.SetActive(true);
            settingsPanel.transform.localScale = Vector3.zero;
            settingsPanel.transform.DOScale(Vector3.one, 0.35f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
            DOVirtual.DelayedCall(0.36f, () => SetTransitioning(false)).SetUpdate(true);
        }
        else
        {
            Debug.LogWarning("[PauseUI] Settings Panel reference not assigned or found in scene.");
        }
    }

    public void CloseSettingsAndReturnToPause()
    {
        if (isTransitioning) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        if (settingsPanel == null)
        {
            SettingsUI settingsUI = FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
            if (settingsUI != null)
            {
                settingsPanel = settingsUI.gameObject;
            }
        }

        if (settingsPanel != null)
        {
            SetTransitioning(true, 0.3f);
            settingsPanel.transform.DOKill();
            settingsPanel.transform.DOScale(Vector3.zero, 0.2f)
                .SetEase(Ease.InBack)
                .SetUpdate(true);
            DOVirtual.DelayedCall(0.21f, () =>
            {
                settingsPanel.SetActive(false);
                settingsPanel.transform.localScale = Vector3.one;
                SetTransitioning(false);
            }).SetUpdate(true);
        }
        else
        {
            SetTransitioning(false);
        }
    }

    /// <summary>
    /// Opens the Main Menu Confirmation Panel when Main Menu button is clicked.
    /// </summary>
    public void OpenMainMenuConfirmation()
    {
        if (isTransitioning) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        if (confirmMainMenuPanel != null)
        {
            SetTransitioning(true, 0.45f);
            confirmMainMenuPanel.transform.DOKill();
            confirmMainMenuPanel.SetActive(true);
            confirmMainMenuPanel.transform.localScale = Vector3.zero;
            confirmMainMenuPanel.transform.DOScale(Vector3.one, 0.35f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
            DOVirtual.DelayedCall(0.36f, () => SetTransitioning(false)).SetUpdate(true);
        }
        else
        {
            // If no confirmation panel is assigned in Inspector, return to Main Menu directly
            ReturnToMainMenuFromPause();
        }
    }

    /// <summary>
    /// Proceed Button Clicked: Resumes time and transitions back to Main Menu via SceneFader.
    /// </summary>
    public void ConfirmReturnToMainMenu()
    {
        if (isTransitioning) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        ReturnToMainMenuFromPause();
    }

    /// <summary>
    /// Cancel Button Clicked: Closes the Confirmation Panel with scale-down animation.
    /// </summary>
    public void CancelReturnToMainMenu()
    {
        if (isTransitioning) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        if (confirmMainMenuPanel != null)
        {
            SetTransitioning(true, 0.3f);
            confirmMainMenuPanel.transform.DOKill();
            confirmMainMenuPanel.transform.DOScale(Vector3.zero, 0.2f)
                .SetEase(Ease.InBack)
                .SetUpdate(true);
            DOVirtual.DelayedCall(0.21f, () =>
            {
                confirmMainMenuPanel.SetActive(false);
                confirmMainMenuPanel.transform.localScale = Vector3.one;
                SetTransitioning(false);
            }).SetUpdate(true);
        }
        else
        {
            SetTransitioning(false);
        }
    }

    /// <summary>
    /// Resumes time and smoothly transitions back to Main Menu via SceneFader.
    /// </summary>
    public void ReturnToMainMenuFromPause()
    {
        SetTransitioning(false);
        UnfreezeAndUnlockPlayer();

        if (confirmMainMenuPanel != null)
        {
            confirmMainMenuPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeToScene(mainMenuSceneName, null);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
