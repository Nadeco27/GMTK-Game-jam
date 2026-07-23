using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public string itemName = "Flashlight";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Inventory inventory = other.GetComponent<Inventory>();

            if (inventory != null)
            {
                inventory.AddItem(itemName);

                Debug.Log("Memanggil Notification");

                NotificationUI.Instance.Show(itemName + " Item Obtained!");

                Destroy(gameObject);
            }
        }
    }
}