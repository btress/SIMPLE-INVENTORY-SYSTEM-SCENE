using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class ItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public ItemDataSO itemData;
    public int currentStack;
    public bool isEquipped { get; private set; }

    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI stackText;
    public GameObject equippedMarker; // Icon/chữ "E" hiện khi đang trang bị (tuỳ chọn)

    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private Canvas mainCanvas;

    public static event Action<ItemDataSO, int, ItemView> OnItemSelected;

    // Slot mà item đang nằm (hoặc nằm trước khi bắt đầu kéo)
    public Transform OriginalParent => originalParent;

    private RectTransform rectTransform => transform as RectTransform;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        mainCanvas = GetComponentInParent<Canvas>().rootCanvas;
        if (equippedMarker != null) equippedMarker.SetActive(isEquipped);
    }

    public void InitItem(ItemDataSO data, int stack)
    {
        itemData = data;
        currentStack = stack;
        if (iconImage != null) iconImage.sprite = data.icon;
        if (stackText != null) stackText.text = stack > 1 ? stack.ToString() : "";
    }

    public void SetEquipped(bool value)
    {
        isEquipped = value;
        if (equippedMarker != null) equippedMarker.SetActive(value);
    }

    public void SetParentSlot(Transform newParent)
    {
        originalParent = newParent;
        transform.SetParent(newParent, false);
        rectTransform.anchoredPosition = Vector2.zero;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        // Tắt raycast để ô bên dưới (SlotUI) nhận được OnDrop
        canvasGroup.blocksRaycasts = false;
        // Đưa item lên layer cao nhất để không bị che khuất
        transform.SetParent(mainCanvas.transform);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / mainCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        if (transform.parent == mainCanvas.transform)
        {
            // Thả ra ngoài, không trúng slot nào -> trả về slot cũ
            SetParentSlot(originalParent);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnItemSelected?.Invoke(itemData, currentStack, this);
    }
}