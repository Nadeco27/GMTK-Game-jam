using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the UI display of Player Health / Movement Resource as numbers.
/// Supports TextMeshProUGUI and Legacy UI Text.
/// Place this script on a UI GameObject in the separate UIScene.
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI Text Component")]
    [Tooltip("TextMeshPro UI Text component (Drag your TextMeshPro object here).")]
    [SerializeField] private TextMeshProUGUI healthTextTMP;

    [Header("Format Settings")]
    [Tooltip("Prefix format shown before the health number (e.g. 'Darah: ' or 'Tinta: ').")]
    [SerializeField] private string prefixFormat = "Darah: ";

    [Tooltip("If true, shows 'Current / Max' format (e.g. '100 / 100'). If false, shows '100'.")]
    [SerializeField] private bool showMaxHealth = false;

    private void Awake()
    {
        // Auto-detect TextMeshProUGUI component on the same GameObject if not assigned in Inspector
        if (healthTextTMP == null)
        {
            healthTextTMP = GetComponent<TextMeshProUGUI>();
        }
    }

    private void OnEnable()
    {
        PlayerHealth.OnHealthChanged += UpdateHealthUI;
        
        // Initial sync if player health already exists
        if (PlayerHealth.Instance != null)
        {
            UpdateHealthUI(PlayerHealth.Instance.CurrentHealth, PlayerHealth.Instance.MaxHealth);
        }
    }

    private void OnDisable()
    {
        PlayerHealth.OnHealthChanged -= UpdateHealthUI;
    }

    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        int displayCurrent = Mathf.CeilToInt(currentHealth);
        int displayMax = Mathf.CeilToInt(maxHealth);

        string textValue = showMaxHealth 
            ? $"{prefixFormat}{displayCurrent} / {displayMax}" 
            : $"{prefixFormat}{displayCurrent}";

        if (healthTextTMP != null)
        {
            healthTextTMP.text = textValue;
        }
    }
}
