# ECA配置ID补齐与重复修复设计文档

**创建时间**：2026-04-16
**最后更新**：2026-04-16
**状态**：已完成
**关联任务**：M0.2-W3 #30
**涉及包**：cn.etetet.eca

## 需求概述

当前编辑器菜单 `ET/ECA/Fill Missing ConfigIds` 只会给场景里的 `ECAPointMarker` 补齐空的 `ConfigId`，如果场景里已经存在重复 `ConfigId`，工具只会提示重复，仍需人工逐个改名。

这会导致两个问题：

1. 用户以为点了“补 ID”后场景已经健康，实际上重复 ID 仍保留。
2. 场景点位一多时，手工排查和重命名重复项效率很低，而且容易漏改。

本次目标是把该工具增强为“补空 + 修重复”一体化工具，在不改运行时语义的前提下，自动把重复 `ConfigId` 修成唯一值。

## 技术方案

### 整体思路

继续复用现有 `ApplyECAConfigIdEditor.FillMissingConfigIds()` 菜单入口，不新增第二个菜单项，避免用户记两套工具。

执行流程调整为：

1. 收集当前场景全部 `ECAPointMarker`，并按层级路径排序，保证结果稳定。
2. 首轮扫描保留每个 `ConfigId` 的第一个出现项，后续同值项判定为“待修复重复项”。
3. 用现有规则为“空 `ConfigId`”和“重复 `ConfigId` 的后续项”统一分配新 ID。
4. 保留 Undo、SceneDirty 和结果弹窗，让用户可以直接撤销或查看修复统计。

### 涉及的包和文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Packages/cn.etetet.eca/Editor/ECAEditor/ApplyECAConfigIdEditor.cs` | 修改 | 扩展工具逻辑，新增重复 `ConfigId` 自动修复 |
| `Book/07-ECA框架/ECA2/ECA配置ID补齐与重复修复设计文档.md` | 新增 | 记录本次工具增强方案 |
| `Book/07-ECA框架/ECA2/ECA配置ID补齐与重复修复开发日志.md` | 新增 | 记录开发过程与验证结论 |
| `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 补任务登记 |
| `Book/08-版本计划/M0.2版本计划.md` | 修改 | 追加变更记录 |

### 数据与修复规则

1. `ConfigId` 判空仍按 `string.IsNullOrWhiteSpace` 处理。
2. 重复判断使用 `Trim()` 后的值，避免首尾空格导致“看起来相同、实际上不同”。
3. 对同一个重复值：
   - 保留排序后的第一个点位原值不变。
   - 后续重复点位按现有 `eca_{scene}_{pointType}_{index:D4}` 规则重新分配。
4. 新分配的 ID 必须避开场景里所有保留值，保证一次执行后场景内全唯一。

### 风险与取舍

1. 对重复组只保留第一个原值，不尝试猜测“哪个才是正确项”。
原因：工具层无法判断业务语义，最稳的是做确定性、可撤销的最小修复。
2. 不额外修改唯一但带空格的 `ConfigId` 原文。
原因：本次范围聚焦“补空”和“修重复”，避免扩大为格式化工具。

## 实现步骤

1. 补计划与设计/开发文档门禁。
2. 修改 `ApplyECAConfigIdEditor`，把重复 `ConfigId` 后续项纳入自动分配列表。
3. 更新弹窗与日志输出，补充重复修复统计。
4. 进行最小验证，并回写实现追踪。

## 验收标准

- [ ] 执行 `ET/ECA/Fill Missing ConfigIds` 后，空 `ConfigId` 能自动补齐。
- [ ] 执行同一工具时，重复 `ConfigId` 的后续项会自动修复为唯一值。
- [ ] 修复结果可撤销，且会标记场景脏状态。
- [ ] 工具完成后弹窗明确显示补空数量和重复修复数量。

## 关联文档

- [ECA框架使用指南-场景配置.md](./ECA框架使用指南-场景配置.md)

## 实现追踪

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 步骤1 | 2026-04-16 | `Book/08-版本计划/M0.2-W3周计划.md`、`Book/08-版本计划/M0.2版本计划.md`、`Book/07-ECA框架/ECA2/ECA配置ID补齐与重复修复设计文档.md`、`Book/07-ECA框架/ECA2/ECA配置ID补齐与重复修复开发日志.md` | 无偏差 |
| 步骤2 | 2026-04-16 | `Packages/cn.etetet.eca/Editor/ECAEditor/ApplyECAConfigIdEditor.cs` | 无偏差 |
| 步骤3 | 2026-04-16 | `Packages/cn.etetet.eca/Editor/ECAEditor/ApplyECAConfigIdEditor.cs` | 输出改为同时显示补空数量与重复修复数量，无额外偏差 |
| 步骤4 | 2026-04-16 | `ET.sln` | `dotnet build ET.sln` 通过，等待 Unity 编辑器内人工回归 |
