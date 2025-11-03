# ET.Item

ET框架物品背包系统模块，提供完整的物品管理、背包容量控制和物品堆叠功能。

## 功能特性

- **物品管理**：支持物品的添加、移除、使用等完整操作
- **背包系统**：支持背包容量管理、槽位管理
- **物品堆叠**：支持可堆叠物品的自动堆叠
- **服务端权威**：添加/移除物品由服务端控制，自动同步客户端
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
- `ClientItemComponent` - 客户端物品背包组件
- `ItemInfo` - 客户端物品信息结构
- `ClientItemComponentSystem` - 客户端背包操作系统

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
ItemComponent itemComponent = unit.GetComponent<ItemComponent>();
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

## 错误码

- `ERR_ItemNotFound`: 物品未找到
- `ERR_ItemNotEnough`: 物品数量不足
- `ERR_ItemUseCountInvalid`: 使用数量无效
- `ERR_ItemAddFailed`: 添加物品失败
- `ERR_ItemUseFailed`: 使用物品失败

## 依赖

- cn.etetet.core
- cn.etetet.excel
- cn.etetet.proto
- cn.etetet.unit

## 版本历史

### 1.1.0 (当前版本)
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
2. **自动通知机制**：AddItem和RemoveItem方法会自动通知客户端，无需手动调用NotifyItemChanges
3. **物品使用**：客户端通过UseItem请求，服务端验证后调用RemoveItem执行
4. **数据同步**：客户端可通过SyncBagData获取完整背包数据，用于登录或重连后的数据恢复