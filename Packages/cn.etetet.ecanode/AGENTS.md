# cn.etetet.ecanode

## 概述

ECA 业务节点扩展包，提供游戏业务相关的 ECA 节点实现。

## 包信息

- **ID**: 53
- **Level**: 5
- **AllowSameLevelAccess**: true

## 节点类型

### 撤离点（Evacuation Point）
- PlayerEvacuationComponent
- PlayerEvacuationComponentSystem
- 功能：玩家进入范围后倒计时撤离，完成后跳转回 Lobby

### 刷怪点（Spawn Monster）
- SpawnMonsterComponent
- SpawnMonsterComponentSystem
- 功能：在指定位置刷新怪物

### 容器（Container）
- ContainerComponent
- ContainerComponentSystem
- 功能：可交互的容器，玩家可以打开获取物品

## 依赖关系

- cn.etetet.eca (核心框架)
- cn.etetet.map (地图系统)
- cn.etetet.move (移动系统)

## 开发约定

- 所有业务节点的 Component 放在 Model/Server
- 所有业务节点的 System 放在 Hotfix/Server
- 命名空间使用 ET.Server
