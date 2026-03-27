# 局外起装 UI Prefab 施工清单

## 1. 范围
- 这份只保留施工必需信息。
- 本期只改 `EquipPanel`。
- 英雄选择继续留在 `RolePanel`，`EquipPanel` 不放英雄列表。

## 2. 主面板

Prefab：
- `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Lobby/Prefabs/LobbyPanel.prefab`

只改：
- `EquipPanel`

不动：
- `RolePanel`
- `MatchPanel`
- `BuildPanel`
- `ExplorePanel`

## 3. EquipPanel 最终布局

- 左侧：4 个固定槽位
- 中间：当前背包二维格子
- 右侧上半：安全格二维格子
- 右侧下半：仓库列表
- 底部：财富、确认状态、操作按钮

## 4. LobbyPanel 现有节点处理

### 4.1 保留并继续使用
- 4 个固定槽位组件
- `PutIntoBag` 按钮
- `u_ComEquipPanelRectTransform`

### 4.2 保留但先隐藏
- `u_ComEquipBagScroll`

说明：
- 这是旧的一维背包列表。
- 第一阶段不要删，只隐藏。
- 等新二维背包接完再清理。

### 4.3 文案改，节点名先不改
- `PutIntoBag`
  改显示文案为：
  - `配置背包`
  或
  - `打开商店`

说明：
- 第一阶段只改按钮显示文字，不建议改节点名。
- 这样不影响现在已有绑定和事件。

## 5. EquipPanel 需要新增的节点

建议都挂在 `EquipPanel` 下。

### 5.1 固定槽位区
- `FixedSlotRoot`

内部直接摆现有 4 个 `EquipSlotItem`：
- `UIEquipSlotItemWeapon`
- `UIEquipSlotItemWeapon2`
- `UIEquipSlotItemArmor`
- `UIEquipSlotItemBag`

### 5.2 当前背包区
- `CurrentBagRoot`
- `CurrentBagTitle`
- `CurrentBagBoardRoot`
- `CurrentBagItemsLayer`
- `CurrentBagItemTemplate`
- `CurrentBagGridRoot`

### 5.3 安全格区
- `SecureBagRoot`
- `SecureBagTitle`
- `SecureHintText`
- `SecureBoardRoot`
- `SecureItemsLayer`
- `SecureItemTemplate`
- `SecureGridRoot`

### 5.4 仓库区
- `WarehouseRoot`
- `WarehouseTitle`
- `WarehouseLoopScroll`
- `WarehouseEmptyText`

### 5.5 底部操作区
- `BottomBarRoot`
- `TotalWealthText`
- `ConfirmStateText`
- `OpenShopButton`
- `ConfirmLoadoutButton`
- `OneKeyUnloadButton`

## 6. 二维格子怎么做

不要从零搭。

直接参考：
- `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Main/Prefabs/Search/SearchPanel.prefab`

只抄结构：
- `BoardRoot`
- `ItemsLayer`
- `ItemTemplate`
- `GridRoot`

本期要抄 4 套：
- 主界面正式背包 1 套
- 主界面正式安全格 1 套
- 弹层草稿背包 1 套
- 弹层草稿安全格 1 套

## 7. 固定槽位卡片

Prefab：
- `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Lobby/Prefabs/EquipSlotItem.prefab`

继续复用。

必须保留：
- `Gun`

说明：
- 现在代码直接把 `Gun` 当图标节点在取。
- 第一阶段不要改这个节点名。

建议新增显示：
- `QualityFrame`
- `SizeText`
- `CapacityText`
- `EmptyLabel`

显示规则：
- 武器/护甲显示占格尺寸
- 背包显示容量
- 空槽显示空状态文案

## 8. 商店/仓库列表项

Prefab：
- `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Lobby/Prefabs/EquipSelectItem.prefab`

继续复用。

建议新增：
- `PriceText`
- `CountText`
- `SizeText`
- `CapacityText`
- `QualityFrame`
- `CategoryTag`
- `DraftTag`

显示规则：
- 商店列表显示价格
- 仓库列表显示数量
- 背包条目显示容量
- 普通条目显示占格尺寸

## 9. 商店弹层

Prefab：
- `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Lobby/Prefabs/EquipSelectView.prefab`

继续复用，改成“商店 + 草稿配置”弹层。

### 9.1 保留
- `u_ComEquipSelectLoopScroll`
- `u_DataGunName`
- `Prepared`
- `Exit`

### 9.2 建议新增
- `CategoryTabRoot`
- `DetailIcon`
- `DetailNameText`
- `DetailPriceText`
- `DetailSizeText`
- `DetailCapacityText`
- `DraftBagRoot`
- `DraftBagBoardRoot`
- `DraftBagItemsLayer`
- `DraftBagItemTemplate`
- `DraftBagGridRoot`
- `DraftSecureRoot`
- `DraftSecureBoardRoot`
- `DraftSecureItemsLayer`
- `DraftSecureItemTemplate`
- `DraftSecureGridRoot`
- `DraftTotalPriceText`
- `ClearDraftButton`

### 9.3 按钮文案建议
- `Prepared`
  改显示文案为：
  - `确认返回`
  或
  - `起装完成`

- `Exit`
  改显示文案为：
  - `关闭`

## 10. 本期制作顺序

### 第一步
先改 `LobbyPanel/EquipPanel` 版式。

### 第二步
把 4 个固定槽位摆好，保留原绑定。

### 第三步
把 `SearchPanel` 的二维格子结构复制进主界面：
- 正式背包
- 正式安全格

### 第四步
把 `EquipSelectView` 改成商店弹层，再复制两套二维格子：
- 草稿背包
- 草稿安全格

### 第五步
补 `EquipSlotItem`、`EquipSelectItem` 的显示节点。

### 第六步
旧 `u_ComEquipBagScroll` 保留但隐藏，等程序切完后再删。

## 11. 你现在可以直接开工的点

- `LobbyPanel.prefab` 的 `EquipPanel` 重排版
- 新增正式背包区
- 新增正式安全格区
- 新增仓库区
- 新增底部操作区
- `EquipSelectView.prefab` 改成商店弹层
- `EquipSlotItem.prefab` 补品质/尺寸/容量显示
- `EquipSelectItem.prefab` 补价格/数量/尺寸显示

## 12. 结论

这一版按施工来做，只记一句：

- `EquipPanel` 负责正式状态
- `EquipSelectView` 负责商店草稿
- 二维格子直接抄 `SearchPanel`
- 旧一维背包先隐藏不删
