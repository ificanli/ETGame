# cn.etetet.archive

## 概述

`cn.etetet.archive` 是玩家数据平台包，负责统一维护玩家档案、仓库与财富快照，以及战绩和事件时间轴复盘数据。

## 当前阶段

- 当前实现为 **运行时内存仓储**。
- Gate/Match 等业务包一律通过 `Archive` 服务访问玩家档案，不直接触碰底层存储实现。
- 后续切换 Mongo 时，只替换仓储实现与服务内部落库逻辑，不改业务调用协议。

## 责任边界

- `ArchiveManagerComponent`：档案服务入口，按 `Account` 管理玩家档案。
- `PlayerArchive`：玩家档案聚合根，维护仓库、财富、上一局摘要和当前进行中的战局上下文。
- `PlayerBattleRecord`：单场战绩详情，包含摘要字段和事件时间轴。
- `ArchiveMessageHelper`：Gate 侧访问 `Archive` 服务的统一入口。

## 开发约定

- 包内 Entity 只存状态，所有读写逻辑在 System 中。
- 档案服务只接受标准化快照/结果，不依赖具体玩法包的业务 Helper。
- 战绩的“回放”当前定义为事件时间轴复盘，不做逐帧播放。
