using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler
{
    // Можно задать смещение, если нужно, чтобы предмет вставал не строго в центр
    public Vector2 snapOffset = Vector2.zero;

    // Этот метод вызывается автоматически, когда на объект бросают другой UI-элемент
    public void OnDrop(PointerEventData eventData)
    {
        // eventData.pointerDrag — это объект, который тащили
        if (eventData.pointerDrag != null)
        {
            DragAndDrop draggable = eventData.pointerDrag.GetComponent<DragAndDrop>();
            if (draggable != null)
            {
                // Можно сразу привязать, но мы уже сделали привязку в DragAndDrop.
                // Здесь можно выполнить дополнительные действия, например, воспроизвести звук.
                Debug.Log($"{eventData.pointerDrag.name} dropped on {gameObject.name}");
            }
        }
    }
}