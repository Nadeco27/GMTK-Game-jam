using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// Main Menu controller enhanced with festive DOTween pop-up entrance animations,
/// interactive button punch feedback, smooth SceneFader transition, and animated Settings panel.
/// </summary>
public class MainMenuInteractable : MonoBehaviour
{
    [Header("Scene Transition Settings")]
    [Tooltip("The exact name of the target gameplay scene to load.")]
    [SerializeField] private string targetSceneName = "Map 1";

    [Header("Main Menu UI References (Optional for Animation)")]
    [Tooltip("Game Logo RectTransform / Transform for entrance & pulse animation.")]
    [SerializeField] private Transform logoTransform;

    [Tooltip("Start Game Button Transform.")]
    [SerializeField] private Transform startButtonTransform;

    [Tooltip("Settings Button Transform.")]
    [SerializeField] private Transform settingsButtonTransform;

    [Tooltip("Exit Game Button Transform.")]
    [SerializeField] private Transform exitButtonTransform;

    [Header("Settings Panel UI References")]
    [Tooltip("The main GameObject / Panel representing the settings menu.")]
    [SerializeField] private GameObject SettingsPanel;

    [Tooltip("Settings Title Text Transform.")]
    [SerializeField] private Transform settingsTitleTransform;

    [Tooltip("Master Volume Row Container Transform (Text + Slider).")]
    [SerializeField] private Transform masterContainerTransform;

    [Tooltip("Music Volume Row Container Transform (Text + Slider).")]
    [SerializeField] private Transform musicContainerTransform;

    [Tooltip("SFX Volume Row Container Transform (Text + Slider).")]
    [SerializeField] private Transform sfxContainerTransform;

    [Tooltip("Close Settings Button Transform.")]
    [SerializeField] private Transform closeButtonTransform;

    [Header("Animation Settings")]
    [Tooltip("Enable festive DOTween entrance & interaction pop-up animations.")]
    [SerializeField] private bool useDOTweenAnimations = true;

    private Tween logoPulseTween;
    private Tween titlePulseTween;

    private void Start()
    {
        // 1. Play SceneFader Fade In from black screen
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeIn(0.6f);
        }

        // 2. Hide Settings Panel initially and ensure scale is reset
        if (SettingsPanel != null)
        {
            SettingsPanel.transform.DOKill();
            SettingsPanel.transform.localScale = Vector3.one;
            SettingsPanel.SetActive(false);
        }

        // 3. Auto-attach UIButtonJuice hover & click animations to all buttons
        AutoAttachButtonJuice();

        // 4. Reset prop hover triggers for props in Main Menu
        MainMenuPropHover[] props = GetComponentsInChildren<MainMenuPropHover>(true);
        foreach (var prop in props)
        {
            if (prop != null) prop.ResetTrigger();
        }

        // 4. Play Festive Main Menu Entrance Animations
        if (useDOTweenAnimations)
        {
            AnimateMainMenuEntrance();
        }
    }

    private void Update()
    {
        if (IsEscapeKeyPressed())
        {
            if (SettingsPanel != null && SettingsPanel.activeSelf)
            {
                CloseSettingPanel();
            }
        }
    }

    private bool IsEscapeKeyPressed()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape)) return true;
#endif

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) return true;
#endif

        return false;
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

    private void OnDestroy()
    {
        logoPulseTween?.Kill();
        titlePulseTween?.Kill();
        if (SettingsPanel != null) SettingsPanel.transform.DOKill();
        DOTween.Kill(this);
    }

    /// <summary>
    /// Animates Main Menu Logo and Buttons popping into screen with fun elastic bounce effects.
    /// </summary>
    private void AnimateMainMenuEntrance()
    {
        // Logo Entrance & Pulsing Bounce Loop
        if (logoTransform != null)
        {
            logoTransform.localScale = Vector3.zero;
            logoTransform.DOScale(Vector3.one, 0.6f).SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    logoPulseTween = logoTransform.DOScale(1.05f, 1.2f)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine);
                });
        }

        // Start Button Elastic Pop-In
        if (startButtonTransform != null)
        {
            startButtonTransform.localScale = Vector3.zero;
            startButtonTransform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack).SetDelay(0.2f);
        }

        // Settings Button Elastic Pop-In
        if (settingsButtonTransform != null)
        {
            settingsButtonTransform.localScale = Vector3.zero;
            settingsButtonTransform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack).SetDelay(0.3f);
        }

        // Exit Button Elastic Pop-In
        if (exitButtonTransform != null)
        {
            exitButtonTransform.localScale = Vector3.zero;
            exitButtonTransform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack).SetDelay(0.4f);
        }
    }

    /// <summary>
    /// Play Scene button clicked listener. Resets game progress to start a fresh run.
    /// </summary>
    public void PlayScene()
    {
        if (startButtonTransform != null && useDOTweenAnimations)
        {
            startButtonTransform.DOPunchScale(new Vector3(-0.15f, -0.15f, 0f), 0.2f, 5, 0.5f);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        // Reset all game progress for a fresh game start
        ResetGameProgress();

        if (SceneFader.Instance != null)
        {
            if (SceneFader.Instance.IsTransitioning) return;
            SceneFader.Instance.FadeToScene(targetSceneName, null);
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    /// <summary>
    /// Resets all persistent game data (active spawn point, player health, doors, shortcuts, inventory, ink trails).
    /// </summary>
    public static void ResetGameProgress()
    {
        LevelConnection.ActiveConnection = null;

        if (PlayerHealth.Instance != null) PlayerHealth.Instance.ResetHealth();
        if (ShortcutManager.Instance != null) ShortcutManager.Instance.ResetAllShortcuts();
        if (InkTrailManager.Instance != null) InkTrailManager.Instance.ResetAllInk();
        if (Inventory.Instance != null) Inventory.Instance.Clear();
        if (ItemManager.Instance != null) ItemManager.Instance.ClearAll();

        KeyDoor.ResetOpenedDoors();
        CrossSceneDoor.ResetOpenedDoors();

        // Destroy persistent player instance so a fresh player spawns at Map 1 initial spawn point
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.ClearTrailRenderers();
            Destroy(PlayerController.Instance.gameObject);
        }

        Debug.Log("[GameReset] Full game progress (player spawn position, health 100%, doors, shortcuts, inventory, ink trails) reset for a NEW GAME.");
    }

    /// <summary>
    /// Quit Game button clicked listener.
    /// </summary>
    public void QuitGame()
    {
        if (exitButtonTransform != null && useDOTweenAnimations)
        {
            exitButtonTransform.DOPunchScale(new Vector3(-0.15f, -0.15f, 0f), 0.2f, 5, 0.5f);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        Application.Quit();
    }

    /// <summary>
    /// Opens or toggles Settings Panel with staggered festive DOTween pop-up animations.
    /// </summary>
    public void OpenSettingPanel()
    {
        if (settingsButtonTransform != null && useDOTweenAnimations)
        {
            settingsButtonTransform.DOPunchScale(new Vector3(-0.15f, -0.15f, 0f), 0.2f, 5, 0.5f);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        if (SettingsPanel != null)
        {
            // If already active, toggle close
            if (SettingsPanel.activeSelf)
            {
                CloseSettingPanel();
                return;
            }

            SettingsPanel.transform.DOKill();
            SettingsPanel.SetActive(true);

            if (useDOTweenAnimations)
            {
                // Settings Panel Elastic Pop-In
                SettingsPanel.transform.localScale = Vector3.zero;
                SettingsPanel.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack).SetUpdate(true);

                // Title Text Entrance & Pulse
                if (settingsTitleTransform != null)
                {
                    settingsTitleTransform.localScale = Vector3.zero;
                    settingsTitleTransform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetDelay(0.1f).SetUpdate(true)
                        .OnComplete(() =>
                        {
                            titlePulseTween?.Kill();
                            titlePulseTween = settingsTitleTransform.DOScale(1.06f, 0.8f)
                                .SetLoops(-1, LoopType.Yoyo)
                                .SetEase(Ease.InOutSine)
                                .SetUpdate(true);
                        });
                }

                // Staggered Pop-In for Master, Music, and SFX Containers
                if (masterContainerTransform != null)
                {
                    masterContainerTransform.localScale = Vector3.zero;
                    masterContainerTransform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetDelay(0.15f).SetUpdate(true);
                }

                if (musicContainerTransform != null)
                {
                    musicContainerTransform.localScale = Vector3.zero;
                    musicContainerTransform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetDelay(0.25f).SetUpdate(true);
                }

                if (sfxContainerTransform != null)
                {
                    sfxContainerTransform.localScale = Vector3.zero;
                    sfxContainerTransform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetDelay(0.35f).SetUpdate(true);
                }

                if (closeButtonTransform != null)
                {
                    closeButtonTransform.localScale = Vector3.zero;
                    closeButtonTransform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetDelay(0.42f).SetUpdate(true);
                }
            }
            else
            {
                SettingsPanel.transform.localScale = Vector3.one;
            }
        }
    }

    /// <summary>
    /// Closes Settings Panel with elastic scale-down animation.
    /// </summary>
    public void CloseSettingPanel()
    {
        if (closeButtonTransform != null && useDOTweenAnimations)
        {
            closeButtonTransform.DOPunchScale(new Vector3(-0.15f, -0.15f, 0f), 0.2f, 5, 0.5f);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        if (SettingsPanel != null)
        {
            if (useDOTweenAnimations)
            {
                titlePulseTween?.Kill();
                SettingsPanel.transform.DOKill();
                SettingsPanel.transform.DOScale(Vector3.zero, 0.25f)
                    .SetEase(Ease.InBack)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        SettingsPanel.SetActive(false);
                        SettingsPanel.transform.localScale = Vector3.one;
                    });
            }
            else
            {
                SettingsPanel.SetActive(false);
                SettingsPanel.transform.localScale = Vector3.one;
            }
        }
    }
}
