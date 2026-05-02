using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public Canvas inventoryCanvas;
    public Button openButton;
    public Button closeButton;
    public MonoBehaviour playerMovement;

    public Image[] slots;  // Сюда перетащишь 5 ячеек (Image)

    private int nextFreeSlot = 0;  // Какая ячейка следующая свободная

    void Start()
    {
        if (inventoryCanvas != null)
            inventoryCanvas.gameObject.SetActive(false);

        if (openButton != null)
            openButton.onClick.AddListener(OpenInventory);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInventory);
    }

    void OpenInventory()
    {
        if (inventoryCanvas != null)
            inventoryCanvas.gameObject.SetActive(true);
        if (playerMovement != null)
            playerMovement.enabled = false;
    }

    void CloseInventory()
    {
        if (inventoryCanvas != null)
            inventoryCanvas.gameObject.SetActive(false);
        if (playerMovement != null)
            playerMovement.enabled = true;
    }

    public bool AddItem(Sprite itemIcon)
    {
        if (nextFreeSlot >= slots.Length)
        {
            Debug.Log("Инвентарь полон!");
            return false;
        }

        // Ставим картинку в первую свободную ячейку
        slots[nextFreeSlot].sprite = itemIcon;
        slots[nextFreeSlot].color = Color.white;  // Делаем видимой
        nextFreeSlot++;

        return true;
    }
}