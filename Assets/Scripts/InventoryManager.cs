using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("Settings")]
    public int totalSlots = 16; // Lưới 4x4
    public Transform gridParent; // InventoryPanel chứa Grid Layout Group
    public GameObject slotPrefab;
    public GameObject itemViewPrefab;

    [Header("Sample Data")]
    public List<ItemDataSO> availableItems; // Kéo các ItemDataSO mẫu vào đây từ Inspector

    // ---- Event cho script nhân vật / hệ thống khác lắng nghe ----
    // Bắn ra khi dùng 1 vật phẩm tiêu hao (đọc data.healHP, data.restoreMP để áp dụng hiệu ứng)
    public static event System.Action<ItemDataSO> OnConsumableUsed;
    // Bắn ra khi trang bị (true) hoặc tháo (false) một món (đọc data.attackBonus, data.defenseBonus)
    public static event System.Action<ItemDataSO, bool> OnEquipChanged;
    // Tuỳ chọn: gán hàm trả về false để chặn việc dùng (ví dụ HP đã đầy thì không tiêu hao)
    public static System.Func<ItemDataSO, bool> CanUseConsumable;

    [Header("UI Detail Panel")]
    public GameObject detailPanel;
    public Image detailIcon;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailTypeText;
    public TextMeshProUGUI detailDescText;
    public TextMeshProUGUI detailStatsText; // Tuỳ chọn: hiện số lượng, chỉ số, trạng thái
    public Button useButton;
    public Button deleteButton;
    public Button deselectButton;

    private readonly List<SlotUI> slots = new List<SlotUI>();
    private readonly Dictionary<EquipSlotType, ItemView> equippedViews = new Dictionary<EquipSlotType, ItemView>();
    private ItemView selectedItemView;
    private TextMeshProUGUI useButtonLabel;

    private void Start()
    {
        GenerateGrid();
        SpawnInitialItems();

        if (detailPanel != null) detailPanel.SetActive(false);

        ItemView.OnItemSelected += HandleItemClicked;

        if (useButton != null)
        {
            useButtonLabel = useButton.GetComponentInChildren<TextMeshProUGUI>();
            useButton.onClick.AddListener(OnUseItemClicked);
        }
        if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteItemClicked);
        if (deselectButton != null) deselectButton.onClick.AddListener(DeselectItem);
    }

    private void OnDestroy()
    {
        ItemView.OnItemSelected -= HandleItemClicked;
    }

    // ---------------------------------------------------------------- Grid

    private void GenerateGrid()
    {
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }
        slots.Clear();

        for (int i = 0; i < totalSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, gridParent);
            SlotUI slotUI = slotObj.GetComponent<SlotUI>();
            slotUI.slotIndex = i;
            slots.Add(slotUI);
        }
    }

    public void SpawnInitialItems()
    {
        // Xóa item cũ (chỉ xóa ItemView, giữ lại các child khác của slot như viền, nền)
        foreach (var slot in slots)
        {
            foreach (var item in slot.GetComponentsInChildren<ItemView>())
            {
                Destroy(item.gameObject);
            }
        }

        // Item cũ đã bị xóa nên bỏ luôn trạng thái trang bị
        foreach (var pair in equippedViews)
        {
            if (pair.Value != null) OnEquipChanged?.Invoke(pair.Value.itemData, false);
        }
        equippedViews.Clear();
        DeselectItem();

        if (availableItems == null || availableItems.Count == 0) return;

        foreach (var slot in slots)
        {
            if (Random.value < 0.7f) // 70% ô có item
            {
                ItemDataSO randomData = availableItems[Random.Range(0, availableItems.Count)];
                int randomStack = Random.Range(1, randomData.maxStack + 1);

                GameObject itemObj = Instantiate(itemViewPrefab, slot.transform);
                ItemView itemView = itemObj.GetComponent<ItemView>();
                itemView.InitItem(randomData, randomStack);
                itemView.SetParentSlot(slot.transform);
            }
        }
    }

    // ---------------------------------------------------------------- Detail panel

    private void HandleItemClicked(ItemDataSO data, int stack, ItemView clickedItemView)
    {
        selectedItemView = clickedItemView;
        RefreshDetailPanel();
    }

    public void DeselectItem()
    {
        selectedItemView = null;
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    private void RefreshDetailPanel()
    {
        if (selectedItemView == null)
        {
            if (detailPanel != null) detailPanel.SetActive(false);
            return;
        }

        ItemDataSO data = selectedItemView.itemData;

        if (detailPanel != null) detailPanel.SetActive(true);
        if (selectedItemView != null) detailIcon.sprite = data.icon;
        if (detailIcon != null) detailIcon.sprite = data.icon;
        if (detailNameText != null) detailNameText.text = data.itemName;
        if (detailTypeText != null) detailTypeText.text = "Loại: " + GetTypeName(data.type);
        if (detailDescText != null) detailDescText.text = data.description;
        if (detailStatsText != null) detailStatsText.text = BuildStatsText(selectedItemView);

        UpdateUseButton();
    }

    private string GetTypeName(ItemType type)
    {
        switch (type)
        {
            case ItemType.Consumable: return "Tiêu hao";
            case ItemType.Equipment: return "Trang bị";
            case ItemType.Material: return "Nguyên liệu";
            default: return type.ToString();
        }
    }

    private string BuildStatsText(ItemView item)
    {
        ItemDataSO d = item.itemData;
        var sb = new StringBuilder();

        switch (d.type)
        {
            case ItemType.Consumable:
                if (d.healHP > 0) sb.AppendLine("Hồi HP: +" + d.healHP);
                if (d.restoreMP > 0) sb.AppendLine("Hồi MP: +" + d.restoreMP);
                sb.Append("Số lượng: " + item.currentStack);
                break;

            case ItemType.Equipment:
                sb.AppendLine("Vị trí: " + d.equipSlot);
                if (d.attackBonus != 0) sb.AppendLine("Tấn công: +" + d.attackBonus);
                if (d.defenseBonus != 0) sb.AppendLine("Phòng thủ: +" + d.defenseBonus);
                if (item.isEquipped) sb.Append("[Đang trang bị]");
                break;

            case ItemType.Material:
                sb.AppendLine("Dùng để chế tạo, không thể sử dụng trực tiếp.");
                sb.Append("Số lượng: " + item.currentStack);
                break;
        }

        return sb.ToString();
    }

    // Nút Use đổi chữ / trạng thái theo loại item
    private void UpdateUseButton()
    {
        if (selectedItemView == null) return;

        string label;
        bool canUse;

        switch (selectedItemView.itemData.type)
        {
            case ItemType.Consumable:
                label = "Dùng";
                canUse = true;
                break;
            case ItemType.Equipment:
                label = selectedItemView.isEquipped ? "Tháo" : "Trang bị";
                canUse = true;
                break;
            default: // Material
                label = "Không thể dùng";
                canUse = false;
                break;
        }

        if (useButton != null) useButton.interactable = canUse;
        if (useButtonLabel != null) useButtonLabel.text = label;
    }

    // ---------------------------------------------------------------- Use / Delete

    private void OnUseItemClicked()
    {
        if (selectedItemView == null) return;

        switch (selectedItemView.itemData.type)
        {
            case ItemType.Consumable:
                UseConsumable(selectedItemView);
                break;
            case ItemType.Equipment:
                ToggleEquip(selectedItemView);
                break;
            case ItemType.Material:
                // Nguyên liệu không có hành động Use
                break;
        }
    }

    // Consumable: hồi HP/MP rồi giảm stack 1
    private void UseConsumable(ItemView item)
    {
        ItemDataSO data = item.itemData;

        // Script khác có thể chặn việc dùng (ví dụ HP đã đầy thì không tiêu hao)
        if (CanUseConsumable != null && !CanUseConsumable(data)) return;

        Debug.Log($"Dùng {data.itemName}: HP +{data.healHP}, MP +{data.restoreMP}");
        OnConsumableUsed?.Invoke(data);

        item.currentStack--;
        if (item.currentStack <= 0)
        {
            Destroy(item.gameObject);
            DeselectItem();
        }
        else
        {
            item.InitItem(data, item.currentStack);
            RefreshDetailPanel();
        }
    }

    // Equipment: trang bị / tháo, mỗi vị trí chỉ mặc được 1 món
    private void ToggleEquip(ItemView item)
    {
        ItemDataSO data = item.itemData;

        if (item.isEquipped)
        {
            UnequipView(item);
        }
        else
        {
            // Đã có món khác ở cùng vị trí -> tháo món đó ra trước
            if (equippedViews.TryGetValue(data.equipSlot, out ItemView old) && old != null)
            {
                UnequipView(old);
            }

            equippedViews[data.equipSlot] = item;
            item.SetEquipped(true);
            Debug.Log($"Trang bị {data.itemName}: ATK +{data.attackBonus}, DEF +{data.defenseBonus}");
            OnEquipChanged?.Invoke(data, true);
        }

        RefreshDetailPanel();
    }

    private void UnequipView(ItemView item)
    {
        EquipSlotType slotType = item.itemData.equipSlot;

        if (equippedViews.TryGetValue(slotType, out ItemView current) && current == item)
        {
            equippedViews.Remove(slotType);
        }

        item.SetEquipped(false);
        OnEquipChanged?.Invoke(item.itemData, false);
    }

    private void OnDeleteItemClicked()
    {
        if (selectedItemView == null) return;

        // Xóa trang bị đang mặc thì phải tháo ra trước để chỉ số không bị giữ lại
        if (selectedItemView.isEquipped) UnequipView(selectedItemView);

        Destroy(selectedItemView.gameObject);
        DeselectItem();
    }
}