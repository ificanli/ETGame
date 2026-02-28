# Match 匹配系统

## 包信息
- **包名**: cn.etetet.match
- **PackageType ID**: 54
- **层级**: 第3层
- **依赖**: cn.etetet.core, cn.etetet.proto, cn.etetet.startconfig, cn.etetet.netinner

## 功能概述
Match系统负责玩家匹配功能，支持多种游戏模式的排队匹配。

### 游戏模式
- PVE (1): 单人PVE
- 1v1 (2): 双人对战
- 3v3 (3): 三对三团战，6人
- 搜打撤 (4): 多人PVP+PVE，默认6人

## 核心组件

### Entity
- **MatchQueueComponent**: 匹配队列组件，挂在Match Scene上
- **MatchRequest**: 匹配请求Entity，代表一个排队中的玩家

### System
- **MatchQueueComponentSystem**: 队列管理逻辑
- **MatchTickTimer**: 定时器，每秒执行匹配和超时清理

### 常量
- **GameModeType**: 游戏模式常量
- **MatchState**: 匹配状态常量（Waiting, Matched, Timeout, Cancelled）

## 匹配流程
1. 客户端发送匹配请求到Gate
2. Gate转发到Match Scene
3. Match Scene创建MatchRequest加入队列
4. 定时器每秒尝试匹配，FIFO先到先得
5. 凑够人数后产出MatchResult
6. 发布MatchSuccessEvent事件
7. 通知Gate推送给客户端

## 测试用例
已实现4个测试用例，覆盖核心功能：
- **Match_BasicFlow_Test**: 基础流程测试（2人1v1匹配）
- **Match_Cancel_Test**: 取消匹配测试
- **Match_Timeout_Test**: 超时清理测试
- **Match_Extraction_Test**: 搜打撤模式测试（4人匹配）

运行测试：通过ET框架的测试系统执行

## 已知问题
⚠️ **定时器功能暂时注释**: ET0037分析器限制了TimerComponent的直接访问。需要配置分析器规则或使用其他方式访问TimerComponent。

✅ **Proto消息ID已解决**: 使用22000/22010系列ID，已避免冲突。

## 开发规范
- 遵循ET框架ECS架构
- Entity负责数据，System负责逻辑
- 使用定时器驱动匹配逻辑
- 支持取消匹配和超时处理（30秒）
