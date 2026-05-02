using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public int playerMoney = 100;

    public Text mainMoneyText;
    public Text shopBalanceText;

    void Start()
    {
        UpdateMoneyUI();
    }

    public void UpdateMoneyUI()
    {
        if (mainMoneyText != null)
            mainMoneyText.text = playerMoney.ToString();

        if (shopBalanceText != null)
            shopBalanceText.text = playerMoney.ToString();
    }

    public bool TrySpendMoney(int amount)
    {
        if (playerMoney >= amount)
        {
            playerMoney -= amount;
            UpdateMoneyUI();
            return true;
        }
        return false;
    }

    public void AddMoney(int amount)
    {
        playerMoney += amount;
        UpdateMoneyUI();
    }
}