# ECA 框架施工进度

## 开始时间：2026-02-26

## 验收目标
配置撤离点，测试从 map 跳转回 lobby 的功能

---

## 第一阶段：包基础结构 ✅

### 1.1 创建包目录
- [x] 创建 `Packages/cn.etetet.eca` 目录
- [x] 创建 `Scripts/Model/Share` 目录
- [x] 创建 `Scripts/Hotfix/Server` 目录
- [x] 创建 `Scripts/Hotfix/Test` 目录
- [x] 创建 `Editor/ECAEditor` 目录

### 1.2 创建包配置文件
- [x] 创建 `packagegit.json`（ID=52）
- [x] 创建 `package.json`
- [x] 创建 `Scripts/Model/Share/PackageType.cs`（ID=52）
- [x] 创建 `AGENTS.md`

---

## 第二阶段：核心数据结构 ✅

### 2.1 节点基类
- [x] 创建 `ECANode.cs`（节点基类）
- [x] 创建 `ECAConnection.cs`（连接数据）

### 2.2 节点类型
- [x] 创建 `ECAStateNode.cs`（状态节点）
- [x] 创建 `ECAEventNode.cs`（事件节点）
- [x] 创建 `ECAConditionNode.cs`（条件节点）
- [x] 创建 `ECAActionNode.cs`（动作节点）

### 2.3 配置根对象
- [x] 创建 `ECAConfig.cs`

---

## 第三阶段：服务端组件 ✅

### 3.1 ECA 点组件
- [x] 创建 `ECAPointComponent.cs`（Entity）
- [x] 创建 `ECAPointComponentSystem.cs`（System）

### 3.2 管理器组件
- [x] 创建 `ECAManagerComponent.cs`（Entity）
- [x] 创建 `ECAManagerComponentSystem.cs`（System）

### 3.3 辅助类
- [x] 创建 `ECAHelper.cs`（范围检测）
- [x] 创建 `ECALoader.cs`（配置加载）

---

## 第四阶段：撤离点功能实现 ✅

### 4.1 撤离点逻辑
- [x] 实现玩家进入撤离点范围检测
- [x] 实现撤离倒计时逻辑
- [x] 实现撤离完成后传送回 Map1（通过 TransferHelper）

### 4.2 撤离点组件（cn.etetet.ecanode）
- [x] 创建 `PlayerEvacuationComponent.cs`（玩家撤离组件）
- [x] 创建 `PlayerEvacuationComponentSystem.cs`（玩家撤离系统）
- [x] 创建 `PlayerEvacuationTimer.cs`（撤离定时器）

---

## 第五阶段：测试用例 ✅

### 5.1 撤离点测试
- [x] 创建 `Ecanode_LoadECAPoints_Test`（加载 ECA 点测试）✅ 通过
- [x] 创建 `Ecanode_PlayerEnterEvacuation_Test`（玩家进入撤离点测试）
- [x] 创建 `Ecanode_PlayerLeaveEvacuation_Test`（玩家离开撤离点测试）
- [x] 创建 `Ecanode_EvacuationComplete_Test`（撤离完成测试）

### 5.2 测试相关修复
- [x] `cn.etetet.eca` 和 `cn.etetet.ecanode` 加入 `MainPackage.txt`
- [x] 修复 `ECANode.cs` 中 `using UnityEngine` 导致 DotNet 编译失败（改为 `#if !DOTNET` 守卫）
- [x] 测试类命名修复：`ECANode_` → `Ecanode_`（符合 ET0036 规范）
- [x] 修复 `await` 后 Entity 访问（`EntityRef<T>` 包装）

---

## 第六阶段：GraphView 编辑器（待实现）

- [ ] 创建 `ECAEditor.cs`（主编辑器窗口）
- [ ] 创建 `ECAGraphView.cs`（GraphView 实现）
- [ ] 实现节点创建、连接、删除、保存/加载

---

## 第七阶段：场景配置支持 ✅

### 7.1 场景标记
- [x] 创建 `ECAPointMarker.cs`（SerializedMonoBehaviour）
- [x] 实现 Gizmos 可视化（交互范围球体）
- [x] 创建 `ECAConfigAsset.cs`（ScriptableObject 配置）
- [x] 创建 `ECASceneHelper.cs`（场景收集器）

### 7.2 配置导出工具
- [x] 创建 `ExportECAConfigEditor.cs`（菜单: ET/ECA/Export ECA Config）
- [x] 创建 `ET.ECA.Editor.asmdef`（Editor 程序集定义，引用 ET.Core/ET.Model/ET.ModelView）
- [x] 收集场景中所有 `ECAPointMarker`，导出为 JSON
- [x] 导出路径：`Packages/cn.etetet.map/Bundles/ECA/{场景名}.txt`
- [x] 使用 `MongoHelper.ToJson()` 序列化（BSON JSON 格式）

---

## 第八阶段：服务端集成 ✅

### 8.1 配置加载集成
- [x] `ECALoader.LoadFromFile(scene, mapName)` — 从导出的 JSON 文件加载 ECA 配置
- [x] `FiberInit_Map.cs` 中集成 ECA 加载（地图初始化时自动调用）

### 8.2 范围检测定时器
- [x] 创建 `ECATimerInvokeType.cs`（`TimerInvokeType.ECACheckRange = 52001`，在 eca 包）
- [x] 创建 `ECACheckRangeTimer.cs`（每 200ms 遍历所有玩家检测 ECA 范围，在 ecanode 包）
- [x] `ECAManagerComponent` 添加 `CheckRangeTimerId` 字段
- [x] `ECALoader.LoadECAPoints` 末尾自动启动定时器
- [x] `ECAManagerComponentSystem.Destroy` 中取消定时器

### 8.3 端到端测试 ✅
- [x] 在 Unity 场景中配置撤离点
- [x] 导出 ECA 配置（ET/ECA/Export ECA Config）
- [x] 测试玩家进入撤离点 → 触发范围检测
- [x] 测试撤离倒计时（10秒）
- [x] 测试撤离完成后传送回 Map1 ✅

---

## 第九阶段：文档完善（待实现）

- [ ] 完善 `AGENTS.md`
- [ ] 编写使用说明
- [ ] 编写配置示例

---

## 分包架构

### cn.etetet.eca (ID=52, Level=2) — 核心框架包

| 目录 | 文件 | 说明 |
|------|------|------|
| Model/Share | `ECAConfig.cs` | 运行时配置数据 |
| Model/Share | `ECANode.cs` | 节点基类 |
| Model/Share | `ECATimerInvokeType.cs` | 定时器类型常量 |
| Model/Share | `PackageType.cs` | 包类型 ID=52 |
| Model/Server | `ECAManagerComponent.cs` | 管理所有 ECA 点 |
| Model/Server | `ECAPointComponent.cs` | 单个 ECA 点组件 |
| Hotfix/Server | `ECALoader.cs` | 配置加载（文件/列表） |
| Hotfix/Server | `ECAHelper.cs` | 范围检测辅助类 |
| Hotfix/Server | `ECAManagerComponentSystem.cs` | 管理器系统 |
| Hotfix/Server | `ECAPointComponentSystem.cs` | ECA 点进入/离开事件 |
| ModelView/Client | `ECAPointMarker.cs` | Unity 场景标记组件 |
| ModelView/Client | `ECASceneHelper.cs` | 场景配置收集器 |
| ModelView/Share | `ECAConfigAsset.cs` | ScriptableObject 配置 |
| Editor/ECAEditor | `ExportECAConfigEditor.cs` | 配置导出工具 |
| Editor | `ET.ECA.Editor.asmdef` | Editor 程序集定义 |

### cn.etetet.ecanode (ID=53, Level=5) — 业务节点包

| 目录 | 文件 | 说明 |
|------|------|------|
| Model/Server | `PlayerEvacuationComponent.cs` | 玩家撤离状态组件 |
| Hotfix/Server | `PlayerEvacuationComponentSystem.cs` | 撤离业务逻辑 |
| Hotfix/Server | `PlayerEvacuationTimer.cs` | 撤离倒计时定时器 |
| Hotfix/Server | `ECACheckRangeTimer.cs` | 范围检测定时器 |
| Hotfix/Test | `ECANode_Evacuation_Test.cs` | 撤离功能测试用例（4个） |

---

## 编译和测试记录

### 编译记录
| 日期 | 结果 | 错误 | 备注 |
|------|------|------|------|
| 2026-02-26 17:46 | ✅ | 0 | 首次编译成功 |
| 2026-02-26 22:10 | ✅ | 0 | 撤离功能完成 |
| 2026-02-27 | ✅ | 0 | 测试通过 + 服务端集成完成 |

### 测试记录
| 日期 | 测试名 | 结果 | 备注 |
|------|--------|------|------|
| 2026-02-27 | Ecanode_LoadECAPoints_Test | ✅ 通过 | 249ms |
| 2026-02-27 | 包注册修复后全量测试 | ✅ 通过 | 加入 MainPackage.txt 后 |

---

## 问题和解决方案

### 问题1：包未注册到 MainPackage.txt
- **描述**：`cn.etetet.eca` 和 `cn.etetet.ecanode` 未在 `MainPackage.txt` 中，导致代码不会被编译进 ET.Hotfix.dll
- **解决方案**：将两个包名添加到 `MainPackage.txt` 末尾
- **状态**：✅ 已解决

### 问题2：ECANode.cs 中 using UnityEngine 导致 DotNet 编译失败
- **描述**：`using UnityEngine` 在 DotNet 编译时找不到命名空间，且 `Object` 基类被解析为 `UnityEngine.Object`
- **解决方案**：用 `#if !DOTNET` 守卫 `using UnityEngine`
- **状态**：✅ 已解决

### 问题3：测试类命名不符合 ET0036 规范
- **描述**：测试类前缀 `ECANode_` 不符合规范，应为 `Ecanode_`（包名后缀首字母大写）
- **解决方案**：重命名所有测试类
- **状态**：✅ 已解决

### 问题4：await 后 Entity 访问违规 (ETAE001)
- **描述**：`player` 和 `scene` 在 `await` 后直接访问
- **解决方案**：使用 `EntityRef<T>` 包装，await 后重新赋值
- **状态**：✅ 已解决

### 问题5：TimerComponent 为 null 导致测试崩溃
- **描述**：LoadECAPoints 末尾启动定时器时，测试场景未添加 TimerComponent
- **解决方案**：启动定时器前检查 `TimerComponent != null`
- **状态**：✅ 已解决

### 问题6：MSBuild g.props 生成脚本路径转义
- **描述**：`DotNet~/obj` 路径中 `\"` 在 Windows 命令行转义引号，导致 OutputPath 参数丢失
- **解决方案**：手动运行 PowerShell 脚本更新 g.props 文件
- **状态**：✅ 已解决（修改 MainPackage.txt 后需手动更新）

### 问题7：Editor asmdef 缺失导致导出菜单不显示
- **描述**：`Editor/ECAEditor/ExportECAConfigEditor.cs` 没有对应的 `.asmdef`，Unity 不编译该脚本
- **解决方案**：创建 `Editor/ET.ECA.Editor.asmdef`，引用 ET.Core、ET.Model、ET.ModelView
- **状态**：✅ 已解决

### 问题8：导出路径与加载路径归属
- **描述**：ECA 配置导出到 `cn.etetet.eca/Bundles/ECA/`，但资源应跟地图放一起
- **解决方案**：统一改为 `Packages/cn.etetet.map/Bundles/ECA/{场景名}.txt`
- **状态**：✅ 已解决

### 问题9：服务端 mapName 与 Unity 场景名不匹配
- **描述**：Unity 编辑器打开的是 Map1 场景，但服务端 Fiber 名称配置为 Map2，导致找不到 ECA 配置文件
- **解决方案**：确保导出文件名与服务端 Fiber 的 `GetSceneConfigName()` 返回值匹配
- **状态**：✅ 已解决

---

## 当前状态

### ✅ 已完成
1. ECA 框架基础架构（分包完成）
2. 场景配置支持（ECAPointMarker, ECAConfigAsset, Gizmos 可视化）
3. 配置导出工具（Editor 菜单导出 JSON 到 `cn.etetet.map/Bundles/ECA/`）
4. Editor 程序集定义（`ET.ECA.Editor.asmdef`）
5. ECA 点加载系统（从文件/列表加载）
6. 范围检测系统（200ms 定时器自动检测）
7. 撤离组件（倒计时、范围检测、取消）
8. 撤离完成传送（通过 `TransferHelper.TransferAtFrameFinish` 传送到 Map1）
9. 服务端集成（FiberInit_Map 自动加载 ECA 配置）
10. 测试用例（4个测试，已通过1个）
11. 编译通过（0 个错误）
12. **端到端测试通过**（Unity Editor ClientServer 模式，玩家进入撤离点 → 10秒倒计时 → 传送到 Map1）

### 📝 待完成
1. GraphView 可视化编辑器（非必需）
2. 更多 ECA 点类型实现（SpawnPoint、Container 等）

## 使用流程

1. **配置撤离点**：在 Unity 场景中的 GameObject 上添加 `ECAPointMarker` 组件，配置 `ECAConfigAsset` 和交互范围
2. **导出配置**：菜单 `ET → ECA → Export ECA Config`，生成 `Packages/cn.etetet.map/Bundles/ECA/{场景名}.txt`
3. **注意**：服务端 mapName 来自 Fiber 名称配置（`root.Name.GetSceneConfigName()`），需确保导出文件名与 Fiber 名匹配
4. **编译**：`dotnet build ET.sln`
4. **启动服务端**：`FiberInit_Map` 自动加载 ECA 点并启动范围检测定时器
5. **进入游戏**：玩家走到撤离点范围内，自动触发撤离倒计时，10秒后传送到 Map1

## 最后更新时间：2026-02-27（端到端测试通过）
