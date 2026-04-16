# Highlighters-URP17兼容修复设计文档

**创建时间**：2026-04-16
**最后更新**：2026-04-16
**状态**：已完成
**关联任务**：M0.2-W3 #29
**涉及包**：-（第三方插件 `Assets/Highlighters-Outlines/URP`）

## 需求概述

当前项目已升级到 `com.unity.render-pipelines.universal 17.0.4`，但第三方描边插件 `Assets/Highlighters-Outlines/URP` 仍使用旧版 URP 兼容写法，导致编译阶段出现 `RenderTargetHandle`、`cameraColorTarget` 等过时 API 错误，整仓无法通过 `dotnet build ET.sln`。

本次目标是在不改动业务模块的前提下，仅修复该插件目录内与 URP 17 兼容相关的编译阻塞，使项目恢复可编译状态。

## 技术方案

### 整体思路

采用“最小兼容迁移”方案，只改第三方插件 URP pass 与 renderer feature 的目标纹理管理方式：

1. 将旧 `RenderTargetHandle` 迁移为 `RTHandle`
2. 将 `renderer.cameraColorTarget` 迁移为 `renderer.cameraColorTargetHandle`
3. 将 `cmd.GetTemporaryRT(...)` 迁移为 `RenderingUtils.ReAllocateHandleIfNeeded(...)`
4. 将旧 `Blit(...)` 迁移为 `Blitter.BlitCameraTexture(...)`
5. 将跨 pass 的纹理引用从“旧标识符占位”改为“引用对应 pass，在执行阶段读取最新 RTHandle”
6. 对同目录内潜在继续报错的 `OverlayPass` 一并修复，避免只过首批错误后继续卡住

### 涉及的文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Assets/Highlighters-Outlines/URP/URP Core/BlurOutline/PerObjectBlurPass.cs` | 修改 | 外发光模糊 pass 迁移到 `RTHandle` + `Blitter` |
| `Assets/Highlighters-Outlines/URP/URP Core/DepthMask/DepthMaskPass.cs` | 修改 | 场景深度遮罩 pass 迁移到 `RTHandle` |
| `Assets/Highlighters-Outlines/URP/URP Core/MeshOutline/MeshOutlinePass.cs` | 修改 | 网格描边对象纹理改用 `RTHandle` |
| `Assets/Highlighters-Outlines/URP/URP Core/ObjectsInfo/ObjectsPass.cs` | 修改 | 高亮对象信息纹理改用 `RTHandle` |
| `Assets/Highlighters-Outlines/URP/URP Core/Overlay/OverlayPass.cs` | 修改 | 覆盖层 pass 改为 `RTHandle`，并避免自读写同一相机颜色目标 |
| `Assets/Highlighters-Outlines/URP/URP Core/User/HighlightsManagerURP.cs` | 修改 | renderer feature 改为传递 pass 引用，适配新目标管理 |

### 风险点

1. `RTHandle` 重分配可能替换句柄对象，不能继续沿用老插件“初始化时缓存目标标识符”的写法
2. `OverlayPass` 原先对相机颜色目标做同源同目标 blit，迁移后需要增加一层颜色拷贝以避免读写冲突
3. 当前工程是脏工作区，只能修改目标插件与文档，不能回退其他模块改动

## 实现步骤

1. 补计划与设计/开发文档门禁
2. 迁移 Highlighters URP pass 到 `RTHandle` 与 `Blitter`
3. 调整 renderer feature 的跨 pass 目标引用方式
4. 执行 `dotnet build ET.sln`，继续收口同插件目录内的兼容错误

## 验收标准

- [x] `Assets/Highlighters-Outlines/URP` 目录内不再出现 `RenderTargetHandle` / `cameraColorTarget` 相关编译错误
- [x] `dotnet build ET.sln` 可以通过
- [x] 修改范围仅限文档和第三方 Highlighters URP 插件，不扩散到无关模块

## 实现追踪

> 开发完成后回填

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 步骤1 | 2026-04-16 | `Book/08-版本计划/M0.2版本计划.md`, `Book/08-版本计划/M0.2-W3周计划.md`, `Book/10-项目架构/Highlighters-URP17兼容修复设计文档.md`, `Book/10-项目架构/Highlighters-URP17兼容修复开发日志.md` | 无偏差 |
| 步骤2 | 2026-04-16 | `Assets/Highlighters-Outlines/URP/URP Core/BlurOutline/PerObjectBlurPass.cs`, `Assets/Highlighters-Outlines/URP/URP Core/DepthMask/DepthMaskPass.cs`, `Assets/Highlighters-Outlines/URP/URP Core/MeshOutline/MeshOutlinePass.cs`, `Assets/Highlighters-Outlines/URP/URP Core/ObjectsInfo/ObjectsPass.cs`, `Assets/Highlighters-Outlines/URP/URP Core/Overlay/OverlayPass.cs`, `Assets/Highlighters-Outlines/URP/URP Core/User/HighlightsManagerURP.cs` | 无偏差 |
| 步骤3 | 2026-04-16 | `ET.sln` | `dotnet build ET.sln` 通过，0 warnings / 0 errors |
