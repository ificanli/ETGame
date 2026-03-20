# 起装商店与仓库切换 Prefab 修改说明

## 当前代码已支持

1. 起装界面来源列表支持 `商店 / 仓库` 两个模式。
2. 若 prefab 没有新增节点，代码会在 `u_ComEquipBagScroll` 上方运行时创建兜底切换按钮。
3. 商店模式下点击物品，再点击装备槽位或点击放入背包，会走商店购买协议并直接放进目标区域。

## 推荐的 prefab 改法

推荐你在 `LobbyPanel.prefab` 的装备页里手动补三层节点，这样样式可控，不依赖代码兜底：

1. 在 `u_ComEquipPanelRectTransform` 下，或 `u_ComEquipBagScroll` 下，新建一个 `RectTransform`
   名字：`LoadoutSourceToggleRoot`
2. 在这个节点下放两个 `Button`
   名字分别为：`LoadoutShopButton`、`LoadoutWarehouseButton`
3. 每个按钮下放一个文字节点
   文案分别为：`商店`、`仓库`

## 代码识别规则

只要名字匹配下面这三个，代码就会自动绑定：

- `LoadoutSourceToggleRoot`
- `LoadoutShopButton`
- `LoadoutWarehouseButton`

按钮下的文字不强制命名，代码会直接取按钮子节点里的 `TMP_Text`。

## 布局建议

1. 把 `LoadoutSourceToggleRoot` 放在 `u_ComEquipBagScroll` 顶部。
2. 两个按钮建议等宽，方便做选中态。
3. 选中态建议直接改按钮底图颜色和文字颜色；代码已经会按选中/非选中切换。
4. `u_ComEquipBagScroll` 的内容区顶部要预留一行高度，避免列表第一项被页签挡住。

## 如果你暂时不改 prefab

功能仍然能跑，但看到的是代码创建的简易页签，只适合先验证逻辑，不适合最终 UI。
