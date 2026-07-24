using UnityEngine;

public class InkButtonVisual : MonoBehaviour
{
    [Header("Deteksi Tinta")]
    [Tooltip("Radius seberapa dekat tinta harus berada di dekat button agar tertekan")]
    public float checkRadius = 0.5f;
    
    [Header("Visuals")]
    public Sprite unpressedSprite;
    public Sprite pressedSprite;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (InkTrailManager.Instance == null) return;

        // Mengecek posisi tinta di scene tempat button ini berada
        bool isPressed = InkTrailManager.Instance.CheckInkNearPosition(gameObject.scene.name, transform.position, checkRadius);
        
        // Ubah gambar button
        sr.sprite = isPressed ? pressedSprite : unpressedSprite;
    }
}