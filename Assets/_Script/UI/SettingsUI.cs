using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("UI Element References")]
    [Tooltip("The main Panel GameObject containing the settings options.")]
    [SerializeField] private GameObject SettingsPanel;
    [Tooltip("Name of the main menu scene.")]
    [SerializeField] private string SceneName;

    [Header("Audio Slider References (Optional)")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    public bool IsSettingsOpen => (SettingsPanel != null ? SettingsPanel.activeInHierarchy : gameObject.activeInHierarchy);

    private void Start()
    {
        InitializeAudioSliders();
    }

    private void OnEnable()
    {
        InitializeAudioSliders();
    }

    private void InitializeAudioSliders()
    {
        if (AudioManager.Instance == null) return;

        if (masterSlider != null)
        {
            masterSlider.minValue = 0.0001f;
            masterSlider.maxValue = 1f;
            masterSlider.value = AudioManager.Instance.GetMasterVolume();
            masterSlider.onValueChanged.RemoveAllListeners();
            masterSlider.onValueChanged.AddListener((val) => AudioManager.Instance.SetMasterVolume(val));
        }

        if (musicSlider != null)
        {
            musicSlider.minValue = 0.0001f;
            musicSlider.maxValue = 1f;
            musicSlider.value = AudioManager.Instance.GetMusicVolume();
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener((val) => AudioManager.Instance.SetMusicVolume(val));
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0.0001f;
            sfxSlider.maxValue = 1f;
            sfxSlider.value = AudioManager.Instance.GetSFXVolume();
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener((val) => AudioManager.Instance.SetSFXVolume(val));
        }
    }

    private void Update()
    {
        // PauseUI handles Escape key during gameplay pause
        if (PauseUI.Instance != null && PauseUI.Instance.IsPaused) return;

        if (IsEscapeKeyPressed())
        {
            if (IsSettingsOpen)
            {
                CloseSettings();
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

    public void OpenSettings()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        if (SettingsPanel != null)
        {
            SettingsPanel.SetActive(true);
        }
        Time.timeScale = 0f;
    }

    public void EnsurePanelActive()
    {
        if (SettingsPanel != null)
        {
            SettingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        if (PauseUI.Instance != null && PauseUI.Instance.IsPaused)
        {
            PauseUI.Instance.CloseSettingsAndReturnToPause();
            return;
        }

        MainMenuInteractable mainMenu = FindFirstObjectByType<MainMenuInteractable>(FindObjectsInactive.Include);
        if (mainMenu != null && (SettingsPanel != null ? SettingsPanel.activeSelf : gameObject.activeSelf))
        {
            mainMenu.CloseSettingPanel();
            return;
        }

        if (SettingsPanel != null)
        {
            SettingsPanel.SetActive(false);
        }
        Time.timeScale = 1f;
    }

    public void goToMainMenu()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        // Load the Main Menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneName);
        Time.timeScale = 1f;
    }
}
