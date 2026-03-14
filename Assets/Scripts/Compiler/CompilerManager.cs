using UnityEngine;

public class CompilerManager : MonoBehaviour
{
    public GameObject CommandPrefab; // Ссылка на префаб (перетащите в инспектор)
    public Transform spawnParent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void SpawnCommand()
    {
        GameObject newCommand = Instantiate(CommandPrefab, spawnParent);

        // Настраиваем позицию (например, в центре экрана или под мышкой)
        RectTransform rect = newCommand.GetComponent<RectTransform>();
    }
}
