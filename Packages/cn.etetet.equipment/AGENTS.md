# cn.etetet.equipment

## 概述

`cn.etetet.equipment` 负责玩家装备、局外起装正式状态、仓库与跑局结果回写相关的服务端权威逻辑。

## 开发约定

- `LoadoutComponent`、`PlayerStorageComponent` 只存状态，不写业务方法。
- 起装、撤离、阵亡等复杂流程放在 `Helper` 中，`Handler` 只做协议编排与错误返回。
- 任何修改玩家正式起装状态或仓库状态的服务端入口，都要考虑 Gate 侧串行化与 `IsConfirmed` 失效逻辑。

## 依赖与边界

- 依赖以 `package.json` 为准，禁止跨包直接访问未声明依赖的符号。
- 本包可以依赖 `cn.etetet.item` 提供二维格子校验与背包物品描述，但不在本包复制底层格子算法。
- 本包负责“当前携带态 / 仓库 / 财富 / 跑局结果”的权威状态，不直接承接地图层的通用同步职责。
