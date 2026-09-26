using UnityEngine;

public enum ItemType
{
    Consumable, // Vật phẩm tiêu hao (thuốc HP,...)
    Equipment,  // Trang bị (mũ, giáp,...)
    Material    // Nguyên liệu
}

public enum EquipSlotType
{
    Head,
    Body,
    Weapon,
    Boots
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemDataSO : ScriptableObject
{
    public string id;
    public string itemName;
    public Sprite icon;
    public ItemType type;
    public int maxStack = 99;
    [TextArea] public string description;

    [Header("Consumable")]
    public int healHP;      // Lượng HP hồi khi dùng
    public int restoreMP;   // Lượng MP hồi khi dùng

    [Header("Equipment")]
    public EquipSlotType equipSlot; // Vị trí trang bị
    public int attackBonus;
    public int defenseBonus;

    private void OnValidate()
    {
        if (maxStack < 1) maxStack = 1;
        // Trang bị không được xếp chồng
        if (type == ItemType.Equipment) maxStack = 1;
    }
}