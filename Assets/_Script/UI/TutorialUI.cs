using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

/// <summary>
/// Controls the paginated tutorial pop-up UI.
/// Enhanced with DOTween animations for elastic pop-up opening/closing,
/// punch scale page transitions, and interactive button bounce feedback.
/// </summary>
public class TutorialUI : MonoBehaviour
{
    public static TutorialUI Instance { get; private set; }

    [Header("Tutorial Content List")]
    [Tooltip("Add, remove, or reorder tutorial pages here.")]
    [SerializeField] private List<TutorialPage> pages = new List<TutorialPage>();

    [Header("UI Element References")]
    [Tooltip("The main Panel GameObject containing the tutorial popup.")]
    [SerializeField] private GameObject tutorialPanel;

    [Tooltip("UI Image container for optional tutorial illustration (will auto-hide if page has no image).")]
    [SerializeField] private Image tutorialImage;

    [Tooltip("TextMeshPro text for tutorial instructions.")]
    [SerializeField] private TextMeshProUGUI tutorialTextTMP;

    [Tooltip("TextMeshPro text for page numbers (Format: Current / Total).")]
    [SerializeField] private TextMeshProUGUI pageIndicatorTMP;

    [Tooltip("Button used to go back to the previous page.")]
    [SerializeField] private Button previousButton;

    [Tooltip("Button used to go to next page or Exit on last page.")]
    [SerializeField] private Button nextButton;

    [Tooltip("TextMeshPro text component inside the Next Button.")]
    [SerializeField] private TextMeshProUGUI nextButtonTextTMP;

    [Header("DOTween Animation Settings")]
    [Tooltip("Enable DOTween juicy animations for the tutorial pop-up.")]
    [SerializeField] private bool useDOTween = true;

    [Tooltip("Duration of the pop-up open scale animation.")]
    [SerializeField] private float openAnimDuration = 0.35f;

    [Header("Auto Trigger Settings")]
    [Tooltip("If true, tutorial will automatically pop up when spawning in Map 1.")]
    [SerializeField] private bool autoTriggerOnMap1 = true;

    [Tooltip("Target scene name considered as Map 1 / Initial scene.")]
    [SerializeField] private string map1SceneName = "Level_1";

    [Tooltip("If true, tutorial will only auto-trigger once per game session.")]
    [SerializeField] private bool triggerOnlyOncePerSession = true;

    private int currentPageIndex = 0;
    private static bool hasTriggeredSession = false;
    private CanvasGroup panelCanvasGroup;
    private Tween activePanelTween;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (tutorialPanel != null)
        {
            panelCanvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = tutorialPanel.AddComponent<CanvasGroup>();
            }
        }

        // Auto-assign button click listeners if assigned in Inspector
        if (previousButton != null)
        {
            previousButton.onClick.RemoveAllListeners();
            previousButton.onClick.AddListener(OnPreviousButtonClicked);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

        AutoAttachButtonJuice();
    }

    private void AutoAttachButtonJuice()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            if (btn != null && btn.GetComponent<UIButtonJuice>() == null)
            {
                btn.gameObject.AddComponent<UIButtonJuice>();
            }
        }
    }

    private void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (autoTriggerOnMap1 && currentScene.Equals(map1SceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            if (!triggerOnlyOncePerSession || !hasTriggeredSession)
            {
                ShowTutorial();
            }
            else
            {
                CloseTutorialPanelSilently();
            }
        }
        else
        {
            CloseTutorialPanelSilently();
        }
    }

    private void OnDestroy()
    {
        activePanelTween?.Kill();
    }

    /// <summary>
    /// Opens the tutorial pop-up from page 1 with an elastic DOTween scale effect.
    /// </summary>
    public void ShowTutorial()
    {
        if (pages == null || pages.Count == 0)
        {
            Debug.LogWarning("[TutorialUI] Cannot show tutorial because the pages list is empty!");
            return;
        }

        hasTriggeredSession = true;
        currentPageIndex = 0;

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);

            if (useDOTween)
            {
                activePanelTween?.Kill();
                tutorialPanel.transform.localScale = Vector3.zero;
                panelCanvasGroup.alpha = 0f;

                // Juicy Elastic Pop In
                tutorialPanel.transform.DOScale(Vector3.one, openAnimDuration).SetEase(Ease.OutBack);
                panelCanvasGroup.DOFade(1f, openAnimDuration * 0.7f);
            }
            else
            {
                tutorialPanel.transform.localScale = Vector3.one;
                panelCanvasGroup.alpha = 1f;
            }
        }

        LockPlayerInput(true);
        UpdatePageDisplay(false);
    }

    /// <summary>
    /// Advances to next tutorial page, or closes tutorial if on the last page.
    /// </summary>
    public void OnNextButtonClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        AnimateButtonPunch(nextButton);

        if (currentPageIndex < pages.Count - 1)
        {
            currentPageIndex++;
            UpdatePageDisplay(true);
        }
        else
        {
            CloseTutorial();
        }
    }

    /// <summary>
    /// Goes back to previous tutorial page.
    /// </summary>
    public void OnPreviousButtonClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        AnimateButtonPunch(previousButton);

        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdatePageDisplay(true);
        }
    }

    /// <summary>
    /// Closes the tutorial pop-up with an elastic exit animation and unlocks player movement.
    /// </summary>
    public void CloseTutorial()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }
        if (tutorialPanel != null && useDOTween)
        {
            activePanelTween?.Kill();
            // Juicy Scale Down Exit
            activePanelTween = tutorialPanel.transform.DOScale(Vector3.zero, 0.25f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    tutorialPanel.SetActive(false);
                });

            panelCanvasGroup.DOFade(0f, 0.2f);
        }
        else if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        LockPlayerInput(false);
    }

    private void CloseTutorialPanelSilently()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.transform.localScale = Vector3.one;
            tutorialPanel.SetActive(false);
        }
    }

    private void UpdatePageDisplay(bool animateTransition)
    {
        if (pages == null || pages.Count == 0) return;

        TutorialPage page = pages[currentPageIndex];

        // 1. Image Display (Hide if image is null)
        if (tutorialImage != null)
        {
            if (page.pageImage != null)
            {
                tutorialImage.gameObject.SetActive(true);
                tutorialImage.sprite = page.pageImage;
            }
            else
            {
                tutorialImage.gameObject.SetActive(false);
            }
        }

        // 2. Text Description
        if (tutorialTextTMP != null)
        {
            tutorialTextTMP.text = page.pageDescription;

            if (useDOTween && animateTransition)
            {
                // Subtle punch scale pop on text when changing pages
                tutorialTextTMP.transform.DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.2f, 4, 0.5f);
            }
        }

        // 3. Page Number Indicator (Format: Current / Total)
        if (pageIndicatorTMP != null)
        {
            pageIndicatorTMP.text = $"{currentPageIndex + 1} / {pages.Count}";
        }

        // 4. Previous Button (Hide on Page 1)
        if (previousButton != null)
        {
            previousButton.gameObject.SetActive(currentPageIndex > 0);
        }

        // 5. Next / Exit Button Text
        if (nextButtonTextTMP != null)
        {
            bool isLastPage = (currentPageIndex == pages.Count - 1);
            nextButtonTextTMP.text = isLastPage ? "Exit" : "Next";
        }
    }

    private void AnimateButtonPunch(Button btn)
    {
        if (btn != null && useDOTween)
        {
            btn.transform.DOPunchScale(new Vector3(-0.1f, -0.1f, 0f), 0.15f, 4, 0.5f);
        }
    }

    private void LockPlayerInput(bool isLocked)
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.enabled = !isLocked;

            Rigidbody2D rb = PlayerController.Instance.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }
}
