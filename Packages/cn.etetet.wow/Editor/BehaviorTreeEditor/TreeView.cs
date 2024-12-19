using System;
using System.Collections.Generic;
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

        public NodeView MouseDownNode;
        
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

            foreach (GraphElement element in change.movedElements)
            {
                if (element is not NodeView movedNode)
                {
                    continue;
                }

                Rect movedRect = movedNode.GetPosition();
                // 检查与其他节点重叠
                foreach ((long _, NodeView otherNode) in this.Nodes)
                {
                    if (otherNode == movedNode)
                    {
                        continue;
                    }

                    Rect otherRect = otherNode.GetPosition();
                    if (!movedRect.Overlaps(otherRect))
                    {
                        continue;
                    }

                    this.MoveToNode(movedNode, otherNode);
                    break;
                }
            }
            return change;
        }
        
        private void MoveToNode(NodeView move, NodeView to)
        {
            //Debug.Log($"Node {move.Id} overlapped with Node {to.Id}");
            if (move.Parent == to.Parent)
            {
                int toIndex = to.Parent.GetChildren().IndexOf(to);
                
                BTNode btNode = move.Node;
                move.Dispose();
                to.Parent.AddChild(new NodeView(this, btNode), toIndex);
            }
            else
            {
                // 不同父节点下，移动到另一个节点下
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
        }
        
        public void InitTree(BehaviorTreeEditor behaviorTreeEditor, BTRoot node)
        {
            this.BehaviorTreeEditor = behaviorTreeEditor;

            this.Nodes.Clear();
            if (this.root != null)
            {
                this.root.Dispose();
                this.root = null;
                this.CopyNode = null;
            }

            if (node == null)
            {
                return;
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

        private void GetMaxId(BTNode node)
        {
            if (node.Id > this.maxId)
            {
                this.maxId = node.Id;
            }

            foreach (BTNode child in node.Children)
            {
                GetMaxId(child);
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
            evt.menu.AppendAction("Create", (o)=>
            {
                this.CreateNode(o).NoContext();
            });
        }

        private async ETTask CreateNode(DropdownMenuAction obj)
        {
            VisualElement windowRoot = this.BehaviorTreeEditor.rootVisualElement;
            Vector2 pos = windowRoot.ChangeCoordinatesTo(windowRoot.parent, obj.eventInfo.mousePosition + this.BehaviorTreeEditor.position.position);
            (SearchTreeEntry searchTreeEntry, SearchWindowContext context) = await this.RightClickMenu.WaitSelect(pos);
            
            Type type = searchTreeEntry.userData as Type;
            BTRoot btNode = Activator.CreateInstance(type) as BTRoot;
            this.InitTree(BehaviorTreeEditor, btNode);
        }

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
            this.InitTree(this.BehaviorTreeEditor, undoRoot);
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
            this.InitTree(this.BehaviorTreeEditor, redoRoot);
        }
    }
}


