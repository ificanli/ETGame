using System;
using System.CodeDom;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ET
{
    public class NodeIMGUI : SerializedScriptableObject
    {
        [NonSerialized, OdinSerialize]
        [HideReferenceObjectPicker]
        [LabelText("节点内容")]
        public BTNode Node;
    }

    public class NodeView : Node, IDisposable
    {
        public long Id;
        
        private TreeView treeView;

        private BTNode Node { get; }

        private NodeView Parent { get; set; }

        private readonly Port inPort;

        private readonly Port outPort;

        public Edge edge;

        private readonly List<NodeView> children = new();

        private readonly Button collapseButton;

        private bool childrenCollapsed;

        public bool ChildrenCollapsed
        {
            get
            {
                return this.childrenCollapsed;
            }
            set
            {
                this.childrenCollapsed = value;
                if (this.childrenCollapsed)
                {
                    this.collapseButton.text = "+";
                    foreach (NodeView child in this.GetChildren())
                    {
                        child.Visibale = false;
                    }
                }
                else
                {
                    this.collapseButton.text = "-";
                    foreach (NodeView child in this.GetChildren())
                    {
                        child.Visibale = true;
                    }
                }
            }
        }

        public bool Visibale
        {
            get
            {
                return this.visible;
            }
            set
            {
                this.visible = value;
                this.edge.visible = this.visible;
            }
        }

        private readonly IMGUIContainer imgui;
        private readonly Button contentCollapseButton;

        private bool contentCollapsed;
        
        public bool ContentCollapsed
        {
            get
            {
                return this.contentCollapsed;
            }
            set
            {
                this.contentCollapsed = value;
                if (this.contentCollapsed)
                {
                    this.contentCollapseButton.text = "隐藏节点内容";
                    this.imgui.style.display = DisplayStyle.Flex;
                }
                else
                {
                    this.contentCollapseButton.text = "显示节点内容";
                    this.imgui.style.display = DisplayStyle.None;
                }

                RefreshExpandedState();
            }
        }

        public NodeView(TreeView treeView, BTNode node)
        {
            this.treeView = treeView;
            this.Id = this.treeView.GeneraterId();
            this.Node = node;
            base.title = node.GetType().Name;

            style.backgroundColor = new Color(0f, 0f, 0f, 1f);

            NodeIMGUI nodeIMGUI = ScriptableObject.CreateInstance<NodeIMGUI>();
            nodeIMGUI.Node = this.Node;

            contentCollapseButton = new Button();
            contentCollapseButton.style.height = 32;
            contentCollapseButton.style.marginLeft = 5;
            contentCollapseButton.clicked += ChangeContentCollapse;
            
            base.contentContainer.Add(this.contentCollapseButton);
            
            UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(nodeIMGUI);

            this.imgui = new(() => { editor.OnInspectorGUI(); });
            Add(this.imgui);
            
            this.AddManipulator(new ResizableManipulator());

            this.inPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            inputContainer.Add(this.inPort);
            this.outPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            outputContainer.Add(this.outPort);

            collapseButton = new Button();
            collapseButton.text = "-";
            collapseButton.style.width = 32;
            collapseButton.style.height = 32;
            collapseButton.style.marginLeft = 5;
            collapseButton.clicked += ChangeCollapse;
            this.titleContainer.Add(this.collapseButton);
            
            this.ContentCollapsed = false;
        }
        
        public void Dispose()
        {
            if (this.Id == 0)
            {
                return;
            }
            this.Id = 0;
            
            this.treeView.RemoveElement(this);
            this.treeView.RemoveElement(this.edge);

            if (this.Parent != null)
            {
                this.Parent.GetChildren().Remove(this);
            }

            foreach (NodeView nodeView in this.GetChildren())
            {
                nodeView.Dispose();
            }
        }

        public List<NodeView> GetChildren()
        {
            return this.children;
        }
        
        public List<NodeView> GetNotCollapsedChildren()
        {
            if (this.ChildrenCollapsed)
            {
                return new List<NodeView>();
            }
            return this.children;
        }

        public void AddChild(NodeView nodeView)
        {
            nodeView.Parent = this;
            this.children.Add(nodeView);
            if (!this.Node.Children.Contains(nodeView.Node))
            {
                this.Node.Children.Add(nodeView.Node);
            }

            this.treeView.AddElement(nodeView);
            nodeView.edge = nodeView.Parent.outPort.ConnectTo(nodeView.inPort);
            this.treeView.AddElement(nodeView.edge);

            foreach (BTNode child in nodeView.Node.Children)
            {
                NodeView childNodeView = new(this.treeView, child);
                nodeView.AddChild(childNodeView);
            }
        }

        public void RemoveChild(NodeView nodeView)
        {
            nodeView.Dispose();
        }

        //点击右键菜单时触发
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Create", this.CreateNode);
            evt.menu.AppendAction("Delete", this.DeleteNode);
            evt.menu.AppendAction("Copy", this.CopyNode);
            evt.menu.AppendAction("Cut", this.CutNode);
            evt.menu.AppendAction("Paste", this.PasterNode);
            evt.menu.AppendAction("Layout", this.Layout);
        }
        
        private void ChangeContentCollapse()
        {
            this.ContentCollapsed = !this.ContentCollapsed;
        }

        private void ChangeCollapse()
        {
            this.ChildrenCollapsed = !this.ChildrenCollapsed;
        }

        private void CutNode(DropdownMenuAction obj)
        {
            if (this.Parent == null)
            {
                return;
            }

            this.treeView.CopyNode = this;
            this.treeView.IsCut = true;
        }

        private void PasterNode(DropdownMenuAction obj)
        {
            if (this.treeView.CopyNode == null)
            {
                return;
            }

            byte[] nodeBytes = Sirenix.Serialization.SerializationUtility.SerializeValue(this.treeView.CopyNode.Node, DataFormat.Binary);
            BTNode clone = Sirenix.Serialization.SerializationUtility.DeserializeValue<BTNode>(nodeBytes, DataFormat.Binary);

            NodeView nodeView = new(this.treeView, clone);
            this.AddChild(nodeView);

            if (this.treeView.IsCut)
            {
                this.treeView.CopyNode.Parent.RemoveChild(this.treeView.CopyNode);
            }

            this.treeView.CopyNode = null;
        }

        private void CopyNode(DropdownMenuAction obj)
        {
            this.treeView.CopyNode = this;
            this.treeView.IsCut = false;
        }

        private void CreateNode(DropdownMenuAction obj)
        {
            VisualElement windowRoot = BehaviorTreeEditor.Instance.rootVisualElement;
            Vector2 pos = windowRoot.ChangeCoordinatesTo(windowRoot.parent,
                obj.eventInfo.mousePosition + BehaviorTreeEditor.Instance.position.position);
            this.treeView.RightClickMenu.OnSelectEntryHandler = OnSelectEntryHandler;
            SearchWindow.Open(new SearchWindowContext(pos), this.treeView.RightClickMenu);
        }

        private bool OnSelectEntryHandler(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            Type type = searchTreeEntry.userData as Type;
            NodeView nodeView = new(this.treeView, Activator.CreateInstance(type) as BTNode);
            this.AddChild(nodeView);
            
            this.treeView.Layout();
            return true;
        }

        private void DeleteNode(DropdownMenuAction obj)
        {
            this.Dispose();
            
            this.treeView.Layout();
        }

        #region Layout

        private const float NodeWidth = 200f; // 节点的宽度
        private const float NodeHeight = 150f; // 节点的高度
        private const float HorizontalSpacing = 50f; // 节点之间的水平间距
        private const float VerticalSpacing = 50f; // 节点之间的垂直间距

        /// <summary>
        /// Reingold-Tilford自动布局入口
        /// </summary>
        private void Layout(DropdownMenuAction obj)
        {
            this.Layout();
        }

        public void Layout()
        {
            // 存储节点的布局信息
            Dictionary<NodeView, NodeData> nodeDataDict = new Dictionary<NodeView, NodeData>();

            // 第一次遍历：计算初始坐标和偏移量
            CalculateInitialPosition(this, 0, 0, nodeDataDict);

            // 第二次遍历：根据根节点调整最终坐标
            SetFinalPosition(this, 0, 0, nodeDataDict);
        }

        /// <summary>
        /// 计算节点的初始位置
        /// </summary>
        private static void CalculateInitialPosition(NodeView node, float depth, float siblingIndex, Dictionary<NodeView, NodeData> nodeDataDict)
        {
            List<NodeView> children = node.GetNotCollapsedChildren();

            NodeData nodeData = new()
            {
                X = depth * (NodeWidth + HorizontalSpacing), // 从左到右
                Y = 0,
                Modifier = 0
            };

            if (children.Count == 0)
            {
                // 如果没有子节点，则按顺序排列
                nodeData.Y = siblingIndex * (NodeHeight + VerticalSpacing);
            }
            else
            {
                // 递归计算子节点位置
                float childY = 0;
                for (int i = 0; i < children.Count; i++)
                {
                    NodeView child = children[i];
                    CalculateInitialPosition(child, depth + 1, i, nodeDataDict);
                    childY += nodeDataDict[child].Y;
                }

                // 让父节点垂直居中所有子节点
                nodeData.Y = childY / children.Count;
            }

            nodeDataDict[node] = nodeData;
        }

        /// <summary>
        /// 设置最终坐标
        /// </summary>
        private static void SetFinalPosition(NodeView node, float offsetX, float offsetY, Dictionary<NodeView, NodeData> nodeDataDict)
        {
            NodeData nodeData = nodeDataDict[node];

            float finalX = nodeData.X + offsetX; // 水平坐标
            float finalY = nodeData.Y + offsetY; // 垂直坐标

            node.SetPosition(new Rect(finalX, finalY, NodeWidth, NodeHeight));

            List<NodeView> children = node.GetNotCollapsedChildren();
            foreach (NodeView child in children)
            {
                SetFinalPosition(child, offsetX, offsetY + nodeData.Modifier, nodeDataDict);
            }
        }

        /// <summary>
        /// 用于存储节点布局信息的类
        /// </summary>
        private class NodeData
        {
            public float X; // X坐标
            public float Y; // Y坐标
            public float Modifier; // 偏移量
        }

        #endregion
        
    }
}