using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public int playerMoney = 100;

    public TMP_Text mainMoneyText;  
    public TMP_Text shopBalanceText;

    void Start()
    {
        UpdateMoneyUI();
    }

    public void UpdateMoneyUI()
    {
        Debug.Log($"Обновляем UI: money={playerMoney}, mainMoneyText={mainMoneyText != null}, shopBalanceText={shopBalanceText != null}");

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