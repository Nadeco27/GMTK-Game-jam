using UnityEngine;

/// <summary>
/// Component attached to an E-key prompt icon (or item object) in the scene.
/// Automatically handles showing/hiding the prompt based on player proximity and InfoPanelUI state,
/// and applies a smooth floating (up and down) animation.
/// </summary>
public class FloatingInteractionPrompt : MonoBehaviour
{
    [Header("Prompt Visual Reference")]
    [Tooltip("The GameObject or SpriteRenderer of the E-key prompt icon. Defaults to this GameObject if unassigned.")]
    [SerializeField] private GameObject promptVisual;

    [Header("Floating Animation Settings")]
    [Tooltip("Speed of the vertical floating oscillation.")]
    [SerializeField] private float floatSpeed = 3f;

    [Tooltip("Vertical height offset amplitude for floating.")]
    [SerializeField] private float floatAmplitude = 0.12f;

    private Vector3 initialLocalPosition;
    private IInteractable targetInteractable;

    private void Awake()
    {
        if (promptVisual == null)
        {
            promptVisual = gameObject;
        }

        // Search for IInteractable on this object or parent
        targetInteractable = GetComponentInParent<IInteractable>();
        if (targetInteractable == null)
        {
            targetInteractable = GetComponent<IInteractable>();
        }

        initialLocalPosition = promptVisual.transform.localPosition;

        // Hide prompt initially
        SetPromptVisible(false);
    }

    private void OnEnable()
    {
        SetPromptVisible(false);
    }

    private void Update()
    {
        bool shouldShow = ShouldShowPrompt();

        if (shouldShow)
        {
            SetPromptVisible(true);
            AnimateFloating();
        }
        else
        {
            SetPromptVisible(false);
        }
    }

    private bool ShouldShowPrompt()
    {
        // 1. Hide if PlayerInteractor is missing or interactions are disabled (e.g. while InfoPanelUI is open)
        if (PlayerInteractor.Instance == null || PlayerInteractor.Instance.IsInteractionDisabled)
        {
            return false;
        }

        // 2. Hide if InfoPanelUI is currently active
        if (InfoPanelUI.Instance != null && InfoPanelUI.Instance.IsPanelActive)
        {
            return false;
        }

        // 3. Check if target interactable is in player range and is the closest interactable
        if (targetInteractable != null)
        {
            if (!targetInteractable.CanInteract(PlayerInteractor.Instance.gameObject))
            {
                return false;
            }

            IInteractable closest = PlayerInteractor.Instance.GetClosestInteractable();
            if (closest != null && (closest == targetInteractable || closest.Equals(targetInteractable)))
            {
                return true;
            }
        }
        else
        {
            // Proximity fallback check if prompt is placed standalone in scene
            float distSqr = (transform.position - PlayerInteractor.Instance.transform.position).sqrMagnitude;
            float radius = PlayerInteractor.Instance.InteractRadius;
            if (distSqr <= radius * radius)
            {
                return true;
            }
        }

        return false;
    }

    private void AnimateFloating()
    {
        if (promptVisual == null) return;
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        promptVisual.transform.localPosition = initialLocalPosition + new Vector3(0f, yOffset, 0f);
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptVisual != null && promptVisual.activeSelf != visible)
        {
            promptVisual.SetActive(visible);
        }
    }
}
