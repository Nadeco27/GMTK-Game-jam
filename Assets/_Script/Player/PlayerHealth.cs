using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages player health / movement limit resource.
/// Decreases health as the player moves around.
/// Handles death sequence, screen fade, and respawns player at their initial birth position in Map 1.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [Header("Health & Movement Settings")]
    [Tooltip("Maximum health / movement resource.")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("Amount of health lost per 1 unit of distance walked.")]
    [SerializeField] private float healthLossPerUnit = 2f;

    [Header("Fallback Respawn Settings")]
    [Tooltip("Fallback scene name if initial birth scene was not recorded.")]
    [SerializeField] private string fallbackSceneName = "Level_1";

    // [Header("Hotbar and hotbar UI Reference")]
    // [Tooltip("Reference to the Hotbar component on the player.")]
    // [SerializeField] private Hotbar hotbar;

    [Header("Death Animation Settings")]
    [Tooltip("Delay in seconds to let the death animation finish playing before fading out.")]
    [SerializeField] private float deathAnimationDelay = 1.2f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    /// <summary>
    /// Event fired when health changes. Parameters: (currentHealth, maxHealth).
    /// </summary>
    public static event Action<float, float> OnHealthChanged;

    /// <summary>
    /// Event fired when player dies.
    /// </summary>
    public static event Action OnPlayerDied;

    private Vector3 initialBirthPosition;
    private string initialBirthSceneName;
    private bool hasRecordedInitialBirth = false;

    private Vector2 lastPosition;
    private Rigidbody2D rb;
    private Animator anim;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        CurrentHealth = maxHealth;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void Start()
    {
        // Record the player's initial birth location and scene when game starts for the first time
        if (!hasRecordedInitialBirth)
        {
            initialBirthPosition = transform.position;
            initialBirthSceneName = SceneManager.GetActiveScene().name;
            hasRecordedInitialBirth = true;
        }

        ResetLastPosition();
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset last position on scene load to prevent distance calculation jump across scenes
        ResetLastPosition();
    }

    /// <summary>
    /// Resets last position tracker to current player position.
    /// Call this whenever teleporting the player to prevent false movement distance calculations.
    /// </summary>
    public void ResetLastPosition()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (IsDead) return;

        // Ignore movement tracking during scene transitions
        if (SceneFader.Instance != null && SceneFader.Instance.IsTransitioning)
        {
            ResetLastPosition();
            return;
        }

        Vector2 currentPos = transform.position;
        float distanceMoved = Vector2.Distance(currentPos, lastPosition);

        if (distanceMoved > 0.001f)
        {
            ConsumeHealth(distanceMoved * healthLossPerUnit);
            lastPosition = currentPos;
        }
    }

    /// <summary>
    /// Reduces player health by amount.
    /// </summary>
    public void ConsumeHealth(float amount)
    {
        if (IsDead) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Refills player health to full capacity and resets animator & controls state.
    /// </summary>
    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        IsDead = false;
        ResetLastPosition();

        if (anim != null)
        {
            anim.SetBool("IsDead", false);
            anim.SetFloat("Speed", 0f);
            if (anim.HasState(0, Animator.StringToHash("Idle")))
            {
                anim.Play("Idle", 0, 0f);
            }
        }

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.enabled = true;
        }

        if (PlayerInteractor.Instance != null)
        {
            PlayerInteractor.Instance.SetInteractionEnabled(true);
        }

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void Die()
    {
        if (IsDead) return;

        IsDead = true;

        // Lock player movement and stop velocity immediately
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.enabled = false;
        }

        if (PlayerInteractor.Instance != null)
        {
            PlayerInteractor.Instance.SetInteractionEnabled(false);
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Trigger Death Animation and zero out movement speed parameter
        if (anim != null)
        {
            anim.SetFloat("Speed", 0f);
            anim.SetBool("IsDead", true);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ink_dead");
        }

        OnPlayerDied?.Invoke();

        StartCoroutine(DeathAndRespawnRoutine());
    }

    private IEnumerator DeathAndRespawnRoutine()
    {
        // 1. Wait for death animation to finish playing completely before starting screen fade
        yield return new WaitForSeconds(deathAnimationDelay);

        // 2. Advance the run counter and purge trails older than 2 runs
        if (InkTrailManager.Instance != null)
        {
            InkTrailManager.Instance.AdvanceToNextRun();
        }

        // 3. Reset level connection so door spawn points are not triggered on respawn
        LevelConnection.ActiveConnection = null;

        // 4. Clear Inventory and Hotbar items on death
        if (Inventory.Instance != null) Inventory.Instance.Clear();
        if (Hotbar.Instance != null) Hotbar.Instance.Clear();

        string targetScene = string.IsNullOrEmpty(initialBirthSceneName) ? fallbackSceneName : initialBirthSceneName;

        // 5. Fade screen out and load initial birth scene
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeToScene(targetScene, null);
            // Wait until scene transition finishes
            yield return new WaitUntil(() => !SceneFader.Instance.IsTransitioning);
        }
        else
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }

        // 6. Teleport player back to the exact initial birth position
        transform.position = initialBirthPosition;

        // 7. Clear TrailRenderer components to prevent purple teleport streaks across screen
        TrailRenderer[] trailRenderers = GetComponentsInChildren<TrailRenderer>(true);
        foreach (TrailRenderer tr in trailRenderers)
        {
            tr.Clear();
        }

        // 8. Reset player state & position for new run
        ResetHealth();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ink_live");
        }
    }
}
