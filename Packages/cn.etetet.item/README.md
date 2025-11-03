# ET.Item

ET框架物品背包系统模块，提供完整的物品管理、背包容量控制和物品堆叠功能。

## 功能特性

- **物品管理**：支持物品的添加、移除、使用等完整操作
- **背包系统**：支持背包容量管理、槽位管理
- **物品堆叠**：支持可堆叠物品的自动堆叠
- **物品移动**：支持物品在背包槽位间移动和堆叠，类似魔兽世界操作
- **智能合并**：相同ConfigId且可堆叠的物品自动合并，未满则部分堆叠
- **服务端权威**：添加/移除/移动物品由服务端控制，自动同步客户端
- **前后端同步**：客户端服务端数据实时同步
- **配置驱动**：基于Luban配置系统的物品数据管理
- **模块化设计**：符合ET框架ECS架构规范

## 核心组件

### 服务端
- `ItemComponent` - 服务端物品背包组件
- `Item` - 物品实体
- `ItemComponentSystem` - 背包操作系统
- `ItemSystem` - 物品操作系统

### 客户端
- `ItemComponent` - 客户端物品背包组件（ET.Client命名空间）
- `Item` - 客户端物品实体（ET.Client命名空间）
- `ItemComponentSystem` - 客户端背包操作系统
- `ItemSystem` - 客户端物品操作系统

### 共享
- `ItemType` - 物品类型枚举
- `ItemConfigCategory` - 物品配置表类别
- `ErrorCode` - 物品相关错误码

## 设计原则

### 服务端权威架构
- **添加物品**：完全由服务端触发（任务奖励、怪物掉落、GM命令等），客户端无权请求添加
- **移除物品**：客户端通过UseItem/DiscardItem等具体操作间接移除，服务端验证后执行
- **自动同步**：所有物品变化由服务端主动推送M2C_UpdateItem通知客户端
- **数据同步**：客户端可通过SyncBagData主动请求完整背包数据

## 使用示例

### 服务端添加物品
```csharp
// 服务端添加物品（会自动通知客户端）
ItemComponent itemComponent = unit.GetComponent<Server.ItemComponent>();
bool success = itemComponent.AddItem(configId: 10001, count: 10);
// 无需手动调用通知，AddItem内部会自动发送M2C_UpdateItem
```

### 服务端移除物品
```csharp
// 服务端移除物品（会自动通知客户端）
bool success = itemComponent.RemoveItem(configId: 10001, count: 5);
// 无需手动调用通知，RemoveItem内部会自动发送M2C_UpdateItem
```

### 其他物品操作
```csharp
// 获取物品数量
int count = itemComponent.GetItemCount(configId: 10001);

// 检查背包是否已满
bool isFull = itemComponent.IsFull();

// 获取指定槽位的物品
Item item = itemComponent.GetItemBySlot(slotIndex: 0);

// 设置背包容量
itemComponent.SetCapacity(capacity: 200);
```

### 服务端移动/堆叠物品
```csharp
// 服务端移动或堆叠物品（会自动通知客户端）
ItemComponent itemComponent = unit.GetComponent<Server.ItemComponent>();
int errorCode = itemComponent.MoveItem(fromSlot: 0, toSlot: 1);
if (errorCode == ErrorCode.ERR_Success)
{
    // 移动/堆叠成功
}
// 无需手动调用通知，MoveItem内部会自动发送M2C_UpdateItem
```

### 客户端请求
```csharp
// 客户端请求使用物品
C2M_UseItem request = C2M_UseItem.Create();
request.SlotIndex = 0;
request.Count = 1;
var response = await fiber.Root.GetComponent<ClientSenderComponent>().Call(request);

// 客户端同步背包数据
C2M_SyncBagData syncRequest = C2M_SyncBagData.Create();
var syncResponse = await fiber.Root.GetComponent<ClientSenderComponent>().Call(syncRequest);

// 客户端移动/堆叠物品
C2M_MoveItem moveRequest = C2M_MoveItem.Create();
moveRequest.FromSlot = 0; // 源槽位
moveRequest.ToSlot = 1;   // 目标槽位
var moveResponse = await fiber.Root.GetComponent<ClientSenderComponent>().Call(moveRequest);

// 客户端获取物品
ItemComponent itemComponent = scene.GetComponent<Client.ItemComponent>();
Item item = itemComponent.GetItemBySlot(0);
if (item != null)
{
    Log.Debug($"Item ConfigId: {item.ConfigId}, Count: {item.Count}");
}
```

## 物品类型

- **Normal (0)**: 普通物品
- **Equipment (1)**: 装备
- **Consumable (2)**: 消耗品
- **Material (3)**: 材料
- **Quest (4)**: 任务物品

## 物品配置

物品配置位于 `Luban/Config/Datas/Item.xlsx`，包含以下字段：

- **Id**: 物品配置ID
- **Name**: 物品名称
- **Desc**: 物品描述
- **Type**: 物品类型
- **MaxStack**: 最大堆叠数
- **Icon**: 图标路径
- **Quality**: 品质等级
- **SellPrice**: 出售价格
- **BuyPrice**: 购买价格
- **UseType**: 使用类型
- **Level**: 等级要求

## 协议定义

协议定义位于 `Proto/Item_C_10800.proto`，包含：

### 客户端请求协议
- **C2M_UseItem**: 使用物品请求
- **C2M_SyncBagData**: 同步背包数据请求
- **C2M_MoveItem**: 移动/交换物品槽位
- **C2M_DiscardItem**: 丢弃物品
- **C2M_SortBag**: 整理背包

### 服务端推送协议
- **M2C_UpdateItem**: 物品更新通知（自动推送）
- **M2C_UpdateBagCapacity**: 背包容量变化通知

### 响应协议
- **M2C_UseItem**: 使用物品响应
- **M2C_SyncBagData**: 背包数据同步响应
- **M2C_MoveItem**: 移动物品响应
- **M2C_DiscardItem**: 丢弃物品响应
- **M2C_SortBag**: 整理背包响应

## 物品移动和堆叠功能

### 功能特性

- **智能堆叠**：将物品A拖到物品B上，如果ConfigId相同且可堆叠，自动合并
- **容量检测**：如果目标槽位未达到最大堆叠数，源物品将尽可能合并到目标
- **自动清理**：源物品完全堆叠到目标后，源槽位自动清空
- **部分堆叠**：如果目标槽位空间不足，只堆叠部分数量，剩余保留在源槽位
- **位置交换**：不同ConfigId或不可堆叠物品将交换位置
- **空槽移动**：拖动物品到空槽位时直接移动

### 堆叠规则

1. **相同物品且可堆叠**（MaxStack > 1）：
   - 目标未满：尽可能堆叠，源物品减少或清空
   - 目标已满：返回错误，无法堆叠

2. **不同物品或不可堆叠**（MaxStack = 1）：
   - 直接交换两个槽位的物品

3. **目标槽位为空**：
   - 直接移动到目标槽位

### 使用示例

```csharp
// 情况1：堆叠相同物品
// 槽位0：药水x5（最大堆叠10）
// 槽位1：药水x3（最大堆叠10）
// 执行 MoveItem(0, 1) 后：
// 槽位0：空
// 槽位1：药水x8

// 情况2：部分堆叠
// 槽位0：药水x7（最大堆叠10）
// 槽位1：药水x8（最大堆叠10）
// 执行 MoveItem(0, 1) 后：
// 槽位0：药水x5
// 槽位1：药水x10（已满）

// 情况3：交换不同物品
// 槽位0：药水x5
// 槽位1：装备x1
// 执行 MoveItem(0, 1) 后：
// 槽位0：装备x1
// 槽位1：药水x5

// 情况4：移动到空槽位
// 槽位0：药水x5
// 槽位1：空
// 执行 MoveItem(0, 1) 后：
// 槽位0：空
// 槽位1：药水x5
```

## 错误码

- `ERR_ItemNotFound`: 物品未找到
- `ERR_ItemNotEnough`: 物品数量不足
- `ERR_ItemUseCountInvalid`: 使用数量无效
- `ERR_ItemAddFailed`: 添加物品失败
- `ERR_ItemUseFailed`: 使用物品失败
- `ERR_ItemSlotInvalid`: 槽位索引无效
- `ERR_ItemCannotStack`: 物品无法堆叠（目标槽位已满）
- `ERR_ItemMoveToSameSlot`: 不能移动到相同槽位

## 依赖

- cn.etetet.core
- cn.etetet.excel
- cn.etetet.proto
- cn.etetet.unit

## 版本历史

### 1.3.0 (当前版本)
- 新增物品移动和堆叠功能（MoveItem）
- 支持相同ConfigId物品的智能堆叠合并
- 支持不同物品的位置交换
- 新增错误码：ERR_ItemSlotInvalid、ERR_ItemCannotStack、ERR_ItemMoveToSameSlot
- 新增C2M_MoveItemHandler消息处理器
- 完善README文档，添加详细的移动和堆叠功能说明

### 1.2.0
- 重构客户端物品系统架构
- ItemInfo结构体改为Item Entity，支持ECS规范
- ClientItemComponent重命名为ItemComponent（使用ET.Client命名空间区分）
- 客户端和服务端通过命名空间区分同名类（ET.Client.Item vs ET.Server.Item）
- 遵循ET框架Entity-Component-System设计原则

### 1.1.0
- 移除客户端直接添加/移除物品的请求
- 优化服务端权威架构，AddItem/RemoveItem自动通知客户端
- 添加ERR_ItemUseFailed错误码
- 优化UseItem逻辑，使用RemoveItem统一处理

### 1.0.0
- 初始版本
- 支持基础物品管理
- 支持物品堆叠
- 背包槽位管理
- 前后端数据同步

## 注意事项

1. **严禁客户端直接添加/移除物品**：所有物品增删必须由服务端触发
2. **自动通知机制**：AddItem、RemoveItem和MoveItem方法会自动通知客户端，无需手动调用NotifyItemChanges
3. **物品使用**：客户端通过UseItem请求，服务端验证后调用RemoveItem执行
4. **物品移动**：客户端通过MoveItem请求，服务端验证后执行移动或堆叠操作
5. **数据同步**：客户端可通过SyncBagData获取完整背包数据，用于登录或重连后的数据恢复
6. **命名空间区分**：客户端使用ET.Client.Item和ET.Client.ItemComponent，服务端使用ET.Server.Item和ET.Server.ItemComponent
7. **EntityRef使用**：客户端ItemComponent使用Dictionary<int, EntityRef<Item>>管理物品，遵循ET框架规范
8. **堆叠限制**：物品是否可堆叠由配置的MaxStack字段控制，MaxStack=1表示不可堆叠