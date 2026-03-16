# 展开态 UI 结构草案

## 1. 目标

- 这份文档只定义展开态 `WorldMapPanel` 需要的 UI 结构。
- 当前先不接代码，只给你一个稳定的节点命名方案，后面我会按这套结构去绑逻辑。
- 结构目标是同时承载三层内容：
  - 地图底图
  - 迷雾遮罩
  - marker / 玩家箭头 / 后续点击层

## 2. 推荐层级

```text
WorldMapPanel
|- TopBar
|  |- TitleText
|  |- CloseButton
|- MapRoot
|  |- MapFrame
|  |  |- MapMask
|  |  |  |- MapTexture
|  |  |  |- FogOverlay
|  |  |  |- MarkerLayer
|  |  |  |- PingLayer
|  |  |- PlayerArrow
|  |- MapNameText
|- RightBar
|  |- LegendRoot
|  |- ToggleFogButton
|- BottomBar
|  |- CoordText
|  |- TipText
```

## 3. 每个节点的职责

### 3.1 `TopBar`

- `TitleText`
  - 面板标题，比如“地图”。
- `CloseButton`
  - 关闭展开态。

### 3.2 `MapRoot`

- 整个地图显示区域的根。
- 建议放在屏幕中间，负责控制整体尺寸。

### 3.3 `MapFrame`

- 地图显示框。
- 建议这里负责底框、美术装饰、阴影，不直接承载地图内容逻辑。

### 3.4 `MapMask`

- 地图内容裁切节点。
- 建议加 `Mask` 或 `RectMask2D`。
- `MapTexture`、`FogOverlay`、`MarkerLayer`、`PingLayer` 都放这里。

### 3.5 `MapTexture`

- `RawImage`
- 用来显示整张地图底图。
- 后面会和折叠态一样，通过 `uvRect` 或整图模式显示。

### 3.6 `FogOverlay`

- `RawImage`
- 放在 `MapTexture` 上面、`MarkerLayer` 下面。
- 用来显示整图迷雾 texture。
- 折叠态目前已经证明这层结构可行，展开态建议直接复用同名节点。

### 3.7 `MarkerLayer`

- `RectTransform`
- 用来承载友军、敌军、怪物、Boss、任务点等 marker。
- 建议纯空节点，不放静态图片。
- 后续 marker 继续走运行时创建或对象池。

### 3.8 `PingLayer`

- `RectTransform`
- 预留给后续点击地图、技能选点、信号提示。
- 现在先空着。

### 3.9 `PlayerArrow`

- `Image`
- 放在 `MapFrame` 下，不放在 `MapMask` 内也可以。
- 用来显示自己朝向。
- 如果展开态要显示全图且自己位置不在中心，也可以改成普通 marker，后面代码可兼容。

### 3.10 `MapNameText`

- 当前地图名。
- 比如 `SDCMap` 或本地化后的显示名。

### 3.11 `RightBar`

- 放图例或筛选开关。
- `LegendRoot`
  - 放“友军 / 敌军 / 怪物 / Boss”的图例。
- `ToggleFogButton`
  - 先预留，不一定立刻启用。

### 3.12 `BottomBar`

- `CoordText`
  - 预留显示光标或点击位置坐标。
- `TipText`
  - 预留显示“点击选择技能目标”等提示。

## 4. 最少必需节点

如果你想先快速把展开态 UI 搭出来，最少只要这几个节点：

```text
WorldMapPanel
|- MapRoot
|  |- MapFrame
|  |  |- MapMask
|  |  |  |- MapTexture
|  |  |  |- FogOverlay
|  |  |  |- MarkerLayer
|  |  |- PlayerArrow
|- CloseButton
```

## 5. 和当前折叠态对齐建议

- 折叠态当前真实使用的关键层已经是：
  - `Minimap Texture`
  - `Minimap Fog`
  - `MarkerLayer`
- 展开态建议完全沿用这个三层模型，只是显示范围从“局部裁切”切到“整图”。
- 这样后面代码可以共用：
  - marker 同步逻辑
  - 迷雾 texture 构建逻辑
  - 世界坐标到地图坐标映射逻辑

## 6. 后续接线原则

- 先由你把 `WorldMapPanel` prefab/YIUI 面板结构搭出来。
- 我后续接代码时，会优先按下面这些节点名绑定：
  - `MapRoot`
  - `MapFrame`
  - `MapMask`
  - `MapTexture`
  - `FogOverlay`
  - `MarkerLayer`
  - `PlayerArrow`
  - `CloseButton`

如果你命名不一样，也可以，但最好提前告诉我，免得后面再改绑定代码。
