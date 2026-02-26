# ECA 框架包设计建议

## 结论：创建新包 `cn.etetet.eca`

### 为什么不直接复用 BT 包

虽然 BehaviorTree 和 ECA 都使用 GraphView，但它们的**核心概念和数据结构完全不同**：

| 维度 | BehaviorTree | ECA 框架 |
|------|-------------|---------|
| **核心概念** | 树形结构，父子节点关系 | 状态图，状态之间的转换 |
| **执行方式** | 从根节点遍历，按树结构执行 | 事件触发，状态转换驱动 |
| **节点类型** | Composite、Action、Condition、Decorator | State、Event、Condition、Action |
| **连接关系** | 父子关系（树形） | 状态流转（图形） |
| **数据结构** | `BTNode` 有 `Children` 列表 | `ECANode` 有 `Connections` 列表 |
| **适用场景** | AI 逻辑、技能系统 | 交互物、触发器、状态机 |

**核心差异**：
- BT 是**树**（Tree），有明确的父子层级
- ECA 是**图**（Graph），状态之间可以任意连接

如果强行复用 BT 包，会导致：
- ❌ 需要大量修改核心数据结构
- ❌ 代码耦合严重，难以维护
- ❌ 概念混淆，不利于理解

---

## 推荐方案：创建新包 + 参考 BT 实现

### 1. 创建新包结构

```
cn.etetet.eca/
├── packagegit.json
├── AGENTS.md
├── Scripts/
│   ├── Model/
│   │   └── Share/
│   │       ├── PackageType.cs
│   │       ├── ECANode.cs              # 节点基类
│   │       ├── ECAStateNode.cs         # 状态节点
│   │       ├── ECAEventNode.cs         # 事件节点
│   │       ├── ECAConditionNode.cs     # 条件节点
│   │       ├── ECAActionNode.cs        # 动作节点
│   │       ├── ECAConnection.cs        # 连接数据
│   │       └── ECAConfig.cs            # 配置根对象
│   └── Hotfix/
│       └── Server/
│           ├── ECAPointComponent.cs
│           ├── ECAStateComponent.cs
│           └── ECALoader.cs
└── Editor/
    └── ECAEditor/
        ├── ECAEditor.cs                # 主编辑器窗口
        ├── ECAGraphView.cs             # GraphView 实现
        ├── ECANodeView.cs              # 节点视图
        ├── ECAStateNodeView.cs         # 状态节点视图
        ├── ECAEventNodeView.cs         # 事件节点视图
        ├── ECAConditionNodeView.cs     # 条件节点视图
        ├── ECAActionNodeView.cs        # 动作节点视图
        └── ECAEditor.uss               # 样式表
```

### 2. 可以参考 BT 包的部分

虽然不能直接复用，但可以**参考**以下部分的实现：

#### ✅ 可以参考的部分

1. **GraphView 基础设置**
   ```csharp
   // 参考 TreeView.cs 的构造函数
   public ECAGraphView()
   {
       Insert(0, new GridBackground());
       SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
       this.AddManipulator(new ContentDragger());
       this.AddManipulator(new SelectionDragger());
       // ...
   }
   ```

2. **节点视图基类**
   ```csharp
   // 参考 NodeView.cs 的实现
   public class ECANodeView : Node
   {
       public ECANode NodeData;
       public Vector2 Position;
       // 端口管理
       // 样式设置
       // 事件处理
   }
   ```

3. **撤销/重做系统**
   ```csharp
   // 参考 TreeView.cs 的 undo/redo 实现
   private readonly Stack<byte[]> undo = new();
   private readonly Stack<byte[]> redo = new();
   ```

4. **右键菜单**
   ```csharp
   // 参考 RightClickMenu.cs
   public class ECAContextMenu
   {
       // 创建节点菜单
       // 删除节点
       // 复制粘贴
   }
   ```

5. **节点序列化**
   ```csharp
   // 参考 BTNode.cs 的序列化方式
   [Serializable]
   public abstract class ECANode : Object
   {
       public int Id;
       public Vector2 Position;
       // ...
   }
   ```

6. **编辑器窗口布局**
   ```csharp
   // 参考 BehaviorTreeEditor.cs 的布局
   // 左侧节点库
   // 中间画布
   // 右侧属性面板
   ```

#### ❌ 不能复用的部分

1. **节点数据结构**
   - BT: `BTNode` 有 `Children` 列表（树形）
   - ECA: `ECANode` 有 `Connections` 列表（图形）

2. **执行逻辑**
   - BT: `BTDispatcher` 遍历树执行
   - ECA: 事件触发状态转换

3. **节点类型**
   - BT: Composite、Action、Condition、Decorator
   - ECA: State、Event、Condition、Action

---

## 具体实现建议

### 第1步：创建包基础结构（1天）

```bash
# 创建包目录
mkdir -p Packages/cn.etetet.eca/Scripts/Model/Share
mkdir -p Packages/cn.etetet.eca/Scripts/Hotfix/Server
mkdir -p Packages/cn.etetet.eca/Editor/ECAEditor
```

**packagegit.json**：
```json
{
  "Id": 50,
  "Name": "ECA",
  "AllowAccessField": true,
  "Level": 2
}
```

**PackageType.cs**：
```csharp
namespace ET
{
    public static class PackageType
    {
        public const int ECA = 50;
    }
}
```

### 第2步：定义核心数据结构（1-2天）

**ECANode.cs**（参考 BTNode.cs）：
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ET
{
    [Serializable]
    public abstract class ECANode : Object
    {
        public int Id;
        public string NodeName;

        #if UNITY_EDITOR
        [HideInInspector]
        public Vector2 Position;

        [TextArea]
        public string Description;
        #endif
    }
}
```

**ECAStateNode.cs**：
```csharp
namespace ET
{
    [Serializable]
    public class ECAStateNode : ECANode
    {
        public string StateName;
        public bool IsInitialState;
        public bool IsResetable;
        public long ResetCooldown;
    }
}
```

**ECAConnection.cs**：
```csharp
namespace ET
{
    [Serializable]
    public class ECAConnection
    {
        public int FromNodeId;
        public string FromPortName;
        public int ToNodeId;
        public string ToPortName;
    }
}
```

**ECAConfig.cs**（类似 BTRoot）：
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace ET
{
    [CreateAssetMenu(fileName = "ECAConfig", menuName = "ET/ECA Config")]
    public class ECAConfig : ScriptableObject
    {
        public string ConfigId;
        public Vector3 WorldPosition;
        public float InteractRange;

        [SerializeReference]
        public List<ECANode> Nodes = new();

        public List<ECAConnection> Connections = new();
    }
}
```

### 第3步：实现 GraphView 编辑器（1-2周）

**ECAGraphView.cs**（参考 TreeView.cs）：
```csharp
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ET
{
    public class ECAGraphView : GraphView
    {
        public ECAConfig Config;

        public ECAGraphView()
        {
            // 参考 TreeView 的初始化
            Insert(0, new GridBackground());
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());

            // 加载样式表
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/cn.etetet.eca/Editor/ECAEditor/ECAEditor.uss");
            styleSheets.Add(styleSheet);

            // 注册回调
            this.graphViewChanged = OnGraphViewChanged;
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            // 处理节点创建、删除、连接
            return change;
        }

        // 创建节点
        public void CreateStateNode(Vector2 position)
        {
            var node = new ECAStateNode
            {
                Id = GenerateId(),
                Position = position
            };
            Config.Nodes.Add(node);

            var nodeView = new ECAStateNodeView(node);
            AddElement(nodeView);
        }
    }
}
```

**ECAEditor.cs**（参考 BehaviorTreeEditor.cs）：
```csharp
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ET
{
    public class ECAEditor : EditorWindow
    {
        private ECAGraphView graphView;
        private ECAConfig currentConfig;

        [MenuItem("ET/ECA/ECA Editor")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<ECAEditor>();
            wnd.titleContent = new GUIContent("ECA Editor");
        }

        private void CreateGUI()
        {
            // 创建 GraphView
            graphView = new ECAGraphView();
            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);

            // 创建工具栏
            CreateToolbar();

            // 创建节点库面板
            CreateNodeLibrary();
        }

        private void CreateToolbar()
        {
            var toolbar = new Toolbar();

            var saveButton = new ToolbarButton(() => SaveConfig())
            {
                text = "保存"
            };
            toolbar.Add(saveButton);

            rootVisualElement.Add(toolbar);
        }

        private void CreateNodeLibrary()
        {
            // 左侧节点库
            var library = new VisualElement();
            library.style.width = 200;
            library.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);

            // 添加节点按钮
            AddNodeButton(library, "状态节点", () => CreateStateNode());
            AddNodeButton(library, "事件节点", () => CreateEventNode());
            AddNodeButton(library, "条件节点", () => CreateConditionNode());
            AddNodeButton(library, "动作节点", () => CreateActionNode());

            rootVisualElement.Add(library);
        }
    }
}
```

### 第4步：实现节点视图（3-5天）

**ECAStateNodeView.cs**（参考 NodeView.cs）：
```csharp
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace ET
{
    public class ECAStateNodeView : Node
    {
        public ECAStateNode StateNode;

        public ECAStateNodeView(ECAStateNode node)
        {
            StateNode = node;
            title = node.StateName;

            // 设置位置
            SetPosition(new Rect(node.Position, Vector2.zero));

            // 创建端口
            var inputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi, typeof(bool));
            inputPort.portName = "进入";
            inputContainer.Add(inputPort);

            var outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output,
                Port.Capacity.Multi, typeof(bool));
            outputPort.portName = "退出";
            outputContainer.Add(outputPort);

            // 添加属性字段
            CreatePropertyFields();
        }

        private void CreatePropertyFields()
        {
            // 使用 UIElements 创建属性编辑字段
            // 参考 NodeView.cs 的实现
        }
    }
}
```

---

## 开发时间估算

| 阶段 | 任务 | 时间 |
|------|------|------|
| 第1步 | 创建包基础结构 | 1天 |
| 第2步 | 定义核心数据结构 | 1-2天 |
| 第3步 | 实现 GraphView 编辑器 | 1-2周 |
| 第4步 | 实现节点视图 | 3-5天 |
| 第5步 | 实现连接和流转逻辑 | 3-5天 |
| 第6步 | 实现保存和加载 | 2-3天 |
| 第7步 | 实现运行时支持 | 1周 |
| 第8步 | 实现调试功能 | 3-5天 |
| 第9步 | 测试和优化 | 3-5天 |

**总计：约 3-4 周**

---

## 依赖关系

**cn.etetet.eca 包的依赖**：
```json
{
  "dependencies": [
    "cn.etetet.core",
    "cn.etetet.proto"
  ]
}
```

**层级**：第2层（和 BehaviorTree 同级）

---

## 总结

### 建议：创建新包

- ✅ 概念清晰，不混淆
- ✅ 数据结构独立，易维护
- ✅ 可以参考 BT 的实现经验
- ✅ 不影响现有 BT 系统

### 可以复用的经验

- GraphView 基础设置
- 节点视图实现
- 撤销/重做系统
- 右键菜单
- 编辑器布局

### 不能复用的部分

- 节点数据结构（树 vs 图）
- 执行逻辑（遍历 vs 事件驱动）
- 节点类型定义

---

## 下一步

1. 创建 `cn.etetet.eca` 包
2. 定义核心数据结构
3. 参考 BT 包实现 GraphView 编辑器
4. 逐步完善功能

需要我开始创建包的基础结构吗？

