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
    private bool wasPressed = false;
    private bool hasInitializedState = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        // Jika unpressedSprite kosong, otomatis gunakan Sprite bawaan di SpriteRenderer
        if (unpressedSprite == null && sr != null)
        {
            unpressedSprite = sr.sprite;
        }
    }

    private void Start()
    {
        InitializeInitialState();
    }

    private void InitializeInitialState()
    {
        if (!hasInitializedState && InkTrailManager.Instance != null)
        {
            wasPressed = InkTrailManager.Instance.CheckInkNearPosition(gameObject.scene.name, transform.position, checkRadius);
            hasInitializedState = true;
        }
    }

    private void Update()
    {
        if (InkTrailManager.Instance == null || sr == null) return;

        if (!hasInitializedState)
        {
            InitializeInitialState();
        }

        // Mengecek posisi tinta di scene tempat button ini berada
        bool isPressed = InkTrailManager.Instance.CheckInkNearPosition(gameObject.scene.name, transform.position, checkRadius);

        // Trigger button_press SFX when button state transitions to pressed during active gameplay
        if (isPressed && !wasPressed)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("button_press");
            }
        }
        wasPressed = isPressed;
        
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