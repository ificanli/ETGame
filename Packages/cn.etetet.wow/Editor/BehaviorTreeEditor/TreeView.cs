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
        public readonly RightClickMenu RightClickMenu = ScriptableObject.CreateInstance<RightClickMenu>();

        public BehaviorTreeEditor BehaviorTreeEditor;

        private UnityEngine.Object scriptableObject;

        private NodeView root;

        public readonly Dictionary<long, NodeView> Nodes = new();

        private readonly Stack<byte[]> undo = new();
        private readonly Stack<byte[]> redo = new();

        public NodeView CopyNode;
        public bool IsCut;
        private int maxId;
        public int GenerateId()
        {
            return ++this.maxId;
        }

        private Label text;
        private double messageEndTime;

        public NodeView MouseDownNode;
        public Vector2 MoveStartPos;
        
        public TreeView()
        {
            Insert(0, new GridBackground());
            
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            //this.AddManipulator(new RectangleSelector());
            //this.AddManipulator(new FreehandSelector());

            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/cn.etetet.wow/Editor/BehaviorTreeEditor/BehaviorTreeEditor.uss");
            styleSheets.Add(styleSheet);
            
            this.RegisterCallback<KeyDownEvent>(OnKeyDown);
            this.RegisterCallback<MouseUpEvent>(OnMouseUp);
            this.graphViewChanged = OnGraphViewChanged;

            this.schedule.Execute((_)=> { this.Update(); }).Every(30);
        }

#region 拖动节点到另外的节点上

        public void OnMouseUp(MouseUpEvent evt)
        {
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
                this.MoveToNode(moveNode, targetNode);
            }
            return change;
        }

        public void ShowText(string s, float timeout = 1)
        {
            this.text = this.Q<Label>("LabelTips");
            this.messageEndTime = EditorApplication.timeSinceStartup + timeout;
            this.text.style.opacity = 1f;
            this.text.text = s;
        }

        private void Update()
        {
            // 如果当前时间超过 _messageEndTime，就隐藏提示
            if (EditorApplication.timeSinceStartup > this.messageEndTime)
            {
                if (this.text != null)
                {
                    this.text.style.opacity = 0f;
                    this.text = null;
                }
            }
        }
        
        private void MoveToNode(NodeView move, NodeView to)
        {
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
            if (move.Parent == to.Parent)
            {
                this.SaveToUndo();
                
                int toIndex = to.Parent.GetChildren().IndexOf(to);
                
                BTNode btNode = move.Node;
                move.Dispose();
                to.Parent.AddChild(new NodeView(this, btNode), toIndex);
            }
            else
            {
                // 不同父节点下，移动到另一个节点下
                if (to.Node is not BTComposite && to.Node is not BTDecorate && to.Node is not BTRoot)
                {
                    return;
                }
                
                this.SaveToUndo();
                
                BTNode btNode = move.Node;
                move.Dispose();
                to.AddChild(new NodeView(this, btNode));
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
            
            Event e = Event.current;
            if (e.keyCode == KeyCode.Z && e.control)
            {
                this.UnDo();
                e.Use(); // 防止事件继续传递
            }
            if (e.keyCode == KeyCode.Y && e.control)
            {
                this.Redo();
                e.Use();
            }

            if (e.keyCode == KeyCode.S && e.control)
            {
                this.Save();
                e.Use();
            }
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
        }

        private void MoveChildrenToRoot(BTRoot node)
        {
            this.Nodes.Clear();
            
            BTRoot btRootNode = (BTRoot)this.root.Node;
            
            this.root.Dispose();
            this.root = null;
            this.CopyNode = null;

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
            return Sirenix.Serialization.SerializationUtility.SerializeValue(this.root.Node, DataFormat.Binary);
        }

        public void SaveToUndo(byte[] bytes = null)
        {
            if (bytes == null)
            {
                bytes = Sirenix.Serialization.SerializationUtility.SerializeValue(this.root.Node, DataFormat.Binary);
            }

            this.undo.Push(bytes);
        }
        
        public void Save()
        {
            EditorUtility.SetDirty(this.scriptableObject);
            AssetDatabase.SaveAssets();
            this.ShowText("Save Finish!");
        }
        
        public void UnDo()
        {
            if (this.undo.Count == 0)
            {
                return;
            }

            
            byte[] redoBytes = Sirenix.Serialization.SerializationUtility.SerializeValue(this.root.Node, DataFormat.Binary);
            this.redo.Push(redoBytes);
            
            byte[] undoBytes = this.undo.Pop();
            BTRoot undoRoot = Sirenix.Serialization.SerializationUtility.DeserializeValue<BTRoot>(undoBytes, DataFormat.Binary);
            this.MoveChildrenToRoot(undoRoot);
        }

        public void Redo()
        {
            if (this.redo.Count == 0)
            {
                return;
            }
            byte[] undoBytes = Sirenix.Serialization.SerializationUtility.SerializeValue(this.root.Node, DataFormat.Binary);
            this.undo.Push(undoBytes);
            
            byte[] redoBytes = this.redo.Pop();
            BTRoot redoRoot = Sirenix.Serialization.SerializationUtility.DeserializeValue<BTRoot>(redoBytes, DataFormat.Binary);
            this.MoveChildrenToRoot(redoRoot);
        }
        
        public void SetRed(NodeView inputNode)
        {
            ClearNodeViewBordColor(this.root);
            
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
    }
}


