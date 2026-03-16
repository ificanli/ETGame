# M0 局外起装 UI 对接计划

## 前置说明

服务端逻辑已全部完成（T0~T8 测试通过，dotnet build 0 错误）。
UI 层只需读取客户端 Component 数据、发送已有 Proto 消息，**不需要改任何服务端代码**。

所有 UI 代码放在 `HotfixView` 层。

---

## 数据速查

| 数据 | 来源 |
|------|------|
| 英雄列表 | `LoadoutComponent.Heroes`（`List<HeroInfo>`） |
| 当前选中英雄 | `LoadoutComponent.SelectedHeroConfigId` |
| 是否已确认起装 | `LoadoutComponent.IsConfirmed` |
| 当前装备槽状态 | 监听 `M2C_UpdateEquipment` |
| 撤离结算数据 | `Wait_M2C_EvacuationSettlement.M2C_EvacuationSettlement` |
| 死亡结算数据 | `Wait_M2C_DeathSettlement.M2C_DeathSettlement` |

---

## 任务列表

### UI-01 请求英雄列表

**界面**：起装大厅入口
**触发时机**：玩家进入起装大厅界面时

```
发送 C2G_GetHeroList
  ↓
收到 G2C_GetHeroList.Heroes
  ↓
写入 Scene.GetComponent<LoadoutComponent>().Heroes
  ↓
通知 UI 刷新英雄列表
```

**完成标准**：`LoadoutComponent.Heroes` 有数据，列表能正常展示。

---

### UI-02 英雄选择面板

**界面**：起装大厅 — 左侧英雄列表
**数据源**：`LoadoutComponent.Heroes`

交互逻辑：
- 遍历 `Heroes` 列表，每个 `HeroInfo` 生成一个英雄卡片
- 点击卡片 → 更新 `LoadoutComponent.SelectedHeroConfigId`
- 当前选中项高亮显示
- `HeroInfo` 字段：`HeroConfigId`、`Name`、`UnitConfigId`

**完成标准**：能选中英雄，选中状态正确高亮。

---

### UI-03 装备槽面板

**界面**：起装大厅 — 右侧装备槽
**4 个槽位**：

| 槽位 | EquipmentSlotType | 说明 |
|------|-------------------|------|
| 主武器 | MainHand (6) | 必填 |
| 副武器 | OffHand (7) | 可选 |
| 护甲 | Chest (2) | 可选 |
| 消耗品 | Consumable1 (10) | 可选 |

交互逻辑：
- 点击槽位 → 弹出该槽位可用装备列表（从 EquipmentConfigCategory 过滤 EquipSlot 匹配的）
- 选中装备 → 槽位显示装备图标和名称
- 未选中时显示空槽占位图

**完成标准**：4 个槽位能独立选择装备，选中后正确显示。

---

### UI-04 确认起装按钮

**界面**：起装大厅 — 底部按钮区
**前置条件**：已选英雄 + 主武器槽位不为空

```
点击"确认起装"
  ↓
构造 C2G_ConfirmLoadout：
  HeroConfigId = SelectedHeroConfigId
  MainWeaponConfigId = 槽位选中值
  SubWeaponConfigId = 槽位选中值（0 表示未选）
  ArmorConfigId = 槽位选中值（0 表示未选）
  ConsumableConfigIds = 消耗品槽位选中值列表
  ↓
发送请求，等待 G2C_ConfirmLoadout
  ↓
成功 → LoadoutComponent.IsConfirmed = true
       解锁"进入地图"按钮
失败 → 弹出错误提示（ERR_LoadoutHeroNotFound / ERR_LoadoutSlotMismatch 等）
```

**完成标准**：确认成功后进图按钮可点击，失败有提示。

---

### UI-05 游戏内装备栏 HUD

**界面**：游戏内 HUD — 屏幕角落装备栏
**触发时机**：进入地图后初始化，之后监听更新

```
进入地图
  ↓
读取初始装备状态（从 M2C_UpdateEquipment 或本地 LoadoutComponent）
  ↓
显示 4 个槽位图标

监听 M2C_UpdateEquipment
  ↓
刷新对应槽位图标
```

`M2C_UpdateEquipment` 字段：`EquipmentData`（含 `SlotIndex`、`ConfigId`、`Count`）

**完成标准**：进图后装备槽正确显示，装备变化时实时刷新。

---

### UI-06 撤离结算弹窗

**界面**：撤离成功后弹出
**触发时机**：收到 `M2C_EvacuationSettlement`

```
Wait_M2C_EvacuationSettlement result =
    await scene.GetComponent<ObjectWait>().Wait<Wait_M2C_EvacuationSettlement>();

M2C_EvacuationSettlement data = result.M2C_EvacuationSettlement;
```

展示内容：
- 标题："撤离成功"
- 物品列表：遍历 `data.Items`，每行显示 `ConfigId` 对应图标 + 名称 + `Count`
- 总财富：`data.TotalWealth`
- 确认按钮：关闭弹窗

**完成标准**：弹窗正确展示带出的物品和财富值。

---

### UI-07 死亡结算弹窗

**界面**：玩家死亡后弹出
**触发时机**：收到 `M2C_DeathSettlement`

```
Wait_M2C_DeathSettlement result =
    await scene.GetComponent<ObjectWait>().Wait<Wait_M2C_DeathSettlement>();
```

展示内容：
- 标题："阵亡"
- 提示文字："所有装备和物品已丢失"
- 确认按钮：关闭弹窗，返回大厅

**完成标准**：死亡后弹窗正确弹出，确认后流程正常。

---

## 开发顺序建议

```
UI-01 请求英雄列表
  ↓
UI-02 英雄选择面板
  ↓
UI-03 装备槽面板
  ↓
UI-04 确认起装按钮    ← 起装大厅完整可用
  ↓
UI-05 游戏内装备 HUD  ← 进图后可见
  ↓
UI-06 撤离结算弹窗
  ↓
UI-07 死亡结算弹窗    ← 全流程打通
```

---

## 约束提醒

- 所有 UI 代码在 `HotfixView` 层，不写在 `Hotfix` 层
- 不修改任何服务端文件
- 不修改 `LoadoutComponent`（Model 层），只读取数据
- 错误码参考 `cn.etetet.equipment/Scripts/Model/Share/ErrorCode.cs`

---

最后更新时间：2026-02-27
