using UnityEngine;

/// <summary>
/// Component placed in scenes to mark spawn positions.
/// Moves the player to this position if this spawn point matches the ActiveConnection.
/// </summary>
public class LevelSpawnPoint : MonoBehaviour
{
    [Header("Connection")]
    [Tooltip("The ScriptableObject representing this spawn point's connection.")]
    [SerializeField] private LevelConnection connection;

    [Header("Default Spawn Point")]
    [Tooltip("Check this if this is the default initial spawn point for a new game or when ActiveConnection is null.")]
    [SerializeField] private bool isDefaultSpawnPoint = false;

    [Header("Player Identification")]
    [SerializeField] private string playerTag = "Player";

    private void Start()
    {
        bool shouldSpawnHere = false;

        if (LevelConnection.ActiveConnection != null)
        {
            shouldSpawnHere = (connection != null && connection == LevelConnection.ActiveConnection);
        }
        else
        {
            // If ActiveConnection is null (new game start), use default spawn point or first unassigned spawn point
            shouldSpawnHere = isDefaultSpawnPoint || (connection == null);
        }

        if (shouldSpawnHere)
        {
            GameObject player = GameObject.FindWithTag(playerTag);
            if (player == null && PlayerController.Instance != null)
            {
                player = PlayerController.Instance.gameObject;
            }

            if (player != null)
            {
                player.transform.position = transform.position;

                // Clear TrailRenderer components to prevent purple teleport streaks across screen
                TrailRenderer[] trailRenderers = player.GetComponentsInChildren<TrailRenderer>(true);
                foreach (TrailRenderer tr in trailRenderers)
                {
                    tr.Clear();
                }

                // Reset player health position tracker so scene teleportation doesn't count as walking distance
                if (PlayerHealth.Instance != null)
                {
                    PlayerHealth.Instance.ResetLastPosition();
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Draw gizmo icon/visualization in Unity Editor view
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawRay(transform.position, Vector3.up * 1f);
    }
}
