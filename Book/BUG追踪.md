# BUG 追踪

> 记录已知 Bug 和待修复问题，按模块分组。
> 格式：`[状态] 描述 — 发现时间 / 发现人 / 备注`
> 状态：🔴 待修复 | 🟡 修复中 | ✅ 已修复

---

## 搜索系统

| ID | 状态 | 描述 | 发现时间 | 根本原因 | 修复建议 |
|----|------|------|---------|---------|---------|
| BUG-005 | 🔴 待修复 | **一键获取 X 品质以上物品无效** — 点击"一键获取"后，UI 上设置的品质过滤无法生效，所有品质的物品都会被取走（或什么都取不到） | 2026-03-26 | **搜索动效未完成时无法取物**：`TakeQualifiedItems` 方法过滤时读取 `runtime.ContainerItems` 是正确的，但物品在搜索动效期间（500ms~2500ms）仍处于遮罩状态；**若搜索动效未结束就点按钮，`SearchedSlots` 中没有该 slot**，但代码里没有对此做判断，会直接发送 `TakeItem` 请求 → 服务端可以正常处理。**真实 Bug 更可能是 Dropdown 没绑定正确**：`QuickChooseDropdown` 通过 `GetComponentInChildren<Dropdown>()` 获取，若 Prefab 里的下拉控件找不到（或 value 未同步），`QuickChooseMinQuality` 会一直是 1，导致"全部都取走了"而非"只取高品质" | 1. 检查 SearchPanel.prefab 中 Dropdown 是否存在且可被 GetComponentInChildren 找到；2. 在 `OnEventClickQuickChooseInvoke` 前加日志确认 `QuickChooseMinQuality` 的实际值；3. 可选：在 `TakeQualifiedItems` 中加入 `SearchedSlots.Contains(slot)` 判断，只取已搜索完成的物品 |
| BUG-006 | 🔴 待修复 | **怪物死亡后尸体搜索阶段仍显示血条** — 怪物被击杀后，原 Unit 销毁、MonsterCorpse 创建，但屏幕上怪物位置残留血条显示 | 2026-03-26 | **根本原因**：`HPViewComponentSystem.Destroy` 与 `HPView3DComponentSystem.Destroy` 方法体均为空，Unit Dispose 时血条 UIBase 未归位到 UICache、3D 血条未隐藏，导致残留渲染。LateUpdate 路径依赖下一帧执行，但 Dispose 后 Entity 不再进入 LateUpdate，残留永久悬浮 | 详见 `Book/BUG修复/BUG-006-怪物死亡尸体残留血条修复方案.md`。改动 2 个文件：`HPViewComponentSystem.Destroy` + `HPView3DComponentSystem.Destroy` 各加防御性隐藏逻辑 |

---

## 武器系统

| ID | 状态 | 描述 | 发现时间 | 备注 |
|----|------|------|---------|------|
| BUG-001 | 🔴 待修复 | **武器切换 UI 显示异常** — 切换武器后 UI 表现有问题（具体表现待补充） | 2026-03-26 | 切换逻辑服务端 ✅，问题在客户端 UI 层 |

---

## 结算系统

| ID | 状态 | 描述 | 发现时间 | 备注 |
|----|------|------|---------|------|
| BUG-002 | 🔴 待修复 | **结算面板击杀数永远显示 0** — `M2C_EvacuationSettlement` 和 `M2C_DeathSettlement` 两条链路均硬编码传 `killNum = 0` | 2026-03-26 | 修复方式：`EvacuationSettlementHelper.Settle()` 中读取玩家击杀计数组件 |

---

## 肉鸽系统

| ID | 状态 | 描述 | 发现时间 | 备注 |
|----|------|------|---------|------|
| BUG-003 | 🔴 待修复 | **ShowTagsBuffId 只配了 1 档** — 达到 2 张或 3 张流派效果时均触发同一个 Buff，无质量梯度区分 | 2026-03-26 | 配置层问题，需补充 2 档和 3 档 BuffId |
| BUG-004 | ✅ 已修复 | **持续同向移动权威拉回抖动** — 摇杆持续同向移动时服务端权威拉回导致客户端抖动 | 2026-03-26 | 2026-03-27 已完成客户端预测、贴墙约束、停步回放与权威融合修正，用户实机验收通过 |

---

## 需求追踪（Feature Backlog）

| ID | 优先级 | 描述 | 提出时间 | 备注 |
|----|--------|------|---------|------|
| FEAT-001 | 🟠 中 | **长按丢弃武器** — 玩家长按武器槽位图标，触发确认/直接丢弃当前武器到地面 | 2026-03-26 | 需要：长按手势检测（UI 层）+ 服务端武器移除逻辑 + 武器掉落到地面（Item Drop） |
