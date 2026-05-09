using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public Canvas inventoryCanvas;
    public Button openButton;          // Кнопка открытия (назначаешь в инспекторе)
    public MonoBehaviour playerMovement;

    void Start()
    {
        if (inventoryCanvas != null)
            inventoryCanvas.gameObject.SetActive(false);

        // Подписка на кнопку открытия
        if (openButton != null)
            openButton.onClick.AddListener(OpenInventory);
    }

    void Update()
    {
        // Закрытие по Escape
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
        // Твой код добавления предмета
        Debug.Log("Предмет добавлен");
        return true;
    }
}