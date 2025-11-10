# 装备系统Helper实现说明

## 概述

本目录包含装备系统的Helper类，实现了从背包装备物品和卸载装备的完整功能，并通过消息系统同步到客户端。

## 核心文件

### EquipmentHelper.cs

装备辅助类，提供装备和卸载装备的核心逻辑和客户端通知：

#### 主要方法

1. **EquipItem(Unit unit, long itemId, EquipmentSlotType slotType)** - 装备装备
   - 从背包中获取指定物品
   - 如果槽位已有装备，自动卸载旧装备
   - 将物品装备到指定槽位
   - 通知客户端装备变化和背包变化
   - 返回错误码（0表示成功）

2. **UnequipItem(Unit unit, EquipmentSlotType slotType)** - 卸载装备
   - 从装备槽位卸下装备
   - 将装备放回背包空槽位
   - 通知客户端装备变化和背包变化
   - 返回错误码（0表示成功）

#### 使用示例

```csharp
// 装备物品
int result = EquipmentHelper.EquipItem(unit, itemId, EquipmentSlotType.MainHand);
if (result == ErrorCode.ERR_Success)
{
    Log.Debug("equipment equipped successfully");
}

// 卸载装备
result = EquipmentHelper.UnequipItem(unit, EquipmentSlotType.MainHand);
if (result == ErrorCode.ERR_Success)
{
    Log.Debug("equipment unequipped successfully");
}
```

## 消息处理

装备系统复用了Item系统的M2C_UpdateItem消息来通知客户端：

- **装备槽位标识**：使用SlotIndex的负数来表示装备槽位
  - 计算方式：`SlotIndex = -((int)SlotType + 1)`
  - 例如：Head槽位(1) → SlotIndex=-2
  - 这样可以区分背包槽位（正数）和装备槽位（负数）

- **装备穿戴通知**：
  - ItemId: 物品ID
  - SlotIndex: 负数表示装备槽位
  - ConfigId: 物品配置ID
  - Count: 1（装备数量总是1）

- **装备卸载通知**：
  - ItemId: 0
  - SlotIndex: 负数表示装备槽位
  - Count: 0（表示移除）

- **背包变化通知**：
  - 装备时：通知背包槽位物品移除（Count=0）
  - 卸载时：通知背包添加物品

## 错误码

定义在 `Scripts/Model/Share/ErrorCode.cs`：

- `ERR_EquipmentSlotNotFound` - 装备槽位未找到
- `ERR_EquipmentNotFound` - 装备未找到
- `ERR_EquipmentItemNotInBag` - 物品不在背包中
- `ERR_EquipmentBagFull` - 背包已满（卸载时）
- `ERR_EquipmentSlotEmpty` - 装备槽位为空（卸载时）
- `ERR_EquipmentInvalidSlotType` - 无效的槽位类型
- `ERR_ItemComponentNotFound` - 背包组件未找到

## 客户端通知机制

装备系统复用了Item系统的消息通知机制，通过M2C_UpdateItem消息实现客户端同步：

### 装备穿戴通知
```csharp
M2C_UpdateItem message = M2C_UpdateItem.Create();
message.ItemId = item.Id;
message.SlotIndex = -((int)slotType + 1); // 负数表示装备槽位
message.ConfigId = item.ConfigId;
message.Count = 1; // 装备数量总是1
```

### 装备卸载通知
```csharp
M2C_UpdateItem message = M2C_UpdateItem.Create();
message.ItemId = 0;
message.SlotIndex = -((int)slotType + 1); // 负数表示装备槽位
message.Count = 0; // Count=0表示移除
```

### 背包变化通知
- 装备时：通知背包槽位物品移除（Count=0）
- 卸载时：通知背包添加物品

## 实现特点

1. **完整的装备流程**：从背包取出 → 装备到槽位 → 通知客户端
2. **自动替换旧装备**：装备新物品时自动卸下旧装备到背包
3. **完整的卸载流程**：从槽位卸下 → 放回背包 → 通知客户端
4. **错误处理**：完善的错误检查和错误码返回
5. **客户端同步**：通过消息系统保证客户端数据同步
6. **事务性**：装备/卸载操作是原子性的

## 注意事项

1. **槽位标识**：装备槽位使用SlotIndex负数表示，区别于背包槽位的正数
2. **背包空间**：卸载装备前会检查背包是否有空槽位
3. **自动卸载**：装备新物品时会自动处理旧装备的卸载
4. **实体管理**：使用EntityRef确保Entity引用的安全性
5. **日志记录**：关键操作都有Debug日志输出
6. **消息复用**：复用Item系统的M2C_UpdateItem消息，避免引入新消息类型

## 依赖关系

- 依赖ItemComponent（背包系统）
- 依赖EquipmentComponent（装备组件）
- 依赖MapMessageHelper（消息通知）
- 依赖ErrorCode（错误码定义）

## 后续优化建议

1. 添加装备条件检查（等级、职业等）
2. 添加装备属性加成计算
3. 添加装备耐久度系统
4. 添加装备强化系统集成
5. 添加装备套装效果
6. 添加装备绑定状态检查