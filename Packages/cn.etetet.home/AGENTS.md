# cn.etetet.home - 基地建设系统

> **基线说明（2026-04-13）**：本文件保留包结构与集成点说明，但家园系统的当前正式需求基线已切换到 `Book/06-家园系统/家园系统需求文档.md`。
> 遇到建筑体系、页面结构、正式版范围判断时，请优先阅读当前基线文档和同目录下的 UI / 开发日志文档，不再以旧一期方案为准。

## 概述

Home 基地建设系统，服务于搜打撤主循环的局外基地养成系统。

## 包信息

- **Id**: 56
- **Level**: 5
- **AllowSameLevelAccess**: true
- **依赖**: core, excel, proto, unit, item

## 核心功能

1. 玩家进入 Home 私有副本后加载基地数据
2. 固定槽位建造/升级/拆除建筑（即时完成）
3. 被动产出收取（惰性结算）
4. 工坊加工（消耗局内带出材料）
5. 情报站悬赏合同（每日刷新出局目标）
6. 角色伤势/装备磨损系统

## 架构设计

### 数据模型
- `PlayerHomeComponent` — 基地元信息，挂在 Unit 上
- `HomeBuilding` — 建筑子Entity，挂在 PlayerHomeComponent 下
- `HomeProductionComponent` — 加工订单管理，挂在 Unit 上
- `HomeContractComponent` — 悬赏合同管理，挂在 Unit 上

### 持久化
- 使用 DB 持久化（不使用 ITransfer）
- 进入 Home 时从 DB 加载，操作后即时保存

### 业务逻辑
- Helper 类承载具体业务逻辑（HomeBuildHelper、HomeUpgradeHelper 等）
- System 类只负责生命周期管理
- Handler 类处理协议消息

## 目录结构

```
Scripts/
├── Model/
│   ├── Share/     — PackageType、ErrorCode、状态枚举
│   ├── Server/    — 服务端 Entity/Component 定义
│   └── Client/    — 客户端镜像组件
├── Hotfix/
│   ├── Server/
│   │   ├── Helper/   — 业务逻辑
│   │   ├── System/   — 生命周期
│   │   └── Handler/  — 协议处理
│   ├── Client/
│   │   ├── Handler/  — 客户端协议处理
│   │   └── System/   — 客户端组件系统
│   └── Test/         — 测试用例
├── HotfixView/Client/ — UI交互层
└── ModelView/Client/  — 场景交互层
```

## 集成点

- `M2M_UnitTransferRequestHandler` — 进入 Home 时调用 HomeEnterHelper
- `ItemHelper` — 资源扣除/添加
- `LobbyPanel` — 基地页签交互

## 需求文档

详细需求见 `Book/06-家园系统/家园系统需求文档.md`
