using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Component for Main Menu prop objects that rotates slightly when touched/hovered by mouse cursor,
/// and rotates back to original rotation when mouse leaves.
/// The direction of rotation depends on mouse approach side (Left -> Left rotation, Right -> Right rotation).
/// Ensures full rotation animation completes even on quick mouse pass-throughs, with anti-spam protection.
/// </summary>
public class MainMenuPropHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Rotation Settings")]
    [Tooltip("Rotation angle in degrees (positive for left tilt, negative for right tilt).")]
    [SerializeField] private float rotationAngle = 15f;

    [Tooltip("Duration to rotate when mouse enters prop.")]
    [SerializeField] private float rotateDuration = 0.25f;

    [Tooltip("Duration to return to original rotation when mouse leaves prop.")]
    [SerializeField] private float returnDuration = 0.35f;

    [Tooltip("Easing type for entrance rotation.")]
    [SerializeField] private Ease enterEase = Ease.OutQuad;

    [Tooltip("Easing type for returning to original rotation.")]
    [SerializeField] private Ease returnEase = Ease.OutQuad;

    private bool isAnimating = false;
    private bool isHovered = false;
    private Quaternion initialLocalRotation;
    private Tween activeTween;

    private void Awake()
    {
        initialLocalRotation = transform.localRotation;

        // Auto-ensure Image / Graphic has Raycast Target enabled for UI pointer events
        UnityEngine.UI.Graphic graphic = GetComponent<UnityEngine.UI.Graphic>();
        if (graphic != null && !graphic.raycastTarget)
        {
            graphic.raycastTarget = true;
        }
    }

    private void OnDestroy()
    {
        activeTween?.Kill();
    }

    // UI EventSystem Mouse Enter
    public void OnPointerEnter(PointerEventData eventData)
    {
        Vector2 mousePos = eventData != null ? eventData.position : GetCurrentMousePosition();
        HandleMouseEnter(mousePos);
    }

    // UI EventSystem Mouse Exit
    public void OnPointerExit(PointerEventData eventData)
    {
        HandleMouseExit();
    }

    // 2D / 3D Physics Collider Mouse Enter
    private void OnMouseEnter()
    {
        HandleMouseEnter(GetCurrentMousePosition());
    }

    // 2D / 3D Physics Collider Mouse Exit
    private void OnMouseExit()
    {
        HandleMouseExit();
    }

    /// <summary>
    /// Handles mouse enter logic. Protected by anti-spam lock so active animations finish cleanly.
    /// Determines mouse direction relative to prop screen position.
    /// </summary>
    public void HandleMouseEnter(Vector2 mouseScreenPosition)
    {
        if (isAnimating) return;

        isAnimating = true;
        isHovered = true;

        Vector2 propScreenPos = GetPropScreenPosition();

        // If mouse is to the right of prop center -> rotate right (negative Z rotation)
        // If mouse is to the left of prop center -> rotate left (positive Z rotation)
        float targetZAngle = (mouseScreenPosition.x > propScreenPos.x) ? -rotationAngle : rotationAngle;
        Vector3 targetEuler = initialLocalRotation.eulerAngles + new Vector3(0f, 0f, targetZAngle);

        activeTween?.Kill();
        activeTween = transform.DOLocalRotate(targetEuler, rotateDuration)
            .SetEase(enterEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                // Entrance rotation completed fully!
                // If mouse already left while rotating, immediately start return animation.
                if (!isHovered)
                {
                    StartReturnRotation();
                }
            });
    }

    /// <summary>
    /// Handles mouse exit logic. If entrance rotation is still playing, return animation will wait until entrance completes.
    /// </summary>
    public void HandleMouseExit()
    {
        isHovered = false;

        // Safely check if entrance animation is actively playing (using IsActive() to prevent invalid tween warnings)
        bool isEnterTweenPlaying = (activeTween != null && activeTween.IsActive() && activeTween.IsPlaying());

        // If entrance animation is not currently playing (i.e. it already completed), start return rotation immediately
        if (!isEnterTweenPlaying)
        {
            StartReturnRotation();
        }
    }

    /// <summary>
    /// Animates prop back to original transform rotation and unlocks anti-spam lock on complete.
    /// </summary>
    private void StartReturnRotation()
    {
        activeTween?.Kill();
        activeTween = transform.DOLocalRotate(initialLocalRotation.eulerAngles, returnDuration)
            .SetEase(returnEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                // Release animation lock after return completes
                isAnimating = false;
            });
    }

    /// <summary>
    /// Calculates the screen position of this prop in pixel coordinates.
    /// </summary>
    private Vector2 GetPropScreenPosition()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
        if (cam == null) cam = Camera.main;

        if (cam != null)
        {
            return RectTransformUtility.WorldToScreenPoint(cam, transform.position);
        }
        return transform.position;
    }

    /// <summary>
    /// Helper to get active mouse screen position.
    /// </summary>
    private Vector2 GetCurrentMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Pointer.current != null)
        {
            return Pointer.current.position.ReadValue();
        }
#endif
        return Input.mousePosition;
    }

    /// <summary>
    /// Resets state and cancels any active animations.
    /// </summary>
    public void ResetTrigger()
    {
        isAnimating = false;
        isHovered = false;
        activeTween?.Kill();
        transform.localRotation = initialLocalRotation;
    }
}
