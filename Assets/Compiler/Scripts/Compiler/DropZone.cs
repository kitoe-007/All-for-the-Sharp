using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler
{
    public Vector2 snapOffset = Vector2.zero;
    [SerializeField] private GameObject prefab;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            DragAndDrop draggable = eventData.pointerDrag.GetComponent<DragAndDrop>();
            if (draggable != null)
            {
                Debug.Log($"{eventData.pointerDrag.name} dropped on {gameObject.name}");
                CreateNew();
            }
        }
    }

    public void CreateNew()
    {
        if (prefab == null)
        {
            Debug.LogError($"DropZone '{name}': prefab не назначен в инспекторе");
            return;
        }

        // Если это UI (RectTransform), спавним как дочерний элемент зоны (в canvas-space),
        // иначе — как обычный объект в world-space.
        var instance = Instantiate(prefab);
        instance.name = $"{prefab.name} (Clone)";

        if (instance.TryGetComponent<RectTransform>(out var rect))
        {
            // UI: важно parent-ить с worldPositionStays=false, иначе может улететь/скейлиться странно.
            rect.SetParent(transform, false);
            rect.anchoredPosition = snapOffset + Vector2.down * 75f; // 50px, чтобы точно было заметно
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.gameObject.SetActive(true);
            rect.SetAsLastSibling(); // поверх других элементов в этой группе

            Debug.Log(
                $"Spawned UI '{instance.name}' active={instance.activeInHierarchy} " +
                $"anchoredPos={rect.anchoredPosition} sizeDelta={rect.sizeDelta} parent='{transform.name}'");
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
}