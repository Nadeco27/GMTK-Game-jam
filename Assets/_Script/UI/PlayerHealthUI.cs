using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Controls the UI display of Player Health / Movement Resource.
/// Supports both numerical text countdown AND vertical bottle/bar liquid fill (Bottom-to-Top).
/// Enhanced with DOTween animations for smooth fill transitions, number rolling, and low ink pulsing.
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

    [Header("Vertical Health Bar / Bottle Liquid Fill UI")]
    [Tooltip("UI Image for the vertical liquid fill (Set Image Type: Filled, Fill Method: Vertical, Fill Origin: Bottom).")]
    [SerializeField] private Image verticalHealthFillImage;

    [Tooltip("Optional Vertical UI Slider (Configured Direction: Bottom To Top).")]
    [SerializeField] private Slider verticalHealthSlider;

    [Header("DOTween Juice Settings")]
    [Tooltip("Enable DOTween animations for health UI.")]
    [SerializeField] private bool useDOTween = true;

    [Tooltip("Duration for the number rolling countdown animation.")]
    [SerializeField] private float numberTweenDuration = 0.2f;

    [Tooltip("Duration for the vertical bar fill tween animation.")]
    [SerializeField] private float fillTweenDuration = 0.25f;

    [Tooltip("Color tint when ink health is low (<= 20%).")]
    [SerializeField] private Color lowHealthColor = new Color(0.95f, 0.25f, 0.25f);

    private float displayedHealthValue = 100f;
    private float cachedMaxHealth = 100f;
    private int lastIntegerHealth = -1;
    private Color originalTextColor = Color.white;
    private Vector3 originalTextScale = Vector3.one;

    private Tween numberTween;
    private Tween fillImageTween;
    private Tween fillSliderTween;
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

        if (verticalHealthFillImage == null)
        {
            // Auto find filled image if attached directly
            Image img = GetComponent<Image>();
            if (img != null && img.type == Image.Type.Filled && img.fillMethod == Image.FillMethod.Vertical)
            {
                verticalHealthFillImage = img;
            }
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
            UpdateBarFillInstant(displayedHealthValue, cachedMaxHealth);
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
        fillImageTween?.Kill();
        fillSliderTween?.Kill();
        lowHealthPulseTween?.Kill();
        punchScaleTween?.Kill();

        if (healthTextTMP != null)
        {
            healthTextTMP.transform.DOKill();
            healthTextTMP.transform.localScale = originalTextScale;
        }

        if (verticalHealthFillImage != null)
        {
            verticalHealthFillImage.DOKill();
        }
    }

    private void UpdateHealthUI(float targetHealth, float maxHealth)
    {
        cachedMaxHealth = maxHealth;
        int targetIntHealth = Mathf.CeilToInt(targetHealth);
        float fillRatio = Mathf.Clamp01(targetHealth / maxHealth);

        // 1. Update Vertical Health Bar / Bottle Fill
        if (useDOTween)
        {
            if (verticalHealthFillImage != null)
            {
                fillImageTween?.Kill();
                fillImageTween = verticalHealthFillImage.DOFillAmount(fillRatio, fillTweenDuration).SetEase(Ease.OutQuad);
            }

            if (verticalHealthSlider != null)
            {
                fillSliderTween?.Kill();
                fillSliderTween = verticalHealthSlider.DOValue(fillRatio, fillTweenDuration).SetEase(Ease.OutQuad);
            }
        }
        else
        {
            UpdateBarFillInstant(targetHealth, maxHealth);
        }

        if (!useDOTween)
        {
            displayedHealthValue = targetHealth;
            UpdateTextDisplay(targetHealth, maxHealth);
            return;
        }

        // 2. Trigger subtle punch scale ONLY when the integer health number actually changes
        if (lastIntegerHealth != -1 && targetIntHealth != lastIntegerHealth)
        {
            TriggerSubtlePunch();
        }
        lastIntegerHealth = targetIntHealth;

        // 3. Smooth Number Rolling Countdown Tween
        numberTween?.Kill();
        numberTween = DOTween.To(() => displayedHealthValue, x =>
        {
            displayedHealthValue = x;
            UpdateTextDisplay(displayedHealthValue, cachedMaxHealth);
        }, targetHealth, numberTweenDuration).SetEase(Ease.OutQuad);

        // 4. Low Ink Color Pulse Effect (<= 20% max health)
        bool isLowHealth = targetHealth <= (maxHealth * 0.2f);
        if (isLowHealth)
        {
            if (healthTextTMP != null && (lowHealthPulseTween == null || !lowHealthPulseTween.IsActive()))
            {
                lowHealthPulseTween = healthTextTMP.DOColor(lowHealthColor, 0.4f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }
        else
        {
            if (healthTextTMP != null && lowHealthPulseTween != null && lowHealthPulseTween.IsActive())
            {
                lowHealthPulseTween.Kill();
                healthTextTMP.color = originalTextColor;
            }
        }
    }

    private void UpdateBarFillInstant(float currentHealth, float maxHealth)
    {
        float fillRatio = Mathf.Clamp01(currentHealth / maxHealth);

        if (verticalHealthFillImage != null)
        {
            verticalHealthFillImage.fillAmount = fillRatio;
        }

        if (verticalHealthSlider != null)
        {
            verticalHealthSlider.value = fillRatio;
        }
    }

    private void TriggerSubtlePunch()
    {
        if (healthTextTMP == null) return;

        healthTextTMP.transform.DOKill(true);
        healthTextTMP.transform.localScale = originalTextScale;

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
        if (healthTextTMP == null) return;

        int displayCurrent = Mathf.CeilToInt(currentHealth);
        int displayMax = Mathf.CeilToInt(maxHealth);

        string textValue = showMaxHealth 
            ? $"{prefixFormat}{displayCurrent} / {displayMax}" 
            : $"{prefixFormat}{displayCurrent}";

        healthTextTMP.text = textValue;
    }
}
