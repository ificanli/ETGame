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
    public class NodeIMGUI: SerializedScriptableObject
    {
        [NonSerialized, OdinSerialize]
        [HideReferenceObjectPicker]
        [LabelText("节点内容")]
        public BTNode Node;
    }
    
    public class NodeView: Node
    {
        private TreeView treeView;
        
        private BTNode Node { get; }

        private NodeView Parent { get; set; }

        private readonly Port inPort;

        private readonly Port outPort;

        public Edge edge;

        private readonly List<NodeView> children = new();
        
        private Button collapseButton;

        private bool collapsed;

        public bool Collapsed
        {
            get
            {
                return this.collapsed;
            }
            set
            {
                this.collapsed = value;
                if (this.collapsed)
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
        
        public NodeView(TreeView treeView, BTNode node)
        {
            this.treeView = treeView;
            this.Node = node;
            base.title = node.GetType().Name;
            
            style.backgroundColor = new Color(0f, 0f, 0f, 1f);

            NodeIMGUI nodeIMGUI = ScriptableObject.CreateInstance<NodeIMGUI>();
            nodeIMGUI.Node = this.Node;
            
            UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(nodeIMGUI);
            
            IMGUIContainer imgui = new(() => { editor.OnInspectorGUI(); });
            
            Add(imgui);
            
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
        }
        
        public List<NodeView> GetChildren()
        {
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
            this.children.Remove(nodeView);
            this.Node.Children.Remove(nodeView.Node);
            this.treeView.RemoveNode(nodeView);
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

        private void ChangeCollapse()
        {
            this.Collapsed = !this.Collapsed;
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

            byte[] nodeBytes= Sirenix.Serialization.SerializationUtility.SerializeValue(this.treeView.CopyNode.Node, DataFormat.Binary);
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
            Vector2 pos = windowRoot.ChangeCoordinatesTo(windowRoot.parent, obj.eventInfo.mousePosition + BehaviorTreeEditor.Instance.position.position);
            this.treeView.RightClickMenu.OnSelectEntryHandler = OnSelectEntryHandler;
            SearchWindow.Open(new SearchWindowContext(pos), this.treeView.RightClickMenu);
        }

        private bool OnSelectEntryHandler(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            Type type = searchTreeEntry.userData as Type;
            NodeView nodeView = new(this.treeView, Activator.CreateInstance(type) as BTNode);
            this.AddChild(nodeView);
            return true;
        }

        private void DeleteNode(DropdownMenuAction obj)
        {
            if (this.Parent == null)
            {
                this.treeView.RemoveNode(this);
                return;
            }
            this.Parent.RemoveChild(this);
        }

        private void Layout(DropdownMenuAction obj)
        {
            // 递归计算布局，从根节点开始
            LayoutNode(this, this.GetPosition().position.x, this.GetPosition().position.y, out _);
        }
        
        private const float HorizontalSpacing = 300f; // 水平间距
        private const float VerticalSpacing = 300f;  // 垂直间距
        
        // 递归布局方法
        private float LayoutNode(NodeView node, float currentX, float startY, out float totalHeight)
        {
            float childY = startY; // 当前子节点的Y坐标
            float maxChildWidth = 0; // 子节点中最大的宽度

            // 获取当前节点的子节点
            List<NodeView> children = node.GetChildren();

            // 如果没有子节点，直接设置当前节点的位置并返回
            if (children.Count == 0)
            {
                node.SetPosition(new Rect(currentX, startY, 200, 150)); // 设置节点位置
                totalHeight = 150; // 节点的高度
                return currentX + 200; // 节点的宽度
            }

            // 遍历子节点，递归布局
            foreach (var child in children)
            {
                float childHeight;
                float nextX = LayoutNode(child, currentX + HorizontalSpacing, childY, out childHeight);

                // 更新垂直方向位置
                childY += childHeight + VerticalSpacing;

                // 计算子节点中的最大宽度
                maxChildWidth = Mathf.Max(maxChildWidth, nextX - currentX);
            }

            // 计算子节点总高度
            totalHeight = childY - startY - VerticalSpacing;

            // 将当前节点居中放置在子节点左侧
            float nodeY = startY + totalHeight / 2f;
            node.SetPosition(new Rect(currentX, nodeY, 200, 150));

            return currentX + maxChildWidth;
        }
    }
}