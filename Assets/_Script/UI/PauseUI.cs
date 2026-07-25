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

        this.pausePanel = source.pausePanel;
        this.darkBackgroundOverlay = source.darkBackgroundOverlay;
        this.pauseTitleTransform = source.pauseTitleTransform;
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

        // 2. Listen for Escape Key
        if (IsEscapeKeyPressed())
        {
            // Do not trigger pause if InfoPanelUI is open (let InfoPanel close first)
            if (InfoPanelUI.Instance != null && InfoPanelUI.Instance.IsPanelActive)
            {
                return;
            }

            // If Settings Panel is open while paused, close Settings and re-open Pause UI
            if (IsPaused && settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettingsAndReturnToPause();
                return;
            }

            // Toggle Pause
            if (IsPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
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
    /// </summary>
    public void PauseGame()
    {
        if (IsPaused) return;

        RebindReferencesIfMissing();

        IsPaused = true;
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

            if (useDOTweenAnimations)
            {
                activeEntranceTween?.Kill();

                pausePanel.transform.localScale = Vector3.zero;
                if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;

                // Elastic Pop-In Entrance (SetUpdate(true) ignores timeScale = 0)
                pausePanel.transform.DOScale(Vector3.one, popAnimDuration).SetEase(Ease.OutBack).SetUpdate(true);
                if (panelCanvasGroup != null) panelCanvasGroup.DOFade(1f, popAnimDuration * 0.6f).SetUpdate(true);

                // Title Text Pulse
                if (pauseTitleTransform != null)
                {
                    pauseTitleTransform.localScale = Vector3.zero;
                    pauseTitleTransform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetDelay(0.1f).SetUpdate(true);
                }

                // Staggered Pop-In for Buttons
                if (resumeButton != null)
                {
                    resumeButton.transform.localScale = Vector3.zero;
                    resumeButton.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetDelay(0.15f).SetUpdate(true);
                }

                if (settingsButton != null)
                {
                    settingsButton.transform.localScale = Vector3.zero;
                    settingsButton.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetDelay(0.25f).SetUpdate(true);
                }

                if (mainMenuButton != null)
                {
                    mainMenuButton.transform.localScale = Vector3.zero;
                    mainMenuButton.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetDelay(0.35f).SetUpdate(true);
                }
            }
            else
            {
                pausePanel.transform.localScale = Vector3.one;
                if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;
            }
        }
    }

    /// <summary>
    /// Resumes the game, unfreezes time, and re-enables player controls.
    /// </summary>
    public void ResumeGame()
    {
        if (!IsPaused) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        if (pausePanel != null && useDOTweenAnimations)
        {
            activeEntranceTween?.Kill();
            pausePanel.transform.DOKill();
            pausePanel.transform.DOScale(Vector3.zero, 0.22f)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    pausePanel.SetActive(false);
                    pausePanel.transform.localScale = Vector3.one;
                    UnfreezeAndUnlockPlayer();
                });
        }
        else
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
                pausePanel.transform.localScale = Vector3.one;
            }
            UnfreezeAndUnlockPlayer();
        }
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
    /// Closes Pause Panel and opens Settings Panel.
    /// <summary>
    /// Opens Settings Panel without disabling Pause Panel.
    /// </summary>
    public void OpenSettingsFromPause()
    {
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
            settingsPanel.transform.DOKill();
            settingsPanel.SetActive(true);
            settingsPanel.transform.localScale = Vector3.zero;
            settingsPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);
        }
        else
        {
            Debug.LogWarning("[PauseUI] Settings Panel reference not assigned or found in scene.");
        }
    }

    public void CloseSettingsAndReturnToPause()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        if (settingsPanel != null)
        {
            settingsPanel.transform.DOKill();
            settingsPanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).SetUpdate(true)
                .OnComplete(() =>
                {
                    settingsPanel.SetActive(false);
                    settingsPanel.transform.localScale = Vector3.one;
                });
        }
    }

    /// <summary>
    /// Opens the Main Menu Confirmation Panel when Main Menu button is clicked.
    /// </summary>
    public void OpenMainMenuConfirmation()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        if (confirmMainMenuPanel != null)
        {
            confirmMainMenuPanel.transform.DOKill();
            confirmMainMenuPanel.SetActive(true);
            confirmMainMenuPanel.transform.localScale = Vector3.zero;
            confirmMainMenuPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);
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
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        if (confirmMainMenuPanel != null)
        {
            confirmMainMenuPanel.transform.DOKill();
            confirmMainMenuPanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).SetUpdate(true)
                .OnComplete(() =>
                {
                    confirmMainMenuPanel.SetActive(false);
                    confirmMainMenuPanel.transform.localScale = Vector3.one;
                });
        }
    }

    /// <summary>
    /// Resumes time and smoothly transitions back to Main Menu via SceneFader.
    /// </summary>
    public void ReturnToMainMenuFromPause()
    {
        UnfreezeAndUnlockPlayer();

        if (confirmMainMenuPanel != null)
        {
            confirmMainMenuPanel.SetActive(false);
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
