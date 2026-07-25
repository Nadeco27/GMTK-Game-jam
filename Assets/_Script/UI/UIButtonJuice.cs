using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// Attach to any UI Button to add smooth DOTween hover scale-up and click punch-scale animations.
/// Automatically handles pointer enter, pointer exit, and pointer click events.
/// Ignores Time.timeScale so animations play smoothly during game pause.
/// </summary>
public class UIButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Hover Animation Settings")]
    [Tooltip("Scale multiplier when mouse hovers over the button (e.g. 1.08 = 8% scale up).")]
    [SerializeField] private float hoverScaleMultiplier = 1.08f;

    [Tooltip("Duration of hover scale transition in seconds.")]
    [SerializeField] private float hoverDuration = 0.15f;

    [Header("Click Animation Settings")]
    [Tooltip("Punch scale intensity when the button is clicked.")]
    [SerializeField] private Vector3 clickPunchScale = new Vector3(-0.12f, -0.12f, 0f);

    [Tooltip("Duration of click punch animation in seconds.")]
    [SerializeField] private float clickDuration = 0.18f;

    [Header("SFX Settings")]
    [Tooltip("If true, automatically plays 'button_click' SFX when clicked.")]
    [SerializeField] private bool playClickSFX = false;

    private Vector3 baseScale = Vector3.one;
    private Tween hoverTween;
    private Tween clickTween;
    private bool isHovered = false;

    private void Awake()
    {
        baseScale = transform.localScale != Vector3.zero ? transform.localScale : Vector3.one;
    }

    private void OnEnable()
    {
        // Reset scale on enable
        if (baseScale == Vector3.zero) baseScale = Vector3.one;
        transform.localScale = baseScale;
        isHovered = false;
    }

    private void OnDisable()
    {
        KillTweens();
        transform.localScale = baseScale;
        isHovered = false;
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    private void KillTweens()
    {
        hoverTween?.Kill();
        clickTween?.Kill();
        transform.DOKill();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Selectable selectable = GetComponent<Selectable>();
        if (selectable != null && !selectable.interactable) return;

        isHovered = true;
        KillTweens();

        hoverTween = transform.DOScale(baseScale * hoverScaleMultiplier, hoverDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        KillTweens();

        hoverTween = transform.DOScale(baseScale, hoverDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Selectable selectable = GetComponent<Selectable>();
        if (selectable != null && !selectable.interactable) return;

        if (playClickSFX && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        clickTween?.Kill();
        transform.localScale = isHovered ? baseScale * hoverScaleMultiplier : baseScale;
        clickTween = transform.DOPunchScale(clickPunchScale, clickDuration, 5, 0.5f).SetUpdate(true);
    }

    /// <summary>
    /// Updates the base scale reference if the button was resized or animated by entrance effects.
    /// </summary>
    public void SetBaseScale(Vector3 newBaseScale)
    {
        baseScale = newBaseScale;
    }
}
