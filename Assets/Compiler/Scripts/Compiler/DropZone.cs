using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

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

            // Новый слот — сосед под текущей зоной (общий родитель), чтобы цепочка drop-зон росла вниз.
            if (parentRt != null && zoneRt != null)
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