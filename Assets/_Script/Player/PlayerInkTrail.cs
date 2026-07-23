using UnityEngine;

/// <summary>
/// Component attached to Player GameObject that paints persistent ink trails
/// via InkTrailManager as the player moves around.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerInkTrail : MonoBehaviour
{
    [Header("Painting Distance & Thresholds")]
    [Tooltip("Minimum distance player must move before registering a new trail point.")]
    [SerializeField] private float minDistanceBetweenPoints = 0.15f;

    [Tooltip("Minimum velocity magnitude required to paint ink.")]
    [SerializeField] private float minVelocityThreshold = 0.1f;

    private Rigidbody2D rb;
    private Vector2 lastRecordedPosition;
    private bool isPainting = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        EnsureManagerExists();
    }

    private void EnsureManagerExists()
    {
        if (InkTrailManager.Instance == null)
        {
            GameObject managerObj = new GameObject("[InkTrailManager]");
            managerObj.AddComponent<InkTrailManager>();
        }
    }

    private void Update()
    {
        if (InkTrailManager.Instance == null) return;

        Vector2 currentPos = transform.position;
        bool isMoving = rb != null && rb.linearVelocity.sqrMagnitude > (minVelocityThreshold * minVelocityThreshold);

        if (isMoving)
        {
            if (!isPainting)
            {
                // Start a new continuous line stroke
                isPainting = true;
                lastRecordedPosition = currentPos;
                InkTrailManager.Instance.StartNewStroke(currentPos);
            }
            else
            {
                // Check if player has moved far enough to add a new point
                if (Vector2.Distance(currentPos, lastRecordedPosition) >= minDistanceBetweenPoints)
                {
                    lastRecordedPosition = currentPos;
                    InkTrailManager.Instance.AddPointToCurrentStroke(currentPos);
                }
            }
        }
        else
        {
            if (isPainting)
            {
                // End the current line stroke when player stops
                isPainting = false;
                InkTrailManager.Instance.EndCurrentStroke();
            }
        }
    }

    private void OnDisable()
    {
        // Finish active stroke when scene unloads or player disables
        if (isPainting && InkTrailManager.Instance != null)
        {
            isPainting = false;
            InkTrailManager.Instance.EndCurrentStroke();
        }
    }
}
