# 局外起装 UI 基于现有 Prefab 改造说明

## 1. 文档用途
- 这份文档不重复讲需求，只讲“基于当前工程里已经有的 prefab，应该怎么改”。
- 目标是尽量复用现有 `Lobby`、`EquipSelectView`、`SearchPanel` 资产，不新起一整套 UI。
- 适合 UI 制作、Prefab 改造、节点整理、后续程序绑定一起参考。

## 2. 总体改造策略

本期最省成本的做法：
- 主界面继续用 `LobbyPanel.prefab`，只重做其中的 `EquipPanel`。
- 商店/背包配置弹层继续用 `EquipSelectView.prefab`，扩成“商店 + 草稿背包 + 草稿安全格”。
- 固定槽位继续用 `EquipSlotItem.prefab`。
- 商店列表项、仓库列表项继续用 `EquipSelectItem.prefab`。
- 二维格子区域直接参考并复用 `SearchPanel.prefab` 的结构，不建议从零重拼。

不建议的做法：
- 不建议新建一套完全独立的起装 Panel。
- 不建议把当前 `EquipBagScroll` 继续沿用成一维背包列表。
- 不建议先删旧节点再重做。当前逻辑还依赖一部分旧字段，第一阶段建议“新增并隐藏旧节点”。

## 3. 可直接复用的现有 Prefab

### 3.1 主面板
- `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Lobby/Prefabs/LobbyPanel.prefab`

用途：
- 继续作为局外大厅/起装主界面。
- 本期重点改造其中的 `EquipPanel`。

### 3.2 固定槽位卡片
- `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Lobby/Prefabs/EquipSlotItem.prefab`

用途：
- 继续作为 4 个固定槽位卡片：
- 枪械1
- 枪械2
- 护甲
- 背包

### 3.3 商店/仓库列表项
- `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Lobby/Prefabs/EquipSelectItem.prefab`

用途：
- 继续作为商店列表项。
- 也可以复用为仓库列表项。

### 3.4 配装弹层
- `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Lobby/Prefabs/EquipSelectView.prefab`

用途：
- 继续作为弹层。
- 改造成“商店购买与草稿摆放界面”。

### 3.5 二维背包结构参考
- `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Main/Prefabs/Search/SearchPanel.prefab`

用途：
- 直接复用其中的二维格子结构。
- 重点参考并复制以下节点组合：
- `BoardRoot`
- `ItemsLayer`
- `ItemTemplate`
- `GridRoot`

说明：
- 这套结构已经在局内搜索里跑通了二维摆放展示。
- 局外背包和安全格优先复用这套层级和表现方式。

## 4. 当前程序已经依赖的 Lobby 节点

当前 `LobbyPanel` 已经生成并绑定的关键字段：
- `u_ComEquipPanelRectTransform`
- `u_ComHeroList`
- `u_ComEquipBagScroll`
- `u_UIEquipSlotItemWeapon`
- `u_UIEquipSlotItemWeapon2`
- `u_UIEquipSlotItemArmor`
- `u_UIEquipSlotItemBag`
- `u_EventClickPutIntoBag`
- `u_EventClickBag`

对应代码位置：
- `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIGen/Lobby/LobbyPanelComponentGen.cs`
- `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem.cs`

当前旧逻辑的特点：
- 4 个固定槽位已经能显示和点击。
- `u_ComHeroList` 当前属于 `RolePanel` 的英雄选择区，不属于本期 `EquipPanel` 改造范围。
- `EquipBagScroll` 现在还是旧的一维背包列表。
- 点击背包槽会打开 `EquipSelectView`。
- 点击 `PutIntoBag` 也会打开旧的选择弹层。

结论：
- 旧字段先保留。
- 新 UI 第一阶段建议加在 `EquipPanel` 内部。
- 等新逻辑接完，再清旧节点和旧绑定。

## 5. LobbyPanel 应该怎么改

## 5.1 哪些区域保留
- `RolePanel` 保留，不是本期重点。
- `MatchPanel` 保留。
- `BuildPanel` 保留。
- `ExplorePanel` 保留。
- `HeroSelectRange` 保留，继续展示英雄列表。
- 4 个 `EquipSlotItem` 保留，继续作为固定槽位。

## 5.2 哪些区域要重做

重点改 `EquipPanel`。

建议把 `EquipPanel` 改成以下布局：
- 左侧：4 个固定槽位
- 中间：当前背包二维格子
- 右侧上半：安全格二维格子
- 右侧下半：仓库列表
- 底部：财富、起装状态、操作按钮

补充说明：
- 英雄选择继续留在 `RolePanel`，不并入 `EquipPanel`。
- `EquipPanel` 只负责起装、仓库、背包、安全格和确认操作。

### 5.2.1 固定槽位区
继续使用现有：
- `u_UIEquipSlotItemWeapon`
- `u_UIEquipSlotItemWeapon2`
- `u_UIEquipSlotItemArmor`
- `u_UIEquipSlotItemBag`

建议补显示元素：
- 品质框
- 空状态标签
- 物品名
- 尺寸文本
- 背包容量文本

其中：
- 背包槽位需要额外显示 `3x3`、`4x4`、`5x5`、`6x6`
- 枪械和护甲槽位显示占格尺寸即可

### 5.2.2 当前背包区
当前 `EquipBagScroll` 不再适合做正式背包区。

建议：
- 保留旧 `u_ComEquipBagScroll` 节点一段时间，但先隐藏视觉。
- 在 `EquipPanel` 下新增一块“当前背包二维格子区”。
- 结构直接复制 `SearchPanel.prefab` 里背包那套：
- `BagBoardRoot`
- `BagItemsLayer`
- `BagItemTemplate`
- `GridRoot`

建议新节点名：
- `CurrentBagBoardRoot`
- `CurrentBagItemsLayer`
- `CurrentBagItemTemplate`
- `CurrentBagGridRoot`

### 5.2.3 安全格区
主界面需要显示安全格。

建议在当前背包区旁边再复制一套二维格子结构：
- `SecureBoardRoot`
- `SecureItemsLayer`
- `SecureItemTemplate`
- `SecureGridRoot`

额外需要：
- 安全格标题
- “死亡保留”提示文案
- 与普通背包不同的底框样式

### 5.2.4 仓库区
当前主界面右侧的 `StorageRange` 可以直接改成仓库列表区。

本期仓库不做二维，继续做列表或分页宫格即可。

建议内容：
- 物品图标
- 名称
- 数量
- 尺寸
- 品质
- 可拖拽区域

建议节点名：
- `WarehouseListRoot`
- `WarehouseLoopScroll`
- `WarehouseEmptyText`

建议直接复用：
- `EquipSelectItem.prefab`

### 5.2.5 底部操作区
建议新增或重命名：
- `TotalWealthText`
- `ConfirmStateText`
- `OpenShopButton`
- `ConfirmLoadoutButton`
- `OneKeyUnloadButton`

当前可复用按钮：
- `PutIntoBag`

建议改名和改用途：
- 原 `PutIntoBag` 改成“配置背包”或“商店”
- 原 `Confirm` 若在 `EquipPanel` 内有现成按钮，可改成“起装完成”

## 6. EquipSelectView 应该怎么改

## 6.1 现状
当前 `EquipSelectView.prefab` 已经具备：
- 一个列表区 `u_ComEquipSelectLoopScroll`
- 一个详情文本 `u_DataGunName`
- 一个确认按钮 `Prepared`
- 一个关闭按钮 `Exit`

当前代码也已经在用：
- 打开弹层
- 刷装备列表
- 选中条目
- 点击 `Prepared` 做确认

## 6.2 建议改造成什么
改成“背包配置界面”。

布局建议：
- 左：商店分类
- 中：商店物品列表
- 右：详情预览
- 下半区左：草稿背包二维格子
- 下半区右：草稿安全格二维格子
- 底部：总价、清空草稿、确认返回

## 6.3 哪些现有节点保留
- `u_ComEquipSelectLoopScroll` 保留，继续做商店列表
- `u_DataGunName` 保留，继续做详情预览文字
- `Prepared` 保留，继续做“确认返回/起装完成”
- `Exit` 保留

## 6.4 需要新增的节点
建议新增：
- `CategoryTabRoot`
- `DetailIcon`
- `DetailNameText`
- `DetailPriceText`
- `DetailSizeText`
- `DetailCapacityText`
- `DraftBagBoardRoot`
- `DraftBagItemsLayer`
- `DraftBagItemTemplate`
- `DraftBagGridRoot`
- `DraftSecureBoardRoot`
- `DraftSecureItemsLayer`
- `DraftSecureItemTemplate`
- `DraftSecureGridRoot`
- `DraftTotalPriceText`
- `ClearDraftButton`

二维格子区域同样直接复制 `SearchPanel.prefab` 的格子结构。

## 7. EquipSlotItem 应该怎么改

当前 `EquipSlotItem.prefab` 已有：
- 图标节点 `Gun`
- 槽位名
- 装备名
- 空状态

建议继续复用，不换 prefab。

建议新增显示节点：
- `QualityFrame`
- `SizeText`
- `CapacityText`
- `EmptyLabel`
- `SelectedOutline`

用途说明：
- `QualityFrame`：表现品质
- `SizeText`：显示 `2x3` 这种占格
- `CapacityText`：背包槽专用，显示 `3x3`
- `EmptyLabel`：空槽时显示“点击装备”

注意：
- 现有代码直接从 `Gun` 节点上拿 `Image` 当 icon
- 第一阶段不要改掉 `Gun` 这个图标节点

## 8. EquipSelectItem 应该怎么改

当前 `EquipSelectItem.prefab` 已有：
- `Icon`
- `Bg`
- `SelectIndicator`
- `EquipNameText`

建议继续复用为：
- 商店条目
- 仓库条目

建议新增节点：
- `PriceText`
- `CountText`
- `SizeText`
- `CapacityText`
- `CategoryTag`
- `DraftTag`
- `QualityFrame`

显示建议：
- 商店条目显示价格
- 仓库条目显示库存数量
- 背包物品显示容量标签
- 普通物品显示占格尺寸

## 9. SearchPanel 里的哪套结构直接抄

优先参考：
- `u_ComContainerBoardRoot`
- `u_ComContainerItemsLayer`
- `u_ComBagBoardRoot`
- `u_ComBagItemsLayer`
- `u_ComContainerItemTemplate`
- `u_ComBagItemTemplate`

对应代码在：
- `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIGen/Main/SearchPanelComponentGen.cs`
- `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/SearchPanelComponentSystem.cs`

建议做法：
- 局外不要直接复用 `SearchPanelComponent` 本体。
- 复用它的节点结构、摆放方式、格子表现。
- 视觉可以调成局外风格，但层级和模板结构尽量一致。

## 10. 推荐的具体改造顺序

### 第一步
先改 `LobbyPanel.prefab` 的 `EquipPanel` 视觉布局：
- 保留旧字段
- 新增“当前背包区”“安全格区”“仓库区”“底部操作区”

### 第二步
把 `SearchPanel` 的二维格子结构复制两套到 `LobbyPanel`：
- 一套给正式背包
- 一套给正式安全格

### 第三步
改 `EquipSelectView.prefab`：
- 保留旧的列表和按钮
- 新增分类、详情、草稿背包、草稿安全格、总价

### 第四步
补 `EquipSlotItem.prefab` 和 `EquipSelectItem.prefab` 的显示节点

### 第五步
等程序新绑定完成后，再删除旧的一维背包视觉和过时按钮

## 11. 本期制作注意事项

- 第一阶段先不要删除 `u_ComEquipBagScroll`，先隐藏即可。
- 第一阶段先不要删除 `PutIntoBag` 按钮，先改文案和位置。
- 第一阶段先不要改掉 `EquipSlotItem` 里的 `Gun` 图标节点名。
- 二维格子区域优先复用 `SearchPanel` 结构，避免后面程序绑定重写。
- 主界面显示正式状态。
- `EquipSelectView` 显示商店草稿状态。
- 主界面和弹层的视觉样式可以不同，但格子尺寸表达、占格方式、图标尺寸建议统一。

## 12. 结论

本期不需要重做整套 UI。

最佳路径是：
- `LobbyPanel` 负责正式状态展示与即时操作
- `EquipSelectView` 负责商店与草稿配置
- `EquipSlotItem` 继续负责 4 个固定槽位
- `EquipSelectItem` 继续负责商店/仓库列表项
- `SearchPanel` 提供二维格子结构参考

这样改的优点：
- 现有 prefab 大部分可复用
- 现有 YIUI 绑定体系可延续
- 程序改造成本最低
- 美术和程序可以并行工作
