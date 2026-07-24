using UnityEngine;

/// <summary>
/// Adds a gentle sway (rotating slightly back and forth on Z-axis) to interactable items.
/// Works automatically with FloatingInteractionPrompt (the pop-up E prompt stays upright).
/// </summary>
public class ItemSway : MonoBehaviour
{
    [Header("Sway Configuration")]
    [Tooltip("Maximum rotation angle in degrees (e.g. 10 to 15 degrees).")]
    [SerializeField] private float maxSwayAngle = 12f;

    [Tooltip("Speed of the sway movement.")]
    [SerializeField] private float swaySpeed = 3f;

    [Tooltip("Optional target transform to sway. If empty, this GameObject's transform will sway.")]
    [SerializeField] private Transform targetTransform;

    private float randomPhaseOffset;

    private void Awake()
    {
        if (targetTransform == null)
        {
            targetTransform = transform;
        }

        // Random offset so multiple items in the scene don't sway in sync
        randomPhaseOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (targetTransform == null) return;

        // Calculate smooth Z-axis rotation angle using sine wave
        float zAngle = Mathf.Sin((Time.time + randomPhaseOffset) * swaySpeed) * maxSwayAngle;
        targetTransform.localRotation = Quaternion.Euler(0f, 0f, zAngle);
    }
}
