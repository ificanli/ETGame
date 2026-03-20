# cn.etetet.statesync

## 概述

`cn.etetet.statesync` 是项目的玩法与演示整合包，包含场景资源、ECA 配置资产与状态同步相关业务脚本。

## 开发约定

- 本包代码放在 `Scripts/` 下，按 ET 分层目录组织（`Model` / `ModelView` / `Hotfix` / `HotfixView`）。
- ECA 相关资产优先放在 `Assets/ECA/`，命名应与交互物语义一致。
- 流程图节点遵循“显示层与业务层分离”原则：
  - 进入/离开范围只做提示显示（如 `ShowInteractButton` / `HideInteractButton`）。
  - 交互后的业务流程放在 `OnPlayerInteract` 触发链路中（如 `StartSearchTimer`、`OpenContainerUI`）。
- 起装界面的来源列表使用 `u_ComEquipBagScroll`，当前支持 `商店/仓库` 两种来源切换。
- 若 prefab 已提供来源页签，优先使用名字为 `LoadoutSourceToggleRoot`、`LoadoutShopButton`、`LoadoutWarehouseButton` 的节点；若未提供，代码会在运行时创建兜底按钮。

## 依赖与边界

- 依赖以 `package.json` 为准，禁止跨包越级访问未声明依赖的符号。
- 变更 ECA 资产时，优先保持与 `cn.etetet.eca`、`cn.etetet.ecanode` 的动作语义一致，避免在资产层硬编码业务逻辑。
