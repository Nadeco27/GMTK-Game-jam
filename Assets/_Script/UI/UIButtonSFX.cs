using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this script to any UI Button (or Canvas container) to automatically play
/// the 'button_click' SFX whenever the button is clicked.
/// </summary>
public class UIButtonSFX : MonoBehaviour
{
    [Tooltip("Sound ID to play on button click. Defaults to 'button_click'.")]
    [SerializeField] private string clickSoundID = "button_click";

    private void Awake()
    {
        // Auto-assign to Button on this object
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(PlayClickSFX);
        }
        else
        {
            // If attached to parent container, auto-assign to all child Buttons
            Button[] childButtons = GetComponentsInChildren<Button>(true);
            foreach (Button b in childButtons)
            {
                if (b != null)
                {
                    b.onClick.AddListener(PlayClickSFX);
                }
            }
        }
    }

    public void PlayClickSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clickSoundID);
        }
    }
}
