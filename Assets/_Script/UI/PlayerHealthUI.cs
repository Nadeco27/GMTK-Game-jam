using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Controls the UI display of Player Health / Movement Resource as numbers.
/// Enhanced with DOTween animations for smooth rolling numbers, punch scale feedback,
/// and low ink pulsing effects.
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

    [Header("DOTween Juice Settings")]
    [Tooltip("Enable DOTween animations for health UI.")]
    [SerializeField] private bool useDOTween = true;

    [Tooltip("Duration for the number rolling countdown animation.")]
    [SerializeField] private float numberTweenDuration = 0.2f;

    [Tooltip("Color tint when ink health is low (<= 20%).")]
    [SerializeField] private Color lowHealthColor = new Color(0.95f, 0.25f, 0.25f);

    private float displayedHealthValue = 100f;
    private float cachedMaxHealth = 100f;
    private int lastIntegerHealth = -1;
    private Color originalTextColor = Color.white;
    private Vector3 originalTextScale = Vector3.one;

    private Tween numberTween;
    private Tween lowHealthPulseTween;
    private Tween punchScaleTween;

    private void Awake()
    {
        if (healthTextTMP == null)
        {
            healthTextTMP = GetComponent<TextMeshProUGUI>();
        }

        if (healthTextTMP != null)
        {
            originalTextColor = healthTextTMP.color;
            originalTextScale = healthTextTMP.transform.localScale;
        }
    }

    private void OnEnable()
    {
        PlayerHealth.OnHealthChanged += UpdateHealthUI;
        
        if (PlayerHealth.Instance != null)
        {
            displayedHealthValue = PlayerHealth.Instance.CurrentHealth;
            cachedMaxHealth = PlayerHealth.Instance.MaxHealth;
            lastIntegerHealth = Mathf.CeilToInt(displayedHealthValue);
            UpdateTextDisplay(displayedHealthValue, cachedMaxHealth);
        }
    }

    private void OnDisable()
    {
        PlayerHealth.OnHealthChanged -= UpdateHealthUI;
        KillTweens();
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    private void KillTweens()
    {
        numberTween?.Kill();
        lowHealthPulseTween?.Kill();
        punchScaleTween?.Kill();

        if (healthTextTMP != null)
        {
            healthTextTMP.transform.DOKill();
            healthTextTMP.transform.localScale = originalTextScale;
        }
    }

    private void UpdateHealthUI(float targetHealth, float maxHealth)
    {
        cachedMaxHealth = maxHealth;
        int targetIntHealth = Mathf.CeilToInt(targetHealth);

        if (!useDOTween)
        {
            displayedHealthValue = targetHealth;
            UpdateTextDisplay(targetHealth, maxHealth);
            return;
        }

        // 1. Trigger subtle punch scale ONLY when the integer health number actually changes
        if (lastIntegerHealth != -1 && targetIntHealth != lastIntegerHealth)
        {
            TriggerSubtlePunch();
        }
        lastIntegerHealth = targetIntHealth;

        // 2. Smooth Number Rolling Countdown Tween
        numberTween?.Kill();
        numberTween = DOTween.To(() => displayedHealthValue, x =>
        {
            displayedHealthValue = x;
            UpdateTextDisplay(displayedHealthValue, cachedMaxHealth);
        }, targetHealth, numberTweenDuration).SetEase(Ease.OutQuad);

        // 3. Low Ink Color Pulse Effect (<= 20% max health)
        bool isLowHealth = targetHealth <= (maxHealth * 0.2f);
        if (isLowHealth)
        {
            if (lowHealthPulseTween == null || !lowHealthPulseTween.IsActive())
            {
                lowHealthPulseTween = healthTextTMP.DOColor(lowHealthColor, 0.4f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }
        else
        {
            if (lowHealthPulseTween != null && lowHealthPulseTween.IsActive())
            {
                lowHealthPulseTween.Kill();
                healthTextTMP.color = originalTextColor;
            }
        }
    }

    private void TriggerSubtlePunch()
    {
        if (healthTextTMP == null) return;

        // Reset scale and kill active scale tweens to prevent scale compounding inflation
        healthTextTMP.transform.DOKill(true);
        healthTextTMP.transform.localScale = originalTextScale;

        // Subtle punch scale pop (8% scale increase)
        punchScaleTween = healthTextTMP.transform.DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.15f, 4, 0.5f)
            .OnComplete(() =>
            {
                if (healthTextTMP != null)
                {
                    healthTextTMP.transform.localScale = originalTextScale;
                }
            });
    }

    private void UpdateTextDisplay(float currentHealth, float maxHealth)
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
