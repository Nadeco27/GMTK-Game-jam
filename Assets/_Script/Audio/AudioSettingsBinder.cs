using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper component that binds UI Sliders (Master, Music, SFX) to AudioManager volume controls.
/// Attach this script to your Settings Panel or Canvas GameObject containing UI sliders.
/// </summary>
public class AudioSettingsBinder : MonoBehaviour
{
    [Header("UI Slider References")]
    [Tooltip("UI Slider for Master Volume (0.0 to 1.0).")]
    [SerializeField] private Slider masterSlider;

    [Tooltip("UI Slider for Music Volume (0.0 to 1.0).")]
    [SerializeField] private Slider musicSlider;

    [Tooltip("UI Slider for SFX Volume (0.0 to 1.0).")]
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        InitializeSliders();
    }

    private void OnEnable()
    {
        InitializeSliders();
        AudioManager.OnVolumeChanged += HandleVolumeChanged;
    }

    private void OnDisable()
    {
        AudioManager.OnVolumeChanged -= HandleVolumeChanged;
    }

    private void HandleVolumeChanged(string paramName, float val)
    {
        if (paramName == "MasterVolume" && masterSlider != null && !Mathf.Approximately(masterSlider.value, val))
        {
            masterSlider.value = val;
        }
        else if (paramName == "MusicVolume" && musicSlider != null && !Mathf.Approximately(musicSlider.value, val))
        {
            musicSlider.value = val;
        }
        else if (paramName == "SFXVolume" && sfxSlider != null && !Mathf.Approximately(sfxSlider.value, val))
        {
            sfxSlider.value = val;
        }
    }

    public void InitializeSliders()
    {
        if (AudioManager.Instance == null) return;

        if (masterSlider != null)
        {
            masterSlider.minValue = 0f;
            masterSlider.maxValue = 1f;
            masterSlider.value = AudioManager.Instance.GetMasterVolume();
            masterSlider.onValueChanged.RemoveAllListeners();
            masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        }

        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.value = AudioManager.Instance.GetMusicVolume();
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.value = AudioManager.Instance.GetSFXVolume();
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        }
    }

    private void OnMasterSliderChanged(float val)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(val);
        }
    }

    private void OnMusicSliderChanged(float val)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(val);
        }
    }

    private void OnSFXSliderChanged(float val)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(val);
        }
    }
}
