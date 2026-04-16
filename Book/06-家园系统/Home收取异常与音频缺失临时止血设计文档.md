# Home收取异常与音频缺失临时止血设计文档

**创建时间**：2026-04-13  
**最后更新**：2026-04-14  
**状态**：已完成  
**关联任务**：M0.2-W3 #18  
**涉及包**：cn.etetet.home, cn.etetet.statesync, cn.etetet.yooassets, cn.etetet.map

## 需求概述

当前局内出现“选肉鸽牌时卡死”的高频问题。现场日志同时暴露出两类异常：

1. `C2M_HomeCollectRequest` 返回 `56013`，客户端把“当前没有可收取产出”当成异常直接抛出，导致 UI 协程报错。
2. 多个音频 key（如 `Audio/BGM/bgm_lobby`、`Audio/SFX/choice_popup`、`Audio/SFX/weapon_autorifle_fire`）在 YooAsset 中 location 无效，导致每次开局/弹牌/开火都持续刷错。

补充复盘最新 `Editor.log` 后确认，上述两项不是“肉鸽牌点不了”的主因。当前真正的问题已经进一步收敛为：

3. 虽然此前已经把 `SendMatchRequest()` 的等待窗关闭改成 `root.YIUIMgr()` 兜底，但 `LobbyPanel/HomePanel` 的正式清理仍然没有下沉到统一的场景切换生命周期。
4. 当前代码里 `SceneChangeStart_CloseHomePanel` 只会在切场时关闭 `HomePanel`，而 `LobbyPanel` 仍然依赖 `LobbyPanelComponentSystem.SendMatchRequest()` 在 `Wait_SceneChangeFinish` 之后的尾部自行关闭。
5. 一旦这条匹配按钮协程没有完整走完，或后续切场路径发生分叉，`LobbyPanel` 就会残留在 root UI 栈中；截图里同时存在 `HomePanel(Clone)` / `LobbyPanel(Clone)` / `MainPanel(Clone)` / `RoguePanel(Clone)`，日志里又完全没有 `[RogueClient] option area click`，说明肉鸽牌点击在更上层就被旧 UI 抢走了。
6. 继续顺着 Hierarchy 里的 `LayerBlock` 追查后，确认这不只是“旧 UI 残留”问题。`LayerBlock` 是 YIUI 运行时自动创建的全局输入屏蔽层，而 `LobbyPanelComponent.OnEventEnterMapInvoke` 对应的是 `UITaskEventP0` 点击事件，默认 `m_BanLayerOption=1`。当前它直接 `await SendMatchRequest()`，把“确认起装 → 发匹配请求 → 等匹配成功 → 等切场完成”整条长链路都包进一次点击里，导致全局屏蔽被一路带进局内，肉鸽牌面板即使成功打开也点不到。

本轮目标是做临时止血，优先保证玩家能继续完整跑局和选牌，不在本轮处理正式音频资源导入与 collector 配置。

## 技术方案

### 整体思路

1. `HomeCollect` 客户端请求改为“不因业务错误自动抛异常”。
2. `LobbyPanel` 的收取逻辑对失败结果和异常统一转成 tip，不再让协程炸栈。
3. `Home` 客户端本地运行时在收取成功后同步把建筑状态重置为 `Idle`，避免 UI 继续显示旧状态。
4. 音频管理器在发现 location 无效或加载失败时，不再继续触发 YooAsset 的错误链路，而是回退到占位音频。
5. `LobbyPanel` 进图链路不再只依赖匹配按钮协程尾部做 UI 清理；切场开始时统一关闭 `HomePanel + LobbyPanel`，进入非 `Home` 地图后再做一次 root UI 栈兜底清理，确保旧大厅层不会残留挡住局内输入。
6. `匹配` 按钮点击只负责“确认起装 + 发匹配请求 + 打开 `MatchView`”，随后立即返回，让 `UITaskEventP0` 释放 `LayerBlock`；`Wait_MatchSuccess / Wait_SceneChangeFinish / EnterMapFinish / CloseLobbyPanel` 改由后台协程继续等待，不再绑死在同一次点击事件里。同时补本地 `IsMatchFlowRunning` 防重入，避免重复点匹配按钮。

### 涉及的包和文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 补登记临时止血任务 |
| `Book/08-版本计划/M0.2版本计划.md` | 修改 | 追加变更记录 |
| `Book/06-家园系统/Home收取异常与音频缺失临时止血设计文档.md` | 新增 | 本设计文档 |
| `Book/06-家园系统/Home收取异常与音频缺失临时止血开发日志.md` | 新增 | 本开发日志 |
| `Packages/cn.etetet.home/Scripts/Hotfix/Client/HomeClientRequestHelper.cs` | 修改 | `Collect` 改为业务错误不抛异常 |
| `Packages/cn.etetet.home/Scripts/Hotfix/Client/HomeClientHelper.cs` | 修改 | 收取成功后同步本地建筑状态 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem_Home.cs` | 修改 | 收取失败统一转 tip，不再炸协程 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/AudioManagerComponentSystem.cs` | 修改 | 音频 location 无效时回退占位音频 |
| `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Lobby/LobbyPanelComponent.cs` | 修改 | 增加匹配链路本地防重入状态 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem.cs` | 修改 | 把匹配点击改为短返回，长等待改后台协程 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/Scene/SceneChangeStart_CloseHomePanel.cs` | 修改 | 切场开始时统一关闭 `HomePanel + LobbyPanel` |
| `Packages/cn.etetet.map/Scripts/HotfixView/Client/Scene/SceneChangeFinishEvent_CreateUIHelp.cs` | 修改 | 进入非 `Home` 地图后再做 root UI 栈兜底清理 |

## 接口设计

不新增协议，不改服务端错误码。

收口点：

1. `HomeClientRequestHelper.Collect(root, buildingId)` 改用 `Call(request, false)`
2. `CollectSelectedHomeBuildingAsync()` 对 `response.Error != ERR_Success` 做业务提示
3. `AudioManagerComponentSystem.GetOrCreateClip()` 先检查 `package.CheckLocationValid(key)`，无效则直接 fallback
4. `SceneChangeStart_CloseHomePanel.Run()` 在 `args.ChangeScene == true` 时，统一关闭 `HomePanel` 和 `LobbyPanel`
5. `SceneChangeFinishEvent_CreateUIHelp.Run()` 在非 `Home` 分支打开 `MainPanel/HUDPanel` 后，再对 root `YIUIMgr` 上残留的 `HomePanel/LobbyPanel` 做一次异步关闭兜底
6. `LobbyPanelComponent.OnEventEnterMapInvoke()` 不再 `await` 整条进图链路；它只等待 `SendMatchRequest()` 完成“确认起装 + 匹配请求 + 打开等待窗”，之后由 `WaitMatchEnterMapFlow()` 在后台继续等待 `Wait_MatchSuccess / Wait_SceneChangeFinish`
7. `LobbyPanelComponent` 新增 `IsMatchFlowRunning`，用于防止重复点击匹配按钮

## 实现步骤

1. 补计划并创建设计/开发文档。
2. 收口 `HomeCollect` 客户端异常处理，改成业务错误提示。
3. 补 `Home` 本地状态写回和音频占位兜底。
4. 把 `LobbyPanel/HomePanel` 清理下沉到 `SceneChangeStart + SceneChangeFinish` 生命周期，恢复肉鸽牌点击。
5. 继续收敛 `LayerBlock`，把 `匹配` 按钮改成短返回点击事件，避免全局输入屏蔽被带进局内。
6. 编译验证并回写文档/计划状态。

## 验收标准

- [ ] `ERR_HomeNothingToCollect(56013)` 不再把客户端协程打成未捕获异常
- [ ] 收取失败会转成可见 tip
- [ ] 音频 location 无效时不再持续输出 `The location is invalid`
- [ ] 切场开始时 `HomePanel` 与 `LobbyPanel` 会被统一清理
- [ ] 进入非 `Home` 地图后 root UI 栈不再残留 `HomePanel/LobbyPanel`
- [ ] 进图后肉鸽牌可正常点击，客户端能发出选牌请求
- [ ] `dotnet build ET.sln` 通过

## 关联文档

- [Home主界面需求文档.md](./Home主界面需求文档.md)
- [Home主界面开发日志.md](./Home主界面开发日志.md)

## 实现追踪

> 开发完成后由 AI 自动填写

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 步骤1 | 2026-04-13 | `Book/08-版本计划/M0.2-W3周计划.md`、`Book/08-版本计划/M0.2版本计划.md`、`Book/06-家园系统/Home收取异常与音频缺失临时止血设计文档.md`、`Book/06-家园系统/Home收取异常与音频缺失临时止血开发日志.md` | 无偏差 |
| 步骤2 | 2026-04-13 | `Packages/cn.etetet.home/Scripts/Hotfix/Client/HomeClientRequestHelper.cs`、`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem_Home.cs` | 无偏差 |
| 步骤3 | 2026-04-13 | `Packages/cn.etetet.home/Scripts/Hotfix/Client/HomeClientHelper.cs`、`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/AudioManagerComponentSystem.cs` | 无偏差 |
| 步骤4 | 2026-04-13 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/Scene/SceneChangeStart_CloseHomePanel.cs`、`Packages/cn.etetet.map/Scripts/HotfixView/Client/Scene/SceneChangeFinishEvent_CreateUIHelp.cs` | 复盘最新日志后确认只修匹配协程尾部还不稳，本步改为场景切换生命周期统一清理 root UI 残留 |
| 步骤5 | 2026-04-14 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Lobby/LobbyPanelComponent.cs`、`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem.cs` | 根因继续收敛到 `LayerBlock`，将匹配点击改为短返回，长等待移到后台协程 |
| 步骤6 | 2026-04-14 | `ET.sln` | `dotnet build ET.sln` 已通过，无新增编译错误 |
