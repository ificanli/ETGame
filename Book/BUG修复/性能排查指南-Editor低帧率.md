# 性能排查指南 - Editor 低帧率

**创建时间**：2026-03-30
**状态**：待排查
**现象**：Editor 下 FPS 仅 15，已确认 VSync 设为 Don't Sync，代码中未设置 targetFrameRate

## 排查步骤

### 第1步：Profiler 定位瓶颈大类

1. Unity 菜单 `Window → Analysis → Profiler`（快捷键 `Ctrl+7`）
2. 点击 Game 窗口里正常操作，让 Profiler 采集 5-10 秒数据
3. 看 CPU 模块的帧耗时分布，确认瓶颈是哪一类：
   - **Rendering** > 50ms → GPU / Draw Call 瓶颈
   - **Scripts** > 30ms → C# 逻辑瓶颈
   - **Physics** > 10ms → 物理计算瓶颈
   - **Editor** 占比很高 → Editor 自身开销

### 第2步：排除 Editor 自身干扰

1. **关闭 Scene 视图** — Scene 窗口打开会导致场景双倍渲染
2. **关闭 Inspector** — 选中复杂对象时 Inspector 每帧刷新开销大
3. **Game 窗口聚焦** — 确保 Game 窗口是活跃窗口
4. **降低 Game 窗口分辨率** — 右上角 Free Aspect 改成固定的小分辨率如 1280x720
5. 以上操作后如果帧率明显回升，说明问题是 Editor 开销而非游戏逻辑

### 第3步：渲染瓶颈排查

如果 Profiler 显示 Rendering 占大头：

1. Game 窗口右上角点 `Stats`，记录以下数据：
   - **Batches**：> 500 说明 Draw Call 过多
   - **Tris / Verts**：> 2M 说明面数过高
   - **SetPass calls**：> 200 说明材质切换过多
2. 菜单 `Window → Analysis → Frame Debugger`，看哪些渲染操作最重
3. 常见优化方向：
   - 开启 GPU Instancing / SRP Batcher
   - 减少实时光源和阴影
   - LOD 和遮挡剔除
   - 降低后处理复杂度

### 第4步：脚本瓶颈排查

如果 Profiler 显示 Scripts 占大头：

1. Profiler CPU 模块切到 `Hierarchy` 视图，按 `Time ms` 降序排列
2. 展开最耗时的调用栈，找到具体函数
3. 常见问题：
   - `GetComponentsInChildren` 每帧调用
   - 大量 `Log.Info` / `Log.Warning` 写盘（Editor 下特别慢）
   - GC Alloc 过高导致频繁 GC
4. Profiler 切到 `Timeline` 视图可以看到单帧内各系统的时间分布

### 第5步：确认是否 Editor 独有

1. 打一个 Development Build（`File → Build Settings → Development Build` 勾选）
2. 运行独立包，看帧率是否正常
3. 如果独立包帧率正常（60+），则问题是 Editor 独有的，优先级可以降低

## 快速自查清单

| 检查项 | 操作 |
|--------|------|
| Scene 窗口是否关闭 | 关掉或最小化 |
| Inspector 是否选中了复杂对象 | 取消选中或关闭 Inspector |
| Game 窗口分辨率 | 降到 1280x720 |
| Quality Settings | 确认 Editor 用的 Quality Level 不是 Ultra |
| Shadows | 临时关闭实时阴影看帧率变化 |
| Post Processing | 临时关闭后处理看帧率变化 |
| 日志输出 | 确认没有每帧大量 Log 输出 |
