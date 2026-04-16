# 参考架构分析：MultiplayerEngine（Unity NGO）

## 概述

参考项目基于 **Unity Netcode for GameObjects (NGO)**，采用**客户端权威 + 服务器验证**混合模式。主要面向 PvE / 合作型多人游戏场景。

---

## 架构核心

| 模块 | 权威方 | 同步方式 |
|------|--------|----------|
| 移动（Transform） | **客户端** | ClientNetworkTransform，60Hz 自动同步 |
| 动画 | **客户端** | ClientNetworkAnimator，60Hz |
| 瞄准点 | Owner 写入 | NetworkVariable，10Hz 节流 |
| 射击 | 全客户端执行 | `[Rpc(SendTo.Everyone)]` |
| 血量/伤害 | **服务器** | NetworkVariable (Server写) |
| 拾取/交互 | 客户端请求→服务器验证 | RPC |

### 移动方案

- `ClientNetworkTransform`：`OnIsServerAuthoritative() → false`
- 客户端用 `CharacterController`（运动学）本地执行移动
- NGO 自动将 Transform 同步给其他客户端
- **无客户端预测/回滚/服务器校正**——客户端就是权威方

### 输入处理

- Unity New Input System
- `InputManager` 只存当前帧状态，**无输入缓冲**
- 输入不上传服务器，只驱动本地 CharacterController
- 移动结果通过 NGO Transform 同步自动广播

### 射击 & 命中

- 客户端 Raycast → `ShootRpc(SendTo.Everyone)` → 每个客户端独立生成弹道
- 弹道移动是本地 Coroutine（非网络物理），确定性一致
- **无服务器端命中检测**——信任客户端 Raycast 结果

---

## 优点

### 1. 极低延迟体验
- 移动完全本地执行，**零网络延迟**
- 不存在预测→校正的抖动问题
- 射击即时反馈，弹道即时显示

### 2. 实现简单
- 利用 NGO 内置 `NetworkTransform` / `NetworkAnimator` / `NetworkVariable`
- 不需要自己写序列化、快照、差量压缩
- RPC 标注式开发 (`[Rpc(SendTo.Server)]`)，代码量少

### 3. 带宽优化细节
- 瞄准点 10Hz 节流 + 变化检测（sqrMagnitude > 0.0025）
- `NetworkVariable` 只在值变化时同步
- 弹道不走网络（确定性本地移动）
- `FixedString32Bytes` 避免 GC 分配

### 4. 性能优化模式
- `Physics.OverlapSphereNonAlloc` 避免 GC
- 弹道对象池（`ObjectPool<Projectile>`）
- 交互检测节流（0.1s 间隔 + 60° 锥形过滤）

### 5. 架构清晰
- 权威模型明确：移动=客户端，血量=服务器
- 每个系统职责单一（InputManager / ThirdPersonController / WeaponManager / HealthManager）
- 生命周期管理完善（Spawn/Despawn 走服务器）

### 6. 传输层抽象
- 支持 Steam Networking Sockets 和 Unity Relay 热切换
- 对游戏逻辑层透明

---

## 缺点

### 1. 严重的反作弊缺陷
- **移动客户端权威 = 可以任意飞行/穿墙/加速**
- 射击客户端 Raycast = 可以伪造命中
- 仅血量有服务器保护
- **不适合 PvP 竞技场景**

### 2. 无服务器端物理验证
- 服务器不运行 CharacterController
- 无法检测非法位移
- 无 NavMesh 约束
- 碰撞完全依赖客户端

### 3. 无客户端预测/回滚机制
- 因为客户端就是权威，不需要预测
- 但也意味着一旦需要切换到服务器权威，**整个移动系统需要重写**
- 无法渐进式增强安全性

### 4. 高带宽消耗
- Transform 60Hz 同步（每个玩家每秒60个位置包）
- Animation 60Hz 同步
- 无差量压缩（依赖 NGO 内部优化，但 NGO 的压缩有限）
- 10人场景 = 10×60×2(位置+动画) = 1200 msg/s

### 5. 确定性假设脆弱
- 弹道依赖"所有客户端同一帧执行 Raycast 得到相同结果"
- 实际上不同客户端的 Transform 同步存在延迟差
- 高延迟下可能出现"我明明打中了但没伤害"的体验

### 6. 无插值/外推
- 远程玩家的移动完全依赖 NGO 的 NetworkTransform 内置插值
- NGO 默认插值质量一般（无自定义缓冲区大小调整）
- 高延迟下远程玩家可能出现明显卡顿

### 7. 刚性依赖 Unity NGO
- 所有网络功能绑定 NGO 框架
- 无法迁移到自定义网络层
- NGO 的 bug 或限制直接影响项目
- 不支持分布式服务器架构

---

## 关键数据

| 指标 | 值 |
|------|-----|
| 移动同步频率 | 60 Hz（NGO Transform） |
| 动画同步频率 | 60 Hz（NGO Animator） |
| 瞄准同步频率 | 10 Hz（节流） |
| 输入缓冲 | 无 |
| 客户端预测 | 无（客户端即权威） |
| 服务器回滚 | 无 |
| 差量压缩 | 无（依赖 NGO 内部） |
| 物理引擎 | CharacterController（客户端本地） |
| 最大延迟容忍 | 低（PvE 场景 100-200ms 可接受） |

---

## 总结

这是一个**面向 PvE/合作模式的快速原型架构**，核心优势是**开发速度快、延迟体验好**。但在安全性、可扩展性、带宽效率方面有明显短板。不适合直接用于竞技 PvP 场景，但其中**节流策略、对象池、非分配物理查询**等细节值得借鉴。
