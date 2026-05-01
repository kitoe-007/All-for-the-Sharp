using UnityEngine;
using UnityEngine.UI;
using TMPro; // У тебя TextMeshPro, так что эта строка нужна

public class ShopItem : MonoBehaviour
{
    public int price;  // Цену укажешь в инспекторе (50, 100, и т.д.)

    private Button button;
    private TMP_Text priceText; // У тебя TextMeshPro, не обычный Text

    void Start()
    {
        button = GetComponent<Button>();

        // Находим текст с ценой (дочерний объект priceText)
        priceText = GetComponentInChildren<TMP_Text>();

        // Ставим цену на кнопку
        if (priceText != null)
            priceText.text = price.ToString();

        // Вешаем действие на кнопку
        if (button != null)
            button.onClick.AddListener(BuyItem);
    }

    void BuyItem()
    {
        // Находим менеджера магазина
        ShopManager shop = FindFirstObjectByType<ShopManager>();

        if (shop != null)
        {
            // Пытаемся купить
            if (shop.TryBuyItem(price))
            {
                Debug.Log($"Куплен товар за {price} монет");
                Destroy(gameObject); // Удаляем товар из магазина
            }
            else
            {
                Debug.Log($"Не хватает денег! Нужно: {price}");
            }
        }
    }
}