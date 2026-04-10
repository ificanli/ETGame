# Windows客户端离线打包设计文档

**创建时间**：2026-03-30
**最后更新**：2026-03-30（补充 HybridCLR `UnityEditor.CoreModule` 引用污染、asmref `.meta` 清理、客户端热更误走工程目录资源路径修复、包内配置资源 `et_xxx` 命名回退修复，以及离线包 Config 收集格式与运行时 Luban 反序列化方式对齐，并增强 Validate 对配置格式的校验）
**状态**：开发中
**关联任务**：M0.2-W3 #7 领导体验包打包 + 发布
**涉及包**：`cn.etetet.loader`、`cn.etetet.yooassets`、`com.etetet.init`、`cn.etetet.archive`、`cn.etetet.proto`、`cn.etetet.map`、`cn.etetet.lockstep`

## 需求概述

当前 Windows 客户端打包依赖人工完成以下前置动作：

1. 手动将 `GlobalConfig.CodeMode` 切到 `Client`
2. 手动将 `YooConfig.EPlayMode` 切到 `OfflinePlayMode`
3. 手动执行 YooAsset 资源包构建
4. 再点击 Unity 的 Build 按钮导出客户端

这个流程的问题是：

- 前置步骤依赖人工记忆，容易遗漏
- `CodeMode` 或 `PlayMode` 漏切时，构建结果不稳定
- 无法沉淀为可复用的一键流程
- 后续接 Jenkins 或其他自动化编排工具时，没有统一入口

本次设计目标是把以上流程收敛成一个 **Windows 客户端离线体验包一键打包方案**，满足以下要求：

- 入口统一为 PowerShell
- 同时提供 Unity 菜单快捷入口，内部复用同一套构建逻辑
- 自动切换 `CodeMode=Client`
- 自动切换 `EPlayMode=OfflinePlayMode`
- 自动准备 HybridCLR 产物（`Generate/All` + `CopyAotDlls`）
- 自动构建 YooAsset AB
- 自动构建 Windows 客户端
- 构建完成后自动恢复构建前的本地开发配置
- 最终只保留构建目录，不额外生成 `zip`

本次不覆盖以下范围：

- Linux 服务端发布
- Docker 或 Aspire 编排
- Android、iOS、WebGL 客户端构建
- CDN 上传与热更新发布链路

## 当前工程现状

### 1. CodeMode 已具备脚本化切换能力

当前工程切换 `GlobalConfig.CodeMode` 后，会调用：

```text
Bin/ET.CodeMode.dll --CodeMode=...
```

同步代码模式和相关程序集引用。说明 `CodeMode` 不是只能靠手点 Inspector，而是可以被自动化流程安全接管。

### 2. YooAsset PlayMode 已集中在配置资产中

当前运行时通过 `YooConfig.EPlayMode` 进入不同初始化分支，已经支持：

- `EditorSimulateMode`
- `OfflinePlayMode`
- `HostPlayMode`
- `WebPlayMode`
- `CustomPlayMode`

因此客户端离线包只要在构建前把配置切到 `OfflinePlayMode`，运行时就会走内置资源文件系统。

### 3. 当前默认配置仍是开发态

当前仓库配置为：

- `GlobalConfig.CodeMode = ClientServer`
- `YooConfig.EPlayMode = EditorSimulateMode`

这适合平时开发，但不适合作为离线体验包的最终构建前状态。

### 4. 现有客户端构建入口未串联 YooAsset AB 构建，且入口场景路径写错

当前 `BuildHelper.Build` 只负责调用 `BuildPipeline.BuildPlayer`，没有先执行 YooAsset AB 构建。因此即使完成了客户端导出，也不能保证离线包带有完整内置资源。

另外，当前 `BuildHelper.Build` 写死的入口场景是：

```text
Packages/cn.etetet.wow/Scenes/Init.unity
```

但该路径在当前工程中不存在。当前工程唯一实际存在的 `Init.unity` 位于：

```text
Packages/cn.etetet.statesync/Scenes/Init.unity
```

这说明现有客户端构建入口本身已经处于失效风险中，后续实现阶段必须一并修复。

### 5. 现有 BuildEditor 不能直接复用为自动化入口

当前 `BuildEditor` 虽然校验了 `CodeMode` 必须是 `Client`，但它在 `OnEnable` 里写死的配置资产路径与仓库实际资产路径不一致，不能直接当作批处理入口复用。

具体差异如下：

| 配置项 | `BuildEditor` 写死路径 | 实际资产路径 |
|------|------|------|
| `GlobalConfig` | `Packages/cn.etetet.loader/Resources/GlobalConfig.asset` | `Packages/com.etetet.init/Resources/GlobalConfig.asset` |
| `YooConfig` | `Packages/cn.etetet.yooassets/YooConfig.asset` | `Packages/cn.etetet.yooassets/Resources/YooConfig.asset` |

### 6. 现有 Publish.ps1 不适用于本任务

现有 `Scripts/Publish.ps1` 面向 Linux 服务端发布，流程中包含：

- `dotnet publish ET.sln -r linux-x64`
- 服务端配置与资源复制

它不覆盖 Windows 客户端离线体验包，也不负责切换 `CodeMode` / `PlayMode` 或构建 YooAsset AB。

### 7. 当前 YooAsset 主配置

当前主包来自 `MainPackage.txt` 第一行：`cn.etetet.statesync`。  
当前主包下的 `AssetBundleCollectorSetting.asset` 中，实际配置的资源包只有一个：

- `DefaultPackage`

为了避免 hard code，自动化方案不会把 `DefaultPackage` 写死在逻辑里，而是按当前 `AssetBundleCollectorSetting` 中声明的资源包列表遍历构建。在当前项目里，这个遍历结果等价于构建 `DefaultPackage`。

## 技术方案

整体采用“两层结构”：

### 1. PowerShell 作为统一入口

PowerShell 负责：

- 解析输出目录参数
- 调用 Unity `-batchmode -quit -executeMethod`
- 收集日志
- 传递成功或失败退出码

PowerShell 不直接修改 Unity 资产，也不直接调用 YooAsset 构建 API。

### 2. Unity Editor 负责真实构建动作

Unity Editor 负责：

- 定位并读取 `GlobalConfig` / `YooConfig`
- 切换 `CodeMode`
- 执行 `ET.CodeMode.dll`
- 切换 `OfflinePlayMode`
- 执行 `HybridCLR/Generate/All`
- 执行 `ET/HybridCLR/CopyAotDlls`
- 构建 YooAsset AB
- 构建 Windows 客户端
- 恢复原始配置

除 PowerShell 批处理入口外，再补五个 Unity 菜单入口，全部复用同一套离线构建/配置切换核心逻辑：

- `ET/Loader/Build Windows Client Bundles Only`
- `ET/Loader/Build Windows Client Offline`
- `ET/Loader/Validate Windows Client Offline Runtime`
- `ET/Loader/Switch To Editor Runtime Config`
- `ET/Loader/Switch To Package Runtime Config`

其中：

- `Build Windows Client Bundles Only` 只做 `CodeMode/PlayMode` 切换、HybridCLR 产物准备和 YooAsset AB 构建，不执行 `BuildPlayer`
- `Validate Windows Client Offline Runtime` 不做 Player 打包，只读取当前内置 manifest，快速校验关键 DLL、AOT 元数据、YIUI 启动资源以及客户端配置 location 是否齐全
- `Build Windows Client Offline` 仍保留完整离线包导出能力
- `Switch To Editor Runtime Config` 一键切到本地编辑器开发态：`CodeMode=ClientServer`、`PlayMode=EditorSimulateMode`
- `Switch To Package Runtime Config` 一键切到接近包体的运行态：`CodeMode=Client`、`PlayMode=OfflinePlayMode`

这样本地排查可以先走“构建 Bundle -> 校验 manifest -> 在 Editor 用 OfflinePlayMode 复现”，只有确认问题已经逼近 Player/IL2CPP 层时才需要跑完整 Windows 打包。

这样拆分的原因是：

- `CodeMode` 和 `YooConfig` 都是 Unity 资产，修改和保存必须在 Unity 上下文中完成
- YooAsset AB 构建和 `BuildPipeline.BuildPlayer` 也必须在 Unity Editor 中执行
- PowerShell 更适合做总编排，也符合当前 ET 工具链要求

## 自动化流程设计

### 步骤 1：PowerShell 入口

新增一个 Windows 客户端离线打包脚本，例如：

```powershell
pwsh -ExecutionPolicy Bypass -File Scripts/BuildWindowsClientOffline.ps1 -OutputDir .\Release\WindowsClient
```

脚本职责：

1. 校验当前目录是否为项目根目录
2. 解析 `OutputDir`
3. 调用 Unity 批处理入口
4. 指定日志文件输出位置
5. 返回 Unity 的退出码

### 步骤 2：Unity 批处理入口

新增一个 Editor 静态入口，例如：

```csharp
ET.ClientBuildAutomation.BuildWindowsOfflineFromCommandLine()
```

该入口负责：

1. 读取命令行参数
2. 查找配置资产
3. 备份当前配置快照
4. 切换构建所需模式
5. 构建 YooAsset AB
6. 构建 Windows 客户端
7. 在 `finally` 中恢复原配置

### 步骤 3：配置资产定位

不能直接复用现有 `BuildEditor` 写死的资产路径，自动化入口需要显式查找真实资产：

- `AssetDatabase.FindAssets("t:GlobalConfig")`
- `AssetDatabase.FindAssets("t:YooConfig")`

若结果为 0 个或多个，则直接失败并给出错误日志。

这样做的原因是：

- 避免依赖当前错误路径
- 避免未来资产迁移后脚本静默失效
- 可以把错误暴露得更早、更清晰

### 步骤 4：CodeMode 切换

构建前执行以下动作：

1. 读取当前 `GlobalConfig.CodeMode`
2. 记录原始值
3. 若当前值不是 `Client`，则写入 `Client`
4. `AssetDatabase.SaveAssets()`
5. `AssetDatabase.Refresh()`
6. 调用 `ProcessHelper.DotNet("Bin/ET.CodeMode.dll --CodeMode=Client", ".", true)`
7. 检查退出码，若失败则终止构建

这里必须保留 `ET.CodeMode.dll` 调用，因为仅修改资产值并不足以同步程序集和代码模式。

这里的 `--CodeMode=Client` 使用的是枚举名称而不是数值。当前 `ET.CodeMode.dll` 的 `Program.cs` 通过命令行库直接解析 `CodeMode` 枚举，现有编辑器代码也是通过 `globalConfig.CodeMode.ToString()` 传参，因此设计上沿用枚举名称格式。

### 步骤 5：PlayMode 切换

构建前执行以下动作：

1. 读取当前 `YooConfig.EPlayMode`
2. 记录原始值
3. 若当前值不是 `OfflinePlayMode`，则写入 `OfflinePlayMode`
4. `AssetDatabase.SaveAssets()`
5. `AssetDatabase.Refresh()`

这一层不需要额外外部工具，修改并保存资产即可。

### 步骤 6：YooAsset AB 构建

YooAsset AB 构建采用“按当前配置遍历构建资源包”的方式，不写死包名。

构建策略：

1. 先读取 `MainPackage.txt` 的第一条非空记录，得到主包名
2. 按主包名拼出目标配置路径：

```text
Packages/{mainPackageName}/Settings/AssetBundleCollectorSetting.asset
```

3. 直接从该路径加载 `AssetBundleCollectorSetting`
4. 若该路径不存在或加载失败，则直接失败，不在多个 `AssetBundleCollectorSetting` 之间做模糊兜底
5. 遍历 `Packages` 列表中的每个 `PackageName`
6. 对每个资源包读取当前配置的构建管线
7. 若无显式配置，则回退到 YooAsset 默认值 `ScriptableBuildPipeline`
8. 使用 `StandaloneWindows64` 作为 `BuildTarget`
9. 构建内置资源并输出到统一构建目录

这样设计的原因是：

- 当前工程里同时存在 `statesync` 和 `lockstep` 两份 `AssetBundleCollectorSetting`
- 这两份配置服务于不同主包，不能简单依赖 `FindAssets` 后取第一条
- 当前主包约定已经由 `MainPackage.txt` 和 YooAsset `SettingLoader` 固化，因此自动化方案应复用同一条约定

当前工程按现有配置，实际会构建：

- `DefaultPackage`

之所以不在代码里直接写死 `DefaultPackage`，是为了遵守“绝对禁止 hard code”的项目约束。

### 步骤 6.5：HybridCLR 产物准备

在开始 YooAsset AB 构建前，自动化入口必须额外执行：

1. `HybridCLR/Generate/All`
2. `ET/HybridCLR/CopyAotDlls`

原因是：

- `Generate/All` 负责生成当前 `CodeMode=Client` 下的 HybridCLR 必需产物
- `CopyAotDlls` 负责把 `HybridCLRData/AssembliesPostIl2CppStrip/{target}` 下的 AOT dll 复制到 `Packages/cn.etetet.loader/Bundles/AotDlls/*.bytes`
- `Packages/cn.etetet.statesync/Settings/AssetBundleCollectorSetting.asset` 已经把 `Packages/cn.etetet.loader/Bundles/AotDlls` 纳入采集

如果漏掉 `CopyAotDlls`，离线包虽然可以成功出包，但运行时 `CodeLoader` 在加载 `mscorlib.dll` 等 AOT 元数据时会拿不到 YooAsset location，最终导致热更启动链路中断，表现为启动后只有背景、没有 UI。

### 步骤 7：Windows 客户端构建

AB 构建成功后，再执行 Windows 客户端导出。

构建参数：

- `BuildTarget = StandaloneWindows64`
- 入口场景：`Packages/cn.etetet.statesync/Scenes/Init.unity`
- 输出目录：`Release/WindowsClient/`
- 可执行文件：`Release/WindowsClient/ET.exe`

这里建议将客户端导出调整为目录结构，而不是继续沿用当前 `./Release/ET.exe` 的扁平输出，原因是：

- 更符合“只保留构建目录”的要求
- 便于后续人工验收与目录归档
- 为后续接版本号目录或 CI 产物目录预留空间

另外，当前 `BuildHelper.Build` 里写死的场景路径是错误的，后续实现阶段需要同步修正，避免自动化入口和现有手工入口继续分叉。

### 步骤 7.5：Editor 快速排查入口

为了避免“每次都等十几分钟完整打包后才知道资源缺失”，新增两条专门面向排查的 Editor 菜单：

1. `ET/Loader/Build Windows Client Bundles Only`
2. `ET/Loader/Validate Windows Client Offline Runtime`

推荐排查顺序：

1. 先点 `Build Windows Client Bundles Only`
2. 再点 `Validate Windows Client Offline Runtime`
3. 若校验通过，再在 Editor 中切离线模式复现运行时问题
4. 只有当问题明显进入 Player/IL2CPP/序列化差异层时，才执行完整 `Build Windows Client Offline`

`Validate Windows Client Offline Runtime` 当前重点校验四类 location：

- `Packages/cn.etetet.loader/Bundles/Code/*.dll.bytes` 对应的热更代码 DLL
- `Packages/cn.etetet.loader/Bundles/AotDlls/*.dll.bytes` 对应的 AOT 元数据 DLL
- YIUI 启动关键资源：`YIUIConstAsset`、`YIUIAtlasData`
- 所有非 `ET.Server.*` 的 `ConfigProcessAttribute` 客户端配置类型，逐个验证 `Type.Name` 和 `et_{lowercase}` 两种 location 至少命中一种

这样可以在几秒内提前拦住“资源没进包 / manifest 没 location / 命名不一致”这类问题，而不是等 Windows Player 启动后再从 `Player.log` 逆推。

### 步骤 7.6：Editor 一键运行配置切换

为了避免每次 `Play` 前都手动去改 `GlobalConfig` 和 `YooConfig`，再新增两条纯配置切换按钮：

1. `ET/Loader/Switch To Editor Runtime Config`
2. `ET/Loader/Switch To Package Runtime Config`

约定如下：

- `Switch To Editor Runtime Config`
  - `GlobalConfig.CodeMode = ClientServer`
  - `YooConfig.EPlayMode = EditorSimulateMode`
- `Switch To Package Runtime Config`
  - `GlobalConfig.CodeMode = Client`
  - `YooConfig.EPlayMode = OfflinePlayMode`

两者都必须复用现有 `EnsureCodeMode` / `EnsurePlayMode` 逻辑：

- `CodeMode` 切换时同步调用 `ET.CodeMode.dll`
- `PlayMode` 切换时保存资产并刷新

这样可以避免再次出现“资产值改了，但 asmref / 代码模式没对齐”的半切换状态。

### 步骤 8：构建后恢复原始配置

恢复策略不写死成：

- `ClientServer`
- `EditorSimulateMode`

而是恢复到构建前的真实快照值。

恢复时机：

- 构建成功后恢复
- 中途任何一步失败后也恢复
- 恢复逻辑必须放在 `finally`

恢复内容：

- `GlobalConfig.CodeMode`
- `YooConfig.EPlayMode`
- 若本次构建期间发生过 `CodeMode` 切换，则在把 `GlobalConfig.CodeMode` 写回原值并保存后，必须再调用一次：

```text
Bin/ET.CodeMode.dll --CodeMode={原始值}
```

恢复程序集引用和代码模式

这样做的原因是：

- 避免未来开发态调整后脚本反向污染本地环境
- 满足项目“禁止 hard code”的要求
- 构建失败时也能尽量保证编辑器环境不被破坏

建议恢复顺序如下：

1. 先恢复 `GlobalConfig.CodeMode` 资产值
2. 保存资产并执行 `ET.CodeMode.dll --CodeMode={原始值}`
3. 再恢复 `YooConfig.EPlayMode`
4. 最后统一 `SaveAssets + Refresh`

## 失败处理与日志策略

### 失败处理

以下情况直接失败并返回非 0 退出码：

1. 找不到 `GlobalConfig`
2. 找不到 `YooConfig`
3. 找到多个 `GlobalConfig` 或多个 `YooConfig`
4. `ET.CodeMode.dll` 执行失败
5. YooAsset AB 构建失败
6. `BuildPipeline.BuildPlayer` 失败

所有失败分支都必须先尝试恢复原始配置，再结束流程。

### 日志策略

分两层记录日志：

1. PowerShell 层日志
   - 记录入口参数
   - 记录 Unity 调用命令
   - 记录最终退出码

2. Unity 层日志
   - 记录 `CodeMode` 切换
   - 记录 `PlayMode` 切换
   - 记录每个 YooAsset 资源包的构建结果
   - 记录 `BuildPlayer` 结果
   - 记录配置恢复结果

## 计划涉及文件

如果本设计文档通过评审并进入开发，预计涉及以下文件：

| 文件 | 操作 | 说明 |
|------|------|------|
| `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs` | 新增 | 统一的 Unity 批处理构建入口 |
| `Packages/cn.etetet.loader/Editor/BuildHelper.cs` | 修改 | 补充可指定输出目录的 Windows 构建方法 |
| `Scripts/BuildWindowsClientOffline.ps1` | 新增 | PowerShell 一键打包入口 |
| `Book/09-运维与发布/Windows客户端离线打包开发日志.md` | 新增 | 进入开发后记录实现过程 |

## 验收标准

- [ ] 执行一次 PowerShell 命令即可完成 Windows 客户端离线包构建
- [ ] 构建前不需要人工手动切 `CodeMode`
- [ ] 构建前不需要人工手动切 `OfflinePlayMode`
- [ ] 构建过程中会自动执行 YooAsset AB 构建
- [ ] 最终产物为单独的 Windows 构建目录
- [ ] 构建完成后自动恢复构建前的本地配置
- [ ] 任一步失败时，脚本返回失败退出码且仍会尝试恢复配置

## 风险与约束

1. YooAsset 的部分构建参数保存在 `EditorPrefs`，天然带有“跟机器相关”的属性。第一版方案先复用现有配置并打印日志；后续若需要完全可复现，再把这些参数显式提升为脚本参数。

2. 当前 `BuildEditor` 中的配置资产路径与仓库实际资产路径不一致，后续实现时不能直接复用其 `OnEnable` 逻辑。

3. 构建切换 `CodeMode=Client` 后，若业务包里存在“客户端消息引用了只生成到 Server/ClientServer 的共享 Proto 类型”的历史协议定义问题，Unity 编辑器会先在脚本编译阶段失败。该类问题不属于打包入口本身，但会在本方案执行时被提前暴露，需修正协议源后重新生成 Proto。

4. `AssetDatabase.Refresh()` 和 `ET.CodeMode.dll` 可能导致先前加载的 ScriptableObject 引用失效，恢复逻辑不能长时间持有旧资产引用，必须按资产路径重新加载后再恢复。

5. 若构建前没有把 HybridCLR AOT dll 同步到 `Packages/cn.etetet.loader/Bundles/AotDlls`，则会出现“构建成功但运行时没有 UI”的假成功结果。

6. 本方案只覆盖 Windows 客户端离线体验包，不等于完整发布系统。

7. 当前 `ET.Model` / `ET.Hotfix` / `ET.ModelView` / `ET.HotfixView` 热更程序集是在编辑器环境下编译出来的，`#if UNITY_EDITOR` 内的代码会随 DLL 编译结果保留下来。对客户端热更代码来说，只要分支内直接读取 `Packages/...` 工程目录文件，就可能在发布包运行时错误访问 `Release/WindowsClient/Packages/...`，表现为配置加载失败、迷雾/导航数据缺失或功能静默失效。因此客户端资源来源不能只靠编译期开关决定，必须改为运行时判定或由 `HotfixView`/资源层代理加载。

8. YooAsset 对 invalid location 并不保证抛异常，某些情况下只会记录日志并返回空资源句柄。若客户端配置加载逻辑把 `et_xxx` 命名回退写在 `catch` 中，就会在包内真实资源名为 `et_matchrobotconfigcategory` 这类场景下漏掉回退，进一步因为 `TextAsset` 为 `null` 触发二次空引用，表现为启动早期配置链路中断、UI 根本没有机会初始化。

9. 当前客户端配置类型全部标记为 `ConfigType.Luban`，运行时固定按 `Luban.ByteBuf` 反序列化；如果离线包 `Config` 组错误收集了 `Packages/cn.etetet.excel/Bundles/Luban/Config/Client/Json`，即使 manifest 中 location 存在，启动时也会把 JSON 文本按二进制解读，表现为 `EquipmentConfigCategory` 等配置在构造阶段报重复 key，最终 UI 无法初始化。因此 `Validate Windows Client Offline Runtime` 不能只校验 location 是否存在，还必须校验配置 location 实际映射到的资源格式是否与 `ConfigType` 一致。

## 实现步骤

1. 新增 Windows 客户端离线打包设计文档，并挂接到 W3 周计划
2. 实现 Unity Editor 批处理入口，完成模式切换、YooAsset AB 构建、Windows 客户端构建与恢复逻辑
3. 新增 PowerShell 一键打包脚本，并完成日志和退出码收口
4. 通过一次实际打包验证流程，确认产物目录完整且本地配置恢复正确

## 实现追踪

> 开发完成后回填

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 步骤1：设计文档落地 | 2026-03-30 | `Book/09-运维与发布/Windows客户端离线打包设计文档.md` | 无偏差 |
| 步骤2：实现批处理入口 | 2026-03-30 | `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs`、`Packages/cn.etetet.loader/Editor/BuildHelper.cs`、`Packages/cn.etetet.loader/Editor/ET.Loader.Editor.asmdef` | 离线包构建时额外强制 `BuildinFileCopyOption=ClearAndCopyAll`，避免当前 EditorPrefs 为 `None` 时产出空内置资源；后续补充 Unity 菜单入口，但内部仍复用同一核心逻辑；恢复阶段改为按资产路径重新加载，避免 `Refresh` 后旧引用失效 |
| 步骤2.5：补充快速排查按钮 | 2026-03-30 | `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs` | 新增 `Build Windows Client Bundles Only` 与 `Validate Windows Client Offline Runtime` 两个 Editor 菜单，把大部分资源/manifest 问题前置到完整 Player 打包之前 |
| 步骤2.6：补充一键运行配置按钮 | 2026-03-30 | `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs` | 新增 `Switch To Editor Runtime Config` 与 `Switch To Package Runtime Config` 两个按钮，统一接管 `Play` 前的运行配置切换 |
| 步骤2.7：修正离线配置格式并增强校验 | 2026-03-30 | `Packages/cn.etetet.statesync/Settings/AssetBundleCollectorSetting.asset`、`Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs` | 客户端配置全部是 `ConfigType.Luban`，因此离线包 `Config` 收集路径从 `Client/Json` 改回 `Client/Binary`；同时 `Validate Windows Client Offline Runtime` 从“只看 location”增强为“同时校验映射资源格式是否与 `ConfigType` 一致” |
| 步骤3：实现 PowerShell 一键脚本 | 2026-03-30 | `Scripts/BuildWindowsClientOffline.ps1` | `Unity.exe` 不做硬编码，改为 `-UnityExe` 参数或 `UNITY_EXE` 环境变量输入 |
| 步骤4：实际打包验证 | 2026-03-30 | `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs`、`Packages/cn.etetet.archive/Proto/Archive_C_12020.proto`、`Packages/cn.etetet.archive/Proto/Archive_C_12024.proto`、`Packages/cn.etetet.archive/Proto/Archive_S_22020.proto`、`Packages/cn.etetet.proto/CodeMode/Model/*`、`Packages/cn.etetet.core/Scripts/Core/Share/Helper/UnityEditorReflectionHelper.cs`、`Packages/cn.etetet.core/Scripts/Core/Share/World/World.cs`、`Packages/cn.etetet.core/Scripts/Core/Share/Entity/EntityRef.cs`、`Packages\cn.etetet.behaviortree\Scripts\Model\Share\OdinUnityObject.cs`、`Packages/com.etetet.init/DotNet~/CodeModeChangeHelper.cs`、`Packages/cn.etetet.map/Scripts/HotfixView/Client/ConfigGetAllConfigBytesClient.cs`、`Packages/cn.etetet.map/CodeMode/HotfixView/ClientServer/RecastFileLoader.cs`、`Packages/cn.etetet.map/Scripts/Hotfix/Client/ECAInteractClientComponentSystem.cs`、`Packages/cn.etetet.map/Scripts/HotfixView/Client/ECAConcealmentConfigLoaderHandler.cs`、`Packages/cn.etetet.lockstep/Scripts/HotfixView/Client/ConfigLoaderInvoker.cs` | 实际打包已成功导出 `Release/WindowsClient`；验证阶段先暴露出 Archive 共享 Proto 的历史问题，随后又暴露出 HybridCLR AOT `.bytes` 未进包导致运行时无 UI，再往后又暴露出运行时程序集混入 `UnityEditor.CoreModule`、asmref `.meta` 孤儿文件，以及客户端热更 DLL 误走 `#if UNITY_EDITOR` 工程目录资源路径的问题；本轮已分别通过“反射桥去编译期编辑器依赖”“删除 asmref 时同步删除 `.meta`”“把客户端配置/Recast/ECA/lockstep 配置读取改为运行时判定或跨层 Invoke 资源加载”，以及“配置资源主名返回 `null` 时继续回退到包内真实命名 `et_xxx`”修复，并通过 `dotnet build ET.sln` 再次验证 |
