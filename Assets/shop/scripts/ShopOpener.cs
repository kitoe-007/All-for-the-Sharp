using UnityEngine;
using UnityEngine.UI;

public class ShopOpener : MonoBehaviour
{
    public Canvas shopCanvas;
    public Button openShopButton;
    public Button closeShopButton;
    public MonoBehaviour playerMovement;

    void Start()
    {
        if (shopCanvas != null)
            shopCanvas.gameObject.SetActive(false);

        if (openShopButton != null)
            openShopButton.onClick.AddListener(Open);

        if (closeShopButton != null)
            closeShopButton.onClick.AddListener(Close);
    }

    void Open()
    {
        if (shopCanvas != null)
            shopCanvas.gameObject.SetActive(true);

        if (playerMovement != null)
            playerMovement.enabled = false;
    }

    void Close()
    {
        if (shopCanvas != null)
            shopCanvas.gameObject.SetActive(false);

        if (playerMovement != null)
            playerMovement.enabled = true;
    }
}