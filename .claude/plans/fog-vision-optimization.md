# 迷雾视野实时跟随优化方案

## 问题
当前迷雾可见区域由 CPU 每 0.1s 刷新一次贴图，玩家移动时视野跟随有明显延迟。

## 核心思路
将"当前可见圆"的计算从 CPU 贴图搬到 Shader 实时计算。CPU 贴图只负责已探索/未探索两种状态（低频更新），Shader 每帧根据玩家位置实时绘制可见圆。

## 改动范围

### 1. Shader — `SceneFogOverlay.shader`
- 新增 uniform：`_VisionCenter`(float2, 玩家XZ)、`_VisionRadius`(float)、`_VisibleColor`(half4)
- 正常渲染路径中，计算出世界坐标后：
  - 先算像素到 `_VisionCenter` 的距离
  - 距离 < `_VisionRadius` → 用 `smoothstep` 平滑过渡到 `_VisibleColor`（透明）
  - 距离 >= `_VisionRadius` → 照常采样迷雾贴图（已探索/未探索）
- `_VisionRadius <= 0` 时跳过实时圆逻辑（兼容 AOI 模式不变）

### 2. SceneFogComponentSystem.cs — CPU 端
- `UpdateOverlayMaterial`：每帧传 `_VisionCenter`、`_VisionRadius`、`_VisibleColor` 给 Shader
- `RefreshFogTexture`：贴图只写两种颜色（已探索 / 未探索），不再写可见色
- 调用 `RefreshLocalFog` 仍保留（用于更新 ExploredCells），但贴图写入逻辑简化

### 3. MinimapRuntimeComponentSystem.cs — 逻辑层
- 无需改动。`RefreshLocalFog` 仍然计算 CurrentVisibleCells 并合并到 ExploredCells，这个逻辑保持低频即可。

## 不改动的部分
- AOI 模式（`FogVisionRadius <= 0`）：涉及多个友方单位位置，暂不搬到 Shader，保持现有 CPU 刷新逻辑
- SceneFogComponent.cs（数据类）：无需新增字段
- MinimapConstKey.cs / 配置表：无需修改
- 调试模式（DebugMode 1-5）：保持不变

## 效果
- `FogVisionRadius > 0` 模式：视野跟随零延迟（逐帧逐像素计算）
- 已探索区域扩展仍为低频更新（可接受，已探索状态不需要实时）
- AOI 模式行为不变