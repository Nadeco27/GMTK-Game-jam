using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
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

    [Header("Scene Gating")]
    [Tooltip("Scenes where the player should be deactivated (main menu, settings, etc).")]
    [SerializeField] private List<string> nonGameplayScenes = new List<string> { "Index" };

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isFacingRight = true;

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

        SceneManager.sceneLoaded += OnSceneLoaded; // subscribe once, here
        UpdateActiveStateForScene(SceneManager.GetActiveScene().name);
    }

    //  private void OnEnable()
    // {
    //     SceneManager.sceneLoaded += OnSceneLoaded;
    // }

    // private void OnDisable()
    // {
    //     SceneManager.sceneLoaded -= OnSceneLoaded;
    // }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // only unsubscribe on real destruction
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only react to the active scene changing, not additive UI scene loads
        if (scene == SceneManager.GetActiveScene())
        {
            UpdateActiveStateForScene(scene.name);
        }
    }

    private void UpdateActiveStateForScene(string sceneName)
    {
        bool disable = nonGameplayScenes.Contains(sceneName);
        gameObject.SetActive(!disable);
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
}
