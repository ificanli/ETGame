# Home收取异常与音频缺失临时止血开发日志

**功能**：Home收取异常与音频缺失临时止血  
**关联设计文档**：[Home收取异常与音频缺失临时止血设计文档.md](./Home收取异常与音频缺失临时止血设计文档.md)  
**关联任务**：M0.2-W3 #18  
**开始时间**：2026-04-13  
**开发者**：AI

## 开发进度

- [x] 步骤1：补计划并创建设计/开发文档
- [x] 步骤2：收口 `HomeCollect` 客户端异常处理
- [x] 步骤3：补音频缺失占位兜底
- [x] 步骤4：把 `LobbyPanel/HomePanel` 清理下沉到场景切换生命周期
- [x] 步骤5：继续收敛 `LayerBlock`，把匹配点击改成短返回
- [x] 步骤6：编译验证并回写文档

## 决策记录

### 2026-04-13 - 先做临时止血，不扩散到正式音频资源修复

- **背景**：用户当前阻塞是“每把都卡”，优先级高于正式资源和 collector 收口。
- **方案**：本轮只保证 `HomeCollect` 不再炸协程，同时让缺失音频直接回退占位音频。
- **原因**：这是最小改动面，能最快恢复可玩性。
- **替代方案**：直接补齐正式音频资源地址和 YooAsset collector。当前排查和回归面更大，不适合作为立即止血。

### 2026-04-13 - 不依赖建筑状态控制收取按钮

- **背景**：`56013` 表面上可以通过“没有产出时禁用按钮”规避，但当前客户端 `HomeBuilding.State` 不是稳定真源。
- **方案**：本轮改成“请求不抛业务异常 + UI 统一兜底提示”，不把按钮可点状态绑定到 `Collecting`。
- **原因**：服务端当前并没有稳定把所有建筑状态写成可供客户端直接信任的收取态，继续依赖该字段会形成假修复。
- **替代方案**：基于本地状态禁用收取按钮。放弃原因是会掩盖真实业务状态，且容易漏掉边界。

### 2026-04-13 - 肉鸽牌点击问题改按 `LobbyPanel` 残留 UI 处理

- **背景**：用户反馈此前对 `HomeCollect` 与音频的止血是无效操作，进游戏后肉鸽牌依旧点不了。
- **方案**：重新复盘 `Editor.log` 后，将主修复点切到 `LobbyPanelComponentSystem.SendMatchRequest()`；匹配成功后不再依赖可能失效的 `self.UIPanel`，统一通过 `root.YIUIMgr()` 兜底关闭 `MatchView` 和整个 `LobbyPanel`。
- **原因**：最新日志明确显示 `OnEventEnterMapInvoke` 在关闭 `MatchView` 时空引用，中断了后续场景切换收尾，才是肉鸽牌点击被遮挡的真实根因。
- **替代方案**：继续深挖 `HomeCollect` 或肉鸽牌 prefab 自身射线。放弃原因是日志已经证明肉鸽弹牌面板成功打开，但点击日志完全没进入，说明更上层有残留 UI 抢占输入。

### 2026-04-13 - 不再把大厅 UI 清理绑死在匹配按钮协程尾部

- **背景**：继续复盘后发现，仅修 `SendMatchRequest()` 里的等待窗/大厅关闭还不够稳，因为 `LobbyPanel` 的正式销毁仍然依赖匹配按钮协程在 `Wait_SceneChangeFinish` 之后自己收尾。
- **方案**：把 `LobbyPanel/HomePanel` 的清理下沉到统一场景切换生命周期：`SceneChangeStart` 先关一轮，进入非 `Home` 地图后再由 `SceneChangeFinishEvent_CreateUIHelp` 对 root UI 栈补一轮兜底。
- **原因**：这条链路不再依赖某个具体按钮协程是否完整走完，更符合“切场时统一清场”的职责分层，也能覆盖未来其他进图入口。
- **替代方案**：继续在 `LobbyPanelComponentSystem.SendMatchRequest()` 里追加更多 try/catch。已放弃，因为这仍然把全局 UI 生命周期绑在单一业务协程上，稳定性不够。

### 2026-04-14 - 匹配点击不再直 await 到进图完成

- **背景**：继续顺着 Hierarchy 里的 `LayerBlock` 追查后，确认它来自 YIUI 全局输入屏蔽层；而 `LobbyPanel` 的 `u_EventEnterMap` 是 `UITaskEventP0`，默认会在点击期间打开 `LayerBlock`。
- **方案**：`OnEventEnterMapInvoke` 只等待“确认起装 + 发匹配请求 + 打开 `MatchView`”，随后立即返回；把 `Wait_MatchSuccess / Wait_SceneChangeFinish / EnterMapFinish / CloseLobbyPanel` 拆到后台协程 `WaitMatchEnterMapFlow()` 继续执行，并补 `IsMatchFlowRunning` 防重入。
- **原因**：这样 `UITaskEventP0` 的点击生命周期会在大厅内尽快结束，不会把全局输入屏蔽层带进局内去挡 `RoguePanel` 点击。
- **替代方案**：直接强制关闭 `LayerBlock`。已放弃，因为这只是掩盖问题，后续其他长链路点击还会再次复现。

## 问题日志

### 2026-04-13 - `ERR_HomeNothingToCollect` 导致 Lobby 协程炸栈

- **现象**：点击家园收取时，`C2M_HomeCollectRequest` 返回 `56013`，客户端直接抛 `RpcException`，`CollectSelectedHomeBuildingAsync().Coroutine()` 进入未捕获异常链。
- **原因**：`HomeClientRequestHelper.Collect()` 默认使用 `Call(request)`，业务错误也会抛异常；Lobby 收取逻辑没有额外兜底。
- **解决**：改为 `Call(request, false)` 返回业务结果，并在 Lobby 侧统一转成 tip；其他 RPC 异常额外 catch，不再卡住交互流程。

### 2026-04-13 - 音频 location 无效持续刷错

- **现象**：`Audio/BGM/bgm_lobby`、`Audio/SFX/choice_popup`、`Audio/SFX/weapon_autorifle_fire` 等 key 缺失时，YooAsset 持续输出 `The location is invalid`。
- **原因**：音频管理器在真实资源模式下直接 `LoadAssetSync`，没有在加载前校验 location 是否有效。
- **解决**：在 `GetOrCreateClip()` 里先做 `package.CheckLocationValid(key)`，无效或加载失败时直接缓存占位音频。

### 2026-04-13 - 肉鸽牌无法点击的真实根因是 `LobbyPanel` 残留

- **现象**：日志中能看到 `M2C_RogueChoicePopup` 已收到，`RoguePanel` 也已经成功打开，但始终没有出现 `[RogueClient] option area click` / `try choose option` / `C2M_RogueChooseOption` 日志。
- **原因**：`LobbyPanelComponent.OnEventEnterMapInvoke` 在匹配成功后调用 `CloseMatchWaitingViewAsync(false)` 时空引用，导致 `Wait_SceneChangeFinish` 之后的 `EnterMapFinish` 和 `LobbyPanel` 关闭链路被截断，大厅 UI 残留在局内挡住了肉鸽牌。
- **解决**：把等待窗和大厅面板的关闭改为 `root` 级别兜底，并吞掉旧 `UIPanel` 失效异常，确保进入战斗后大厅层一定会收掉。

### 2026-04-13 - 仅修匹配协程尾部仍可能留下 stale UI

- **现象**：最新截图里仍同时存在 `HomePanel(Clone)`、`LobbyPanel(Clone)`、`MainPanel(Clone)`、`RoguePanel(Clone)`，且点击牌时没有任何 `[RogueClient] option area click` 日志。
- **原因**：`SceneChangeStart_CloseHomePanel` 只关 `HomePanel`，`LobbyPanel` 仍然没有统一场景切换清理入口；只要匹配按钮协程尾部没有完整执行，旧大厅层仍会残留在 root UI 栈上。
- **解决**：改成 `SceneChangeStart` 先统一关闭 `HomePanel/LobbyPanel`，非 `Home` 地图 `SceneChangeFinish` 再补一次 root UI 兜底关闭。

### 2026-04-13 - `ETAE001` 分析器告警由本轮 `Scene` 局部变量跨 `await` 引入

- **现象**：第一次执行 `dotnet build ET.sln` 时，`LobbyPanelComponentSystem.cs(903,19)` 报错 `ETAE001: 变量 'root' 是 Entity 或其子类类型，不允许在 await 之后访问`。
- **原因**：本轮新增的 `Scene root` 局部变量在 `Wait<Wait_MatchSuccess>()` 之后继续被访问，违反了项目的 `EntityRef` / await 访问规范。
- **解决**：把进图收尾阶段拆成 `matchSuccessRoot / closeWaitingRoot / sceneChangeRoot / finishRoot` 四个 await 前创建的局部，不再让同一个 `Scene` 局部跨 await 继续使用；第二次全量编译已确认这条错误消失。

### 2026-04-14 - `LayerBlock` 的真实持有者是匹配按钮点击本身

- **现象**：局内能看到 `RoguePanel` 已打开，但卡牌完全点不到；Hierarchy 里还能看到 `LayerBlock` 处于激活状态。
- **原因**：`LobbyPanelComponent.OnEventEnterMapInvoke` 是 `UITaskEventP0` 点击事件，默认 `m_BanLayerOption=1`；它又直接 `await SendMatchRequest()`，把“确认起装 → 匹配请求 → 等匹配成功 → 等切场”整条长链路都纳入同一次点击，导致 `LayerBlock` 一直不释放。
- **解决**：把 `SendMatchRequest()` 收口为“只负责起装确认、匹配请求和打开等待窗”，后续等待匹配/切图的部分改由后台协程执行；点击事件只持有很短时间的 `LayerBlock`，不会再把屏蔽层带进局内。

### 2026-04-14 - 新增 `IsMatchFlowRunning` 后首次编译触发 `ETAE001`

- **现象**：第一次执行 `dotnet build ET.sln` 时，`LobbyPanelComponentSystem.cs(116,33/50/118,21)` 报错 `ETAE001`。
- **原因**：`OnEventEnterMapInvoke` 在 `await self.SendMatchRequest(...)` 之后，仍在 `finally` 中直接访问 `self`。
- **解决**：改成在 `await` 前保存 `EntityRef<LobbyPanelComponent>`，`finally` 里重新取回实体后再访问；第二次 `dotnet build ET.sln` 已通过。

### 2026-04-13 - 全量编译被既有测试文件阻塞

- **现象**：两次执行 `dotnet build ET.sln` 都失败在 `Packages/cn.etetet.test/Scripts/Hotfix/Test/Test_PlayerCorpseLoot_Insurance_Test.cs(423,73)`。
- **原因**：现有测试代码把 `Luban.ByteBuf` 传给了只接受 `SimpleJSON.JSONNode` 的接口，属于仓库当前已有编译错误。
- **解决**：本轮未扩散修复该无关问题；已确认 `ET.Hotfix` 与 `ET.HotfixView` 仍能产出，当前止血改动未新增新的编译错误。

## 变更清单

| 时间 | 文件 | 操作 | 说明 |
|------|------|------|------|
| 2026-04-13 | `Book/06-家园系统/Home收取异常与音频缺失临时止血设计文档.md` | 新增 | 建立设计文档 |
| 2026-04-13 | `Book/06-家园系统/Home收取异常与音频缺失临时止血开发日志.md` | 新增 | 建立开发日志 |
| 2026-04-13 | `Packages/cn.etetet.home/Scripts/Hotfix/Client/HomeClientRequestHelper.cs` | 修改 | `Collect` 改为业务错误不抛异常 |
| 2026-04-13 | `Packages/cn.etetet.home/Scripts/Hotfix/Client/HomeClientHelper.cs` | 修改 | 收取成功后同步本地建筑状态为 `Idle` |
| 2026-04-13 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem_Home.cs` | 修改 | 收取失败统一提示并兜住 RPC/运行时异常 |
| 2026-04-13 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/AudioManagerComponentSystem.cs` | 修改 | 音频 key 无效时回退占位音频，避免继续打 YooAsset 错误链 |
| 2026-04-13 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem.cs` | 修改 | 匹配成功后改为 root 级关闭等待窗与大厅面板，避免残留 UI 挡住肉鸽牌 |
| 2026-04-13 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/Scene/SceneChangeStart_CloseHomePanel.cs` | 修改 | 切场开始时统一关闭 `HomePanel/LobbyPanel` |
| 2026-04-13 | `Packages/cn.etetet.map/Scripts/HotfixView/Client/Scene/SceneChangeFinishEvent_CreateUIHelp.cs` | 修改 | 进入非 `Home` 地图后再做 root UI 栈兜底清理 |
| 2026-04-14 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Lobby/LobbyPanelComponent.cs` | 修改 | 增加匹配链路本地防重入状态 `IsMatchFlowRunning` |
| 2026-04-14 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem.cs` | 修改 | 匹配点击改为短返回，匹配成功与切图等待改走后台协程 |
| 2026-04-13 | `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 回写任务18真实根因为 `LobbyPanel` 残留挡点击 |
| 2026-04-13 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 追加任务18真实根因与本轮兜底关闭方案 |
| 2026-04-14 | `Book/06-家园系统/Home收取异常与音频缺失临时止血设计文档.md` | 修改 | 补充 `LayerBlock` 真正来源与匹配点击长 await 根因 |
| 2026-04-14 | `Book/06-家园系统/Home收取异常与音频缺失临时止血开发日志.md` | 修改 | 记录本轮新的技术决策、问题日志与变更清单 |
| 2026-04-14 | `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 回写任务18最新根因为 `LayerBlock` 被匹配点击长 await 持有 |
| 2026-04-14 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 追加任务18最新修复：匹配点击短返回 + 后台等待进图 |
| 2026-04-14 | `ET.sln` | 验证 | 首次编译命中 `ETAE001`，修正 `EntityRef` 后第二次 `dotnet build ET.sln` 已通过 |

## 开发总结

> 开发结束后填写

- **实际完成**：`HomeCollect` 业务错误已改为结果返回，Lobby 收取链路已补业务提示与异常兜底；收取成功后本地建筑状态会回写为 `Idle`；音频 key 缺失时会直接回退占位音频，不再继续打 `The location is invalid`；大厅 UI 清理由“匹配协程尾部兜底”进一步下沉到 `SceneChangeStart + SceneChangeFinish` 双层收口；在此基础上又继续把 `匹配` 按钮从“长 await 点击事件”改为“短返回 + 后台等待匹配/切图”，避免 YIUI 的 `LayerBlock` 被一路带进局内挡住肉鸽牌。
- **未完成**：尚未做 Unity 局内手工回归，当前只完成代码与编译验证。
- **与设计的偏差**：无功能偏差；本轮中途新增两条 `ETAE001` 分析器错误，已在同轮通过 `EntityRef` 修正，最终 `dotnet build ET.sln` 已通过。
- **后续待办**：进游戏复验“进入地图后 `LayerBlock` 不再常驻激活、肉鸽牌点击能出现 `[RogueClient] option area click` / `try choose option` / `C2M_RogueChooseOption` 日志”。
