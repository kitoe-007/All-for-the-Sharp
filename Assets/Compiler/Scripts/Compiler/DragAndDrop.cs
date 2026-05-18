using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class DragAndDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private RectTransform parentRectTransform;
    private Vector2 dragOffset;

    /// <summary>Жест начался над полем ввода — не перетаскиваем блок и не уничтожаем его в OnEndDrag.</summary>
    private bool dragCancelledForNestedInput;

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

    private static bool IsPointerPressOnNestedInput(PointerEventData eventData, Transform blockRoot)
    {
        GameObject go = eventData.pointerPressRaycast.gameObject;
        if (go == null)
            go = eventData.pointerPress;
        if (go == null || blockRoot == null)
            return false;
        if (!go.transform.IsChildOf(blockRoot))
            return false;
        return go.GetComponentInParent<InputField>() != null ||
               go.GetComponentInParent<TMP_InputField>() != null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragCancelledForNestedInput = IsPointerPressOnNestedInput(eventData, transform);
        if (dragCancelledForNestedInput)
            return;

        Transform paletteParentBeforeDrag = rectTransform != null ? rectTransform.parent : null;
        CompilerCommandType dragKind = commandSpawn != null
            ? commandSpawn.CommandKind
            : CompilerCommandKind.FromRootName(gameObject.name);
        int paletteSlotForClone = (int)dragKind;

        // Под root Canvas — иначе RectMask2D у Scroll View (палитра) обрезает превью при перетаскивании.
        var rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (rootCanvas != null && rectTransform != null)
        {
            rectTransform.SetParent(rootCanvas.transform, worldPositionStays: true);
            rectTransform.SetAsLastSibling();
        }

        parentRectTransform = rectTransform.parent as RectTransform;

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false; // чтобы луч проходил сквозь объект

        // Для Screen Space Overlay (и в целом для UI) считаем смещение,
        // чтобы элемент не "прыгал" и чтобы курсор совпадал с позицией перетаскивания.
        if (parentRectTransform != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPointerPos))
        {
            dragOffset = rectTransform.anchoredPosition - localPointerPos;
        }

        if (compilerManager != null && compilerManager.SpawnCommandCloneOnBeginDrag)
        {
            bool fromPalette = compilerManager.spawnParent != null &&
                               paletteParentBeforeDrag == compilerManager.spawnParent;
            if (fromPalette)
                compilerManager.SpawnCommand(dragKind, paletteSlotForClone);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragCancelledForNestedInput)
            return;

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
        if (dragCancelledForNestedInput)
        {
            dragCancelledForNestedInput = false;
            return;
        }

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