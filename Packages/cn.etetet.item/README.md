# ET.Item

ET框架物品背包系统模块，提供完整的物品管理、背包容量控制和物品堆叠功能。

## 功能特性

- **物品管理**：支持物品的添加、移除、使用等完整操作
- **背包系统**：支持背包容量管理、槽位管理
- **物品堆叠**：支持可堆叠物品的自动堆叠
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

## 使用示例

```csharp
// 添加物品到背包
bool success = itemComponent.AddItem(configId: 10001, count: 10);

// 移除物品
bool success = itemComponent.RemoveItem(configId: 10001, count: 5);

// 获取物品数量
int count = itemComponent.GetItemCount(configId: 10001);

// 检查背包是否已满
bool isFull = itemComponent.IsFull();

// 获取指定槽位的物品
Item item = itemComponent.GetItemBySlot(slotIndex: 0);
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

协议定义位于 `Proto/Item_C_10600.proto`，包含：

- 添加物品请求/响应
- 移除物品请求/响应
- 使用物品请求/响应
- 背包数据同步
- 物品移动/交换
- 物品丢弃
- 背包整理

## 依赖

- cn.etetet.core
- cn.etetet.excel
- cn.etetet.proto
- cn.etetet.unit

## 版本历史

### 1.0.0
- 初始版本
- 支持基础物品管理
- 支持物品堆叠
- 背包槽位管理
- 前后端数据同步