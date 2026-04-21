# 我们的架构分析：ETGame 移动系统

## 概述

ETGame 采用 **ET 框架 ECS 架构**，移动系统是**完全服务器权威**模式，配合**客户端预测 + 权威校正 + 视觉插值**三层机制。

---

## 架构核心

| 模块 | 权威方 | 同步方式 |
|------|--------|----------|
| 移动位置 | **服务器** | 16ms Tick → M2C_JoystickMove 广播 |
| 输入 | 客户端上报 | C2M_JoystickInput (ILocationMessage) |
| NavMesh 碰撞 | **服务器 + 客户端双端** | PathfindingComponent |
| 视觉渲染 | 客户端本地 | UnitViewInterpolationComponent |

### 数据流

```
客户端输入:
  JoystickView → EventMain_JoystickInput → InputSystemComponent
  → SyncDirectionalMove() [33ms节流] → C2M_JoystickInput

服务器处理:
  C2M_JoystickInputHandler → JoystickMoveComponent.SetDirection()
  → HighFrequencyScheduler [16ms Tick]
  → TickFixedStep(): 速度计算 + NavMesh约束 + 位置更新
  → M2C_JoystickMove 广播 (Self + BroadcastWithoutSelf)

客户端接收:
  M2C_JoystickMoveHandler → 序列号去重 → 更新权威位置
  → ChangePosition事件 → UnitViewInterpolationComponent
  → 本地预测 + 权威校正 + 视觉平滑
```

---

## 优点

### 1. 完全服务器权威——安全可靠
- 所有位置计算在服务器端完成
- 客户端只上报方向，不上报位置
- 序列号机制防止旧包重放
- **天然防作弊**：飞行/加速/穿墙全部无效

### 2. NavMesh 双端校验——防穿墙
- 服务器端：`PathfindingComponent.TryMoveAlongSurface()` 约束移动
- 客户端预测：同样使用 NavMesh 约束
- `EnableNavRaycast` 开关可控
- 墙壁滑行支持（不是硬停）

### 3. 客户端预测——低延迟手感
- 自己的 Unit：本地输入立即驱动预测位置
- 不等服务器响应，视觉上零延迟
- 预测限额：不超过权威位置 0.2s × speed 的距离
- 权威校正渐进式（CorrectionRate = 8.0/s），不抖动

### 4. 高频服务器 Tick
- 移动 Tick: **16ms（62.5Hz）**
- 独立于主循环，不受帧率影响
- 3帧追赶机制防止丢帧堆积
- 精确的 FixedDeltaTime 计算

### 5. 输入节流——节省带宽
- 33ms 最小发送间隔
- 方向无变化不发送
- 急转弯（>90° 变化）立即发送
- 停止和启动立即发送

### 6. 完善的序列号机制
- `InputSequence`：客户端输入去重
- `MoveSequence`：服务器广播去重
- `LastProcessedInputSequence`：回声确认
- 防止网络乱序导致的位置跳变

### 7. 远程玩家外推
- 根据服务器广播的速度和方向外推
- 新位置到达时平滑校正
- 动画速度与实际视觉移动速度同步

### 8. 分布式架构支持
- 基于 ET 框架 Actor Location 消息模型
- 支持多 Map 服务器
- 支持跨进程 Transfer
- 天然支持大规模在线

---

## 缺点

### 1. 服务器广播频率过高——核心瓶颈
- **每个移动中的 Unit 每 16ms 广播一次 M2C_JoystickMove**
- 每个包约 60+ 字节（UnitId + Position + Rotation + Direction + Speed + Sequences）
- 10个移动玩家 × 62.5Hz × 10个观察者 = **6250 msg/s**
- 这是目前"移动卡"的最可能原因
- **参考架构的 NetworkVariable 只在值变化时同步，而我们每 Tick 都广播**

### 2. 每 Tick 都广播即使位置未变
- 当玩家持续朝一个方向移动时，方向没变但仍然每 16ms 广播
- 缺少"位置差量阈值"——即使移动了 0.001m 也广播
- 缺少"方向未变则降频"的优化

### 3. 客户端预测与权威校正的参数可能需要调优
- `CorrectionRate = 8.0`：对于高延迟可能太激进（抖动）
- `SnapThreshold = 2m`：对于瞬移场景可能太大
- 缺少基于 RTT 的动态调整
- 停止输入后的"tail-chasing"防护虽有，但阈值可能不够

### 4. 输入上报 33ms 节流——可能略高
- 33ms ≈ 30Hz
- 参考架构虽然不上报输入，但移动同步也是 60Hz
- 急转弯场景下 33ms 可能导致服务器接收到的方向变化滞后
- 手感上可能感觉"转弯不灵敏"

### 5. 缺少动画同步优化
- 目前动画速度是客户端根据视觉速度本地计算
- 没有直接同步动画状态（如参考架构的 NetworkAnimator）
- 远程玩家的动画可能与实际位置不同步

### 6. 广播策略粗糙
- `NoticeType.BroadcastWithoutSelf` 广播给所有 AOI 范围内玩家
- 没有基于距离的频率降级（远处玩家可以低频同步）
- 没有基于重要性的优先级排序

### 7. M2C_JoystickMove 包体较大
- 包含完整 Position (3 float) + Rotation (4 float) + Direction (2 float) + Speed (1 float)
- 10 个 float = 40 字节 + UnitId (8) + 序列号 (8) = 56+ 字节
- 缺少差量压缩
- 缺少量化压缩（如位置用 short 表示厘米精度）

### 8. 停止广播的延迟
- `PendingStopBroadcast` 机制需要等到当前 Tick 结束
- 最坏情况下停止消息延迟一个 Tick（16ms）
- 测试表明正常场景 120ms 内收到停止确认

---

## 关键数据

| 指标 | 值 |
|------|-----|
| 服务器移动 Tick | 16ms (62.5Hz) |
| 输入上报频率 | 33ms 节流 (~30Hz) |
| 广播频率 | 62.5Hz（每 Tick） |
| 客户端预测 | 有（本地输入驱动） |
| 服务器回滚 | 无 |
| 差量压缩 | 无 |
| NavMesh 校验 | 双端（服务器+客户端预测） |
| 序列号去重 | 有（输入+移动双序列） |
| 权威校正 | 渐进式 (CorrectionRate=8.0/s) |
| 预测限额 | 0.2s × speed |
| 包体大小 | ~56+ 字节/包 |
| 停止延迟 | 正常 <120ms |

---

## 总结

ETGame 的移动系统在**安全性、正确性、分布式支持**方面显著优于参考架构。但在**带宽效率**方面存在明显瓶颈——62.5Hz 的无条件广播是移动卡顿的主要嫌疑。核心问题不在架构设计思路，而在**广播策略和包体优化**。
