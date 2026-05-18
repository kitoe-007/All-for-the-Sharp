using UnityEngine;
using UnityEngine.UI;

public class HandbookManager : MonoBehaviour
{
    public Canvas handbookCanvas;
    public Button openButton;
    public MonoBehaviour playerMovement;  // если нужно блокировать движение при открытии

    void Start()
    {
        if (handbookCanvas != null)
            handbookCanvas.gameObject.SetActive(false);

        if (openButton != null)
            openButton.onClick.AddListener(OpenHandbook);
    }

    void Update()
    {
        // Закрытие справочника по Escape
        if (handbookCanvas != null && handbookCanvas.gameObject.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseHandbook();
            }
        }
    }

    void OpenHandbook()
    {
        if (handbookCanvas != null)
            handbookCanvas.gameObject.SetActive(true);
        if (playerMovement != null)
            playerMovement.enabled = false;
        Debug.Log("Справочник открыт");
    }

    void CloseHandbook()
    {
        if (handbookCanvas != null)
            handbookCanvas.gameObject.SetActive(false);
        if (playerMovement != null)
            playerMovement.enabled = true;
        Debug.Log("Справочник закрыт");
    }
}