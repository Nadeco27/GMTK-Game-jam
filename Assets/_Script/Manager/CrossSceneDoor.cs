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
    
    private bool isOpen = false;

    private string GetDoorID()
    {
        return $"{gameObject.scene.name}_{gameObject.name}_{transform.position.x:F2}_{transform.position.y:F2}";
    }

    private void Start()
    {
        if (openedDoorIDs.Contains(GetDoorID()))
        {
            OpenDoor();
        }
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
            OpenDoor();
        }
    }

    private void OpenDoor()
    {
        isOpen = true;
        openedDoorIDs.Add(GetDoorID());

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