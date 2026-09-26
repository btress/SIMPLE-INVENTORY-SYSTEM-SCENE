using UnityEngine;
using UnityEngine.EventSystems;

public class SlotUI : MonoBehaviour, IDropHandler
{
    public int slotIndex;

    // Item đang nằm trong slot này (null nếu ô trống)
    public ItemView GetItem()
    {
        return GetComponentInChildren<ItemView>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        ItemView dragged = eventData.pointerDrag.GetComponent<ItemView>();
        if (dragged == null) return;

        // Lúc này dragged đang nằm dưới Canvas nên GetItem() chỉ trả về item vốn có của ô đích
        ItemView existing = GetItem();
        Transform fromSlot = dragged.OriginalParent;

        // Ô đích đã có item -> đưa nó về ô mà item kéo xuất phát (hoán đổi)
        if (existing != null && existing != dragged && fromSlot != null)
        {
            existing.SetParentSlot(fromSlot);
        }

        dragged.SetParentSlot(transform);
    }
}