using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UpdateHotbarUI : MonoBehaviour
{
    [SerializeField] private Image[] slotIcons;

    private Hotbar linkedHotbar;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryLinkHotbar();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnlinkHotbar();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Player scene might load after this UI scene did — try again each time a scene loads
        if (linkedHotbar == null)
            TryLinkHotbar();
    }

    private void TryLinkHotbar()
    {
        if (Hotbar.Instance == null) return; // player scene not loaded yet, will retry on next sceneLoaded

        linkedHotbar = Hotbar.Instance;
        linkedHotbar.OnHotbarChanged += Refresh;
        Refresh();
    }

    private void UnlinkHotbar()
    {
        if (linkedHotbar != null)
        {
            linkedHotbar.OnHotbarChanged -= Refresh;
            linkedHotbar = null;
        }
    }

    private void Refresh()
    {
        for (int i = 0; i < slotIcons.Length; i++)
        {
            var slot = linkedHotbar.slots[i];
            if (slot.item != null)
            {
                slotIcons[i].sprite = slot.item.icon;
                slotIcons[i].enabled = true;
            }
            else
            {
                slotIcons[i].enabled = false;
            }
        }
    }
}