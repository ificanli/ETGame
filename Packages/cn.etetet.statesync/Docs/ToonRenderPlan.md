# 风格化卡通渲染方案记录

## 目标

项目目标不是写实卡通，也不是硬二次元，而是：

- 更卡通
- 更吸引低龄用户
- 更有动画片感
- 角色和场景属于同一个风格世界

当前结论：

- 角色和场景需要分成两类 shader
- 但不能做成两套完全不同的风格
- 正确方向是“同一套风格语言，两个材质家族”

## 当前阶段结论

当前建议的推进顺序：

1. 先定角色风格
2. 再开始正式场景模型制作
3. 场景资产出来后再定场景 toon shader
4. 最后统一后处理和整体色彩

原因：

- 没有正式场景资产时，场景 shader 很难精调
- 角色是否讨喜，比场景是否完整更早暴露问题
- 先把角色风格立住，能避免后续场景方向跑偏

## 已完成内容

### 1. 角色卡通 shader 第一版

文件：

- [ETCharacterToon.shader](/D:/05ET/MatchTest/ETGame/Packages/cn.etetet.statesync/Assets/GameRes/Graphics/Shaders/Toon/ETCharacterToon.shader)

当前已具备能力：

- 基础 toon 明暗分层
- 主光方向分层
- Rim Light
- Outline 外轮廓
- 法线贴图支持
- 面向低龄角色的 Friendly Lift 脸部友好化提亮

当前新增的角色向核心参数：

- `_ToonThreshold`
- `_ToonSmoothness`
- `_ShadowColor`
- `_LightBoost`
- `_ShadowStrength`
- `_HalfLambert`
- `_RimColor`
- `_RimIntensity`
- `_OutlineColor`
- `_OutlineWidth`
- `_FriendlyTint`
- `_FriendlyThreshold`
- `_FriendlySmoothness`
- `_FriendlyFrontPower`
- `_FriendlyIntensity`

### 2. 角色测试材质已经调过一轮

测试目录：

- [agou](/D:/05ET/MatchTest/ETGame/Packages/cn.etetet.statesync/Bundles/HeroDisplay/agou)

测试材质：

- [Material.001.mat](/D:/05ET/MatchTest/ETGame/Packages/cn.etetet.statesync/Bundles/HeroDisplay/agou/Material.001.mat)
- [Myshader.mat](/D:/05ET/MatchTest/ETGame/Packages/cn.etetet.statesync/Bundles/HeroDisplay/agou/Myshader.mat)

当前判断：

- 角色 toon 方向可行
- 已经明显比初始 PBR 更接近“动画片感”
- 可作为角色卡通渲染第一版基线

### 3. 场景卡通 shader 第一版起底

文件：

- [ETSceneToon.shader](/D:/05ET/MatchTest/ETGame/Packages/cn.etetet.statesync/Assets/GameRes/Graphics/Shaders/Toon/ETSceneToon.shader)

当前思路：

- 场景不用角色那套描边方式
- 场景更强调整体色阶统一、颜色渐变、弱真实感
- 保留法线和 AO，但降低写实脏感

当前场景向能力：

- 基础 toon 明暗分层
- 顶部到底部颜色渐变
- 法线贴图支持
- AO 弱化接入
- 很轻的风格化高光

### 4. 场景试接材质

文件：

- [Rock7.mat](/D:/05ET/MatchTest/ETGame/Packages/cn.etetet.statesync/Assets/GameRes/Blink/Scenes/Environment/Stylized/Cliffs/Materials_Cliffs/Rock7.mat)

说明：

- 这只是“场景 shader 起底验证”
- 不是正式场景最终方案
- 因为目前还没有正式场景模型，所以暂时不继续精调

## 当前审美判断

### 角色部分

当前版本属于“可以继续向正式方案推进”，但还没有完全定版。

优点：

- 角色已经具备卡通体积感
- 白毛区域比初版更干净
- 衣服颜色开始有动画片上色感
- 角色整体讨喜度比原始 PBR 更高

问题：

- 脸部仍然可以更讨喜
- 眼周、嘴鼻附近仍有轻微脏感
- 描边还不算最终质量
- 还没有做脸部专用遮罩级别控制

当前综合判断：

- 角色卡通 shader 第一版可用
- 适合作为后续角色风格基线

### 场景部分

当前不建议深入精调，原因是缺少正式场景资产。

如果没有正式场景模型，以下判断都会失真：

- 法线强度
- AO 强度
- 顶底渐变范围
- 岩石、地面、树木之间的统一关系
- 角色与环境的综合色彩平衡

## 后续正式推进建议

### A. 角色后续可继续增强的方向

后续如果继续做角色 shader，优先级建议如下：

1. 脸部专用控制
2. 第二层阴影
3. 白毛/皮肤专门提亮逻辑
4. 头发或耳朵边缘的更柔和高光
5. 更稳定的局部描边控制

推荐增强项：

- 面部遮罩贴图
- 面部单独阈值
- 脸部阴影方向控制
- 白色区域单独明度保护

### B. 场景正式开始时的接入顺序

正式场景资产开始制作后，建议按下面顺序推进：

1. 地面、岩石、墙体
2. 树、草、灌木
3. 装饰物、道具、发光物

原因：

- 第一批最影响整体世界观
- 第二批最影响场景“呼吸感”和亲和力
- 第三批再做可以避免反复返工

### C. 场景 toon shader 未来重点

场景正式推进时，重点不是加更多参数，而是统一以下风格规则：

- 主光颜色
- 阴影颜色
- 天空和雾颜色
- 场景整体饱和度上限
- 地面和岩石的冷暖关系
- 植被的明度层级

## 到时候找我时，最好一起给的信息

你后面开始做正式场景时，建议直接把下面这些信息一起给我：

- 当前使用的角色截图
- 场景模型白模截图
- 第一批场景材质球
- 你想参考的风格图
- 你觉得“太真实”或“太脏”的地方

如果能一起给这几类截图，后续收敛会快很多：

- 近景角色 + 场景同框
- 中景环境
- 地面特写
- 岩石特写
- 树和草特写

## 当前阶段建议

当前最合理的动作不是继续磨 shader，而是：

1. 开始正式场景模型制作
2. 保留当前角色 shader 作为风格基线
3. 场景资产第一批出来后，再继续统一场景 toon shader

## 备注

如果后续继续这条线，默认直接基于以下文件继续迭代：

- [ETCharacterToon.shader](/D:/05ET/MatchTest/ETGame/Packages/cn.etetet.statesync/Assets/GameRes/Graphics/Shaders/Toon/ETCharacterToon.shader)
- [ETSceneToon.shader](/D:/05ET/MatchTest/ETGame/Packages/cn.etetet.statesync/Assets/GameRes/Graphics/Shaders/Toon/ETSceneToon.shader)
- [Material.001.mat](/D:/05ET/MatchTest/ETGame/Packages/cn.etetet.statesync/Bundles/HeroDisplay/agou/Material.001.mat)
- [Myshader.mat](/D:/05ET/MatchTest/ETGame/Packages/cn.etetet.statesync/Bundles/HeroDisplay/agou/Myshader.mat)
- [Rock7.mat](/D:/05ET/MatchTest/ETGame/Packages/cn.etetet.statesync/Assets/GameRes/Blink/Scenes/Environment/Stylized/Cliffs/Materials_Cliffs/Rock7.mat)
