using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    public int playerMoney = 99;

    public Text mainMoneyText;      // Обычный текст на экране
    public TMP_Text shopBalanceText; // TextMeshPro внутри магазина

    void Start()
    {
        UpdateMoneyUI();
    }

    public void UpdateMoneyUI()
    {
        if (mainMoneyText != null)
            mainMoneyText.text = playerMoney.ToString();

        if (shopBalanceText != null)
            shopBalanceText.text = $"{playerMoney}";
    }

    public bool TryBuyItem(int price)
    {
        if (playerMoney >= price)
        {
            playerMoney -= price;
            UpdateMoneyUI();
            return true;
        }
        return false;
    }
}