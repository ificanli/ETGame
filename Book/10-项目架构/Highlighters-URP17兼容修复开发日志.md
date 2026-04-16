# Highlighters-URP17兼容修复开发日志

**功能**：Highlighters URP17兼容修复
**关联设计文档**：[Highlighters-URP17兼容修复设计文档.md](./Highlighters-URP17兼容修复设计文档.md)
**关联任务**：M0.2-W3 #29
**开始时间**：2026-04-16
**开发者**：AI

## 开发进度

- [x] 步骤1：补计划与设计/开发文档门禁
- [x] 步骤2：迁移 Highlighters URP pass 到 `RTHandle` 与 `Blitter`
- [x] 步骤3：执行 `dotnet build ET.sln` 并收口同插件目录编译错误

## 决策记录

### 2026-04-16 - 采用最小兼容迁移
- **背景**：报错集中在第三方描边插件的 URP API 过期
- **方案**：只在 `Assets/Highlighters-Outlines/URP` 目录内迁移 `RTHandle`、`cameraColorTargetHandle` 和 `Blitter`
- **原因**：用户目标是先恢复编译，不应扩散到其他无关模块
- **替代方案**：整体替换第三方插件版本；放弃原因是当前仓库已有脏工作区，且替换整套资源风险过高

### 2026-04-16 - 跨 pass 纹理引用改为持有 pass 引用
- **背景**：`ReAllocateHandleIfNeeded` 可能在每帧重分配时替换 `RTHandle` 对象
- **方案**：由消费方 pass 持有生产方 pass 引用，在执行阶段读取最新 RTHandle
- **原因**：避免沿用旧插件“初始化时缓存目标标识符”导致的空句柄或陈旧句柄问题
- **替代方案**：初始化时直接缓存 `RTHandle`；放弃原因是句柄对象可能被重分配替换，不可靠

## 问题日志

### 2026-04-16 - URP 包缓存目录不是版本号目录
- **现象**：直接按 `com.unity.render-pipelines.universal@17.0.4` 读取本机包源码失败
- **原因**：Unity 本地 `Library/PackageCache` 使用 hash 后缀目录名
- **解决**：改为读取实际缓存目录 `com.unity.render-pipelines.universal@e1fa89d1f997`

## 变更清单

| 时间 | 文件 | 操作 | 说明 |
|------|------|------|------|
| 2026-04-16 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 补登记 W3 任务29 |
| 2026-04-16 | `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 补登记 W3 任务29 |
| 2026-04-16 | `Book/10-项目架构/Highlighters-URP17兼容修复设计文档.md` | 新增 | 建立本轮兼容修复设计文档 |
| 2026-04-16 | `Book/10-项目架构/Highlighters-URP17兼容修复开发日志.md` | 新增 | 建立本轮兼容修复开发日志 |
| 2026-04-16 | `Assets/Highlighters-Outlines/URP/URP Core/BlurOutline/PerObjectBlurPass.cs` | 修改 | 外发光模糊 pass 迁移到 `RTHandle` 与 `Blitter` |
| 2026-04-16 | `Assets/Highlighters-Outlines/URP/URP Core/DepthMask/DepthMaskPass.cs` | 修改 | 深度遮罩 pass 迁移到 `RTHandle` |
| 2026-04-16 | `Assets/Highlighters-Outlines/URP/URP Core/MeshOutline/MeshOutlinePass.cs` | 修改 | 网格描边 pass 迁移到 `RTHandle` |
| 2026-04-16 | `Assets/Highlighters-Outlines/URP/URP Core/ObjectsInfo/ObjectsPass.cs` | 修改 | 对象信息 pass 迁移到 `RTHandle` |
| 2026-04-16 | `Assets/Highlighters-Outlines/URP/URP Core/Overlay/OverlayPass.cs` | 修改 | 覆盖层 pass 补颜色拷贝并改为 `Blitter` |
| 2026-04-16 | `Assets/Highlighters-Outlines/URP/URP Core/User/HighlightsManagerURP.cs` | 修改 | renderer feature 改为传递 pass 引用并补资源释放 |
| 2026-04-16 | `ET.sln` | 验证 | `dotnet build ET.sln` 通过，0 warnings / 0 errors |

## 开发总结

> 开发结束后填写

- **实际完成**：已完成 `Assets/Highlighters-Outlines/URP` 的 URP17 兼容迁移，整仓 `dotnet build ET.sln` 通过。
- **未完成**：无。
- **与设计的偏差**：无偏差，按最小兼容迁移方案完成。
- **后续待办**：等待你在 Unity/运行时确认描边效果表现是否与旧版本一致。
