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

    public void CloseSettings()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
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
