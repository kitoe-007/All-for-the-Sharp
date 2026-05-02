using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItem : MonoBehaviour
{
    public int price;
    public string itemName;
    public Sprite itemIcon;  // Перетащи сюда спрайт предмета

    private Button button;
    private TMP_Text priceText;

    void Start()
    {
        button = GetComponent<Button>();
        priceText = GetComponentInChildren<TMP_Text>();

        if (priceText != null)
            priceText.text = price.ToString();

        if (button != null)
            button.onClick.AddListener(TryBuy);
    }

    void TryBuy()
    {
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        InventoryManager inv = FindFirstObjectByType<InventoryManager>();

        if (shop != null && inv != null)
        {
            if (shop.TrySpendMoney(price))
            {
                // Добавляем предмет в инвентарь
                bool added = inv.AddItem(itemIcon);

                if (added)
                {
                    Debug.Log($"Куплено: {itemName}");
                    Destroy(gameObject);
                }
                else
                {
                    // Инвентарь полон — возвращаем деньги
                    shop.AddMoney(price);
                    Debug.Log("Инвентарь полон!");
                }
            }
            else
            {
                Debug.Log($"Не хватает денег! Нужно: {price}");
            }
        }
    }
}