# ET.Item

ET框架物品背包系统模块，提供完整的物品管理、背包容量控制和物品堆叠功能。

## 功能特性

- **物品管理**：支持物品的添加、移除、使用等完整操作
- **背包系统**：支持背包容量管理、槽位管理
- **物品堆叠**：支持可堆叠物品的自动堆叠
- **物品移动**：支持物品在背包槽位间移动和堆叠，类似魔兽世界操作
- **整理背包**：支持一键整理背包，相同ConfigId物品自动堆叠并排序，类似魔兽世界整理功能
- **智能合并**：相同ConfigId且可堆叠的物品自动合并，未满则部分堆叠
- **服务端权威**：添加/移除/移动物品由服务端控制，自动同步客户端
- **前后端同步**：客户端服务端数据实时同步
- **配置驱动**：基于Luban配置系统的物品数据管理
- **模块化设计**：符合ET框架ECS架构规范

## 核心组件

### 服务端
- `ItemComponent` - 服务端物品背包组件
- `Item` - 物品实体
- `ItemComponentSystem` - 背包数据查询系统（纯数据操作，不涉及消息）
- `ItemHelper` - 物品业务逻辑辅助类（处理涉及消息通知的复杂操作）
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

### 架构分层设计

系统采用清晰的职责分离架构：

#### ItemComponentSystem - 数据查询层
- **职责**：纯数据查询和简单管理操作
- **特点**：保持简洁，不涉及网络消息发送
- **方法**：
  - `GetItemCount()` - 获取物品总数量
  - `FindEmptySlot()` - 查找空槽位
  - `GetUsedSlotCount()` - 获取已使用槽位数量
  - `IsFull()` - 检查背包是否已满
  - `GetItemBySlot()` - 获取指定槽位的物品
  - `GetItemById()` - 通过ItemId获取物品
  - `Clear()` - 清空背包

#### ItemHelper - 业务逻辑层
- **职责**：处理涉及消息通知的复杂业务逻辑
- **特点**：封装完整业务流程，包括数据修改和消息通知
- **方法**：
  - `AddItem()` - 添加物品并通知客户端
  - `RemoveItem()` - 移除物品并通知客户端
  - `RemoveItemById()` - 通过ID移除物品并通知客户端
  - `MoveItem()` - 移动或堆叠物品并通知客户端
  - `SortBag()` - 整理背包，相同ConfigId物品堆叠并排序
  - `SetCapacity()` - 设置容量并通知客户端
  - `NotifyItemUpdate()` - 通知单个物品更新
  - `NotifyItemRemove()` - 通知物品移除
  - `NotifyItemChanges()` - 通知所有物品变化
  - `NotifyCapacityChange()` - 通知容量变化

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
bool success = ItemHelper.AddItem(itemComponent, configId: 10001, count: 10, reason: ItemChangeReason.QuestReward);
// 无需手动调用通知，AddItem内部会自动发送M2C_UpdateItem
```

### 服务端移除物品
```csharp
// 服务端移除物品（会自动通知客户端）
bool success = ItemHelper.RemoveItem(itemComponent, configId: 10001, count: 5, reason: ItemChangeReason.UseItem);
// 无需手动调用通知，RemoveItem内部会自动发送M2C_UpdateItem
```

### 其他物品操作
```csharp
// 获取物品数量（使用ItemComponentSystem）
int count = itemComponent.GetItemCount(configId: 10001);

// 检查背包是否已满（使用ItemComponentSystem）
bool isFull = itemComponent.IsFull();

// 获取指定槽位的物品（使用ItemComponentSystem）
Item item = itemComponent.GetItemBySlot(slotIndex: 0);

// 设置背包容量（使用ItemHelper，会自动通知客户端）
ItemHelper.SetCapacity(itemComponent, capacity: 200);
```

### 服务端移动/堆叠物品
```csharp
// 服务端移动或堆叠物品（会自动通知客户端）
ItemComponent itemComponent = unit.GetComponent<Server.ItemComponent>();

// 通过ItemId移动物品
Item item = itemComponent.GetItemById(itemId);
int errorCode = ItemHelper.MoveItem(itemComponent, itemId: item.Id, toSlot: 1);
if (errorCode == ErrorCode.ERR_Success)
{
    // 移动/堆叠成功
}
// 无需手动调用通知，MoveItem内部会自动发送M2C_UpdateItem
```

### 通过ItemId获取物品
```csharp
// 服务端通过ItemId直接获取物品
Item item = itemComponent.GetItemById(itemId);
if (item != null && !item.IsDisposed)
{
    Log.Debug($"Item ConfigId: {item.ConfigId}, Count: {item.Count}, Slot: {item.SlotIndex}");
}
```

### 客户端请求
```csharp
// 客户端请求使用物品（通过ItemId）
C2M_UseItem request = C2M_UseItem.Create();
request.ItemId = item.Id; // 使用ItemId而非SlotIndex
request.Count = 1;
var response = await fiber.Root.GetComponent<ClientSenderComponent>().Call(request);

// 客户端同步背包数据
C2M_SyncBagData syncRequest = C2M_SyncBagData.Create();
var syncResponse = await fiber.Root.GetComponent<ClientSenderComponent>().Call(syncRequest);

// 客户端移动/堆叠物品（通过ItemId）
C2M_MoveItem moveRequest = C2M_MoveItem.Create();
moveRequest.ItemId = item.Id; // 使用ItemId指定要移动的物品
moveRequest.ToSlot = 1;        // 目标槽位
var moveResponse = await fiber.Root.GetComponent<ClientSenderComponent>().Call(moveRequest);

// 客户端整理背包
C2M_SortBag sortRequest = C2M_SortBag.Create();
var sortResponse = await fiber.Root.GetComponent<ClientSenderComponent>().Call(sortRequest);
// 整理后，相同ConfigId的物品会自动堆叠并按ConfigId排序

// 客户端通过槽位获取物品
ItemComponent itemComponent = scene.GetComponent<Client.ItemComponent>();
Item item = itemComponent.GetItemBySlot(0);
if (item != null)
{
    Log.Debug($"Item ID: {item.Id}, ConfigId: {item.ConfigId}, Count: {item.Count}");
}

// 客户端通过ItemId获取物品
Item itemById = itemComponent.GetItemById(itemId);
if (itemById != null)
{
    Log.Debug($"Item ConfigId: {itemById.ConfigId}, Count: {itemById.Count}, Slot: {itemById.SlotIndex}");
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

## 整理背包功能

### 功能特性

- **一键整理**：自动整理背包中的所有物品
- **智能堆叠**：相同ConfigId的物品会尽量堆叠在一起
- **自动排序**：物品按ConfigId升序排列，从槽位0开始紧密排列
- **考虑堆叠上限**：每堆物品不会超过MaxStack限制
- **无缝体验**：整理过程自动完成，客户端实时同步

### 整理规则

1. **收集统计**：遍历所有物品，按ConfigId分组统计总数量
2. **按ID排序**：将所有ConfigId按升序排列
3. **重新堆叠**：根据MaxStack限制，重新创建物品堆
4. **紧密排列**：从槽位0开始依次放置，中间无空隙

### 使用示例

```csharp
// 服务端调用
ItemComponent itemComponent = unit.GetComponent<Server.ItemComponent>();
int errorCode = ItemHelper.SortBag(itemComponent);
if (errorCode == ErrorCode.ERR_Success)
{
    // 整理成功
}

// 客户端请求
C2M_SortBag request = C2M_SortBag.Create();
var response = await fiber.Root.GetComponent<ClientSenderComponent>().Call(request);
```

### 整理效果示例

```csharp
// 整理前：
// 槽位0：药水(10002)x5
// 槽位1：空
// 槽位2：装备(10001)x1
// 槽位3：药水(10002)x3
// 槽位4：材料(10003)x10
// 槽位5：空
// 槽位6：药水(10002)x7（最大堆叠10）

// 整理后（按ConfigId排序且堆叠）：
// 槽位0：装备(10001)x1
// 槽位1：药水(10002)x10
// 槽位2：药水(10002)x5
// 槽位3：材料(10003)x10
// 槽位4：空
// 槽位5：空
// 槽位6：空
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

## 物品追踪机制

### ItemId设计

系统采用基于`Item.Id`的物品追踪机制，而非基于槽位索引（SlotIndex）：

- **唯一标识**：每个物品实体都有唯一的`Item.Id`（Entity的ID）
- **完整追踪**：可以追踪物品的完整生命周期（创建、移动、使用、移除）
- **直接访问**：服务端和客户端都可以通过ItemId直接获取物品，无需通过槽位
- **永不为0**：ItemId永远不为0（Entity的ID由ET框架管理，从1开始递增）

### 物品移除语义

物品的移除和槽位清空统一通过`Count=0`表示：

1. **物品被移除**（使用、丢弃等）
   - 服务端发送：`ItemId + SlotIndex + Count=0`
   - 表示该物品在该槽位被完全移除
   - 客户端通过SlotIndex定位并移除物品Entity

2. **槽位清空**（物品移动导致）
   - 服务端发送：`原ItemId + 原SlotIndex + Count=0`
   - 表示物品从原槽位移走（可能移动到新槽位）
   - 客户端通过SlotIndex清空该槽位
   - 同时会收到新槽位的更新消息

### 优势

- **可追踪性**：每个物品都有唯一ID，便于日志记录和问题排查
- **灵活操作**：操作物品时使用ItemId，不受槽位变化影响
- **架构清晰**：利用ET框架Entity机制，无需额外的ID管理
- **语义统一**：Count=0统一表示移除，逻辑清晰简单

## 代码实现详解

### ItemHelper - 业务逻辑层实现

#### AddItem 方法实现
```csharp
public static bool AddItem(ItemComponent self, int configId, int count, ItemChangeReason reason)
{
    // 1. 参数验证
    if (count <= 0) return false;
    
    // 2. 获取物品配置
    ItemConfig itemConfig = ItemConfigCategory.Instance.Get(configId);
    if (itemConfig == null) return false;
    
    // 3. 堆叠逻辑：先尝试叠加到已有物品
    if (itemConfig.MaxStack > 1)
    {
        // 遍历所有槽位，找到相同ConfigId且未满的物品
        // 计算可以叠加的数量，更新物品Count
    }
    
    // 4. 创建新物品：剩余数量创建新物品堆
    while (remainCount > 0)
    {
        // 查找空槽位
        // 创建新Item实体
        // 设置ConfigId和Count
    }
    
    // 5. 自动通知客户端：遍历所有更新的物品，发送M2C_UpdateItem
    foreach (long itemId in updatedItemIds)
    {
        NotifyItemUpdate(self, item);
    }
}
```

**关键特性**：
- 自动处理物品堆叠逻辑
- 支持部分堆叠（当新增数量超过单堆上限时）
- 自动通知客户端，无需手动调用
- 返回bool表示是否添加成功

#### RemoveItem 方法实现
```csharp
public static bool RemoveItem(ItemComponent self, int configId, int count, ItemChangeReason reason)
{
    // 1. 检查物品数量是否足够
    int totalCount = self.GetItemCount(configId);
    if (totalCount < count) return false;
    
    // 2. 遍历槽位，按顺序移除物品
    for (int i = 0; i < self.SlotItems.Count; ++i)
    {
        Item item = self.SlotItems[i];
        if (item.ConfigId == configId)
        {
            if (item.Count <= remainCount)
            {
                // 完全移除该物品
                NotifyItemRemove(self, item);  // Count=0表示移除
                item.Dispose();
            }
            else
            {
                // 部分移除，减少Count
                item.ReduceCount(remainCount);
                NotifyItemUpdate(self, item);
            }
        }
    }
}
```

**关键特性**：
- 先验证物品数量是否足够
- 支持跨槽位移除（自动处理多个槽位）
- 自动通知客户端移除或更新
- 完全移除时销毁物品实体

#### MoveItem 方法实现
```csharp
public static int MoveItem(ItemComponent self, long itemId, int toSlot)
{
    // 1. 获取源物品
    Item fromItem = self.GetItemById(itemId);
    int fromSlot = fromItem.SlotIndex;
    
    // 2. 获取目标槽位物品
    Item toItem = self.GetItemBySlot(toSlot);
    
    // 3. 情况1：目标槽位为空 -> 直接移动
    if (toItem == null)
    {
        self.ClearSlot(fromSlot);
        self.SetSlotItem(toSlot, fromItem);
        NotifyItemUpdate(self, fromItem);
        return ErrorCode.ERR_Success;
    }
    
    // 4. 情况2：ConfigId不同 -> 交换位置
    if (fromItem.ConfigId != toItem.ConfigId)
    {
        SwapItems(self, fromSlot, toSlot, fromItem, toItem);
        return ErrorCode.ERR_Success;
    }
    
    // 5. 情况3：ConfigId相同且可堆叠 -> 尝试堆叠
    int maxStack = itemConfig.MaxStack;
    int canStackCount = maxStack - toItem.Count;
    
    if (canStackCount > 0)
    {
        int stackCount = Math.Min(canStackCount, fromItem.Count);
        toItem.AddCount(stackCount);
        fromItem.ReduceCount(stackCount);
        
        if (fromItem.Count <= 0)
        {
            NotifyItemRemove(self, fromItem);
            fromItem.Dispose();
        }
        else
        {
            NotifyItemUpdate(self, fromItem);
        }
        NotifyItemUpdate(self, toItem);
    }
}
```

**关键特性**：
- 支持三种操作：移动、交换、堆叠
- 智能判断目标槽位状态
- 部分堆叠时保留源物品
- 完全堆叠时销毁源物品

#### SortBag 方法实现
```csharp
public static int SortBag(ItemComponent self)
{
    // 第一步：收集所有物品信息，按ConfigId分组统计数量
    Dictionary<int, int> configIdToCount = new();
    foreach (Item item in self.SlotItems)
    {
        configIdToCount[item.ConfigId] += item.Count;
    }
    
    // 第二步：通知客户端清空所有旧槽位（Count=0）
    foreach (Item item in self.SlotItems)
    {
        M2C_UpdateItem clearMessage = M2C_UpdateItem.Create();
        clearMessage.Count = 0;  // 清空标记
        MapMessageHelper.NoticeClient(unit, clearMessage, NoticeType.Self);
    }
    
    // 第三步：销毁所有旧物品
    foreach (Item item in self.SlotItems)
    {
        item?.Dispose();
    }
    
    // 第四步：按ConfigId排序，重新创建物品堆
    List<int> sortedConfigIds = new(configIdToCount.Keys);
    sortedConfigIds.Sort();  // 升序排列
    
    int currentSlot = 0;
    foreach (int configId in sortedConfigIds)
    {
        int maxStack = itemConfig.MaxStack;
        int remainCount = configIdToCount[configId];
        
        // 创建物品堆，每堆最多maxStack个
        while (remainCount > 0)
        {
            int stackCount = Math.Min(remainCount, maxStack);
            Item newItem = self.AddChild<Item>();
            newItem.ConfigId = configId;
            newItem.Count = stackCount;
            self.SetSlotItem(currentSlot, newItem);
            remainCount -= stackCount;
            currentSlot++;
        }
    }
    
    // 第五步：通知客户端新物品
    foreach (Item item in self.SlotItems)
    {
        NotifyItemUpdate(self, item);
    }
}
```

**关键特性**：
- 完整重组背包结构
- 按ConfigId升序排列
- 自动堆叠相同物品
- 从槽位0开始紧密排列
- 消除背包中的空隙

### ItemComponentSystem - 数据查询层

**服务端职责**：
- 纯数据查询和管理
- 不涉及网络消息
- 提供基础操作接口

**主要方法**：
```csharp
// 查询类方法
GetItemCount(configId)      // 获取物品总数量
GetItemBySlot(slotIndex)    // 获取指定槽位物品
GetItemById(itemId)         // 通过ID获取物品
FindEmptySlot()             // 查找空槽位
GetUsedSlotCount()          // 获取已使用槽位数
IsFull()                    // 检查背包是否已满

// 管理类方法
SetCapacity(capacity)       // 设置背包容量
SetSlotItem(slotIndex, item) // 设置槽位物品
ClearSlot(slotIndex)        // 清空槽位
Clear()                     // 清空背包
```

### 客户端 ItemComponentSystem

**客户端职责**：
- 接收服务端推送的物品更新
- 维护本地物品缓存
- 提供UI查询接口

**关键方法**：
```csharp
// 更新方法
UpdateItem(itemId, slotIndex, configId, count)
// 当count=0时表示移除物品
// 当count>0时表示更新或创建物品

// 查询方法（与服务端相同）
GetItemBySlot(slotIndex)
GetItemById(itemId)
GetItemCount(configId)
```

## 版本历史

### 1.7.0 (当前版本)
- **完善代码实现**：详细记录ItemHelper各方法的实现逻辑
- 添加AddItem、RemoveItem、MoveItem、SortBag的完整实现说明
- 说明各方法的关键特性和处理流程
- 补充ItemComponentSystem和客户端系统的职责说明
- 优化代码文档，便于开发者理解系统设计

### 1.6.0
- **新增功能**：整理背包功能（SortBag）
- 新增ItemHelper.SortBag方法，支持一键整理背包
- 相同ConfigId的物品自动堆叠并按ConfigId升序排列
- 从槽位0开始紧密排列，消除空隙
- 新增C2M_SortBagHandler消息处理器
- 完善README文档，添加详细的整理背包功能说明
- 整理效果类似魔兽世界的整理背包功能

### 1.5.0
- **架构重构**：职责分离，创建ItemHelper辅助类
- 将涉及消息通知的方法从ItemComponentSystem迁移到ItemHelper
- ItemComponentSystem保持简洁，只负责数据查询和简单管理
- ItemHelper负责处理复杂业务逻辑和消息通知
- 优化代码组织结构，提高可维护性
- 迁移的方法：AddItem、RemoveItem、RemoveItemById、MoveItem、SetCapacity、NotifyItemUpdate、NotifyItemRemove、NotifyItemChanges、NotifyCapacityChange、SwapItems

### 1.4.0
- **重大重构**：从基于SlotIndex改为基于Item.Id的物品追踪机制
- 所有客户端请求（UseItem、MoveItem、DiscardItem）改用ItemId参数
- 新增GetItemById方法，支持通过ItemId直接获取物品
- 优化M2C_UpdateItem协议，添加ItemId字段
- ItemData结构添加ItemId字段，保留SlotIndex用于UI显示
- 统一使用Count=0表示物品移除或槽位清空
- 客户端Item Entity使用AddChildWithId保持与服务端ID一致
- 完善物品追踪和生命周期管理

### 1.3.0
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
2. **使用ItemHelper处理业务逻辑**：涉及消息通知的操作（添加、移除、移动物品等）应使用ItemHelper，而非直接调用ItemComponentSystem
3. **自动通知机制**：ItemHelper的AddItem、RemoveItem和MoveItem方法会自动通知客户端，无需手动调用NotifyItemChanges
4. **数据查询使用ItemComponentSystem**：纯数据查询（如GetItemCount、IsFull等）使用ItemComponentSystem的方法
5. **物品使用**：客户端通过UseItem请求（使用ItemId），服务端验证后调用ItemHelper.RemoveItem执行
6. **物品移动**：客户端通过MoveItem请求（使用ItemId），服务端验证后调用ItemHelper.MoveItem执行移动或堆叠操作
7. **数据同步**：客户端可通过SyncBagData获取完整背包数据，用于登录或重连后的数据恢复
8. **命名空间区分**：客户端使用ET.Client.Item和ET.Client.ItemComponent，服务端使用ET.Server.Item和ET.Server.ItemComponent
9. **EntityRef使用**：客户端ItemComponent使用Dictionary<int, EntityRef<Item>>管理物品，遵循ET框架规范
10. **堆叠限制**：物品是否可堆叠由配置的MaxStack字段控制，MaxStack=1表示不可堆叠
11. **ItemId追踪**：所有物品操作都使用ItemId，便于追踪物品生命周期和问题排查
12. **Count=0语义**：Count=0统一表示物品移除或槽位清空，ItemId永远不为0
13. **ID一致性**：客户端使用AddChildWithId创建Item Entity，确保与服务端ID一致
14. **整理背包**：整理背包会重新分配所有槽位，所有旧ItemId失效，客户端会收到完整更新