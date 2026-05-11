using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DropZone : MonoBehaviour, IDropHandler
{
    public Vector2 snapOffset = Vector2.zero;

    [Tooltip("Префаб, который создаётся после успешного drop (новая зона / слот под текущей DropZone).")]
    [FormerlySerializedAs("prefab")]
    [SerializeField] private GameObject prefabAfterDrop;
    [Tooltip("Расстояние между текущей зоной и следующей склонированной DropZone (UI).")]
    [SerializeField] private float nextDropSpacingPixels = 75f;

    [Tooltip(
        "Если префаб Holder случайно сохранён с перетаскиваемой командой внутри, новый слот будет с «лишним» блоком. Включено — удаляем все дочерние объекты с DragAndDrop у только что созданного экземпляра (кроме корня).")]
    [SerializeField] private bool clearDragAndDropChildrenOnSpawn = true;

    [Tooltip("Для имён Holder_1, Holder_2, … как VariableCommand в CompilerManager. Пусто — ищется в сцене.")]
    [SerializeField] private CompilerManager compilerManager;

    private void Awake()
    {
        if (compilerManager == null)
            compilerManager = FindFirstObjectByType<CompilerManager>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        DragAndDrop draggable = eventData.pointerDrag.GetComponent<DragAndDrop>();
        if (draggable == null)
            return;

        draggable.MarkDropAccepted();

        int removedOtherCommands = RemoveOtherCommandsInThisSlot(eventData.pointerDrag);
        bool alreadyChildOfThisDropZone = eventData.pointerDrag.transform.parent == transform;

        RectTransform dragRect = draggable.GetComponent<RectTransform>();
        if (dragRect != null)
        {
            dragRect.SetParent(transform, false);
            // Левый край по горизонтали, по вертикали — середина зоны (при необходимости поменяйте anchor Y на 1 или 0).
            dragRect.anchorMin = new Vector2(0f, 0.5f);
            dragRect.anchorMax = new Vector2(0f, 0.5f);
            dragRect.pivot = new Vector2(0f, 0.5f);
            dragRect.anchoredPosition = snapOffset;
            dragRect.localRotation = Quaternion.identity;
            dragRect.localScale = Vector3.one;
            dragRect.SetAsLastSibling();
        }
        else
        {
            draggable.transform.SetParent(transform, false);
            draggable.transform.localPosition = new Vector3(snapOffset.x, snapOffset.y, 0f);
            draggable.transform.localRotation = Quaternion.identity;
            draggable.transform.localScale = Vector3.one;
        }

        Debug.Log($"{eventData.pointerDrag.name} dropped on {gameObject.name}");

        // Новый Holder под слотом — только первый дроп с «чужого» родителя на пустой слот.
        // При замене команды или повторном сбросе на ту же DropZone слот не размножаем.
        if (removedOtherCommands == 0 && !alreadyChildOfThisDropZone)
            CreateNew();
    }

    public void CreateNew()
    {
        if (prefabAfterDrop == null)
        {
            Debug.LogError(
                $"DropZone '{name}': Prefab After Drop не назначен — укажите префаб в компоненте Drop Zone на этом объекте.");
            return;
        }

        // Если это UI (RectTransform), спавним как дочерний элемент зоны (в canvas-space),
        // иначе — как обычный объект в world-space.
        var instance = Instantiate(prefabAfterDrop);
        if (compilerManager != null)
            instance.name = compilerManager.TakeNextHolderName();
        else
            instance.name = $"{prefabAfterDrop.name} (Clone)";
        RemoveDragAndDropChildrenFromSpawn(instance);

        if (instance.TryGetComponent<RectTransform>(out var rect))
        {
            var zoneRt = transform as RectTransform;
            var parentRt = transform.parent as RectTransform;
            var scroll = GetComponentInParent<ScrollRect>();
            var scrollContent = compilerManager != null ? compilerManager.ScrollContent as RectTransform : null;
            if (scrollContent == null && scroll != null)
                scrollContent = scroll.content;

            // Новый слот — сосед под текущей зоной (общий родитель), чтобы цепочка drop-зон росла вниз.
            if (zoneRt != null && scrollContent != null)
            {
                // Ключевой момент: новый holder должен быть ребёнком ScrollRect.content,
                // иначе scroll не увеличит content bounds и вниз "нечего" скроллить.
                rect.SetParent(scrollContent, false);
                rect.anchorMin = zoneRt.anchorMin;
                rect.anchorMax = zoneRt.anchorMax;
                rect.pivot = zoneRt.pivot;
                rect.sizeDelta = zoneRt.sizeDelta;
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
                float h = zoneRt.rect.height > 0f ? zoneRt.rect.height : Mathf.Abs(zoneRt.sizeDelta.y);
                rect.anchoredPosition = zoneRt.anchoredPosition + Vector2.down * (h + nextDropSpacingPixels);
            }
            else if (parentRt != null && zoneRt != null)
            {
                rect.SetParent(parentRt, false);
                rect.anchorMin = zoneRt.anchorMin;
                rect.anchorMax = zoneRt.anchorMax;
                rect.pivot = zoneRt.pivot;
                rect.sizeDelta = zoneRt.sizeDelta;
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
                float h = zoneRt.rect.height > 0f ? zoneRt.rect.height : Mathf.Abs(zoneRt.sizeDelta.y);
                rect.anchoredPosition = zoneRt.anchoredPosition + Vector2.down * (h + nextDropSpacingPixels);
            }
            else
            {
                rect.SetParent(transform, false);
                rect.anchoredPosition = snapOffset + Vector2.down * nextDropSpacingPixels;
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
            }

            rect.gameObject.SetActive(true);
            rect.SetAsLastSibling();

            Debug.Log(
                $"Spawned UI '{instance.name}' active={instance.activeInHierarchy} " +
                $"anchoredPos={rect.anchoredPosition} sizeDelta={rect.sizeDelta} parent='{rect.transform.parent?.name}'");

            // После добавления/перемещения UI элементов ScrollRect часто не успевает пересчитать bounds/content size.
            // Принудительно пересобираем layout, чтобы можно было докрутить до новых holder'ов.
            compilerManager?.ForceUpdateScrollContentLayout();

            // Если это ScrollView — докручиваем вниз.
            if (scroll != null)
            {
                Canvas.ForceUpdateCanvases();
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 0f;
            }
        }
        else
        {
            instance.transform.SetParent(null, true);
            instance.transform.position = transform.position + Vector3.down * 5f;
            instance.transform.rotation = Quaternion.identity;

            Debug.Log(
                $"Spawned GO '{instance.name}' active={instance.activeInHierarchy} pos={instance.transform.position}");
        }
    }

    /// <summary>
    /// Перед приёмом новой команды удаляет прежнюю в этом слоте: прямые дети Holder с
    /// <see cref="DragAndDrop"/> и прямые дети этой DropZone (кроме перетаскиваемого объекта).
    /// </summary>
    /// <returns>Сколько других команд удалено (для решения, спавнить ли новый Holder).</returns>
    private int RemoveOtherCommandsInThisSlot(GameObject incomingDrag)
    {
        if (incomingDrag == null)
            return 0;

        int removed = 0;

        Transform holder = FindHolderAncestor(transform);
        if (holder != null)
        {
            for (int i = holder.childCount - 1; i >= 0; i--)
            {
                Transform c = holder.GetChild(i);
                if (c.gameObject == incomingDrag)
                    continue;
                if (c.GetComponent<DragAndDrop>() != null)
                {
                    Destroy(c.gameObject);
                    removed++;
                }
            }
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform c = transform.GetChild(i);
            if (c.gameObject == incomingDrag)
                continue;
            if (c.GetComponent<DragAndDrop>() != null)
            {
                Destroy(c.gameObject);
                removed++;
            }
        }

        return removed;
    }

    private static Transform FindHolderAncestor(Transform start)
    {
        for (Transform t = start; t != null; t = t.parent)
        {
            if (IsHolderRootName(t.name))
                return t;
        }

        return null;
    }

    private static bool IsHolderRootName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        if (string.Equals(name, "Holder", StringComparison.OrdinalIgnoreCase))
            return true;
        return Regex.IsMatch(name, @"^Holder_\d+$", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Префаб Holder часто сохраняют с уже вложенной командой — тогда каждый новый слот клонирует её.
    /// Удаляем вложенные объекты с <see cref="DragAndDrop"/>, не трогая корень нового экземпляра.
    /// </summary>
    private void RemoveDragAndDropChildrenFromSpawn(GameObject root)
    {
        if (!clearDragAndDropChildrenOnSpawn)
            return;

        var draggables = root.GetComponentsInChildren<DragAndDrop>(true);
        foreach (var d in draggables)
        {
            if (d.transform == root.transform)
                continue;
            Destroy(d.gameObject);
        }
    }
}