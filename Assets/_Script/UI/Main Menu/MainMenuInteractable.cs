using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuInteractable : MonoBehaviour
{
    [Tooltip("The exact name of the scene to load. Make sure it is added to Build Settings.")]
    [SerializeField] private string targetSceneName;
    [Tooltip("The GameObject representing the settings panel in the UI.")]
    [SerializeField] private GameObject SettingsPanel;

    public void PlayScene()
    {
        SceneManager.LoadScene(targetSceneName);
    }
    public void QuitGame()
    {
        Application.Quit();
    }
    public void OpenSettingPanel()
    {
        SettingsPanel.SetActive(!SettingsPanel.activeSelf);
    }
    public void CloseSettingPanel()
    {
        SettingsPanel.SetActive(false);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
