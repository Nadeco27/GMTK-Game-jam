using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Simple 2D WASD Player Controller supporting both Unity New Input System and Legacy Input Manager.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("Persistence")]
    [Tooltip("If true, this player object will not be destroyed when switching scenes.")]
    [SerializeField] private bool isPersistent = true;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isFacingRight = true;

    private Animator anim;

    private void Awake()
    {
        if (isPersistent)
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Instance = this;
        }

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        anim = GetComponent<Animator>();
        // Auto-ensure PlayerInteractor and Inventory components exist on player
        if (GetComponent<PlayerInteractor>() == null)
        {
            gameObject.AddComponent<PlayerInteractor>();
        }

        if (GetComponent<Inventory>() == null)
        {
            gameObject.AddComponent<Inventory>();
        }
    }

    private void Update()
    {
        Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (input == Vector2.zero)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
        }
#endif

        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        moveInput = input;

        if (anim != null)
        {
            anim.SetFloat("Speed", moveInput.sqrMagnitude);
        }

        // Flip Sprite based on horizontal direction
        if (moveInput.x > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (moveInput.x < 0 && isFacingRight)
        {
            Flip();
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    /// <summary>
    /// Instantly clears all active trail renderers on the player to prevent magenta teleport streaks across screen.
    /// </summary>
    public void ClearTrailRenderers()
    {
        TrailRenderer[] trailRenderers = GetComponentsInChildren<TrailRenderer>(true);
        foreach (TrailRenderer tr in trailRenderers)
        {
            tr.Clear();
        }
    }
}
