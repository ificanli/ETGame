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
    [HideMonoScript]
    public class NodeIMGUI : SerializedScriptableObject
    {
        [NonSerialized, OdinSerialize]
        [HideReferenceObjectPicker]
        [InlineProperty, HideLabel] // 去掉折叠和标题
        public BTNode Node;
    }
    
    public class NodeView : Node, IDisposable
    {
        private readonly TreeView treeView;

        public BTNode Node { get; private set; }

        private NodeView Parent { get; set; }

        private readonly Port inPort;

        private readonly Port outPort;

        private Label idLabel;

        private Edge edge;

        private readonly List<NodeView> children = new();

        private readonly Button collapseButton;

        private bool childrenCollapsed;

        public int Id
        {
            get
            {
                return this.Node.Id;
            }
            set
            {
                this.Node.Id = value;
            }
        }

        private Vector2 position;
        
        public Vector2 Position
        {
            get
            {
                return this.position;
            }
            set
            {
                this.position = value;
                this.SetPosition(new Rect(this.position, this.GetPosition().size));
            }
        }

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
                        child.Visible = false;
                    }
                }
                else
                {
                    this.collapseButton.text = "-";
                    foreach (NodeView child in this.GetChildren())
                    {
                        child.Visible = true;
                    }
                }
            }
        }

        public bool Visible
        {
            get
            {
                return this.visible;
            }
            set
            {
                this.visible = value;
                this.edge.visible = this.visible;

                if (!this.visible)
                {
                    foreach (NodeView child in this.GetChildren())
                    {
                        child.Visible = value;
                    }
                }
                else
                {
                    if (!this.childrenCollapsed)
                    {
                        foreach (NodeView child in this.GetChildren())
                        {
                            child.Visible = value;
                        }
                    }
                }
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
            this.Node = node;
            base.title = node.GetType().Name;

            if (node.Id == 0)
            {
                this.Id = this.treeView.GenerateId();
            }

            this.idLabel = new Label(node.Id.ToString());
            idLabel.style.width = 32;
            idLabel.style.height = 20;
            idLabel.style.marginTop = 5;
            this.titleContainer.Add(this.idLabel);
            
            style.backgroundColor = new Color(0f, 0f, 0f, 1f);

            NodeIMGUI nodeIMGUI = ScriptableObject.CreateInstance<NodeIMGUI>();
            nodeIMGUI.Node = this.Node;

            contentCollapseButton = new Button();
            contentCollapseButton.style.height = 20;
            contentCollapseButton.style.marginLeft = 5;
            contentCollapseButton.clicked += ChangeContentCollapse;
            
            base.contentContainer.Add(this.contentCollapseButton);
            
            Editor editor = Editor.CreateEditor(nodeIMGUI);

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
            
            this.treeView.AddElement(this);
            this.treeView.AddNode(this);
        }
        
        public void Dispose()
        {
            long id = this.Id;
            if (id == 0)
            {
                return;
            }
            this.Id = 0;
            
            this.treeView.RemoveElement(this);
            if (this.edge != null)
            {
                this.treeView.RemoveElement(this.edge);
            }

            if (this.Parent != null && this.Parent.Id != 0)
            {
                this.Parent.GetChildren().Remove(this);
                this.Parent.Node.Children.Remove(this.Node);
            }

            this.treeView.RemoveNode(id);

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

        public void AddChild(BTNode node)
        {
            NodeView nodeView = new(this.treeView, node);
            nodeView.Parent = this;
            this.children.Add(nodeView);
            if (!this.Node.Children.Contains(nodeView.Node))
            {
                this.Node.Children.Add(nodeView.Node);
            }
            
            nodeView.edge = nodeView.Parent.outPort.ConnectTo(nodeView.inPort);
            this.treeView.AddElement(nodeView.edge);

            foreach (BTNode child in nodeView.Node.Children)
            {
                nodeView.AddChild(child);
            }
        }

        public void RemoveChild(NodeView nodeView)
        {
            nodeView.Dispose();
        }

        //点击右键菜单时触发
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Create", (o)=>this.CreateNode(o).NoContext());
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
            this.treeView.Layout();
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

        private static void ClearId(BTNode node)
        {
            node.Id = 0;
            foreach (BTNode child in node.Children)
            {
                ClearId(child);
            }
        }

        private void PasterNode(DropdownMenuAction obj)
        {
            if (this.treeView.CopyNode == null)
            {
                return;
            }

            byte[] nodeBytes = Sirenix.Serialization.SerializationUtility.SerializeValue(this.treeView.CopyNode.Node, DataFormat.Binary);
            BTNode clone = Sirenix.Serialization.SerializationUtility.DeserializeValue<BTNode>(nodeBytes, DataFormat.Binary);
            ClearId(clone);
            this.AddChild(clone);

            if (this.treeView.IsCut)
            {
                this.treeView.CopyNode.Parent.RemoveChild(this.treeView.CopyNode);
            }

            this.treeView.CopyNode = null;
            
            this.treeView.Layout();
        }

        private void CopyNode(DropdownMenuAction obj)
        {
            this.treeView.CopyNode = this;
            this.treeView.IsCut = false;
        }

        private async ETTask CreateNode(DropdownMenuAction obj)
        {
            VisualElement windowRoot = this.treeView.BehaviorTreeEditor.rootVisualElement;
            Vector2 pos = windowRoot.ChangeCoordinatesTo(windowRoot.parent,
                obj.eventInfo.mousePosition + this.treeView.BehaviorTreeEditor.position.position);
            (SearchTreeEntry searchTreeEntry, SearchWindowContext context) = await this.treeView.RightClickMenu.WaitSelect(pos);
            Type type = searchTreeEntry.userData as Type;
            BTNode btNode = Activator.CreateInstance(type) as BTNode;
            this.AddChild(btNode);
            
            this.treeView.Layout();
        }

        private void DeleteNode(DropdownMenuAction obj)
        {
            this.Dispose();
            
            this.treeView.Layout();
        }

        #region Layout

        private const float HorizontalSpacing = 300f; // 节点之间的水平间距
        private const float VerticalSpacing = 100f; // 节点之间的垂直间距

        private void Layout(DropdownMenuAction obj)
        {
            this.Layout();
        }
        
        void AjustPosition(Vector2 offset)
        {
            this.Position -= offset;
            foreach (NodeView child in this.GetChildren())
            {
                child.AjustPosition(offset);
            }
        }
        
        
        public void Layout()
        {
            Vector2 oldPos = this.GetPosition().position;
            Vector2 newPos = LayoutNode(this, oldPos.x, oldPos.y, out _, out _);
            Vector2 offset = newPos - oldPos;
            AjustPosition(offset);
        }

        /// <summary>
        /// 递归布局节点（从左往右排列）
        /// </summary>
        private static Vector2 LayoutNode(NodeView node, float currentX, float startY, out float totalHeight, out float nextX)
        {
            float childY = startY; // 当前子节点的Y坐标
            float maxChildWidth = 0; // 子节点中最大的宽度

            // 获取当前节点的子节点
            List<NodeView> children = node.GetNotCollapsedChildren();

            // 如果没有子节点，直接设置当前节点的位置并返回
            if (children.Count == 0)
            {
                node.Position = new Vector2(currentX, startY); // 设置节点位置
                totalHeight = VerticalSpacing; // 节点的高度
                nextX = currentX + HorizontalSpacing; // 节点的宽度
                return new Vector2(currentX, startY);
            }

            // 遍历子节点，递归布局
            foreach (NodeView child in children)
            {
                float childHeight;
                LayoutNode(child, currentX + HorizontalSpacing, childY, out childHeight, out nextX);

                // 更新垂直方向位置
                childY += childHeight + VerticalSpacing;

                // 计算子节点中的最大宽度
                maxChildWidth = Mathf.Max(maxChildWidth, nextX - currentX);
            }

            // 计算子节点总高度
            totalHeight = childY - startY - VerticalSpacing;

            // 将当前节点居中放置在子节点左侧
            float nodeY = startY + (totalHeight - VerticalSpacing) / 2f;
            node.Position = new Vector2(currentX, nodeY);

            nextX = currentX + maxChildWidth;

            return new Vector2(currentX, nodeY);
        }

        #endregion
        
    }
}