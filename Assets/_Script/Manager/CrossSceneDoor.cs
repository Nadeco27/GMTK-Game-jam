using System.Collections.Generic;
using UnityEngine;

public class CrossSceneDoor : MonoBehaviour
{
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
    public Animator doorAnimator; // Gunakan animator yang sudah Anda buat sebelumnya
    
    private bool isOpen = false;

    private void Update()
    {
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
        else if (!allButtonsPressed && isOpen)
        {
            CloseDoor();
        }
    }

    private void OpenDoor()
    {
        isOpen = true;
        if (doorCollider != null) doorCollider.enabled = false;
        
        if (doorAnimator != null) 
        {
            doorAnimator.SetBool("IsOpen", true); // Sesuaikan dengan parameter animasi Anda
        }
    }

    private void CloseDoor()
    {
        isOpen = false;
        if (doorCollider != null) doorCollider.enabled = true;
        
        if (doorAnimator != null) 
        {
            doorAnimator.SetBool("IsOpen", false); // Sesuaikan dengan parameter animasi Anda
        }
    }
}