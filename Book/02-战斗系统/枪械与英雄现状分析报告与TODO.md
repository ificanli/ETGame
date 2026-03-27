# 枪械与英雄现状分析报告与 TODO

更新时间：2026-03-02  
范围：局外起装、局内切枪与射击、锁定模式、动画与持枪模型、英雄技能

## 1. 需求对照结论

| 需求项 | 现状判定 | 说明 |
|---|---|---|
| 局外选择并装配英雄/枪械/背包，进图后生效 | 已实现（主链路） | 已有起装确认与进图应用逻辑 |
| 局内 UI 显示2把枪并点击切换 | 部分实现 | 有 `SwitchWeapon`，但未见完整“局内武器栏 UI + 协议 + 同步”闭环 |
| 射击触发：部分武器可移动射击、部分静止射击 | 未实现（按武器分型） | 未看到按武器配置控制“移动可射/静止可射” |
| 锁定方式：位置/方向/强制目标追踪 | 部分实现 | 目标选择体系存在，但武器系统中主要是追踪子弹 |
| 动画：持枪模型、切枪换模型、无枪空手动画 | 关键缺口 | 有 Animator 与 BindPoint，但未见完整武器挂接/切换状态机链路 |
| 英雄技能（桃子/草莓/葡萄） | 未落地到本批需求 | 未见与本次英雄定位强绑定的完整技能映射实现 |

## 2. 代码证据定位

## 2.1 已实现链路（局外到进图）

1. 起装确认与合法性校验  
`Packages/cn.etetet.equipment/Scripts/Hotfix/Server/C2G_ConfirmLoadoutHandler.cs:23`  
`Packages/cn.etetet.equipment/Scripts/Hotfix/Server/C2G_ConfirmLoadoutHandler.cs:68`

2. 英雄列表获取  
`Packages/cn.etetet.equipment/Scripts/Hotfix/Server/C2G_GetHeroListHandler.cs:12`

3. 进图读取起装并映射英雄 UnitConfig  
`Packages/cn.etetet.map/Scripts/Hotfix/Server/C2G_EnterMapHandler.cs:22`  
`Packages/cn.etetet.map/Scripts/Hotfix/Server/C2G_EnterMapHandler.cs:25`  
`Packages/cn.etetet.map/Scripts/Hotfix/Server/C2G_EnterMapHandler.cs:38`

4. 起装应用到装备槽  
`Packages/cn.etetet.equipment/Scripts/Hotfix/Server/LoadoutHelper.cs:27`  
`Packages/cn.etetet.equipment/Scripts/Hotfix/Server/LoadoutHelper.cs:33`  
`Packages/cn.etetet.equipment/Scripts/Hotfix/Server/LoadoutHelper.cs:45`

## 2.2 局内武器与射击现状

1. 双武器数据结构存在（Rifle/SMG）  
`Packages/cn.etetet.statesync/Scripts/Model/Share/WeaponComponent.cs:11`  
`Packages/cn.etetet.statesync/Scripts/Model/Share/WeaponComponent.cs:14`  
`Packages/cn.etetet.statesync/Scripts/Model/Share/WeaponComponent.cs:17`

2. 切枪方法存在  
`Packages/cn.etetet.statesync/Scripts/Hotfix/Share/WeaponComponentSystem.cs:159`

3. 硬编码证据（需配置化）  
`Packages/cn.etetet.statesync/Scripts/Hotfix/Share/WeaponComponentSystem.cs:16`  
`Packages/cn.etetet.statesync/Scripts/Hotfix/Share/WeaponComponentSystem.cs:99`  
`Packages/cn.etetet.statesync/Scripts/Hotfix/Server/BTNode/BTWeaponFireHandler.cs:29`  
`Packages/cn.etetet.statesync/Scripts/Hotfix/Share/BulletComponentSystem.cs:14`

4. 当前子弹为目标追踪飞行  
`Packages/cn.etetet.statesync/Scripts/Hotfix/Share/BulletComponentSystem.cs:39`  
`Packages/cn.etetet.statesync/Scripts/Hotfix/Share/BulletComponentSystem.cs:48`

## 2.3 UI/目标选择/动画现状

1. Main UI 现有 ActionBar 偏技能，不是武器栏  
`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/ActionBarComponentSystem.cs:19`  
`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/ActionBarSlotComponentSystem.cs:78`

2. 目标选择器体系存在（位置/单体/圆形）  
`Packages/cn.etetet.spell/Scripts/Model/Share/TargetSelector.cs:6`  
`Packages/cn.etetet.btnode/Scripts/Hotfix/Server/Root/BTTargetSelectorPositionHandler.cs`  
`Packages/cn.etetet.btnode/Scripts/Hotfix/Server/Root/BTTargetSelectorSingleHandler.cs`

3. 动画与挂点基础存在，但缺武器模型装配业务  
`Packages/cn.etetet.map/Scripts/HotfixView/Client/Unit/AnimatorComponentSystem.cs:64`  
`Packages/cn.etetet.map/Scripts/Model/Share/Map/Unit/BindPoint.cs:11`  
`Packages/cn.etetet.map/Scripts/Model/Share/Map/Unit/BindPoint.cs:12`  
`Packages/cn.etetet.map/Scripts/HotfixView/Client/EffectUnitHelper.cs:13`

## 3. 测试与构建现状（阻塞项）

## 3.1 测试失败根因

两个关键测试失败点一致，均卡在登录阶段请求 Router：

1. `Equipment_LoadoutConfirm_Test`  
`Logs/All.log:396` -> 请求 `http://127.0.0.1:10101/get_router`  
`Logs/All.log:397` -> 请求失败  
`Logs/All.log:561` -> 用例失败

2. `Test_Weapon_SwitchWeapon_Test`  
`Logs/All.log:795` -> 请求 `http://127.0.0.1:10101/get_router`  
`Logs/All.log:796` -> 请求失败  
`Logs/All.log:960` -> 用例失败

结论：当前无法确认真实业务逻辑通过率，优先阻塞为“测试环境服务未起（Router/Gate链路）”。

## 3.2 构建状态

`dotnet build ET.sln` 当前日志表现为“生成失败，0错误0警告”，失败集中在 `MSBuild` 任务链路。  
参考：`msbuild_diagnostic.log:1180`、`msbuild_diagnostic.log:1190`。

## 4. TODO（按优先级）

## P0（本周必须完成）

1. 打通测试环境基础链路（Router/Gate/Match可用）  
涉及包：`cn.etetet.login`、`cn.etetet.router`、`cn.etetet.startconfig`、`cn.etetet.test`  
关键文件：`Packages/cn.etetet.login/Scripts/Hotfix/Client/NetClient/Router/RouterAddressComponentSystem.cs`、`Logs/All.log`  
验收标准：`Equipment_LoadoutConfirm_Test` 和 `Test_Weapon_SwitchWeapon_Test` 不再因 `get_router` 失败。

2. 完成局内“2把枪按钮切换”全链路  
涉及包：`cn.etetet.statesync`（UI + 消息 + 服务端处理）  
关键文件：`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main`、`Packages/cn.etetet.statesync/Scripts/Hotfix/Share/WeaponComponentSystem.cs`  
验收标准：进入局内后 UI 可见双武器按钮，点击后服务端状态切换并广播，客户端表现同步。

3. 武器参数配置化（去 hardcode）  
涉及包：`cn.etetet.statesync`、`cn.etetet.excel`  
关键文件：`Packages/cn.etetet.statesync/Scripts/Hotfix/Share/WeaponComponentSystem.cs`、`Packages/cn.etetet.statesync/Scripts/Hotfix/Server/BTNode/BTWeaponFireHandler.cs`、`Packages/cn.etetet.statesync/Scripts/Hotfix/Share/BulletComponentSystem.cs`  
验收标准：弹药、射速、伤害、射程、子弹速度全部从配置读取，代码中不再写固定数值。

4. 动画与模型装配系统（持枪/空手/切枪换模）  
涉及包：`cn.etetet.map`、`cn.etetet.statesync`  
关键文件：`Packages/cn.etetet.map/Scripts/HotfixView/Client/Unit/AnimatorComponentSystem.cs`、`Packages/cn.etetet.map/Scripts/Model/Share/Map/Unit/BindPoint.cs`  
验收标准：  
`1)` 有枪时挂接对应武器模型并播放持枪动作；  
`2)` 切枪时替换模型并切换动作状态；  
`3)` 无枪时自动回退空手动作。

## P1（功能完善）

1. 锁定模式三分型接入武器系统（位置/方向/追踪）  
涉及包：`cn.etetet.statesync`、`cn.etetet.spell`、`cn.etetet.btnode`  
关键文件：`Packages/cn.etetet.statesync/Scripts/Hotfix/Share/BulletComponentSystem.cs`、`Packages/cn.etetet.btnode/Scripts/Hotfix/Server/Root`  
验收标准：至少 3 把不同武器分别验证 3 种锁定模式。

2. 射击触发分型（可移动射击 vs 静止射击）  
涉及包：`cn.etetet.statesync`、`cn.etetet.move`  
关键文件：`Packages/cn.etetet.statesync/Scripts/Hotfix/Server/BTNode/BTWeaponFireHandler.cs`  
验收标准：冲锋枪可边移动边射击；霰弹/步枪/火箭炮静止才可触发。

3. 初版武器模板落配置并联调  
目标武器：霰弹枪1号、步枪1号、步枪2号、火箭炮1号、冲锋枪1号  
验收标准：每把武器的射程/攻速/伤害/锁定模式/移动射击规则符合设计稿。

4. 英雄技能映射（桃子/草莓/葡萄）  
涉及包：`cn.etetet.spell`、`cn.etetet.statesync`  
验收标准：  
`1)` 桃子：范围持续治疗友方；  
`2)` 草莓：召唤炮台自动攻击；  
`3)` 葡萄：释放后移速 +15%。

## P2（质量与可维护性）

1. 回归测试补齐与稳定化  
涉及包：`cn.etetet.test`、`cn.etetet.equipment`、`cn.etetet.statesync`  
关键文件：`Packages/cn.etetet.test/Scripts/Hotfix/Test`、`Packages/cn.etetet.equipment/Scripts/Hotfix/Test`  
验收标准：新增覆盖“切枪、锁定模式、移动射击、英雄技能”的自动化测试。

2. 构建链路排障并固化  
涉及文件：`msbuild_diagnostic.log`、`build_report.log`  
验收标准：`dotnet build ET.sln` 在当前机器可稳定通过，形成排障记录。

## 5. 推荐执行顺序

1. 先做 P0-1（环境打通），否则所有功能验证都不可信。  
2. 再做 P0-2 和 P0-3（切枪链路 + 配置化），这是局内枪械体验最小闭环。  
3. 接着做 P0-4（动画与模型），补足视觉反馈。  
4. 然后推进 P1（锁定模式、移动射击、英雄技能）。  
5. 最后以 P2 测试与构建收口。
