using UnityEngine;

/// <summary>
/// Component attached to an E-key prompt icon in the scene.
/// Automatically handles showing/hiding the prompt based on player proximity,
/// locks X-axis left/right movement from parent sway, and animates vertical bobbing.
/// </summary>
public class FloatingInteractionPrompt : MonoBehaviour
{
    [Header("Prompt Visual Reference")]
    [Tooltip("The GameObject or SpriteRenderer of the E-key prompt icon. Defaults to child visual or SpriteRenderer if unassigned.")]
    [SerializeField] private GameObject promptVisual;

    [Header("Floating Animation Settings")]
    [Tooltip("Speed of the vertical floating oscillation.")]
    [SerializeField] private float floatSpeed = 3f;

    [Tooltip("Vertical height offset amplitude for floating.")]
    [SerializeField] private float floatAmplitude = 0.12f;

    private Vector3 initialWorldOffset;
    private IInteractable targetInteractable;
    private SpriteRenderer promptSpriteRenderer;
    private float currentYBobbingOffset = 0f;

    private void Awake()
    {
        // Search for IInteractable on this object or parent
        targetInteractable = GetComponentInParent<IInteractable>();
        if (targetInteractable == null)
        {
            targetInteractable = GetComponent<IInteractable>();
        }

        // If promptVisual is not set, try to use child visual or this gameObject
        if (promptVisual == null)
        {
            if (transform.childCount > 0)
            {
                promptVisual = transform.GetChild(0).gameObject;
            }
            else
            {
                promptVisual = gameObject;
            }
        }

        promptSpriteRenderer = promptVisual.GetComponent<SpriteRenderer>();

        // Record initial world offset relative to parent so editor placement is preserved 1:1
        if (transform.parent != null)
        {
            initialWorldOffset = transform.position - transform.parent.position;
        }
        else
        {
            initialWorldOffset = transform.position;
        }

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

    private void LateUpdate()
    {
        // 1. Lock World Rotation so prompt stays upright
        transform.rotation = Quaternion.identity;

        // 2. Maintain exact initial position offset set in Editor, avoiding sway rotation & scale displacement
        if (transform.parent != null)
        {
            transform.position = transform.parent.position + initialWorldOffset + new Vector3(0f, currentYBobbingOffset, 0f);
        }
        else
        {
            transform.position = initialWorldOffset + new Vector3(0f, currentYBobbingOffset, 0f);
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

        // 3. Check if target interactable is in player range and is closest
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

        // Proximity fallback check
        if (PlayerInteractor.Instance != null)
        {
            float distSqr = (transform.position - PlayerInteractor.Instance.transform.position).sqrMagnitude;
            float radius = PlayerInteractor.Instance.InteractRadius;
            return distSqr <= radius * radius;
        }

        return false;
    }

    private void AnimateFloating()
    {
        currentYBobbingOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptVisual != null)
        {
            if (promptVisual == gameObject)
            {
                // If promptVisual is the same GameObject, toggle SpriteRenderer to avoid disabling this script
                if (promptSpriteRenderer != null)
                {
                    promptSpriteRenderer.enabled = visible;
                }
            }
            else
            {
                if (promptVisual.activeSelf != visible)
                {
                    promptVisual.SetActive(visible);
                }
            }
        }
    }
}
