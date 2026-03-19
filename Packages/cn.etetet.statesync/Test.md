# StateSync 搜索界面三模式测试设计

## 测试目标

验证 `SearchPanel` 三模式改造的核心基础逻辑，优先覆盖最容易回归的部分：

1. 容器打开时的模式解析。
2. 背包模式与容器模式的关闭判定差异。
3. 玩家尸体打开链路是否携带正确的 `ui_key`。
4. 三模式对应的显示文案与样式 key 是否稳定。

## 测试用例清单

### StateSync_SearchPanel_ModeRouting_Test

验证搜索界面模式解析与关闭判定：

1. `corpse_` 前缀点位会被识别为 `CorpseLoot`。
2. 玩家尸体默认识别为 `Player` 子类型。
3. 普通容器点位会被识别为 `ContainerSearch`。
4. `ContainerSearch` 与 `CorpseLoot` 会走 `CloseContainer` 类型关闭。
5. `BackpackInspect` 不会走容器关闭链路。

### StateSync_PlayerCorpseLoot_OpenUiKey_Test

验证玩家尸体创建后的流图配置：

1. 尸体点位创建成功。
2. 流图中存在 `OpenContainerUI` 节点。
3. 该节点包含 `ui_key` 参数。
4. `ui_key` 值必须是 `SearchPanelComponent`。

### StateSync_SearchPanel_DisplayInfo_Test

验证搜索界面的显示信息解析：

1. 背包模式会解析成 `bag` 样式，且不显示快捷操作文案。
2. 普通容器会解析成 `container` 样式。
3. 地面掉落会解析成 `ground_drop` 样式。
4. 玩家尸体会解析成 `corpse_player` 样式，并切换搜刮文案。
5. 手动传入的标题和副标题会覆盖默认文案。

## 实施顺序

1. 先写失败测试。
2. 再补模式枚举、模式解析 Helper 和关闭判定 Helper。
3. 再补玩家尸体流图的 `ui_key`。
4. 再补显示文案与样式 Helper。
5. 编译并执行目标测试。
