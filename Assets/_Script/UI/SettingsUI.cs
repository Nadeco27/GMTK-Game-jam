using UnityEngine;

public class SettingsUI : MonoBehaviour
{
    [Header("UI Element References")]
    [Tooltip("The main Panel GameObject containing the settings options.")]
    [SerializeField] private GameObject SettingsPanel;
    [Tooltip("Name of the main menu scene.")]
    [SerializeField] private string SceneName;

    public void OpenSettings()
    {
        if (SettingsPanel != null)
        {
            SettingsPanel.SetActive(true);
        }
        Time.timeScale = 0f;
    }

    public void CloseSettings()
    {
        if (SettingsPanel != null)
        {
            SettingsPanel.SetActive(false);
        }
        Time.timeScale = 1f;
    }

    public void goToMainMenu()
    {
        // Load the Main Menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneName);
        Time.timeScale = 1f;
    }
}
