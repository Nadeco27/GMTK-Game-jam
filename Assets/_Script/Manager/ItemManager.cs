using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    private readonly HashSet<string> collectedKeys = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        PlayerHealth.OnPlayerDied += ClearAll;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            PlayerHealth.OnPlayerDied -= ClearAll;
            Instance = null;
        }
    }

    public bool IsCollected(string key) => collectedKeys.Contains(key);
    public void MarkCollected(string key) => collectedKeys.Add(key);

    public void ClearAll()
    {
        collectedKeys.Clear();
        Debug.Log("[ItemManager] Cleared collected items on player death or new game.");
    }
}