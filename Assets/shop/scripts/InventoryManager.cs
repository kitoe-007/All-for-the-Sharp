using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public Canvas inventoryCanvas;
    public Button openButton;
    public MonoBehaviour playerMovement;
    public Image[] slots;  // СЮДА ПЕРЕТАЩИ 5 ЯЧЕЕК (Image)
    public int nextFreeSlot = 0;

    void Start()
    {
        ResetInventory();  // ← Добавь ЭТУ СТРОКУ

        if (inventoryCanvas != null)
            inventoryCanvas.gameObject.SetActive(false);

        if (openButton != null)
            openButton.onClick.AddListener(OpenInventory);
    }

    public void ResetInventory()
    {
        nextFreeSlot = 0;

        // Очищаем все слоты визуально
        foreach (Image slot in slots)
        {
            if (slot != null)
            {
                slot.sprite = null;
                slot.color = new Color(1, 1, 1, 0); // Прозрачный
            }
        }

        Debug.Log("Инвентарь сброшен!");
    }

    void Update()
    {
        if (inventoryCanvas != null && inventoryCanvas.gameObject.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseInventory();
            }
        }
    }

    public void OpenInventory()
    {
        if (inventoryCanvas != null)
            inventoryCanvas.gameObject.SetActive(true);
        if (playerMovement != null)
            playerMovement.enabled = false;
        Debug.Log("Инвентарь открыт");
    }

    public void CloseInventory()
    {
        if (inventoryCanvas != null)
            inventoryCanvas.gameObject.SetActive(false);
        if (playerMovement != null)
            playerMovement.enabled = true;
        Debug.Log("Инвентарь закрыт");
    }

    public bool AddItem(Sprite itemIcon)
    {
        Debug.Log($"nextFreeSlot = {nextFreeSlot}, slots.Length = {slots.Length}");

        if (nextFreeSlot >= slots.Length)
        {
            Debug.Log($"Инвентарь реально полон! nextFreeSlot={nextFreeSlot}, максимум={slots.Length}");
            return false;
        }

        if (itemIcon == null)
        {
            Debug.LogError("itemIcon = null!");
            return false;
        }

        slots[nextFreeSlot].sprite = itemIcon;
        slots[nextFreeSlot].color = Color.white;
        nextFreeSlot++;

        Debug.Log($"Предмет добавлен в слот {nextFreeSlot - 1}");
        return true;
    }
}