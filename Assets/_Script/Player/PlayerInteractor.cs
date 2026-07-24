using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Component attached to Player GameObject that detects nearby IInteractable objects
/// and triggers interaction when the configured interact key is pressed.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    public static PlayerInteractor Instance { get; private set; }

    [Header("Interaction Configuration")]
    [Tooltip("The key used to trigger interaction (e.g. KeyCode.E). Editable in Inspector.")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Tooltip("Radius around player to scan for interactable items.")]
    [SerializeField] private float interactRadius = 1.5f;

    [Tooltip("Layer mask for interactable objects. Set to Everything by default.")]
    [SerializeField] private LayerMask interactableLayer = ~0;

    [Header("Debug & State")]
    [Tooltip("If true, player cannot trigger new interactions (e.g. while Info Panel is open).")]
    [SerializeField] private bool isInteractionDisabled = false;

    public bool IsInteractionDisabled => isInteractionDisabled;
    public KeyCode InteractKey => interactKey;
    public float InteractRadius => interactRadius;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetInteractionEnabled(bool isEnabled)
    {
        isInteractionDisabled = !isEnabled;
    }

    private void Update()
    {
        if (isInteractionDisabled) return;

        if (IsInteractKeyPressed())
        {
            TryInteract();
        }
    }

    private bool IsInteractKeyPressed()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(interactKey)) return true;
#endif

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            // Support default E key or key specified by interactKey
            switch (interactKey)
            {
                case KeyCode.E:
                    if (Keyboard.current.eKey.wasPressedThisFrame) return true;
                    break;
                case KeyCode.F:
                    if (Keyboard.current.fKey.wasPressedThisFrame) return true;
                    break;
                case KeyCode.Space:
                    if (Keyboard.current.spaceKey.wasPressedThisFrame) return true;
                    break;
                case KeyCode.Return:
                    if (Keyboard.current.enterKey.wasPressedThisFrame) return true;
                    break;
            }
        }
#endif

        return false;
    }

    /// <summary>
    /// Finds the closest valid IInteractable within range and triggers interaction.
    /// </summary>
    public bool TryInteract()
    {
        IInteractable closestInteractable = GetClosestInteractable();
        if (closestInteractable != null && closestInteractable.CanInteract(gameObject))
        {
            closestInteractable.Interact(gameObject);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the closest valid IInteractable in range.
    /// </summary>
    public IInteractable GetClosestInteractable()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactableLayer);
        
        IInteractable closest = null;
        float minDistanceSqr = float.MaxValue;

        foreach (Collider2D col in colliders)
        {
            if (col == null) continue;

            // Search for IInteractable on the collider's GameObject or parents
            IInteractable interactable = col.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                interactable = col.GetComponent<IInteractable>();
            }

            if (interactable != null && interactable.CanInteract(gameObject))
            {
                float distSqr = (col.transform.position - transform.position).sqrMagnitude;
                if (distSqr < minDistanceSqr)
                {
                    minDistanceSqr = distSqr;
                    closest = interactable;
                }
            }
        }

        return closest;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize interaction radius in Unity Editor Scene view
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
