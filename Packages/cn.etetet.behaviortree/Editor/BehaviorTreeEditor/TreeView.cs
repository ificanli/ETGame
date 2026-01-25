using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.Serialization;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ET
{
    [UxmlElement]
    public partial class TreeView: GraphView
    {

        public static TreeView ActiveTreeView { get; private set; }

        public readonly RightClickMenu RightClickMenu = ScriptableObject.CreateInstance<RightClickMenu>();

        public BehaviorTreeEditor BehaviorTreeEditor;

        private UnityEngine.Object scriptableObject;

        private NodeView root;

        public readonly Dictionary<long, NodeView> Nodes = new();

        private const int MaxUndoSteps = 50; // 最大撤销深度
        private readonly Stack<byte[]> undo = new();
        private readonly Stack<byte[]> redo = new();

        private int maxId;
        public int GenerateId()
        {
            return ++this.maxId;
        }

        public NodeView MouseDownNode;
        public Vector2 MoveStartPos;

        public bool IsDisposed { get; private set; }

        private bool isForceAsChildPressed;
        
        public TreeView()
        {
            Insert(0, new GridBackground());

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            //this.AddManipulator(new RectangleSelector());
            //this.AddManipulator(new FreehandSelector());

            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/cn.etetet.behaviortree/Editor/BehaviorTreeEditor/BehaviorTreeEditor.uss");
            styleSheets.Add(styleSheet);

            this.RegisterCallback<KeyDownEvent>(OnKeyDown);
            this.RegisterCallback<KeyUpEvent>(OnKeyUp);
            this.RegisterCallback<MouseUpEvent>(OnMouseUp);
            this.RegisterCallback<MouseDownEvent>(OnMouseDown);
            this.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            this.graphViewChanged = OnGraphViewChanged;

            // 注册DetachFromPanel事件，在TreeView从面板移除时清理资源
            this.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Cleanup();
        }

        /// <summary>
        /// 清理TreeView的资源，防止内存泄漏
        /// </summary>
        public void Cleanup()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.IsDisposed = true;

            if (ReferenceEquals(ActiveTreeView, this))
            {
                ActiveTreeView = null;
            }
            
            this.root?.Dispose();
            
            // 销毁RightClickMenu，防止内存泄漏
            if (this.RightClickMenu != null)
            {
                UnityEngine.Object.DestroyImmediate(this.RightClickMenu);
            }

            // 注销回调
            this.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            this.UnregisterCallback<KeyUpEvent>(OnKeyUp);
            this.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            this.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            this.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            this.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

#region 拖动节点到另外的节点上
        private void OnMouseDown(MouseDownEvent evt)
        {
            // 按住 Alt(Option) 拖动到目标节点：强制作为目标节点的子节点
            // 兼容旧逻辑：Shift 同样可触发该行为
            this.isForceAsChildPressed = evt.altKey || evt.shiftKey;
            ActiveTreeView = this;
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            this.isForceAsChildPressed = evt.altKey || evt.shiftKey;
        }

        public void OnMouseUp(MouseUpEvent evt)
        {
            this.isForceAsChildPressed = evt.altKey || evt.shiftKey;
            this.MouseDownNode = null;
            
            this.Layout();
        }
        
        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (this.MouseDownNode == null)
            {
                return change;
            }
            
            // 如果有移动过的元素，说明用户刚刚结束了一次拖拽
            if (change.movedElements == null)
            {
                return change;
            }

            NodeView moveNode = null;
            foreach (GraphElement element in change.movedElements)
            {
                if (element is not NodeView nodeView)
                {
                    continue;
                }

                moveNode = nodeView;
                break;
            }

            // 移动距离太小，可能是误操作
            float distance = (moveNode.GetPosition().position - this.MoveStartPos).magnitude;
            if (distance < 20)
            {
                return change;
            }

            Rect movedRect = moveNode.GetPosition();
            // 检查与其他节点重叠
            
            float maxV = 0;
            NodeView targetNode = null;
            foreach ((long _, NodeView otherNode) in this.Nodes)
            {
                if (otherNode == moveNode)
                {
                    continue;
                }

                Rect otherRect = otherNode.GetPosition();
                if (!movedRect.Overlaps(otherRect))
                {
                    continue;
                }
                
                float xMin = Mathf.Max(movedRect.xMin, otherRect.xMin);
                float yMin = Mathf.Max(movedRect.yMin, otherRect.yMin);
                float xMax = Mathf.Min(movedRect.xMax, otherRect.xMax);
                float yMax = Mathf.Min(movedRect.yMax, otherRect.yMax);

                Rect overRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
                float v3 = overRect.width * overRect.height;
                // 覆盖的面积要大于两个长方形其中一个的一半，避免误操作
                if (v3 > maxV)
                {
                    maxV = overRect.width * overRect.height;
                    targetNode = otherNode;                    
                }
            }
            if (targetNode != null)
            {
                this.MoveToNode(moveNode, targetNode, this.isForceAsChildPressed);
            }
            return change;
        }

        private void MoveToNode(NodeView move, NodeView to, bool forceAsChild)
        {
            ActiveTreeView = this;

            // 父节点不能移到子节点上
            NodeView tmp = to;
            while (true)
            {
                if (tmp == null)
                {
                    break;
                }
                if (tmp.Id == move.Id)
                {
                    return;
                }
                tmp = tmp.Parent;
            }
            
            //Debug.Log($"Node {move.Id} overlapped with Node {to.Id}");
            if (!forceAsChild && move.Parent == to.Parent)
            {
                this.SaveToUndo();
                
                int toIndex = to.Parent.GetChildren().IndexOf(to);
                
                byte[] nodeData = Sirenix.Serialization.SerializationUtility.SerializeValue(move.Node, DataFormat.Binary);
                move.Dispose();
                BTNode clone = Sirenix.Serialization.SerializationUtility.DeserializeValue<BTNode>(nodeData, DataFormat.Binary);
                to.Parent.AddChild(new NodeView(this, clone), toIndex);
            }
            else
            {
                // 不同父节点下 / 或者按住Shift强制作为目标子节点
                if (to.Node is not BTComposite && to.Node is not BTDecorate && to.Node is not BTRoot)
                {
                    this.BehaviorTreeEditor?.ShowText("目标节点不支持添加子节点");
                    return;
                }
                
                this.SaveToUndo();
                
                byte[] nodeData = Sirenix.Serialization.SerializationUtility.SerializeValue(move.Node, DataFormat.Binary);
                move.Dispose();
                BTNode clone = Sirenix.Serialization.SerializationUtility.DeserializeValue<BTNode>(nodeData, DataFormat.Binary);
                to.AddChild(new NodeView(this, clone));
            }
        }
#endregion
        
        private void OnKeyDown(KeyDownEvent evt)
        {
            // 检查当前 GraphView 是否在活动窗口中
            if (EditorWindow.focusedWindow != this.BehaviorTreeEditor)
            {
                return; // 如果不是当前 GraphView，直接退出
            }

            // 使用 evt 参数，而不是 Event.current
            // macOS: commandKey, Windows/Linux: ctrlKey
            bool isModifier = evt.commandKey || evt.ctrlKey;

            if (evt.keyCode == KeyCode.Z && isModifier)
            {
                this.UnDo();
                evt.StopPropagation(); // 防止事件继续传递
            }
            if (evt.keyCode == KeyCode.Y && isModifier)
            {
                this.Redo();
                evt.StopPropagation();
            }
            if (evt.keyCode == KeyCode.S && isModifier)
            {
                this.Save();
                evt.StopPropagation();
            }

            this.isForceAsChildPressed = evt.altKey || evt.shiftKey;
        }

        private void OnKeyUp(KeyUpEvent evt)
        {
            this.isForceAsChildPressed = evt.altKey || evt.shiftKey;
        }
        
        public void InitTree(BehaviorTreeEditor behaviorTreeEditor, UnityEngine.Object so, BTRoot node)
        {
            this.BehaviorTreeEditor = behaviorTreeEditor;
            this.scriptableObject = so;

            this.Nodes.Clear();

            if (this.root != null)
            {
                this.root.Dispose();
            }

            this.maxId = 0;
            GetMaxId(node);

            this.root = new NodeView(this, node);
            foreach (BTNode child in node.Children)
            {
                this.root.AddChild(new NodeView(this, child));
            }

            this.Layout();

            // 设置当前关注的TreeId，用于路径记录过滤
            this.BehaviorTreeEditor.PathRecorder.SetCurrentTreeId(node.TreeId);
        }

        private void MoveChildrenToRoot(BTRoot node)
        {
            this.Nodes.Clear();
            
            BTRoot btRootNode = (BTRoot)this.root.Node;
            
            this.root.Dispose();
            this.root = null;
            BTClipboard.Instance.Clear();

            btRootNode.Children.Clear();
            btRootNode.Children.AddRange(node.Children);
            node = btRootNode;

            this.maxId = 0;
            GetMaxId(node);
            
            this.root = new NodeView(this, node);
            foreach (BTNode child in node.Children)
            {
                this.root.AddChild(new NodeView(this, child));
            }

            this.Layout();
        }

        private void GetMaxId(BTNode node)
        {
            node.Children ??= new List<BTNode>();
            
            if (node.Id > this.maxId)
            {
                this.maxId = node.Id;
            }

            if (node.Children != null)
            {
                foreach (BTNode child in node.Children)
                {
                    GetMaxId(child);
                }
            }
        }

        public void AddNode(NodeView node)
        {
            this.Nodes.Add(node.Id, node);
        }

        public NodeView GetNode(long id)
        {
            this.Nodes.TryGetValue(id, out NodeView node);
            return node;
        }
        
        public NodeView RemoveNode(long id)
        {
            this.Nodes.Remove(id, out NodeView node);
            return node;
        }

        //点击右键菜单时触发
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
        }

        //private async ETTask CreateNode(DropdownMenuAction obj)
        //{
        //    VisualElement windowRoot = this.BehaviorTreeEditor.rootVisualElement;
        //    Vector2 pos = windowRoot.ChangeCoordinatesTo(windowRoot.parent, obj.eventInfo.mousePosition + this.BehaviorTreeEditor.position.position);
        //    (SearchTreeEntry searchTreeEntry, SearchWindowContext context) = await this.RightClickMenu.WaitSelect(pos);
        //    
        //    Type type = searchTreeEntry.userData as Type;
        //    BTRoot btNode = Activator.CreateInstance(type) as BTRoot;
        //    this.InitTree(BehaviorTreeEditor, this.SO, btNode);
        //}

        public void Layout()
        {
            if (this.root == null)
            {
                return;
            }
            
            this.root.Layout();
        }

        public byte[] BackupRoot()
        {
            try
            {
                return Sirenix.Serialization.SerializationUtility.SerializeValue(this.root.Node, DataFormat.Binary);
            }
            catch (Exception e)
            {
                Debug.LogError($"Backup root node failed: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取当前打开的行为树ID
        /// </summary>
        public long GetCurrentTreeId()
        {
            if (this.root?.Node is BTRoot btRoot)
            {
                return btRoot.TreeId;
            }
            return 0;
        }

        public UnityEngine.Object GetCurrentScriptableObject()
        {
            return this.scriptableObject;
        }

        public bool TrySetCurrentTreeId(long treeId, out long oldTreeId)
        {
            oldTreeId = 0;
            if (this.root?.Node is not BTRoot btRoot)
            {
                return false;
            }

            oldTreeId = btRoot.TreeId;
            btRoot.TreeId = treeId;
            this.BehaviorTreeEditor?.PathRecorder.SetCurrentTreeId(treeId);
            return true;
        }

        public void SaveToUndo(byte[] bytes = null)
        {
            try
            {
                if (bytes == null)
                {
                    bytes = Sirenix.Serialization.SerializationUtility.SerializeValue(this.root.Node, DataFormat.Binary);
                }

                this.undo.Push(bytes);

                // 限制撤销栈深度，防止内存无限增长
                if (this.undo.Count > MaxUndoSteps)
                {
                    // 将栈转为数组，移除最底层元素，重新压入栈
                    byte[][] undoArray = this.undo.ToArray();
                    this.undo.Clear();

                    // 跳过最底层的元素（最旧的快照）
                    for (int i = undoArray.Length - 2; i >= 0; i--)
                    {
                        this.undo.Push(undoArray[i]);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Save to undo failed: {e.Message}");
            }
        }
        
        public void Save()
        {
            if (this.scriptableObject == null)
            {
                return;
            }

            // 在保存前校验BTInput
            if (!ValidateAllNodes(out List<string> errors))
            {
                // 校验失败，显示错误信息
                string errorMessage = "Save Failed! Invalid BTInput:\n" + string.Join("\n", errors);
                this.BehaviorTreeEditor?.ShowText(errorMessage, 5f);
                Debug.LogError(errorMessage);
                return;
            }

            EditorUtility.SetDirty(this.scriptableObject);
            AssetDatabase.SaveAssets();
            this.BehaviorTreeEditor?.ShowText("Save Finish!");
        }
        
        public void UnDo()
        {
            if (this.undo.Count == 0)
            {
                return;
            }

            try
            {
                byte[] redoBytes = Sirenix.Serialization.SerializationUtility.SerializeValue(this.root.Node, DataFormat.Binary);
                this.redo.Push(redoBytes);

                // 限制重做栈深度
                if (this.redo.Count > MaxUndoSteps)
                {
                    byte[][] redoArray = this.redo.ToArray();
                    this.redo.Clear();
                    for (int i = redoArray.Length - 2; i >= 0; i--)
                    {
                        this.redo.Push(redoArray[i]);
                    }
                }

                byte[] undoBytes = this.undo.Pop();
                BTRoot undoRoot = Sirenix.Serialization.SerializationUtility.DeserializeValue<BTRoot>(undoBytes, DataFormat.Binary);
                this.MoveChildrenToRoot(undoRoot);
            }
            catch (Exception e)
            {
                Debug.LogError($"Undo operation failed: {e.Message}");
                this.BehaviorTreeEditor?.ShowText("Undo Failed! See console for details.", 3f);
            }
        }

        public void Redo()
        {
            if (this.redo.Count == 0)
            {
                return;
            }

            try
            {
                byte[] undoBytes = Sirenix.Serialization.SerializationUtility.SerializeValue(this.root.Node, DataFormat.Binary);
                this.undo.Push(undoBytes);

                // 限制撤销栈深度
                if (this.undo.Count > MaxUndoSteps)
                {
                    byte[][] undoArray = this.undo.ToArray();
                    this.undo.Clear();
                    for (int i = undoArray.Length - 2; i >= 0; i--)
                    {
                        this.undo.Push(undoArray[i]);
                    }
                }

                byte[] redoBytes = this.redo.Pop();
                BTRoot redoRoot = Sirenix.Serialization.SerializationUtility.DeserializeValue<BTRoot>(redoBytes, DataFormat.Binary);
                this.MoveChildrenToRoot(redoRoot);
            }
            catch (Exception e)
            {
                Debug.LogError($"Redo operation failed: {e.Message}");
                this.BehaviorTreeEditor?.ShowText("Redo Failed! See console for details.", 3f);
            }
        }
        
        public void SetRed(NodeView inputNode)
        {
            ClearNodeViewBordColor(this.root);

            ActiveTreeView = this;
            this.MouseDownNode = inputNode;
            List<string> inputs = NodeFieldHelper.GetInputs(inputNode, typeof(BTInput));
            this.SetRed(this.root, inputNode, inputs);
        }
        
        public static void ClearNodeViewBordColor(NodeView nodeView)
        {
            nodeView.SetBorderColor(Color.black);
            foreach (NodeView child in nodeView.GetChildren())
            {
                ClearNodeViewBordColor(child);
            }
        }

        // 返回值，是否继续遍历
        private bool SetRed(NodeView node, NodeView endNode, List<string> inputs)
        {
            if (node.Id == endNode.Id)
            {
                return false;
            }

            FieldInfo[] fieldInfos = node.Node.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var btInputFields = fieldInfos.Where(field =>
                    field.GetCustomAttributes(typeof(BTOutput), false).Any()
            );
            foreach (FieldInfo field in btInputFields)
            {
                string v = field.GetValue(node.Node) as string;
                if (inputs.Contains(v))
                {
                    node.SetBorderColor(Color.yellow);
                    break;
                }
            }

            foreach (NodeView child in node.GetChildren())
            {
                if (!SetRed(child, endNode, inputs))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 校验整棵树的所有BTInput字段是否合法
        /// </summary>
        /// <param name="errors">输出错误信息列表</param>
        /// <returns>是否所有BTInput都合法</returns>
        private bool ValidateAllNodes(out List<string> errors)
        {
            errors = new List<string>();

            if (this.root == null)
            {
                return true;
            }

            // 清除所有节点的边框颜色
            ClearNodeViewBordColor(this.root);

            // 遍历所有节点进行校验
            ValidateNodeRecursive(this.root, new Dictionary<string, Type>(), errors);

            // 如果有错误，将对应节点标记为红色
            if (errors.Count > 0)
            {
                foreach ((long _, NodeView nodeView) in this.Nodes)
                {
                    if (HasInvalidInput(nodeView, out _))
                    {
                        nodeView.SetBorderColor(Color.red);
                    }
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// 递归校验节点及其子节点的BTInput
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <param name="availableOutputs">当前可用的BTOutput集合（名称->类型）</param>
        /// <param name="errors">错误信息列表</param>
        private void ValidateNodeRecursive(NodeView node, Dictionary<string, Type> availableOutputs, List<string> errors)
        {
            // 直接使用公共的BTNodeValidator进行校验
            BTNodeValidator.ValidateNodeRecursive(node.Node, availableOutputs, errors);
        }

        /// <summary>
        /// 检查节点是否有不合法的BTInput
        /// </summary>
        /// <param name="node">要检查的节点</param>
        /// <param name="invalidFields">不合法的字段名列表</param>
        /// <returns>是否有不合法的输入</returns>
        private bool HasInvalidInput(NodeView node, out List<string> invalidFields)
        {
            invalidFields = new List<string>();

            // 收集从root到该节点路径上所有可用的BTOutput
            HashSet<string> availableOutputs = new HashSet<string>();
            CollectAvailableOutputs(this.root, node, availableOutputs);

            // 检查该节点的所有BTInput
            FieldInfo[] fieldInfos = node.Node.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var btInputFields = fieldInfos.Where(field => field.GetCustomAttributes(typeof(BTInput), false).Any());

            foreach (FieldInfo field in btInputFields)
            {
                string inputValue = field.GetValue(node.Node) as string;

                if (string.IsNullOrEmpty(inputValue))
                {
                    continue;
                }

                if (!availableOutputs.Contains(inputValue))
                {
                    invalidFields.Add(field.Name);
                }
            }

            return invalidFields.Count > 0;
        }

        /// <summary>
        /// 收集从root到目标节点路径上所有可用的BTOutput
        /// </summary>
        /// <param name="currentNode">当前遍历的节点</param>
        /// <param name="targetNode">目标节点</param>
        /// <param name="outputs">输出集合</param>
        /// <returns>是否找到目标节点</returns>
        private bool CollectAvailableOutputs(NodeView currentNode, NodeView targetNode, HashSet<string> outputs)
        {
            if (currentNode.Id == targetNode.Id)
            {
                return true;
            }

            // 添加当前节点的BTOutput
            FieldInfo[] fieldInfos = currentNode.Node.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var btOutputFields = fieldInfos.Where(field => field.GetCustomAttributes(typeof(BTOutput), false).Any());

            foreach (FieldInfo field in btOutputFields)
            {
                string outputValue = field.GetValue(currentNode.Node) as string;
                if (!string.IsNullOrEmpty(outputValue))
                {
                    outputs.Add(outputValue);
                }
            }

            // 递归遍历子节点
            foreach (NodeView child in currentNode.GetChildren())
            {
                if (CollectAvailableOutputs(child, targetNode, outputs))
                {
                    return true;
                }
            }

            return false;
        }

        #region 调试功能

        /// <summary>
        /// 高亮显示路径中的节点
        /// </summary>
        /// <param name="path">节点ID列表</param>
        public void HighlightPath(List<int> path)
        {
            // 清除所有节点的高亮
            ClearHighlight();

            if (path == null || path.Count == 0)
            {
                return;
            }

            // 高亮路径中的节点
            foreach (int nodeId in path)
            {
                if (this.Nodes.TryGetValue(nodeId, out NodeView nodeView))
                {
                    nodeView.style.backgroundColor = new Color(0f, 0.5f, 0f, 1f); // 绿色
                }
            }
        }

        /// <summary>
        /// 清除所有节点的高亮
        /// </summary>
        public void ClearHighlight()
        {
            foreach ((long _, NodeView nodeView) in this.Nodes)
            {
                nodeView.style.backgroundColor = new Color(0f, 0f, 0f, 1f); // 黑色
            }
        }

        #endregion
    }
}
