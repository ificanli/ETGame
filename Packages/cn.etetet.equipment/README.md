# ET.Equipment

ET框架装备系统模块，提供完整的装备管理和装备穿戴功能。

## 设计理念

装备系统基于Item的扩展设计，极其简洁：
- **Item是装备的基础**：装备就是一个特殊的Item
- **EquipmentItemComponent**：挂在Item上，标记该Item为装备，记录装备属性（强化等级）
- **Item.SlotIndex**：在不同组件中有不同含义
  - 在ItemComponent（背包）中：SlotIndex表示背包槽位索引（0到capacity-1）
  - 在EquipmentComponent（装备栏）中：SlotIndex表示装备槽位类型（1-9，对应EquipmentSlotType枚举值）

## 核心组件

### EquipmentItemComponent
挂在Item上的装备组件，包含：
- `EnhanceLevel`：装备强化等级

### Item.SlotIndex
Item的SlotIndex字段在不同地方有不同含义：
- **在背包（ItemComponent）中**：SlotIndex = 背包槽位索引（0, 1, 2, ...）
- **在装备栏（EquipmentComponent）中**：SlotIndex = 装备槽位类型（1=头部, 2=胸部, ...）
- Item在哪个组件中，就由哪个组件管理，SlotIndex就是那个组件的槽位索引

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
}

// 判断装备在哪里
// 如果Item是ItemComponent的子Entity，说明在背包中，SlotIndex是背包槽位
// 如果Item是EquipmentComponent的子Entity，说明已穿戴，SlotIndex是装备槽位类型

// 穿戴装备（通过EquipmentHelper）
EquipmentHelper.EquipItem(unit, itemId, EquipmentSlotType.MainHand);

// 卸下装备（通过EquipmentHelper）
EquipmentHelper.UnequipItem(unit, EquipmentSlotType.MainHand);
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

**重要说明：装备与道具消息分离**
- 装备更新使用独立的 `M2C_UpdateEquipment` 消息
- 道具更新使用 `M2C_UpdateItem` 消息
- 两种消息互不干扰，职责清晰

**消息字段：**
- `SlotType`: 装备槽位类型
  - 正数（1-9）：表示装备穿戴到对应槽位
  - -1：表示卸下装备
- `ItemId`: 物品ID
- `ConfigId`: 物品配置ID

**使用示例：**
```csharp
// 服务端通知装备穿戴
M2C_UpdateEquipment message = M2C_UpdateEquipment.Create();
message.SlotType = (int)EquipmentSlotType.MainHand; // 装备到主手
message.ItemId = item.Id;
message.ConfigId = item.ConfigId;

// 服务端通知装备卸下
M2C_UpdateEquipment message = M2C_UpdateEquipment.Create();
message.SlotType = -1; // -1表示卸下装备
message.ItemId = item.Id;
message.ConfigId = item.ConfigId;
```

### C2M_EnhanceEquipment / M2C_EnhanceEquipment
强化装备请求和响应

## 依赖

- cn.etetet.core
- cn.etetet.excel
- cn.etetet.proto
- cn.etetet.item（Item系统）
- cn.etetet.unit（Unit系统）