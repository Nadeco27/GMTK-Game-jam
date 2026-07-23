using UnityEngine;
using TMPro;
using System.Collections;

public class NotificationUI : MonoBehaviour
{
    public static NotificationUI Instance;

    public GameObject panel;
    public TMP_Text textNotification;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(string message)
    {
        StopAllCoroutines();
        StartCoroutine(ShowRoutine(message));
    }

    IEnumerator ShowRoutine(string message)
    {
        panel.SetActive(true);
        textNotification.text = message;

        yield return new WaitForSeconds(2f);

        panel.SetActive(false);
    }
}