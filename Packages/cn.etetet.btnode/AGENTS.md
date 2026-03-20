# cn.etetet.btnode

## 概述

`cn.etetet.btnode` 是 ET 行为树业务节点扩展包，承载怪物、宠物、机器人等 AI 的节点定义与运行逻辑。

## 开发约定

- 运行时行为树逻辑优先放在 `Scripts/Hotfix`，节点配置/数据定义按 ET 分层放在 `Model` 或 `ModelView`。
- 行为树节点遵循 ET 架构约束：Entity 只存状态，逻辑放在对应 `System` / `Handler` 中。
- 涉及 `await` 的 AI 协程不能跨 `await` 持有旧 `Entity`、`Component`、`TimerComponent` 直接继续使用。
- `await` 前如需保留引用，请保存 `EntityRef<T>` 或普通值；`await` 后必须重新取回实体并判空/判 `IsDisposed`。
- 定时等待优先通过 `Scene`/`Root` 重新获取 `TimerComponent`，不要把 `TimerComponent` 跨协程挂起缓存起来。

## 边界说明

- 跨包访问必须以 `package.json` 声明的依赖为准，禁止绕过包边界直接访问未声明模块。
- AI 节点里的技能、Buff、仇恨、寻路调用要保持单一职责，避免在单个节点里堆叠过多业务分支。
