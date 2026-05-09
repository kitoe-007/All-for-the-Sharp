using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public int money = 100;
    public Text moneyText;

    void Start()
    {
        moneyText.text = money.ToString();
    }
}