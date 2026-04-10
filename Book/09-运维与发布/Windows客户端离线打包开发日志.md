# Windows客户端离线打包开发日志

**功能**：Windows 客户端离线打包
**关联设计文档**：[Windows客户端离线打包设计文档](./Windows客户端离线打包设计文档.md)
**关联任务**：M0.2-W3 #7
**开始时间**：2026-03-30
**开发者**：AI

## 开发进度

- [x] 步骤1：补齐设计文档并核对工程事实
- [x] 步骤2：创建开发日志并推进计划状态到开发中
- [x] 步骤3：实现 Unity Editor 批处理构建入口
- [x] 步骤4：实现 PowerShell 一键打包脚本
- [x] 步骤5：执行构建与编译验证并回写文档
- [x] 步骤6：补充 Editor 快速排查按钮
- [x] 步骤7：补充一键运行配置切换按钮

## 决策记录

### 2026-03-30 - 先补包级 AGENTS 与开发日志再动代码
- **背景**：`cn.etetet.loader` 当前没有包级 `AGENTS.md`，而本轮实现会修改该包的 Editor 构建入口。
- **方案**：先补 `Packages/cn.etetet.loader/AGENTS.md`，同时创建本次开发日志，再进入代码实现。
- **原因**：符合仓库对包规范与文档驱动开发的要求，避免后续实现完成后再补元信息。
- **替代方案**：直接进入代码实现。

### 2026-03-30 - 离线包构建时强制拷贝内置 AB
- **背景**：YooAsset 当前 `BuildinFileCopyOption` 的 EditorPrefs 默认值是 `None`，如果直接沿用，资源包构建可能不会拷进 `StreamingAssets`，离线包会打出来但跑不起来。
- **方案**：在本次离线打包入口中，不直接沿用当前包的 `BuildinFileCopyOption`，而是固定对离线包构建使用 `ClearAndCopyAll`。
- **原因**：离线体验包的目标是产出可直接运行的客户端，优先保证包可用，避免把“面板当前没勾拷内置资源”这种人为状态带进自动化结果。
- **替代方案**：完全复用当前 EditorPrefs 配置；问题是当前配置为 `None` 时风险过高。

### 2026-03-30 - 不支持的 YooAsset 管线自动回退到 ScriptableBuildPipeline
- **背景**：离线体验包需要真实 AB，但当前包的 YooAsset 管线可能被用户手动切到 `EditorSimulateBuildPipeline`。
- **方案**：自动化入口只直接支持 `BuiltinBuildPipeline` 和 `ScriptableBuildPipeline`，其它值统一告警并回退到 `ScriptableBuildPipeline`。
- **原因**：保证离线包构建稳定，不把编辑器模拟构建误带进正式体验包。
- **替代方案**：遇到非离线管线直接失败；问题是第一次使用脚本时门槛更高。

### 2026-03-30 - 补 Unity 菜单但不分叉构建逻辑
- **背景**：本地人工打包场景下，用户希望直接在 Unity 里点击菜单完成离线包构建，而不是每次回到 PowerShell 执行脚本。
- **方案**：在 `ET/Loader` 下增加 `Build Windows Client Offline` 菜单项，内部直接复用现有 `BuildWindowsOffline` 核心逻辑，默认输出到 `Release/WindowsClient`。
- **原因**：满足“点一下就构建”的使用习惯，同时避免菜单入口和 PowerShell 入口各维护一套构建流程。
- **替代方案**：继续只保留 PowerShell 入口；问题是本地人工打包操作成本更高。

### 2026-03-30 - Archive 共享 Proto 下沉到客户端可见源文件
- **背景**：离线打包菜单切到 `CodeMode=Client` 后，`Archive_C_12020.cs` 开始引用 `ArchiveBattleRecordSummaryProto`、`ArchiveBattleEventProto`，但这两个类型只从 `Archive_S_22020.proto` 生成到了 `Server/ClientServer`。
- **方案**：新增客户端也会生成的共享协议源 `Archive_C_12024.proto`，把两个纯数据 Proto 抽到该文件中，并让 `Archive_C_12020.proto` / `Archive_S_22020.proto` 统一引用这份共享定义。
- **原因**：问题根源在协议源文件归类，而不是生成产物本身；修源定义并重生 Proto 才能保证后续 `CodeMode=Client` 与 `ClientServer` 都稳定通过编译。
- **替代方案**：手改 `Packages/cn.etetet.proto/CodeMode/Model/Client/*.cs` 生成文件；问题是下次执行 Proto2CS 会被覆盖。

### 2026-03-30 - HybridCLR AOT `.bytes` 必须并入离线打包流程
- **背景**：Windows 离线包已经可以成功导出，但运行后只有背景，没有 UI。
- **方案**：在 `ClientBuildAutomation.BuildWindowsOffline` 中，把 `HybridCLR/Generate/All` 后的 `ET/HybridCLR/CopyAotDlls` 一起纳入自动化流程。
- **原因**：AOT dll 的 `.bytes` 资源位于 `Packages/cn.etetet.loader/Bundles/AotDlls`，只有执行 `CopyAotDlls` 后，YooAsset catalog 才会包含 `mscorlib.dll` 等 location，运行时才能成功加载 AOT 元数据。
- **替代方案**：在运行时给 `CodeLoader` 增加兜底文件读取；问题是会让构建结果继续依赖本地目录状态，偏离离线包自洽目标。

### 2026-03-30 - 运行时程序集里的编辑器 API 改成反射桥接
- **背景**：`HybridCLR/Generate/All` 在 `MonoPInvokeCallbackAnalyzer` 阶段报 `resolve AOT dll:UnityEditor.CoreModule failed`。
- **方案**：把 `ET.Core`、`ET.BehaviorTree` 里残留的 `UnityEditor.Selection`、`UnityEditor.AssetDatabase`、`UnityEditor.InitializeOnLoadMethod` 直接引用全部改成运行时反射桥接，避免热更或 AOT 分析链路看到 `UnityEditor.CoreModule`。
- **原因**：这类代码即便包在 `#if UNITY_EDITOR` 里，只要当前 Unity 编译热更 dll 时定义了 `UNITY_EDITOR`，运行时程序集仍会把 `UnityEditor` 带进引用链，最终卡死 HybridCLR 预生成。
- **替代方案**：只在 HybridCLR 配置里排除相关程序集；问题是根因仍然留在运行时代码里，后续其他工具链仍可能继续炸。

### 2026-03-30 - 反射桥不使用静态缓存，也不直接依赖 UnityEngine
- **背景**：第一次实现 `UnityEditorReflectionHelper` 时，`dotnet build ET.sln` 暴露了两个仓库约束：`ET.Core` 的 `DotNet~` 项目不直接引用 `UnityEngine`，且静态字段必须带标签，触发了 `CS0246` 与 `ET0015`。
- **方案**：将反射桥改为纯 `System.Reflection + object` 形式，所有编辑器类型在方法内临时解析，不再引入静态字段缓存；同时把 `SynchronizationContextKeeper` 改为在 `World` 初始化时捕获当前上下文。
- **原因**：这样既满足 `DotNet~` 编译约束，也不会新增分析器错误，同时保留修复 `UnityEditor.CoreModule` 引用污染的目标。
- **替代方案**：给静态字段补 `[StaticField]` 并继续保留 `UnityEngine` 依赖；问题是 `DotNet~` 仍会先在 `UnityEngine` 上编译失败。

### 2026-03-30 - AOT `.bytes` 拷贝按实际裁剪结果容错
- **背景**：离线打包执行 `ET/HybridCLR/CopyAotDlls` 时，`HybridCLRSettings.patchAOTAssemblies` 中的 `ET.Recast.dll` 在 `HybridCLRData/AssembliesPostIl2CppStrip/StandaloneWindows64` 不存在，导致 `File.Copy` 直接抛异常中断构建。
- **方案**：将 `CopyAotDll` 改为“源目录不存在或一个都没拷到才失败”，对配置中缺失但当前裁剪产物不存在的 dll 仅输出 warning 并跳过。
- **原因**：`patchAOTAssemblies` 是期望清单，不等于每次当前目标平台都会实际产出同名裁剪 dll；离线包需要对这种差异有容错能力。
- **替代方案**：直接从 `HybridCLRSettings.asset` 删除 `ET.Recast.dll`；问题是这会把配置硬编码成当前一次构建的结果，后续如果重新引入该程序集还得再改回去。

### 2026-03-30 - 客户端热更资源来源改成运行时判定，不再依赖 `#if UNITY_EDITOR`
- **背景**：`Player.log` 显示 Windows 离线包启动时，`ConfigGetAllConfigBytesClient` 去读取 `Release/WindowsClient/Packages/cn.etetet.excel/.../et_matchrobotconfigcategory.bytes`，说明热更 DLL 在玩家包里仍执行了编译期 `UNITY_EDITOR` 分支。
- **方案**：把客户端配置/Recast/lockstep 配置读取从编译期开关改成运行时 `Application.isEditor` 判定；对 `ET.Hotfix` 无法直接引用 `UnityEngine/YooAsset` 的 `ECA` 客户端读取，则改成 `Hotfix -> HotfixView` 的同步 `Invoke` 代理加载。
- **原因**：当前热更程序集是在编辑器环境编译出来的，只靠 `#if UNITY_EDITOR` 决定资源来源会把“读工程目录”的代码编进 DLL，导致发布包运行时误访问 `Release/WindowsClient/Packages/...`。
- **替代方案**：继续维持 `#if UNITY_EDITOR`，只靠重新打包规避；问题是根因还在，后续任何类似路径读取都会继续踩坑。

### 2026-03-30 - 配置资源回退不能只放在异常分支
- **背景**：修完工程目录误读后，最新 `Player.log` 继续报 `Failed to mapping location to asset path : MatchRobotConfigCategory`，随后在 `ConfigGetAllConfigBytesClient.Handle` 因 `TextAsset` 为空触发 `NullReferenceException`。
- **方案**：将 `ConfigGetAllConfigBytesClient.LoadConfigAsset` 改为顺序尝试 `configName` 和 `et_{configName.ToLowerInvariant()}` 两个 location；即便第一种没有抛异常、只是返回 `null`，也继续尝试第二种，并在两次都失败时抛出带 location 列表的明确异常。
- **原因**：离线包清单已证明真实资源名是 `et_matchrobotconfigcategory`；而 `ResourcesComponent.LoadAssetAsync` 对 invalid location 的表现是“写日志后返回空对象”，不是保证抛到 `catch`，因此原先“只在异常时回退”的实现不可靠。
- **替代方案**：仅在 `Handle` 层补空指针判定；问题是虽然能避免 NRE，但依然无法命中包内真实资源名，UI 仍起不来。

### 2026-03-30 - 增加“Bundles Only + Offline Runtime Validate”快速排查按钮
- **背景**：完整 Windows 打包耗时长，而当前遇到的多数问题都发生在 Player 启动前的资源准备阶段，例如 AOT `.bytes` 缺 location、配置地址命名不一致、YIUI 启动资源未进包。
- **方案**：在 `ClientBuildAutomation` 中新增两个 Editor 菜单：
  - `ET/Loader/Build Windows Client Bundles Only`
  - `ET/Loader/Validate Windows Client Offline Runtime`
- **原因**：`Build Windows Client Bundles Only` 复用现有离线构建主链路，但跳过 `BuildPlayer`；`Validate Windows Client Offline Runtime` 直接读取当前内置 manifest，快速校验代码 DLL、AOT DLL、YIUI 启动资源，以及所有非 `ET.Server.*` 客户端配置类型的 location 是否存在，可以把大部分问题前置到完整打包之前。
- **替代方案**：继续只保留完整 `Build Windows Client Offline`；问题是排查反馈周期过长，定位效率太低。

### 2026-03-30 - 增加“编辑器运行配置 / 包体运行配置”一键切换按钮
- **背景**：即使有了 `Bundles Only` 和 `Validate`，用户在 Editor 里点 `Play` 前仍然需要手动把 `GlobalConfig.CodeMode` / `YooConfig.EPlayMode` 改成目标状态，操作仍然容易漏。
- **方案**：在 `ClientBuildAutomation` 中再新增两个 Editor 菜单：
  - `ET/Loader/Switch To Editor Runtime Config`
  - `ET/Loader/Switch To Package Runtime Config`
- **原因**：这两个按钮只做运行配置切换，分别落到 `ClientServer + EditorSimulateMode` 和 `Client + OfflinePlayMode`，并且继续复用 `EnsureCodeMode` / `EnsurePlayMode`，避免出现资产值和代码模式不一致。
- **替代方案**：继续让用户手动点资产切换；问题是排查过程仍然容易因人为漏改配置而产生伪问题。

### 2026-03-30 - 客户端离线配置统一回到 Luban Binary
- **背景**：最新 `Player.log` 已经从“找不到 location”进一步收敛到 `EquipmentConfigCategory` 在 `Luban.ByteBuf` 反序列化阶段报重复 key，说明当前离线包虽然能找到配置资源，但配置内容格式不对。
- **方案**：将 `Packages/cn.etetet.statesync/Settings/AssetBundleCollectorSetting.asset` 的客户端 `Config` 收集路径从 `Packages/cn.etetet.excel/Bundles/Luban/Config/Client/Json` 改回 `Packages/cn.etetet.excel/Bundles/Luban/Config/Client/Binary`。
- **原因**：当前客户端配置类型全部标记为 `ConfigType.Luban`，运行时固定走 `ConfigDeserialize_Luban`，只能读取 Luban 二进制；继续把 Json 文本收进包只会在启动阶段制造“location 存在但内容不可反序列化”的假象。
- **替代方案**：保留 `Client/Json` 收集，改造客户端配置反序列化走 JSON；问题是现有自动生成的 Client 配置类型构造函数全部基于 `Luban.ByteBuf`，改动面和风险都明显更大。

### 2026-03-30 - 构建入口需要主动防旧 StreamingAssets 产物
- **背景**：即使 `AssetBundleCollectorSetting.asset` 已经改成 `Client/Binary`，用户在 Editor 点击 `Validate Windows Client Offline Runtime` 时仍然读到 `Assets/StreamingAssets/Bundles/DefaultPackage/DefaultPackage_2026-03-30-1095.bytes`，manifest 里还是 `Client/Json`。同时当前系统时间已经晚于该版本号生成时间，说明校验命中的其实是旧内置包。
- **方案**：在 `ClientBuildAutomation` 中补三层保护：加载 `AssetBundleCollectorSetting` 前强制 `ImportAsset`；每次构建包前先删掉对应 `Assets/StreamingAssets/Bundles/{package}` 旧目录；默认构建版本号从分钟级改成秒级，避免同一分钟内新旧产物撞名。
- **原因**：这样可以把“明明源码改了，但 Validate 还在读旧 manifest”的情况尽量堵住，避免排查被旧 Bundle 污染。
- **替代方案**：仅靠人工记得先删 `Assets/StreamingAssets/Bundles/DefaultPackage`；问题是流程仍然脆弱，且和本轮“尽量减少手工步骤”的目标相冲突。

### 2026-03-30 - 离线打包前必须刷新热更 DLL
- **背景**：`Player.log` 最新错误已从资源层收敛到 `System.MissingMethodException: MethodNotFind ET.EventSystem+<PublishAsync>d__4\`2[ET.Scene,ET.EntryEvent1]::.ctor`。这说明包体启动时，Player 自带运行时代码和 Bundle 中的 `ET.Hotfix/ET.Model` 不一致。
- **方案**：在 `ClientBuildAutomation.BuildWindowsBundlesOnly/BuildWindowsOffline` 中，于切到 `BuildTarget=StandaloneWindows64` 且设好 `CodeMode=Client`、`PlayMode=OfflinePlayMode` 后，强制执行 `AssemblyTool.DoCompile()`，先把 `Temp/Bin/Debug` 与 `Packages/cn.etetet.loader/Bundles/Code/*.dll.bytes` 刷到同一版，再继续 HybridCLR 和 YooAsset 构建；同时让 `Validate Windows Client Offline Runtime` 额外比对 `Temp/Bin/Debug/*.dll` 与 `Bundles/Code/*.dll.bytes` 的哈希，提前发现代码包过期。
- **原因**：当前包体运行时是“Player 自带 ET.Core/Loader + Bundle 里的 ET.Model/Hotfix”，如果构建前没刷新 `Bundles/Code`，就会出现签名对不上、主纤程初始化直接崩掉的假资源问题。
- **替代方案**：继续依赖人工先按 F6 编译再打包；问题是流程不稳定，且与一键离线打包目标相违背。

## 问题日志

### 2026-03-30 - 现有 BuildHelper 场景路径失效
- **现象**：现有 `BuildHelper.Build` 写死 `Packages/cn.etetet.wow/Scenes/Init.unity`。
- **原因**：仓库实际唯一 `Init.unity` 位于 `Packages/cn.etetet.statesync/Scenes/Init.unity`，旧路径已失效。
- **解决**：在本次实现中同步修正现有构建入口，避免自动化入口与手工入口使用不同路径。

### 2026-03-30 - Unity 编译器不接受 `switch case when` 参数解析写法
- **现象**：Unity 编译 `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs` 时在参数解析段报 `CS1002`、`CS1513`。
- **原因**：`ParseBuildRequest` 使用了 `case "-outputDir" when ...` 这一类写法，当前 Unity 使用的 C# 编译器不支持该语法。
- **解决**：将参数解析改为等价的普通 `if/else` 分支，保持参数行为不变，并重新通过 `dotnet build ET.sln` 验证。

### 2026-03-30 - `Debug` 与 `BuildReport` 命名空间冲突
- **现象**：Unity 编译 `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs` 时继续报 `CS0104`，提示 `Debug` 在 `System.Diagnostics.Debug` 与 `UnityEngine.Debug` 之间冲突，`BuildReport` 在 `YooAsset.Editor.BuildReport` 与 `UnityEditor.Build.Reporting.BuildReport` 之间冲突。
- **原因**：文件同时引入了 `System.Diagnostics`、`YooAsset.Editor` 和 `UnityEditor.Build.Reporting`，但代码里直接使用了未限定的类型名。
- **解决**：将 `Debug`、`Process`、Unity 的 `BuildReport` 改为显式别名，避免与 YooAsset 和系统命名空间发生歧义，并重新通过 `dotnet build ET.sln` 验证。

### 2026-03-30 - `CodeMode=Client` 下 Archive 共享 Proto 在客户端缺失
- **现象**：点击 `ET/Loader/Build Windows Client Offline` 后，Unity 在 `Packages/cn.etetet.proto/CodeMode/Model/Client/Archive_C_12020.cs` 报 `CS0246` 和 `MEMPACK019`，提示 `ArchiveBattleRecordSummaryProto`、`ArchiveBattleEventProto` 不存在或不可序列化。
- **原因**：`Archive_C_12020.proto` 里的客户端响应引用了两个共享数据类型，但这两个类型只定义在 `Archive_S_22020.proto`。当前 Proto 生成器不会根据 `import` 自动把 `_S_` 协议同步生成到 `Client` 代码模式，因此 `CodeMode=Client` 必然缺类。
- **解决**：把两个共享数据类型移动到新增的 `Archive_C_12024.proto`，执行 `dotnet Bin/ET.Proto2CS.dll` 重生协议，再分别在 `Client` 和 `ClientServer` 模式下通过 `dotnet build ET.sln` 验证。

### 2026-03-30 - 恢复环境时持有的配置资产引用失效
- **现象**：构建失败后的恢复阶段再次报 `MissingReferenceException`，`ClientBuildAutomation.RestoreEnvironment` 在 `EditorUtility.SetDirty(globalConfig/yooConfig)` 处崩溃。
- **原因**：打包过程中执行了 `AssetDatabase.Refresh()` 和 `ET.CodeMode.dll`，先前读取出来的 `GlobalConfig` / `YooConfig` 对象引用已经失效，但恢复逻辑仍直接复用旧引用。
- **解决**：构建入口改为先记录资产路径，切模式和恢复时都按路径重新加载最新资产对象，再执行 `SetDirty + SaveAssets + Refresh`。

### 2026-03-30 - 当前机器缺少项目要求的 Unity 6000.0.58f2
- **现象**：`ProjectSettings/ProjectVersion.txt` 要求 Unity `6000.0.58f2`，当前机器仅发现 `2021.3.8f1c1` 和 `2022.3.50f1c1`。
- **原因**：本机 Unity 安装版本与项目要求不一致。
- **解决**：本轮先完成 `dotnet build ET.sln` 和 PowerShell 脚本解析验证；实际批处理打包入口需在具备 Unity `6000.0.58f2` 的机器上再跑一遍真机构建验证。

### 2026-03-30 - 包体启动后无 UI，根因是 AOT 元数据没有进包
- **现象**：Windows 离线包能正常出包，但运行后只有背景，没有 UI；`Player.log` 出现 `Failed to mapping location to asset path : mscorlib.dll`、`Failed to load all assets ! The location is invalid : mscorlib.dll`，随后 `ET.CodeLoader.DownloadAsync()` / `Start()` 抛 `NullReferenceException`。
- **原因**：离线打包流程只执行了 `HybridCLR/Generate/All`，没有执行 `ET/HybridCLR/CopyAotDlls`，导致 `Packages/cn.etetet.loader/Bundles/AotDlls` 下没有最新 AOT `.bytes`，YooAsset catalog 中缺少 `mscorlib.dll` 等 location。
- **解决**：把 `ET/HybridCLR/CopyAotDlls` 并入 `ClientBuildAutomation`，确保构建 AB 前就把最新 AOT `.bytes` 准备好。

### 2026-03-30 - 编辑器运行时报 `EquipmentConfig` 重复键 `6`，实际是 CodeMode 半切换
- **现象**：编辑器运行时在 `EquipmentConfig` 反序列化阶段报 `An item with the same key has already been added. Key: 6`。
- **原因**：当前本地 `GlobalConfig.CodeMode` 被留在 `Client`，但 Unity 运行中的程序集仍是 `ClientServer` 版本。结果 `ConfigGetAllConfigBytesClient` 按 `Client` 加载不带 `KV` 的 `EquipmentConfigCategory.bytes`，却被 `ClientServer` 的 `EquipmentConfig` 按“带 `KV`”结构错误解读，最终把后续记录错读成连续的键 `6`。
- **解决**：将 `GlobalConfig.CodeMode` 恢复为 `ClientServer`，执行 `dotnet Bin/ET.CodeMode.dll --CodeMode=ClientServer` 重新对齐 asmref 和代码模式，再让 Unity 重新编译。

### 2026-03-30 - `RogueBuffAssetGeneratorEditor` 在 `CodeMode=Client` 下编译失败
- **现象**：离线打包切到 `CodeMode=Client` 后，`Packages/cn.etetet.statesync/Editor/RogueBuffAssetGeneratorEditor.cs` 报 `CS0234`，提示 `ET.Server` 命名空间不存在。
- **原因**：该 Editor 工具直接 `using ET.Server` 并静态引用 `RogueBuffConfigLoader`，但 `CodeMode=Client` 下 `ET.Hotfix` 不再包含服务端命名空间。
- **解决**：去掉编译期 `ET.Server` 依赖，改为菜单执行时通过反射查找 `ET.Server.RogueBuffConfigLoader.BuildBuiltinBuffConfigs`；若当前代码模式不包含 Server 逻辑，则在运行时给出明确提示，而不是先卡死编辑器编译。

### 2026-03-30 - `HybridCLR/Generate/All` 报 `UnityEditor.CoreModule` 无法解析
- **现象**：Unity 菜单打包时，`PrebuildCommand.GenerateAll()` 在 `MonoPInvokeCallbackAnalyzer` 阶段抛 `resolve AOT dll:UnityEditor.CoreModule failed`。
- **原因**：`cn.etetet.core` 和 `cn.etetet.behaviortree` 的运行时代码里残留了 `UnityEditor.Selection`、`UnityEditor.AssetDatabase`、`UnityEditor.InitializeOnLoadMethod` 的编译期引用，导致热更程序集引用链混入 `UnityEditor.CoreModule`。
- **解决**：新增 `UnityEditorReflectionHelper`，把这些调用全部切到字符串反射；同时将 `SynchronizationContextKeeper` 从 `InitializeOnLoadMethod` 改为 `World` 初始化时主动捕获上下文，再通过 `dotnet build ET.sln` 验证通过。

### 2026-03-30 - `CodeMode` 切换删除 asmref 时遗留孤儿 `.meta`
- **现象**：恢复或切换 `CodeMode` 后，Unity `AssetDatabase.Refresh()` 报 `A meta data file (.meta) exists but its asset '.../AssemblyReference.asmref' can't be found`。
- **原因**：`Packages/com.etetet.init/DotNet~/CodeModeChangeHelper.cs` 删除 `AssemblyReference.asmref` 时，只删了主文件，没有同步删掉同名 `.meta`。
- **解决**：补齐 `DeleteAssemblyReference` 的 `.meta` 清理逻辑，让后续任何一次 `ET.CodeMode.dll` 切换都自动收掉这类孤儿文件。

### 2026-03-30 - `CopyAotDlls` 因缺少 `ET.Recast.dll` 中断
- **现象**：Unity 菜单打包阶段在 `ET.HybridCLREditor.CopyAotDll()` 抛 `FileNotFoundException`，提示找不到 `HybridCLRData/AssembliesPostIl2CppStrip/StandaloneWindows64/ET.Recast.dll`。
- **原因**：当前 Windows 客户端裁剪产物目录里并没有 `ET.Recast.dll`，但 `CopyAotDll` 仍按 `patchAOTAssemblies` 逐个强制拷贝，没有对“配置存在但本次裁剪结果不存在”的情况做容错。
- **解决**：将 `CopyAotDll` 改为按实际存在的裁剪产物拷贝，并对缺失项打印 warning；若源目录不存在或最终一个都没拷到，仍保持失败。

### 2026-03-30 - Windows 包启动时客户端热更误读工程目录
- **现象**：`C:\Users\luxinyu\AppData\LocalLow\test\ET\Player.log` 报 `DirectoryNotFoundException`，路径为 `D:\05ET\MatchTest\ETGame\Release\WindowsClient\Packages\cn.etetet.excel\Bundles\Luban\Config\Client\Binary\et_matchrobotconfigcategory.bytes`，窗口表现为棕色空屏、无 UI。
- **原因**：`Packages/cn.etetet.map/Scripts/HotfixView/Client/ConfigGetAllConfigBytesClient.cs` 使用了 `#if UNITY_EDITOR` 决定配置来源；由于热更 DLL 是在编辑器环境编译的，这段“读工程目录”逻辑被编进 DLL 后，在玩家包里仍会执行。排查同类代码后，又发现 `RecastFileLoader`、`lockstep` 客户端配置读取，以及 `ECA` 客户端本地隐蔽区配置都存在同类风险。
- **解决**：`ConfigGetAllConfigBytesClient`、`RecastFileLoader`、`lockstep` 客户端配置读取统一改为运行时 `Application.isEditor` 判定；`ECAInteractClientComponentSystem` 改为通过新增的 `ECAConcealmentConfigLoaderHandler` 在 `HotfixView` 层同步 `Invoke` 读取，避免 `ET.Hotfix` 直接依赖 `UnityEngine/YooAsset`；随后再次执行 `dotnet build ET.sln`，结果 `0 warning / 0 error`。

### 2026-03-30 - 包内配置真实命名是 `et_xxx`，旧回退逻辑漏执行
- **现象**：修完工程目录误读后，Windows 包和 Editor 运行都继续在 `MatchRobotConfigCategory` 加载阶段中断；`Player.log` 先报 `Failed to mapping location to asset path : MatchRobotConfigCategory`，随后 `ConfigGetAllConfigBytesClient.Handle` 空引用。
- **原因**：`Release/WindowsClient/ET_Data/StreamingAssets/Bundles/DefaultPackage/DefaultPackage_2026-03-30-1030.bytes` 已证明包内真实资源名是 `et_matchrobotconfigcategory`。但旧实现只有 `LoadAssetAsync(TextAsset)` 抛异常才回退到 `et_xxx`，而当前 YooAsset 实际只是写错并返回 `null`。
- **解决**：将 `LoadConfigAsset` 调整为“主名返回 `null` 也继续尝试 `et_xxx`”，并在两次都失败时抛出明确异常，避免真正原因被二次 `NullReferenceException` 覆盖。

### 2026-03-30 - 离线包把客户端 Luban 配置收成了 Json
- **现象**：`Release/WindowsClient/ET_Data/StreamingAssets/Bundles/...manifest` 中，`EquipmentConfigCategory`、`RogueLevelEntryConfigCategory` 等 location 全部映射到 `Packages/cn.etetet.excel/Bundles/Luban/Config/Client/Json/*.json`；运行时随后在 `EquipmentConfigCategory..ctor(Luban.ByteBuf)` 里报重复 key，UI 起不来。
- **原因**：`AssetBundleCollectorSetting.asset` 的 `Config` 组错误地收集了 `Client/Json`，但客户端这些配置类型全是 `ConfigType.Luban`，运行时仍按 `Luban.ByteBuf` 反序列化。
- **解决**：将收集路径改为 `Client/Binary`，并增强 `Validate Windows Client Offline Runtime`，让它额外校验配置 location 实际映射到的资源扩展名和目录是否与 `ConfigType` 一致，避免以后再出现“manifest 看起来有 location，但内容格式错了”的伪通过。

## 变更清单

| 时间 | 文件 | 操作 | 说明 |
|------|------|------|------|
| 2026-03-30 | `Packages/cn.etetet.loader/AGENTS.md` | 新增 | 为将被修改的 loader 包补齐包级规范说明 |
| 2026-03-30 | `Book/09-运维与发布/Windows客户端离线打包开发日志.md` | 新增 | 建立本次打包自动化开发日志 |
| 2026-03-30 | `Packages/cn.etetet.loader/Editor/ET.Loader.Editor.asmdef` | 修改 | 增加对 `ET.YooAssets.Editor` 的编辑器程序集引用 |
| 2026-03-30 | `Packages/cn.etetet.loader/Editor/BuildHelper.cs` | 修改 | 修正默认入口场景为 `cn.etetet.statesync/Scenes/Init.unity`，并补充可指定输出路径的构建方法 |
| 2026-03-30 | `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs` | 新增 | 新增 Windows 客户端离线包批处理构建入口，负责切模式、构建 YooAsset AB、构建客户端并恢复环境 |
| 2026-03-30 | `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs` | 修改 | 将参数解析改为 Unity 兼容的 `if/else` 分支，修复 `CS1002`、`CS1513` 编译错误 |
| 2026-03-30 | `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs` | 修改 | 为 `Debug`、`Process`、Unity `BuildReport` 增加显式别名，修复 `CS0104` 命名冲突 |
| 2026-03-30 | `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs` | 修改 | 增加 `ET/Loader/Build Windows Client Offline` Unity 菜单入口，复用离线打包主流程 |
| 2026-03-30 | `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs` | 修改 | 切模式与恢复阶段改为按资产路径重新加载配置对象，修复失败回滚时的 `MissingReferenceException` |
| 2026-03-30 | `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs` | 修改 | 将 `ET/HybridCLR/CopyAotDlls` 并入离线打包流程，并把 HybridCLR 反射入口改为按已加载程序集兜底查找 |
| 2026-03-30 | `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs` | 修改 | 将 HybridCLR 入口改为强类型直接调用，并为 `ET.Loader.Editor` 补上 `ET.HybridCLR.Editor` 程序集依赖，消除旧反射入口类型查找失败 |
| 2026-03-30 | `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs` | 修改 | 新增 `Build Windows Client Bundles Only` 与 `Validate Windows Client Offline Runtime` 两个 Editor 按钮，前者只构建离线 AB，后者直接读取当前内置 manifest 进行快速资源校验 |
| 2026-03-30 | `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs` | 修改 | 新增 `Switch To Editor Runtime Config` 与 `Switch To Package Runtime Config` 两个 Editor 按钮，分别一键切到开发态和包体态运行配置 |
| 2026-03-30 | `Packages/cn.etetet.loader/Editor/ET.Loader.Editor.asmdef` | 修改 | 增加 `ET.HybridCLR.Editor` 编辑器程序集引用，支撑离线打包流程直接调用 HybridCLR 编辑器入口 |
| 2026-03-30 | `Packages/cn.etetet.statesync/Editor/RogueBuffAssetGeneratorEditor.cs` | 修改 | 去除对 `ET.Server` 的编译期依赖，改为运行时反射调用服务端 Buff 配置生成器，修复 `CodeMode=Client` 下的编辑器编译阻塞 |
| 2026-03-30 | `Packages/cn.etetet.core/Scripts/Core/Share/Helper/UnityEditorReflectionHelper.cs` | 新增 | 新增无编译期 `UnityEditor` 依赖的反射桥，供运行时代码安全访问编辑器 API |
| 2026-03-30 | `Packages/cn.etetet.core/Scripts/Core/Share/World/World.cs` | 修改 | 将 `SynchronizationContextKeeper` 改为 `World` 初始化时捕获上下文，移除 `UnityEditor.InitializeOnLoadMethod` 依赖 |
| 2026-03-30 | `Packages/cn.etetet.core/Scripts/Core/Share/Entity/EntityRef.cs` | 修改 | 将层级选中逻辑改为走反射桥，移除对 `UnityEditor.Selection` 的编译期引用 |
| 2026-03-30 | `Packages/cn.etetet.behaviortree/Scripts/Model/Share/OdinUnityObject.cs` | 修改 | 将资源查找逻辑改为走反射桥，移除对 `UnityEditor.AssetDatabase` 的编译期引用 |
| 2026-03-30 | `Packages/com.etetet.init/DotNet~/CodeModeChangeHelper.cs` | 修改 | 删除 asmref 时同步删除 `.meta`，避免 CodeMode 切换留下孤儿资源元文件 |
| 2026-03-30 | `Packages/com.etetet.init/AGENTS.md` | 新增 | 为 init 包补齐包级约束说明，明确 asmref 与 `.meta` 必须联动处理 |
| 2026-03-30 | `Packages/cn.etetet.hybridclr/Editor/HybridCLREditor.cs` | 修改 | `CopyAotDlls` 改为按实际存在的裁剪产物拷贝，缺失项只记 warning，避免 `ET.Recast.dll` 缺失时直接中断离线包构建 |
| 2026-03-30 | `Packages/cn.etetet.archive/Proto/Archive_C_12020.proto` | 修改 | 客户端战绩协议改为引用共享的客户端可见 Proto 定义 |
| 2026-03-30 | `Packages/cn.etetet.archive/Proto/Archive_C_12024.proto` | 新增 | 抽出战绩详情和战绩摘要的共享 Proto，确保 `CodeMode=Client` 也能生成对应类型 |
| 2026-03-30 | `Packages/cn.etetet.archive/Proto/Archive_S_22020.proto` | 修改 | 服务端协议改为复用 `Archive_C_12024.proto` 中的共享战绩 Proto 定义 |
| 2026-03-30 | `Packages/cn.etetet.proto/CodeMode/Model/Client/*.cs` | 修改 | 重新生成客户端协议代码，补齐 Archive 共享 Proto 类型 |
| 2026-03-30 | `Packages/cn.etetet.proto/CodeMode/Model/Server/*.cs` | 修改 | 重新生成服务端协议代码，同步 Archive 共享 Proto 调整 |
| 2026-03-30 | `Packages/cn.etetet.proto/CodeMode/Model/ClientServer/*.cs` | 修改 | 重新生成双端协议代码，同步 Archive 共享 Proto 调整 |
| 2026-03-30 | `Scripts/BuildWindowsClientOffline.ps1` | 新增 | 新增 PowerShell 一键离线打包脚本，统一接管 Unity batchmode 调用与日志落盘 |
| 2026-03-30 | `Book/09-运维与发布/Windows客户端离线打包设计文档.md` | 修改 | 回写实际实现进度、构建策略和当前验证缺口 |
| 2026-03-30 | `Packages/com.etetet.init/Resources/GlobalConfig.asset` | 修改 | 将失败构建遗留的 `CodeMode=Client` 恢复为 `ClientServer` |
| 2026-03-30 | `Packages/cn.etetet.yooassets/Resources/YooConfig.asset` | 修改 | 将失败构建遗留的 `OfflinePlayMode` 恢复为 `EditorSimulateMode` |
| 2026-03-30 | `Book/09-运维与发布/Windows客户端离线打包开发日志.md` | 修改 | 继续记录 `UnityEditor.CoreModule` 引用污染、asmref.meta 孤儿文件，以及 `dotnet build ET.sln` 回归验证结果 |
| 2026-03-30 | `Packages/cn.etetet.map/Scripts/HotfixView/Client/ConfigGetAllConfigBytesClient.cs` | 修改 | 将客户端配置读取从编译期开关改成运行时 `Application.isEditor` 判定，避免玩家包误读 `Packages/...` |
| 2026-03-30 | `Packages/cn.etetet.map/Scripts/HotfixView/Client/ConfigGetAllConfigBytesClient.cs` | 修改 | 配置资源加载改为“主名返回 `null` 也继续回退到 `et_xxx`”，并在两次都失败时抛明确异常，修复 `MatchRobotConfigCategory` 在包内真实命名为 `et_matchrobotconfigcategory` 时的漏回退问题 |
| 2026-03-30 | `Packages/cn.etetet.map/CodeMode/HotfixView/ClientServer/RecastFileLoader.cs` | 修改 | 将 Recast 读取从 `#if UNITY_EDITOR` 改成运行时判定，避免玩家包误走工程目录 |
| 2026-03-30 | `Packages/cn.etetet.map/Scripts/Model/Client/ECAInteractClientComponent.cs` | 修改 | 新增 `ECAConcealmentConfigLoader` Invoke 参数结构，供 Hotfix 向 HotfixView 代理读取隐蔽区配置 |
| 2026-03-30 | `Packages/cn.etetet.map/Scripts/Hotfix/Client/ECAInteractClientComponentSystem.cs` | 修改 | 本地隐蔽区配置改为通过同步 `Invoke` 从 HotfixView 代理加载，避免 Hotfix 层直接依赖 `UnityEngine/YooAsset` |
| 2026-03-30 | `Packages/cn.etetet.map/Scripts/HotfixView/Client/ECAConcealmentConfigLoaderHandler.cs` | 新增 | 在 HotfixView 层统一处理 ECA 文本配置的编辑器/玩家包读取逻辑 |
| 2026-03-30 | `Packages/cn.etetet.lockstep/Scripts/HotfixView/Client/ConfigLoaderInvoker.cs` | 修改 | lockstep 客户端配置读取改为运行时判定，避免单配置读取仍回落到工程目录 |
| 2026-03-30 | `Book/09-运维与发布/Windows客户端离线打包设计文档.md` | 修改 | 补充客户端热更误走工程目录资源路径的风险、修法与追踪 |
| 2026-03-30 | `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 同步记录任务7本轮对客户端热更资源路径问题的修复与当前验证状态 |
| 2026-03-30 | `Packages/cn.etetet.statesync/Settings/AssetBundleCollectorSetting.asset` | 修改 | 将客户端 `Config` 资源收集从 `Client/Json` 改回 `Client/Binary`，与 `ConfigType.Luban` 运行时反序列化保持一致 |
| 2026-03-30 | `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs` | 修改 | 增强 `Validate Windows Client Offline Runtime`，从“location 存在即可”提升为“location 映射到的资源格式必须与 `ConfigType` 一致” |
| 2026-03-30 | `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs` | 修改 | 在 Bundle 构建前强制重新导入 `AssetBundleCollectorSetting`、清理旧 `StreamingAssets/Bundles/{package}`，并把默认构建版本改成秒级，减少旧 manifest 被误校验的概率 |
| 2026-03-30 | `Packages/cn.etetet.loader/Editor/ClientBuildAutomation.cs` | 修改 | 在离线构建前强制执行 `AssemblyTool.DoCompile()` 刷新 `Bundles/Code`，并让 `Validate` 校验代码包哈希是否与 `Temp/Bin/Debug` 一致，提前拦截代码包过期问题 |
| 2026-03-30 | `Book/09-运维与发布/Windows客户端离线打包设计文档.md` | 修改 | 补充离线包配置格式错配的根因与校验策略 |
| 2026-03-30 | `Book/09-运维与发布/Windows客户端离线打包开发日志.md` | 修改 | 记录本轮离线配置格式错配的根因、修复和校验增强 |

## 开发总结

> 开发结束后填写

- **实际完成**：已实现 Unity Editor 批处理入口、修正 `BuildHelper` 默认场景路径、补齐 PowerShell 一键离线打包脚本，并补充完整离线包按钮、`Bundles Only + Offline Runtime Validate` 快速排查按钮，以及 `编辑器运行配置 / 包体运行配置` 一键切换按钮；同时完成 Archive 共享 Proto 修正、恢复逻辑稳定性修复、HybridCLR AOT `.bytes` 自动拷贝、本地 `CodeMode` 半切换环境修复、运行时程序集 `UnityEditor` 引用清理、asmref `.meta` 清理、客户端热更误走工程目录资源路径修复、包内 `et_xxx` 配置资源命名回退修复，以及离线包客户端配置从 `Client/Json` 修正为 `Client/Binary` 并增强离线配置格式校验，并通过 `dotnet build ET.sln` 验证。
- **未完成**：尚未在最新代码下重新执行一次 Unity 真机离线包构建并回看运行时日志，当前仍缺最终的包内启动回归。
- **与设计的偏差**：离线包构建时没有直接沿用当前包的 `BuildinFileCopyOption`，而是强制使用 `ClearAndCopyAll`，以确保内置资源一定进入 `StreamingAssets`。
- **后续待办**：在 Unity 中先点 `ET/Loader/Switch To Package Runtime Config`，再按 `Build Windows Client Bundles Only -> Validate Windows Client Offline Runtime -> Play` 的顺序排查；重点看 `Validate` 是否还报配置格式不匹配。排完后可点 `ET/Loader/Switch To Editor Runtime Config` 回到开发态。若资源链路全部通过，再执行 `ET/Loader/Build Windows Client Offline` 或 `Scripts/BuildWindowsClientOffline.ps1` 做最终 Player 回归。
