using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    [SerializeField] private CompilerManager compilerManager;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Если ссылку не задали в инспекторе, пробуем найти CompilerManager в сцене
        if (compilerManager == null)
        {
            compilerManager = FindObjectOfType<CompilerManager>();
            if (compilerManager == null)
            {
                Debug.LogError("DragAndDrop: не найден объект с компонентом CompilerManager в сцене");
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false; // чтобы луч проходил сквозь объект
        if (compilerManager != null)
            {
                compilerManager.SpawnCommand();
            }
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Получаем объект под мышкой (исключая самого себя)
        GameObject dropTarget = GetDropTarget(eventData);

        if (dropTarget != null && dropTarget.TryGetComponent<DropZone>(out var dropZone))
        {
            // Прикрепляемся к зоне: становимся её дочерним элементом
            transform.SetParent(dropTarget.transform);
            
            // Устанавливаем локальную позицию в ноль (или можно выровнять по центру зоны)
            rectTransform.anchoredPosition = Vector2.zero;

            // Вызываем SpawnCommand только если ссылка успешно найдена
            
        }
        else
        {
            // Возвращаем на место, если не попали в зону
            Destroy(gameObject);
        }
    }

    private GameObject GetDropTarget(PointerEventData eventData)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            // Пропускаем самого себя
            if (result.gameObject != gameObject)
                return result.gameObject;
        }
        return null;
    }
}