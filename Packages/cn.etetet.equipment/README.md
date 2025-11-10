# ET.Equipment

ET框架装备系统模块，提供完整的装备管理和装备穿戴功能。

## 设计理念

装备系统基于Item的扩展设计，极其简洁：
- **Item是装备的基础**：装备就是一个特殊的Item
- **EquipmentItemComponent**：挂在Item上，标记该Item为装备，记录装备属性（强化等级）
- **Item.SlotIndex**：复用Item的SlotIndex字段，表示装备穿戴的槽位
  - 值 >= 0：表示装备已穿戴，值为装备槽位类型
  - 值 = -1：表示装备未穿戴（在背包中）

## 核心组件

### EquipmentItemComponent
挂在Item上的装备组件，包含：
- `EnhanceLevel`：装备强化等级

### Item.SlotIndex
复用Item的SlotIndex字段：
- 当Item有EquipmentItemComponent时，SlotIndex表示装备槽位
- 装备穿戴时，SlotIndex被设置为装备槽位类型的值
- 装备卸下时，SlotIndex被设置为-1

## 装备槽位类型

支持9个装备槽位（EquipmentSlotType枚举）：
- Head = 1（头部）
- Chest = 2（胸部）
- Waist = 3（腰部）
- Legs = 4（腿部）
- Feet = 5（脚部）
- MainHand = 6（主手武器）
- OffHand = 7（副手武器）
- Accessory1 = 8（饰品1）
- Accessory2 = 9（饰品2）

## 使用示例

```csharp
// 检查Item是否为装备
EquipmentItemComponent equipmentComponent = item.GetComponent<EquipmentItemComponent>();
if (equipmentComponent != null)
{
    // 这是一个装备
    int enhanceLevel = equipmentComponent.EnhanceLevel;
    
    // 检查是否已穿戴
    if (item.SlotIndex >= 0)
    {
        EquipmentSlotType slotType = (EquipmentSlotType)item.SlotIndex;
        // 装备已穿戴在slotType槽位
    }
    else
    {
        // 装备未穿戴，在背包中
    }
}

// 穿戴装备
item.SlotIndex = (int)EquipmentSlotType.MainHand;

// 卸下装备
item.SlotIndex = -1;
```

## 消息协议

### C2M_EquipItem / M2C_EquipItem
穿戴装备请求和响应

### C2M_UnequipItem / M2C_UnequipItem
卸下装备请求和响应

### C2M_SyncEquipmentData / M2C_SyncEquipmentData
同步装备数据请求和响应

### M2C_UpdateEquipment
装备更新通知（服务器推送）

### C2M_EnhanceEquipment / M2C_EnhanceEquipment
强化装备请求和响应

## 依赖

- cn.etetet.core
- cn.etetet.excel
- cn.etetet.proto
- cn.etetet.item（Item系统）
- cn.etetet.unit（Unit系统）