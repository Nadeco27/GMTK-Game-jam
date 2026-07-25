using System.Collections.Generic;
using UnityEngine;

public class CrossSceneDoor : MonoBehaviour
{
    private static readonly HashSet<string> openedDoorIDs = new HashSet<string>();

    [System.Serializable]
    public struct ButtonRequirement
    {
        [Tooltip("Nama Scene (ex: Map 8)")]
        public string sceneName;
        [Tooltip("Koordinat X dan Y posisi button di dalam scene tersebut")]
        public Vector2 buttonPosition;
        [Tooltip("Radius deteksi tinta (samakan dengan yang ada di InkButtonVisual)")]
        public float checkRadius;
    }

    [Header("Syarat Puzzle Lintas Scene")]
    public List<ButtonRequirement> requiredButtons;

    [Header("Door Components")]
    public Collider2D doorCollider;
    public SpriteRenderer doorRenderer;
    public Animator doorAnimator; // Optional (jika pintu tidak pakai animasi, bisa dikosongkan)
    [Tooltip("Jika di-centang, GameObject pintu akan di-disable saat terbuka")]
    public bool disableGameObjectOnOpen = false;

    [Header("Notification Settings")]
    [Tooltip("Pesan notifikasi yang tampil saat player bertabrakan dengan pintu yang belum terbuka.")]
    [SerializeField] private string lockedNotificationMessage = "It won't budge.";
    
    private bool isOpen = false;
    private float nextNotificationTime = 0f;

    private string GetDoorID()
    {
        return $"{gameObject.scene.name}_{gameObject.name}_{transform.position.x:F2}_{transform.position.y:F2}";
    }

    private void Awake()
    {
        if (doorCollider == null) doorCollider = GetComponent<Collider2D>();
        if (doorRenderer == null) doorRenderer = GetComponent<SpriteRenderer>();
        if (doorAnimator == null) doorAnimator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (openedDoorIDs.Contains(GetDoorID()))
        {
            OpenDoor(isInitialLoad: true);
        }
    }

    /// <summary>
    /// Resets all opened cross-scene doors. Called when starting a new game from Main Menu.
    /// </summary>
    public static void ResetOpenedDoors()
    {
        openedDoorIDs.Clear();
        Debug.Log("[CrossSceneDoor] Reset all opened cross-scene doors for a new game.");
    }

    private void Update()
    {
        if (isOpen) return; // If already unlocked persistently, stay open

        if (InkTrailManager.Instance == null) return;

        bool allButtonsPressed = true;

        // Mengecek semua daftar button yang diperlukan
        foreach (var req in requiredButtons)
        {
            if (!InkTrailManager.Instance.CheckInkNearPosition(req.sceneName, req.buttonPosition, req.checkRadius))
            {
                allButtonsPressed = false; // Jika ada 1 yang tidak kena tinta, syarat gagal
                break;
            }
        }

        // Eksekusi Pintu
        if (allButtonsPressed && !isOpen)
        {
            OpenDoor(isInitialLoad: false);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandlePlayerCollision(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        HandlePlayerCollision(collider.gameObject);
    }

    private void HandlePlayerCollision(GameObject targetObj)
    {
        if (isOpen) return;

        if (targetObj.CompareTag("Player") || targetObj.GetComponent<PlayerController>() != null)
        {
            if (Time.time >= nextNotificationTime)
            {
                nextNotificationTime = Time.time + 2.0f;
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX("fail_buzz");
                }
                if (!string.IsNullOrEmpty(lockedNotificationMessage))
                {
                    NotificationUI.ShowNotification(lockedNotificationMessage);
                }
            }
        }
    }

    private void OpenDoor(bool isInitialLoad = false)
    {
        isOpen = true;
        openedDoorIDs.Add(GetDoorID());

        if (!isInitialLoad)
        {
            StartCoroutine(PlayDoorOpenSFXWithDelay(0.3f));
        }

        if (doorCollider != null) doorCollider.enabled = false;
        if (doorRenderer != null) doorRenderer.enabled = false;
        
        if (doorAnimator != null) 
        {
            doorAnimator.SetBool("IsOpen", true);
        }

        if (disableGameObjectOnOpen)
        {
            gameObject.SetActive(false);
        }
    }

    private System.Collections.IEnumerator PlayDoorOpenSFXWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("door_open");
        }
    }

    private void CloseDoor()
    {
        isOpen = false;
        if (disableGameObjectOnOpen)
        {
            gameObject.SetActive(true);
        }

        if (doorCollider != null) doorCollider.enabled = true;
        if (doorRenderer != null) doorRenderer.enabled = true;
        
        if (doorAnimator != null) 
        {
            doorAnimator.SetBool("IsOpen", false);
        }
    }
}