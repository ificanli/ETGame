# 移动系统优化计划 v2：客户端权威 + 服务端校验

## 方案确认

| 参数 | 值 |
|------|-----|
| 权威模型 | **客户端权威移动** |
| 安全模型 | **服务端校验 + 非法拉回** |
| 游戏类型 | PvE 为主，有其他玩家 |
| 客户端碰撞 | NavMesh 约束（本地） |
| 上报频率 | 20Hz（50ms 间隔） |
| 非法处理 | 静默拉回（PvE 宽松策略） |

---

## 架构对比

### 当前架构（服务器权威）
```
客户端发方向(30Hz) → 服务器算位置(62.5Hz) → 服务器广播位置(62.5Hz) → 客户端校正
                      ↑ NavMesh 碰撞         ↑ 每个观察者都收
```
**问题：服务器是瓶颈，广播量巨大**

### 新架构（客户端权威 + 服务端校验）
```
客户端算位置(每帧) → 本地 NavMesh 碰撞 → 立即渲染（零延迟）
       ↓
上报位置+方向(20Hz) → 服务器校验 → 合法：转发给其他玩家(20Hz)
                                  → 非法：发纠正包拉回客户端
```
**优势：自己角色零延迟，服务器只做校验+转发，广播量降 70%+**

---

## 详细设计

### 1. 协议变更

#### 新消息：C2M_MoveState（客户端 → 服务器，20Hz）
```
C2M_MoveState : ILocationMessage
{
    float PosX, PosY, PosZ;       // 客户端当前位置（权威）
    float DirX, DirZ;             // 移动方向（用于服务端外推 + 转发）
    float Speed;                  // 当前速度
    uint Sequence;                // 客户端序列号
    long ClientTimeMs;            // 客户端时间戳（用于校验速度）
}
```

#### 新消息：M2C_MoveSync（服务器 → 其他客户端，20Hz 转发）
```
M2C_MoveSync : IMessage
{
    long UnitId;
    float PosX, PosY, PosZ;       // 服务器确认的位置
    float DirX, DirZ;             // 方向（用于客户端外推）
    float Speed;                  // 速度（用于客户端外推）
    uint Sequence;                // 序列号
}
```
**注意：去掉了 Rotation（4 float = 16 字节），客户端从 Direction 推算**

#### 新消息：M2C_MoveCorrection（服务器 → 作弊客户端，仅非法时）
```
M2C_MoveCorrection : IMessage
{
    float PosX, PosY, PosZ;       // 服务器纠正的合法位置
    uint AckSequence;             // 纠正的是哪个序列号
}
```

### 2. 客户端移动（本地权威）

#### InputSystemComponent 改造
```
每帧 Update:
  1. 读取摇杆/键盘输入 → 计算世界方向
  2. speed = unit.NumericComponent.Speed
  3. delta = direction * speed * Time.deltaTime
  4. NavMesh.TryMoveAlongSurface(currentPos, currentPos + delta) → newPos
  5. unit.Position = newPos  （立即生效，零延迟）
  6. 更新动画

每 50ms (20Hz):
  7. 发送 C2M_MoveState { newPos, direction, speed, seq++, clientTime }
```

**关键变化：**
- 移动计算从服务器 `JoystickMoveComponentSystem.TickFixedStep()` 移到客户端
- 客户端有完整 NavMesh 数据，碰撞本地检测
- 不再需要等服务器回包，自己角色**零延迟**

#### 上报节流策略
```
立即发送条件（不等 50ms）：
  - 启动移动（从静止到移动）
  - 停止移动（从移动到静止）
  - 急转弯（方向变化 > 45°）

跳过发送条件：
  - 静止状态且上一次已发送停止包
  - 位置变化 < 0.01m 且方向未变（原地踏步）
```

### 3. 服务端校验（轻量级）

#### MoveValidationComponent（新组件，挂在服务器 Unit 上）
```csharp
[ComponentOf(typeof(Unit))]
public class MoveValidationComponent : Entity
{
    float3 LastValidPosition;          // 上一次合法位置
    long LastValidateTimeMs;           // 上一次校验时间
    uint LastClientSequence;           // 上一次客户端序列号
    int ViolationCount;               // 连续违规次数
}
```

#### 校验规则（PvE 宽松）
```
收到 C2M_MoveState 时：
  1. 序列号检查：seq <= lastSeq → 丢弃（旧包）
  2. 速度检查：
     - deltaTime = (clientTime - lastValidateTime)
     - maxDistance = maxSpeed * deltaTime * 1.5  （1.5倍容忍）
     - actualDistance = distance(newPos, lastValidPos)
     - if actualDistance > maxDistance → 非法（加速/飞行）
  3. NavMesh 检查：
     - NavMesh.SamplePosition(newPos, maxDistance=1.0m)
     - if 失败 → 非法（穿墙/出界）
  4. 高度检查：
     - if abs(newPos.y - lastValidPos.y) > 合理值 → 非法（飞行）

合法处理：
  - 更新 LastValidPosition = newPos
  - 更新 unit.Position = newPos（服务器记录）
  - 转发 M2C_MoveSync 给 AOI 内其他玩家

非法处理：
  - ViolationCount++
  - 发送 M2C_MoveCorrection { LastValidPosition, ackSeq }
  - 客户端收到后强制 Snap 到纠正位置
  - 连续 10 次违规 → 日志告警（不踢人，PvE 宽松）
```

#### 校验开销评估
```
服务器不再做的事（节省）：
  ✗ 每 16ms 计算移动（62.5Hz NavMesh + 物理）
  ✗ 每 16ms 广播 M2C_JoystickMove

服务器现在做的事（轻量）：
  ✓ 每 50ms 收一个 C2M_MoveState → 简单校验（距离+NavMesh采样）
  ✓ 校验通过 → 转发（20Hz vs 之前 62.5Hz）

服务器负载降低预估：70%+
```

### 4. 其他玩家的客户端渲染

#### 外推 + 插值（UnitViewInterpolationComponent 改造）

收到 M2C_MoveSync 时（20Hz，每 50ms 一次）：
```
1. 更新权威位置 = msg.Position
2. 更新外推参数：direction = msg.Direction, speed = msg.Speed

每帧 Update（远程玩家）：
3. if speed > 0:
     外推位置 += direction * speed * deltaTime
     NavMesh约束外推位置（可选，防止外推穿墙）
4. 视觉位置 = Lerp(当前视觉位置, 外推位置, smoothFactor)
5. 新权威包到达时：外推位置平滑拉向权威位置（而非跳变）

停止处理：
6. speed == 0 → 停止外推，视觉位置滑向权威位置
```

**20Hz 够不够？**
- 匀速直线移动：外推非常准确，50ms 间隔看不出卡顿
- 急转弯：最多 50ms 延迟才收到新方向，略有偏差但 PvE 可接受
- 停止：最多 50ms 后收到停止包，有轻微滑行（可本地预判优化）

### 5. 停止的特殊处理

停止是移动中最敏感的时刻（滑行感最明显）：

```
客户端：
  - 松开摇杆 → 立即停止渲染移动
  - 立即发送 C2M_MoveState { speed=0 }（不等 50ms）

服务器：
  - 收到 speed=0 → 立即转发 M2C_MoveSync { speed=0 }

其他客户端：
  - 收到 speed=0 → 停止外推，视觉位置平滑减速到权威位置
  - 减速时间：100-150ms（避免硬停）
```

---

## 改动范围

### 新增文件
| 文件 | 位置 | 说明 |
|------|------|------|
| MoveValidationComponent.cs | Model/Server | 服务端校验组件 |
| MoveValidationComponentSystem.cs | Hotfix/Server | 校验逻辑 |
| C2M_MoveStateHandler.cs | Hotfix/Server | 服务端接收+校验+转发 |
| M2C_MoveSyncHandler.cs | Hotfix/Client | 客户端接收远程玩家位置 |
| M2C_MoveCorrectionHandler.cs | Hotfix/Client | 客户端处理纠正包 |

### 改造文件
| 文件 | 改动 |
|------|------|
| InputSystemComponent / System | 本地计算移动位置（代替发方向） |
| UnitViewInterpolationComponent / System | 改造外推逻辑适配 20Hz |
| Proto StateSync_C_10700.cs × 3 | 新增 3 个消息 |
| JoystickMoveComponent / System | 可保留作为 AI/机器人移动，但玩家不再使用 |

### 可废弃（后续清理）
| 文件 | 原因 |
|------|------|
| C2M_JoystickInput 相关 | 不再上报方向，改为上报位置 |
| 服务器 HighFrequency Move16ms Tick | 玩家移动不再需要服务器 Tick |

---

## 带宽对比

| 场景：10 个移动玩家，10 个观察者 | 当前 | 优化后 |
|---|---|---|
| 客户端 → 服务器 | 10 × 30Hz = 300 msg/s | 10 × 20Hz = 200 msg/s |
| 服务器 → 客户端（广播） | 10 × 62.5Hz × 10 = 6250 msg/s | 10 × 20Hz × 10 = 2000 msg/s |
| 服务器总消息量 | **6550 msg/s** | **2200 msg/s** (~66% 降低) |
| 每包大小 | ~56 字节 | ~36 字节 (~36% 降低) |
| 总带宽 | ~366 KB/s | ~79 KB/s (**78% 降低**) |

---

## 实施阶段

### 第一阶段：核心切换
1. 定义新协议（C2M_MoveState, M2C_MoveSync, M2C_MoveCorrection）
2. 客户端本地移动（InputSystemComponent 改造）
3. 服务端校验组件（MoveValidationComponent）
4. 服务端接收+校验+转发 Handler
5. 客户端远程玩家接收 Handler

### 第二阶段：打磨体验
6. 远程玩家外推优化（UnitViewInterpolationComponent）
7. 停止减速平滑
8. 纠正包的视觉处理（Snap vs 平滑拉回）
9. 急转弯时的立即上报

### 第三阶段：清理 & 测试
10. 编写测试用例（校验规则、序列号去重、非法拉回）
11. 清理旧的服务器移动 Tick 代码
12. 性能测试（带宽、CPU 对比）

---

## 风险 & 缓解

| 风险 | 缓解 |
|------|------|
| NavMesh 数据客户端/服务端不一致 | 同一份 SDCMap.bytes，校验用 SamplePosition 容忍 1m |
| 客户端作弊加速 | 速度校验 1.5 倍容忍 + 连续违规告警 |
| 20Hz 急转弯不够 | 急转弯立即发送，不等 50ms |
| 旧代码兼容 | JoystickMoveComponent 保留给 AI/机器人用 |
| 纠正包导致抖动 | PvE 宽松校验，正常玩家几乎不触发纠正 |
