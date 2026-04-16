# Home主界面开发日志

**功能**：Home主界面首期实现  
**关联设计文档**：[Home主界面需求文档.md](Home主界面需求文档.md)  
**关联任务**：M0.2-W3 #15  
**开始时间**：2026-04-10  
**开发者**：AI

## 开发进度

- [x] 步骤1：补计划并创建需求/开发文档
- [x] 步骤2：打通进入 Home 的快照推送与客户端缓存
- [x] 步骤3：接通 `建造/升级/拆除/收取` 的服务端 Handler
- [x] 步骤4：调整 Lobby 默认落点并接入 Home 首期主界面
- [x] 步骤5：完成首页操作链路并跑通编译验证
- [x] 步骤6：回写设计/计划文档

## 决策记录

### 2026-04-10 - 首期载体继续复用 LobbyPanel.BuildPanel

- **背景**：用户要求必须做 `Home` 方向真实功能，但当前工程没有独立 `HomePanel` 资源体系。
- **方案**：先把 `LobbyPanel.BuildPanel` 作为 `Home` 首期主界面载体，先拿到首页承接和真实操作闭环。
- **原因**：现有容器已存在，能最快把工作重心放到“真实功能接通”而不是“新 prefab 体系搭建”。
- **替代方案**：直接新建完整 `HomePanel` prefab + YIUI 生成链。当前工作量明显更大，会推迟首个真实闭环落地，因此本轮不选。

### 2026-04-10 - 本期范围收敛为首页承接 + 建造/升级/拆除/收取

- **背景**：`Home` 全量规划覆盖生产、合同、仓储、战损、出击准备等完整外围循环，单轮无法安全全部接完。
- **方案**：本期只接首页承接、快照同步和 `建造/升级/拆除/收取` 首段真实闭环。
- **原因**：这四条链路已有服务端 Helper，可在当前代码基线上形成第一段可人工体验的真实功能。
- **替代方案**：一次做完整家园 UI 闭环。风险过高，且容易把首页承接问题继续拖延。

### 2026-04-10 - `BuildPanel` 先用运行时结构承接首期首页

- **背景**：当前 `LobbyPanel.BuildPanel` 仍是旧占位壳，没有足够的静态节点可直接绑定 `Home` 首页。
- **方案**：不新建完整 `HomePanel` prefab，先在 `BuildPanel` 容器下运行时补齐首页骨架、建筑列表和详情操作区。
- **原因**：用户要求先做真实未完成功能并尽快跑通闭环；在现有资源基础上，运行时承接是本轮最小、最稳的落地路径。
- **替代方案**：直接改 prefab 并重走完整 YIUI 资源生成链。当前会明显放大工作面，因此推迟到正式资源期处理。

### 2026-04-10 - Home 首页静态骨架迁回 prefab，并通过 YIUIMCP 菜单构建

- **背景**：首期功能闭环虽然已经跑通，但页面主体仍由 `LobbyPanelComponentSystem_Home.cs` 在运行时动态创建，这不符合 YIUI 页面主结构应优先落资源层的约束。
- **方案**：新增 `HomeYiuiBuilder` 编辑器菜单 `ET/YIUI/Build Home Resources`，由它把 `Home` 首页静态骨架写入 `LobbyPanel.BuildPanel` prefab；客户端运行时只保留数据刷新、按钮绑定和建筑列表实例化。
- **原因**：这样既满足“用 MCP 做”的要求，也把页面骨架从运行时代码迁回 prefab，后续维护和继续扩展 `Home` 首页都会更稳。
- **替代方案**：继续保留运行时代码建壳，只用 MCP 做编译和日志验证。这样无法真正把 YIUI 页面结构迁回资源层，因此不选。

### 2026-04-10 - 保留中文文案，统一切到中文 TMP 字体

- **背景**：`Home` 首页已经回到 prefab 资源层，但当前引用的大厅模板字体不包含中文字形，导致默认中文文案显示异常。
- **方案**：保留原有中文文案，不改成英文；静态 prefab 和运行时动态文本统一切到 `SourceHanSansSC-VF SDF`。
- **原因**：页面语义本来就是中文口径，直接换字体比改文案更符合项目当前 UI 语言环境。
- **替代方案**：把 `Home` 页全部改成英文默认文案。用户已明确不接受，因此不选。

### 2026-04-10 - 收口残留混合英文展示文案

- **背景**：字体切换后，首页标题和少量提示里仍残留 `Home 场景`、`Build 页`、`HomeVersion` 这类混合英文展示文本。
- **方案**：只收口用户可见展示文案，统一改成“基地首页 / 家园场景 / 建造页 / 家园版本”中文口径，不改变量名、节点名和协议字段。
- **原因**：用户明确要求保留中文文案，展示层继续保留混合英文会让实际观感不一致。
- **替代方案**：只换字体，不处理残留英文。虽然不影响功能，但不满足最终展示要求，因此不选。

### 2026-04-10 - 原 `SourceHanSansSC-VF SDF` 不是可用中文 TMP 资产，改为运行构建真实中文字体

- **背景**：Unity 场景中实际显示仍然是方块，进一步核对后发现 `SourceHanSansSC-VF SDF.asset` 的字符表并不包含 `基地首页 / 建造入口 / 建筑详情` 等 Home 实际用字。
- **方案**：不再继续引用这个失效 TMP 资产；改为在 `ET/YIUI/Build Home Resources` 流程里，使用包内真实中文 `ttf` `锐字云字库粗圆体GBK_爱给网_aigei_com.ttf` 自动生成 `Resources/Fonts/HomeChinese SDF.asset`，再由 Home prefab 和运行时统一引用它。
- **原因**：问题根因不是“没有切路径”，而是“切到的 TMP 资产本身没烘出中文”。只有重新生成真正包含中文字形的 TMP 资产，方块问题才能消失。
- **替代方案**：继续沿用 `SourceHanSansSC-VF SDF` 或 `Arial Unicode SDF`。两者当前字符表都不覆盖 Home 所需汉字，因此不选。

## 问题日志

### 2026-04-10 - `ET.Model` 分析器阻塞客户端 Home 运行时 DTO

- **现象**：`dotnet build ET.sln` 首次失败，`HomeClientComponent.cs` 内 3 个普通 class 被 `ET0032` 拦截。
- **原因**：`Model` 程序集新增的普通 class 未显式标记 `[EnableClass]`。
- **解决**：为 `HomeClientBuildingData`、`HomeClientProductionOrderData`、`HomeClientContractData` 补齐 `[EnableClass]`，保持原有调用方式不变。

### 2026-04-10 - `ET.HotfixView` 分析器约束导致 Home 页面脚本二次编译失败

- **现象**：首轮 `Home` 页面脚本补完后，编译继续报 `ET0004`、`ET0008` 和 TMP 过时 API 错误。
- **原因**：Hotfix 程序集不允许 `static readonly` 字段；非异步上下文直调 `ETTask` 方法需显式 `.Coroutine()`；`TMP_Text.enableWordWrapping` 已废弃。
- **解决**：将统计标签改成 `switch` 方法返回；按钮异步点击改成显式 `.Coroutine()`；文本换行改为 `textWrappingMode`。

## 变更清单

| 时间 | 文件 | 操作 | 说明 |
|------|------|------|------|
| 2026-04-10 | `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 登记 W3 任务15 |
| 2026-04-10 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 回写版本变更记录 |
| 2026-04-10 | `Book/06-家园系统/Home主界面需求文档.md` | 新增 | 创建需求文档 |
| 2026-04-10 | `Book/06-家园系统/Home主界面开发日志.md` | 新增 | 创建开发日志 |
| 2026-04-10 | `Packages/cn.etetet.home/Scripts/Model/Client/HomeClientComponent.cs` | 新增/修改 | 新增客户端 Home 运行时缓存 DTO，并补齐 `[EnableClass]` 通过模型分析器 |
| 2026-04-10 | `Packages/cn.etetet.home/Scripts/Hotfix/Client/HomeClientComponentSystem.cs` | 新增 | 补客户端 Home 缓存生命周期与查询辅助 |
| 2026-04-10 | `Packages/cn.etetet.home/Scripts/Hotfix/Client/HomeClientHelper.cs` | 新增 | 补快照/请求响应写回本地运行时缓存 |
| 2026-04-10 | `Packages/cn.etetet.home/Scripts/Hotfix/Client/HomeClientRequestHelper.cs` | 新增 | 封装客户端 `建造/升级/拆除/收取` 请求 |
| 2026-04-10 | `Packages/cn.etetet.home/Scripts/Hotfix/Client/M2C_HomeSnapshotHandler.cs` | 新增 | 接入 Home 快照推送 |
| 2026-04-10 | `Packages/cn.etetet.home/Scripts/Hotfix/Client/M2C_HomeContractsChangedHandler.cs` | 新增 | 接入合同列表变更推送 |
| 2026-04-10 | `Packages/cn.etetet.home/Scripts/Hotfix/Server/Helper/HomeEnterHelper.cs` | 修改 | 进入 Home 时补齐生产/合同组件与默认合同刷新 |
| 2026-04-10 | `Packages/cn.etetet.home/Scripts/Hotfix/Server/Helper/HomeSnapshotMessageHelper.cs` | 新增 | 统一构造 Home 快照消息 |
| 2026-04-10 | `Packages/cn.etetet.home/Scripts/Hotfix/Server/Handler/C2M_HomeBuildRequestHandler.cs` | 新增 | 接通建造请求 |
| 2026-04-10 | `Packages/cn.etetet.home/Scripts/Hotfix/Server/Handler/C2M_HomeUpgradeRequestHandler.cs` | 新增 | 接通升级请求 |
| 2026-04-10 | `Packages/cn.etetet.home/Scripts/Hotfix/Server/Handler/C2M_HomeDemolishRequestHandler.cs` | 新增 | 接通拆除请求 |
| 2026-04-10 | `Packages/cn.etetet.home/Scripts/Hotfix/Server/Handler/C2M_HomeCollectRequestHandler.cs` | 新增 | 接通收取请求 |
| 2026-04-10 | `Packages/cn.etetet.map/Scripts/Hotfix/Server/Map/M2M_UnitTransferRequestHandler.cs` | 修改 | 玩家进入 Home 时主动推送快照 |
| 2026-04-10 | `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Lobby/Prefabs/LobbyPanel.prefab` | 修改 | 将 `Home` 首页静态骨架落到 `BuildPanel` prefab |
| 2026-04-10 | `Packages/cn.etetet.statesync/Editor/HomeYiuiBuilder.cs` | 新增 | 新增 `ET/YIUI/Build Home Resources` 编辑器菜单，供 `YIUIMCP ExecuteMenu` 驱动构建 `Home` 资源 |
| 2026-04-10 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Lobby/LobbyPanelComponent.Home.cs` | 新增 | 扩展 `LobbyPanel` 的 Home 运行时字段 |
| 2026-04-10 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem.cs` | 修改 | Home 场景默认落到 `BuildPanel` |
| 2026-04-10 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem_LoadoutUi.cs` | 修改 | 补 Home UI 的自动刷新入口 |
| 2026-04-10 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem_Home.cs` | 新增/修改 | 收掉运行时整页建壳，改为消费 prefab 中的 `Home` 静态骨架，并保留列表动态项与交互刷新 |
| 2026-04-10 | `Packages/cn.etetet.statesync/Editor/HomeYiuiBuilder.cs` | 修改 | `Home` 首页静态文本改为继续保留中文，并统一改用 `SourceHanSansSC-VF SDF` |
| 2026-04-10 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem_Home.cs` | 修改 | 运行时动态文本保留中文文案，并优先加载中文 `TMP_FontAsset` |
| 2026-04-10 | `Packages/cn.etetet.statesync/Editor/HomeYiuiBuilder.cs` | 修改 | 首页标题收口为纯中文“基地首页” |
| 2026-04-10 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem_Home.cs` | 修改 | 将残留 `Home 场景 / Build 页 / HomeVersion` 展示文案收口为中文 |
| 2026-04-10 | `Packages/cn.etetet.statesync/Editor/HomeYiuiBuilder.cs` | 修改 | 在 Home 资源构建流程中自动生成可用中文 TMP 字体 `HomeChinese SDF` |
| 2026-04-10 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem_Home.cs` | 修改 | Home 运行时字体资源路径改为 `Fonts/HomeChinese SDF` |

## 开发总结

- **实际完成**：补齐了 `Home` 进入时快照推送、客户端运行时缓存、`建造/升级/拆除/收取` 请求处理，并把 `Home` 首页静态骨架落到 `LobbyPanel.BuildPanel` prefab，再通过 `YIUIMCP ExecuteMenu` 实测驱动资源构建成功。
- **未完成**：仍未新建独立 `HomePanel`；工坊、合同、仓储等独立页面仍未展开。
- **与设计的偏差**：没有扩展为独立 `HomePanel` prefab，但首期首页主骨架已不再由运行时代码拼装，而是回落到现有 `LobbyPanel.BuildPanel` 资源层。
- **后续待办**：进入 Unity/游戏内执行完整人工流程，确认 `Home` 入图、空态、建造、升级、收取、拆除和“去配装/去匹配”跳转表现。
