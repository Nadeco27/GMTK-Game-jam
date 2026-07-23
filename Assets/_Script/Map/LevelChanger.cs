using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Component attached to a trigger zone (Collider 2D with Is Trigger checked) 
/// that handles scene transition when the player enters.
/// </summary>
public class LevelChanger : MonoBehaviour
{
    [Header("Connection & Scene")]
    [Tooltip("The ScriptableObject representing this specific doorway/transition connection.")]
    [SerializeField] private LevelConnection connection;

    [Tooltip("The exact name of the scene to load. Make sure it is added to Build Settings.")]
    [SerializeField] private string targetSceneName;

    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";

    private bool hasTriggered;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasTriggered) return;

        if (collision.CompareTag(playerTag))
        {
            if (connection == null)
            {
                Debug.LogError($"[LevelChanger] Connection ScriptableObject is not assigned on {gameObject.name}!");
                return;
            }

            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogError($"[LevelChanger] Target Scene Name is empty on {gameObject.name}!");
                return;
            }

            hasTriggered = true;

            // Use SceneFader if available, otherwise fall back to Async scene load
            if (SceneFader.Instance != null)
            {
                SceneFader.Instance.FadeToScene(targetSceneName, connection);
            }
            else
            {
                LevelConnection.ActiveConnection = connection;
                SceneManager.LoadSceneAsync(targetSceneName);
            }
        }
    }
}
