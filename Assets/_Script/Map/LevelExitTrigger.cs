using UnityEngine;

/// <summary>
/// Component attached to an Exit trigger object (Collider 2D with Is Trigger checked).
/// When player enters, triggers LevelExitUI from UIScene to show the exit panel and lock movement/interaction.
/// </summary>
public class LevelExitTrigger : MonoBehaviour
{
    [Header("Scene Transition Settings")]
    [Tooltip("Target scene name for main menu (Make sure it is added to Build Settings).")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";

    private bool isTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTriggered) return;

        if (collision.CompareTag(playerTag) || collision.GetComponent<PlayerController>() != null)
        {
            isTriggered = true;
            LevelExitUI.ShowExit(mainMenuSceneName);
        }
    }
}
