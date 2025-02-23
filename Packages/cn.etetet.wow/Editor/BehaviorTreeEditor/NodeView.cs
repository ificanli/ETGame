using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public NodeView Parent { get; private set; }

        private readonly Port inPort;

        private readonly Port outPort;

        private Edge edge;

        private readonly List<NodeView> children = new();

        private readonly Button collapseButton;

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
        
        public Vector2 Position
        {
            get
            {
                return this.Node.Position;
            }
            set
            {
                this.Node.Position = value;
                this.SetPosition(new Rect(this.Node.Position, this.GetPosition().size));
            }
        }

        public bool ChildrenCollapsed
        {
            get
            {
                return this.Node.ChildrenCollapsed;
            }
            set
            {
                this.Node.ChildrenCollapsed = value;
                if (this.Node.ChildrenCollapsed)
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
                    if (!this.ChildrenCollapsed)
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
        
        public bool ContentCollapsed
        {
            get
            {
                return this.Node.IsCollapsed;
            }
            set
            {
                this.Node.IsCollapsed = value;
                if (!this.Node.IsCollapsed)
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

            Label idLabel = new(node.Id.ToString());
            idLabel.style.width = 32;
            idLabel.style.height = 20;
            idLabel.style.marginTop = 5;
            this.titleContainer.Add(idLabel);
            
            style.backgroundColor = new Color(0f, 0f, 0f, 1f);

            NodeIMGUI nodeIMGUI = ScriptableObject.CreateInstance<NodeIMGUI>();
            nodeIMGUI.Node = this.Node;

            contentCollapseButton = new Button();
            contentCollapseButton.style.height = 20;
            contentCollapseButton.style.marginLeft = 5;
            contentCollapseButton.clicked += ChangeContentCollapse;
            
            base.contentContainer.Add(this.contentCollapseButton);
            
            this.tooltip = this.Node.Desc;
            
            Editor editor = Editor.CreateEditor(nodeIMGUI);

            this.imgui = new(() =>
            {
                byte[] nodeBytes = null;
                Event evt = Event.current;
                if (evt != null && (evt.type == EventType.KeyDown || evt.type == EventType.MouseDown || evt.type == EventType.DragPerform))
                {
                    nodeBytes = this.treeView.BackupRoot();
                }

                editor.OnInspectorGUI(); 
                
                if (GUI.changed)
                {
                    this.tooltip = this.Node.Desc;
                    if (nodeBytes != null)
                    {
                        this.treeView.SaveToUndo(nodeBytes);
                    }
                }
            });
            Add(this.imgui);
            
            this.inPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            inputContainer.Add(this.inPort);

            this.outPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            outputContainer.Add(this.outPort);
            
            // 有孩子，显示折叠按钮
            if (this.Node.Children is { Count: > 0 })
            {
                collapseButton = new Button();
                collapseButton.text = "-";
                collapseButton.style.width = 32;
                collapseButton.style.height = 32;
                collapseButton.style.marginLeft = 5;
                collapseButton.clicked += ChangeCollapse;
                this.titleContainer.Add(this.collapseButton);
                
                this.ChildrenCollapsed = this.Node.ChildrenCollapsed;
            }

            this.treeView.AddElement(this);
            this.treeView.AddNode(this);
            
            this.ContentCollapsed = this.Node.IsCollapsed;
            this.Position = this.Node.Position;

            RegisterCallback<MouseDownEvent>(OnMouseDown);
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

        private void OnMouseDown(MouseDownEvent evt)
        {
            this.treeView.MouseDownNode = this;
            this.treeView.MoveStartPos = this.Position;
            this.treeView.SetRed(this);
        }
        
        public void SetBorderColor(Color color)
        {
            style.borderTopColor    = new StyleColor(color);
            style.borderRightColor  = new StyleColor(color);
            style.borderBottomColor = new StyleColor(color);
            style.borderLeftColor   = new StyleColor(color);

            style.borderTopWidth    = 2;
            style.borderRightWidth  = 2;
            style.borderBottomWidth = 2;
            style.borderLeftWidth   = 2;
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

        public void AddChild(NodeView nodeView, int index = -1)
        {
            nodeView.Parent = this;
            if (index == -1)
            {
                this.children.Add(nodeView);
            }
            else
            {
                this.children.Insert(index, nodeView);
            }

            if (!this.Node.Children.Contains(nodeView.Node))
            {
                if (index == -1)
                {
                    this.Node.Children.Add(nodeView.Node);
                }
                else
                {
                    this.Node.Children.Insert(index, nodeView.Node);
                }
            }
            
            nodeView.edge = nodeView.Parent.outPort.ConnectTo(nodeView.inPort);
            this.treeView.AddElement(nodeView.edge);
            
            if (this.ChildrenCollapsed)
            {
                nodeView.Visible = false;
            }
            if (!this.Visible)
            {
                nodeView.Visible = false;
            }

            if (nodeView.Node.Children != null)
            {
                foreach (BTNode child in nodeView.Node.Children)
                {
                    nodeView.AddChild(new NodeView(this.treeView, child));
                }
            }
        }

        public void RemoveChild(NodeView nodeView)
        {
            nodeView.Dispose();
        }

        //点击右键菜单时触发
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.ClearItems();
            evt.menu.AppendAction("Create", (o)=>this.CreateNode(o).NoContext());
            evt.menu.AppendAction("Delete", this.DeleteNode);
            evt.menu.AppendAction("Copy", this.CopyNode);
            evt.menu.AppendAction("Cut", this.CutNode);
            if (this.treeView.CopyNode != null)
            {
                evt.menu.AppendAction("Paste", this.PasterNode);
            }

            if (this.Parent != null)
            {
                evt.menu.AppendAction("Change", (o)=>this.Change(o).NoContext());
            }
        }

        private async ETTask Change(DropdownMenuAction obj)
        {
            if (this.Parent == null)
            {
                return;
            }
            
            VisualElement windowRoot = this.treeView.BehaviorTreeEditor.rootVisualElement;
            Vector2 pos = windowRoot.ChangeCoordinatesTo(windowRoot.parent,
                obj.eventInfo.mousePosition + this.treeView.BehaviorTreeEditor.position.position);
            (SearchTreeEntry searchTreeEntry, SearchWindowContext context) = await this.treeView.RightClickMenu.WaitSelect(pos);
            
            this.treeView.SaveToUndo();
            
            Type type = searchTreeEntry.userData as Type;
            BTNode btNode = Activator.CreateInstance(type) as BTNode;
            
            if (btNode is BTRoot)
            {
                this.treeView.ShowText("can not change to root node!");
                return;
            }
            
            if (this.GetChildren().Count > 0)
            {
                this.treeView.ShowText("node has child!");
                return;
            }

            int index = this.Parent.GetChildren().IndexOf(this);
            
            // 把孩子复制过去
            if (this.GetChildren().Count > 0)
            {
                foreach (NodeView child in this.GetChildren())
                {
                    btNode.Children.Add(child.Node);
                }
            }
            
            // 删除当前节点
            this.Dispose();

            NodeView replaceNodeView = new(this.treeView, btNode);
            this.Parent.AddChild(replaceNodeView, index);
            
            this.treeView.Layout();
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
            
            this.treeView.SaveToUndo();

            byte[] nodeBytes = Sirenix.Serialization.SerializationUtility.SerializeValue(this.treeView.CopyNode.Node, DataFormat.Binary);
            BTNode clone = Sirenix.Serialization.SerializationUtility.DeserializeValue<BTNode>(nodeBytes, DataFormat.Binary);
            ClearId(clone);
            this.AddChild(new NodeView(this.treeView, clone));

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
            
            this.treeView.SaveToUndo();
            
            Type type = searchTreeEntry.userData as Type;
            BTNode btNode = Activator.CreateInstance(type) as BTNode;
            this.AddChild(new NodeView(this.treeView, btNode));
            
            this.treeView.Layout();
        }

        private void DeleteNode(DropdownMenuAction obj)
        {
            this.treeView.SaveToUndo();
            
            this.Dispose();
            
            this.treeView.Layout();
        }

        #region Layout

        private const float HorizontalSpacing = 300f; // 节点之间的水平间距
        private const float VerticalSpacing = 100f; // 节点之间的垂直间距

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