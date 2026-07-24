using UnityEngine;

public class InkButtonVisual : MonoBehaviour
{
    [Header("Deteksi Tinta")]
    [Tooltip("Radius seberapa dekat tinta harus berada di dekat button agar tertekan")]
    public float checkRadius = 0.5f;
    
    [Header("Visuals - Sprites (Optional)")]
    [Tooltip("Centang HANYA jika Anda ingin mengganti gambar Sprite saat ditekan")]
    public bool changeSpriteOnPress = false;
    public Sprite unpressedSprite;
    public Sprite pressedSprite;

    [Header("Visuals - Colors")]
    [Tooltip("Ubah warna saat button ditekan menjadi gelap")]
    public bool changeColorOnPress = true;
    public Color unpressedColor = Color.white;
    public Color pressedColor = new Color(0.3f, 0.3f, 0.3f, 1.0f); // Gelap saat ditekan

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        // Jika unpressedSprite kosong, otomatis gunakan Sprite bawaan di SpriteRenderer
        if (unpressedSprite == null && sr != null)
        {
            unpressedSprite = sr.sprite;
        }
    }

    private void Update()
    {
        if (InkTrailManager.Instance == null || sr == null) return;

        // Mengecek posisi tinta di scene tempat button ini berada
        bool isPressed = InkTrailManager.Instance.CheckInkNearPosition(gameObject.scene.name, transform.position, checkRadius);
        
        // Ubah gambar sprite button HANYA jika fitur changeSpriteOnPress diaktifkan
        if (changeSpriteOnPress && pressedSprite != null)
        {
            sr.sprite = isPressed ? pressedSprite : (unpressedSprite != null ? unpressedSprite : sr.sprite);
        }

        // Ubah warna button menjadi gelap saat ditekan
        if (changeColorOnPress)
        {
            sr.color = isPressed ? pressedColor : unpressedColor;
        }
    }
}