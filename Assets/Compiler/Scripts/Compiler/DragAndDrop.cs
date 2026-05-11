using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private RectTransform parentRectTransform;
    private Vector2 dragOffset;

    /// <summary>Выставляется в <see cref="DropZone.OnDrop"/> при успешном сбросе.</summary>
    private bool dropAccepted;

    [SerializeField] private CompilerManager compilerManager;
    /// <summary>Используется, если на объекте нет <see cref="CommandSpawn"/>.</summary>
    [SerializeField] private CompilerCommandType commandType = CompilerCommandType.VariableCommand;
    [SerializeField] private GameObject prefab;

    private CommandSpawn commandSpawn;

    public void Spawn()
    {
        Instantiate(prefab, transform.position, Quaternion.identity);
    }
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        parentRectTransform = rectTransform.parent as RectTransform;
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        commandSpawn = GetComponent<CommandSpawn>();

        // Если ссылку не задали в инспекторе, пробуем найти CompilerManager в сцене
        if (compilerManager == null)
        {
            compilerManager = FindFirstObjectByType<CompilerManager>();
            if (compilerManager == null)
            {
                Debug.LogError("DragAndDrop: не найден объект с компонентом CompilerManager в сцене");
                
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Вынести под root Canvas, иначе RectMask2D у Scroll View палитры обрезает превью при перетаскивании.
        var rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (rootCanvas != null && rectTransform != null)
        {
            rectTransform.SetParent(rootCanvas.transform, worldPositionStays: true);
            rectTransform.SetAsLastSibling();
        }

        parentRectTransform = rectTransform.parent as RectTransform;

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false; // чтобы луч проходил сквозь объект

        // Смещение считаем уже в координатах актуального родителя (после переноса на canvas).
        if (parentRectTransform != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPointerPos))
        {
            dragOffset = rectTransform.anchoredPosition - localPointerPos;
        }

        if (compilerManager != null)
        {
            CompilerCommandType kind = commandSpawn != null ? commandSpawn.CommandKind : commandType;
            compilerManager.SpawnCommand(kind);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parentRectTransform == null)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPointerPos))
        {
            rectTransform.anchoredPosition = localPointerPos + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // OnDrop может вызываться после OnEndDrag в том же кадре — даём событиям завершиться.
        StartCoroutine(ResolveDropAfterUiEvents());
    }

    private IEnumerator ResolveDropAfterUiEvents()
    {
        yield return null;
        if (!dropAccepted)
        {
            // Нельзя ждать конца кадра на этом объекте: Destroy остановит корутину до WaitForEndOfFrame.
            if (compilerManager != null)
                compilerManager.ScheduleTryCollapseRedundantEmptyHoldersAfterMissedDrop();
            Destroy(gameObject);
        }
        else
            dropAccepted = false;
    }

    /// <summary>Вызывается из <see cref="DropZone"/> при успешном IDropHandler.OnDrop.</summary>
    internal void MarkDropAccepted()
    {
        dropAccepted = true;
    }
}