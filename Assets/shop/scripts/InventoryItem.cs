using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{
    public string itemName;
    public Sprite itemIcon;
    public int buyPrice;

    private Image iconImage;
    private Button button;

    void Start()
    {
        iconImage = GetComponent<Image>();
        button = GetComponent<Button>();

        if (iconImage != null && itemIcon != null)
            iconImage.sprite = itemIcon;

        if (button != null)
            button.onClick.AddListener(UseItem);
    }

    void UseItem()
    {
        Debug.Log($"Использован предмет: {itemName}");
        // Здесь можно добавить эффект (лечение, урон и т.д.)
        // После использования можно удалить предмет: Destroy(gameObject);
    }
}