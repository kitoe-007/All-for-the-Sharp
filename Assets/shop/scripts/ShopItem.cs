using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItem : MonoBehaviour
{
    public int price;
    public string itemName;
    public Sprite itemIcon;

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

        Debug.Log($"Товар {itemName}: цена {price}");
    }

    void TryBuy()
    {
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        InventoryManager inv = FindFirstObjectByType<InventoryManager>();

        Debug.Log($"Попытка купить {itemName} за {price}. Денег у игрока: {shop?.playerMoney}");

        if (shop != null && inv != null)
        {
            if (shop.TrySpendMoney(price))
            {
                bool added = inv.AddItem(itemIcon);
                if (added)
                {
                    Debug.Log($"Успешно куплено! Осталось денег: {shop.playerMoney}");
                    Destroy(gameObject, 0.1f);
                }
                else
                {
                    shop.AddMoney(price);
                    Debug.Log("Инвентарь полон, деньги возвращены");
                }
            }
            else
            {
                Debug.Log($"НЕ ХВАТАЕТ ДЕНЕГ! Нужно: {price}, Есть: {shop.playerMoney}");
            }
        }
    }
}