using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Singleton UI manager for displaying bottom-right item notifications.
/// Automatically handles inactive scene object lookup and icon/text population.
/// </summary>
public class NotificationUI : MonoBehaviour
{
    private static NotificationUI _instance;

    public static NotificationUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<NotificationUI>(FindObjectsInactive.Include);
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("UI References")]
    [Tooltip("The panel container for the item notification.")]
    public GameObject panel;

    [Tooltip("Text component displaying the notification message.")]
    public TMP_Text textNotification;

    [Tooltip("Image component displaying the item sprite icon.")]
    public Image itemIconImage;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // If existing Instance has no panel assigned, but THIS instance has panel assigned:
            if (_instance.panel == null && this.panel != null)
            {
                Destroy(_instance); // Remove the old empty script component
                _instance = this;
                if (transform.parent == null)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else
            {
                // Destroy ONLY this duplicate script component, NEVER the GameObject or its child UI elements
                Destroy(this);
                return;
            }
        }
        else
        {
            _instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        AutoLinkReferences();

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void AutoLinkReferences()
    {
        if (panel == null)
        {
            panel = gameObject;
        }

        if (textNotification == null)
        {
            textNotification = GetComponentInChildren<TMP_Text>(true);
        }

        if (itemIconImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                if (panel != null && img.gameObject != panel)
                {
                    itemIconImage = img;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Displays the item notification panel with text message and optional sprite icon for 2 seconds.
    /// </summary>
    public void Show(string message, Sprite icon = null)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        AutoLinkReferences();

        StopAllCoroutines();
        StartCoroutine(ShowRoutine(message, icon));
    }

    private IEnumerator ShowRoutine(string message, Sprite icon)
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (textNotification != null)
        {
            textNotification.text = message;
            textNotification.gameObject.SetActive(true);
        }

        if (itemIconImage != null)
        {
            if (icon != null)
            {
                itemIconImage.sprite = icon;
                itemIconImage.enabled = true;
                itemIconImage.gameObject.SetActive(true);
            }
            else
            {
                itemIconImage.enabled = false;
                itemIconImage.gameObject.SetActive(false);
            }
        }

        yield return new WaitForSeconds(2f);

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
}