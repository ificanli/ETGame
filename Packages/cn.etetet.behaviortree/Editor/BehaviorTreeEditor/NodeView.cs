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

        private bool isDisposed;

        /// <summary>
        /// 获取节点是否已被销毁
        /// </summary>
        public bool IsDisposed => this.isDisposed;

        private readonly Port inPort;

        private readonly Port outPort;

        private Edge edge;

        private bool isResizing;
        private Vector2 resizeStartMousePosition;
        private float resizeStartWidth;
        private float currentResizeWidth;
        private float lastLayoutHeight;
        private bool layoutQueued;

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

        private NodeIMGUI nodeIMGUI;
        private Editor editor;

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
                    this.imgui.style.display = DisplayStyle.Flex;
                }
                else
                {
                    this.imgui.style.display = DisplayStyle.None;
                }

                RefreshExpandedState();
                RequestLayout();
            }
        }

        public bool DescCollapsed
        {
            get
            {
                return this.Node.DescCollapsed;
            }
            set
            {
                this.Node.DescCollapsed = value;
                UpdateTooltip();
                RequestLayout();
            }
        }

        private void UpdateTooltip()
        {
            if (this.DescCollapsed)
            {
                this.tooltip = "";
            }
            else
            {
                this.tooltip = this.Node.Desc;
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

            this.TryApplyTitleColor(node.GetType());

            // 确保旧节点也默认隐藏Desc（旧节点DescCollapsed为false）
            if (!this.Node.DescCollapsed)
            {
                this.Node.DescCollapsed = true;
            }

            bool hasChildren = this.Node.Children is { Count: > 0 };

            Label idLabel = new(node.Id.ToString());
            idLabel.style.width = 32;
            idLabel.style.height = 20;
            idLabel.style.marginTop = 5;
            idLabel.style.marginRight = new StyleLength(StyleKeyword.Auto);
            this.titleContainer.Add(idLabel);

            EnsureEditorLayoutDefaults();

            style.backgroundColor = new Color(0f, 0f, 0f, 1f);
            ApplyNodeWidth();
            this.titleContainer.style.flexGrow = 1;
            AddResizeHandle();
            Label titleLabel = this.titleContainer.Q<Label>("title-label");
            if (titleLabel != null)
            {
                titleLabel.style.minWidth = 0;
                titleLabel.style.flexGrow = 1;
                titleLabel.style.flexShrink = 1;
                titleLabel.style.whiteSpace = WhiteSpace.NoWrap;
                titleLabel.style.textOverflow = TextOverflow.Ellipsis;
                titleLabel.style.overflow = Overflow.Hidden;
                titleLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            }

            this.nodeIMGUI = ScriptableObject.CreateInstance<NodeIMGUI>();
            this.nodeIMGUI.Node = this.Node;

            // 根据DescCollapsed状态设置tooltip
            UpdateTooltip();

            this.editor = Editor.CreateEditor(this.nodeIMGUI);

            this.imgui = new(() =>
            {
                if (this.isDisposed)
                {
                    return;
                }
                byte[] nodeBytes = null;
                Event evt = Event.current;
                if (evt != null && (evt.type == EventType.KeyDown || evt.type == EventType.MouseDown || evt.type == EventType.DragPerform))
                {
                    nodeBytes = this.treeView.BackupRoot();
                }

                float widthBefore = GetNodeWidth();
                float spacingBefore = GetHorizontalSpacing();

                BTNodeDrawer.IsDrawingInBehaviorTreeEditor = true;
                try
                {
                    this.editor.OnInspectorGUI();
                }
                finally
                {
                    BTNodeDrawer.IsDrawingInBehaviorTreeEditor = false;
                }

                if (GUI.changed)
                {
                    UpdateTooltip();
                    if (nodeBytes != null)
                    {
                        this.treeView.SaveToUndo(nodeBytes);
                    }

                    bool widthChanged = !Mathf.Approximately(widthBefore, GetNodeWidth());
                    bool spacingChanged = !Mathf.Approximately(spacingBefore, GetHorizontalSpacing());

                    if (widthChanged)
                    {
                        ApplyNodeWidth();
                    }

                    if (widthChanged || spacingChanged)
                    {
                        this.treeView.Layout();
                    }
                }
            });
            Add(this.imgui);
            
            this.inPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            inputContainer.Add(this.inPort);

            this.outPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            outputContainer.Add(this.outPort);
            
            // 有孩子，显示折叠按钮
            if (hasChildren)
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
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void TryApplyTitleColor(Type nodeType)
        {
            BTNodeTitleColorAttribute titleColor = nodeType.GetCustomAttribute<BTNodeTitleColorAttribute>(inherit: true);
            if (titleColor == null)
            {
                return;
            }

            this.titleContainer.style.backgroundColor = new StyleColor(new Color(titleColor.R, titleColor.G, titleColor.B, titleColor.A));
        }
        
        public void Dispose()
        {
            // 防止重复Dispose
            if (this.isDisposed)
            {
                return;
            }
            this.isDisposed = true;

            long id = this.Id;

            // 注销事件回调，防止内存泄漏
            UnregisterCallback<MouseDownEvent>(OnMouseDown);
            UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            UnregisterCallback<MouseUpEvent>(OnMouseUp);
            UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            // 清理IMGUIContainer
            if (this.imgui != null)
            {
                this.imgui.RemoveFromHierarchy();
            }

            // 销毁Editor和NodeIMGUI，防止内存泄漏
            if (this.editor != null)
            {
                UnityEngine.Object.DestroyImmediate(this.editor);
                this.editor = null;
            }

            if (this.nodeIMGUI != null)
            {
                UnityEngine.Object.DestroyImmediate(this.nodeIMGUI);
                this.nodeIMGUI = null;
            }

            this.treeView.RemoveElement(this);
            if (this.edge != null)
            {
                this.treeView.RemoveElement(this.edge);
            }

            if (this.Parent != null && !this.Parent.IsDisposed)
            {
                this.Parent.GetChildren().Remove(this);

                if (!this.treeView.IsDisposed)  // 编辑器关闭的时候，不要删除Node，只有手动delete NodeView才会删除
                {
                    this.Parent.Node.Children.Remove(this.Node);
                }
            }

            this.treeView.RemoveNode(id);

            // 使用ToList()创建副本，防止在遍历时修改集合
            foreach (NodeView nodeView in this.GetChildren().ToList())
            {
                nodeView.Dispose();
            }
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (TryStartResize(evt))
            {
                return;
            }

            this.treeView.MouseDownNode = this;
            this.treeView.MoveStartPos = this.Position;
            this.treeView.SetRed(this);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (this.isDisposed || this.treeView.IsDisposed)
            {
                return;
            }

            float newHeight = evt.newRect.height;
            if (newHeight <= 0f)
            {
                return;
            }

            if (Mathf.Approximately(this.lastLayoutHeight, newHeight))
            {
                return;
            }

            this.lastLayoutHeight = newHeight;
            RequestLayout();
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (!this.isResizing)
            {
                return;
            }

            Vector2 delta = evt.mousePosition - this.resizeStartMousePosition;
            float newWidth = Mathf.Max(MinNodeWidth, this.resizeStartWidth + delta.x);
            this.currentResizeWidth = newWidth;

            style.width = newWidth;

            if (this.Node != null && !Mathf.Approximately(this.Node.EditorNodeWidth, newWidth))
            {
                this.Node.EditorNodeWidth = newWidth;
            }

            evt.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (!this.isResizing)
            {
                return;
            }

            this.isResizing = false;
            this.ReleaseMouse();

            if (this.Node != null)
            {
                this.Node.EditorNodeWidth = Mathf.Max(MinNodeWidth, this.currentResizeWidth);
            }

            ApplyNodeWidth();

            if (!this.treeView.IsDisposed)
            {
                this.treeView.Layout();
            }

            evt.StopPropagation();
        }

        private bool TryStartResize(MouseDownEvent evt)
        {
            if (evt.button != 0)
            {
                return false;
            }

            if (!IsInResizeZone(evt.localMousePosition, this.layout))
            {
                return false;
            }

            this.isResizing = true;
            this.resizeStartMousePosition = evt.mousePosition;
            this.resizeStartWidth = this.layout.width;
            this.currentResizeWidth = this.resizeStartWidth;

            if (!this.treeView.IsDisposed)
            {
                this.treeView.SaveToUndo();
            }

            this.CaptureMouse();
            evt.StopPropagation();
            return true;
        }

        private void RequestLayout()
        {
            if (this.layoutQueued || this.treeView.IsDisposed)
            {
                return;
            }

            this.layoutQueued = true;
            EditorApplication.delayCall += () =>
            {
                this.layoutQueued = false;
                if (this.isDisposed || this.treeView.IsDisposed)
                {
                    return;
                }

                this.treeView.Layout();
            };
        }

        private static bool IsInResizeZone(Vector2 mousePosition, Rect elementLayout)
        {
            return mousePosition.x >= elementLayout.width - ResizableManipulator.DefaultResizeZone &&
                   mousePosition.y >= elementLayout.height - ResizableManipulator.DefaultResizeZone;
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
                    if (child == null)
                    {
                        continue;
                    }

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
            evt.menu.AppendAction("Create", (o)=>this.CreateNode(o).Coroutine());
            evt.menu.AppendAction("Delete", this.DeleteNode);
            evt.menu.AppendAction("Copy", this.CopyNode);
            evt.menu.AppendAction("Cut", this.CutNode);
            if (BTClipboard.Instance.HasData())
            {
                evt.menu.AppendAction("Paste", this.PasterNode);
            }

            if (this.Parent != null)
            {
                evt.menu.AppendAction("Change", (o)=>this.Change(o).Coroutine());
            }

            // 添加折叠/展开节点内容菜单项
            string contentToggleText = this.ContentCollapsed ? "显示节点内容" : "隐藏节点内容";
            evt.menu.AppendAction(contentToggleText, (o) => this.ChangeContentCollapse());

            // 添加折叠/展开节点说明菜单项
            string descToggleText = this.DescCollapsed ? "显示节点说明" : "隐藏节点说明";
            evt.menu.AppendAction(descToggleText, (o) => this.ChangeDescCollapse());

            // 添加显示/隐藏编辑器布局菜单项
            string layoutToggleText = this.Node.EditorLayoutVisible ? "隐藏编辑器布局" : "显示编辑器布局";
            evt.menu.AppendAction(layoutToggleText, (o) => this.ChangeEditorLayoutVisible());
        }

        private async ETTask Change(DropdownMenuAction obj)
        {
            if (this.Parent == null)
            {
                return;
            }
            
            Vector2 pos = GUIUtility.GUIToScreenPoint(obj.eventInfo.localMousePosition);
            (SearchTreeEntry searchTreeEntry, SearchWindowContext context) = await this.treeView.RightClickMenu.WaitSelect(pos);
            
            this.treeView.SaveToUndo();
            
            Type type = searchTreeEntry.userData as Type;
            BTNode btNode = Activator.CreateInstance(type) as BTNode;

            if (this.GetChildren().Count > 0)
            {
                this.treeView.BehaviorTreeEditor?.ShowText("node has child!");
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

        private void ChangeDescCollapse()
        {
            this.DescCollapsed = !this.DescCollapsed;
        }

        private void ChangeEditorLayoutVisible()
        {
            if (this.Node == null)
            {
                return;
            }

            this.Node.EditorLayoutVisible = !this.Node.EditorLayoutVisible;
            this.editor?.Repaint();
            RequestLayout();
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

            BTClipboard.Instance.Cut(this.treeView, this, this.Node);
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
            BTNode clone = BTClipboard.Instance.Paste();
            if (clone == null)
            {
                return;
            }

            this.treeView.SaveToUndo();

            ClearId(clone);
            this.AddChild(new NodeView(this.treeView, clone));

            // 完成粘贴操作，如果是剪切则删除源节点
            BTClipboard.Instance.FinishPaste();

            this.treeView.Layout();
        }

        private void CopyNode(DropdownMenuAction obj)
        {
            BTClipboard.Instance.Copy(this.Node);
        }

        private async ETTask CreateNode(DropdownMenuAction obj)
        {
            Vector2 pos = GUIUtility.GUIToScreenPoint(obj.eventInfo.localMousePosition);
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

        private const float MinNodeWidth = BTNode.EditorNodeWidthMin; // 节点最小宽度
        private const float DefaultNodeWidth = BTNode.EditorNodeWidthDefault; // 节点默认宽度
        private const float DefaultHorizontalSpacing = BTNode.EditorHorizontalSpacingDefault; // 节点之间的水平间距
        private const float VerticalSpacing = 50f; // 节点之间的垂直间距
        private const float ResizeHandleSize = ResizableManipulator.DefaultResizeZone; // 拖拽把手尺寸

        private void EnsureEditorLayoutDefaults()
        {
            if (this.Node == null)
            {
                return;
            }

            if (this.Node.EditorNodeWidth < MinNodeWidth)
            {
                this.Node.EditorNodeWidth = MinNodeWidth;
            }

            if (this.Node.EditorHorizontalSpacing <= 0f)
            {
                this.Node.EditorHorizontalSpacing = DefaultHorizontalSpacing;
            }
        }

        private float GetNodeWidth()
        {
            EnsureEditorLayoutDefaults();
            return this.Node != null ? this.Node.EditorNodeWidth : DefaultNodeWidth;
        }

        private float GetHorizontalSpacing()
        {
            EnsureEditorLayoutDefaults();
            return this.Node != null ? this.Node.EditorHorizontalSpacing : DefaultHorizontalSpacing;
        }

        private float GetHorizontalStep()
        {
            float nodeWidth = GetNodeWidth();
            return nodeWidth + GetHorizontalSpacing();
        }

        private void ApplyNodeWidth()
        {
            style.width = GetNodeWidth();
        }

        private float GetNodeHeight()
        {
            float height = this.layout.height;
            if (height <= 0f)
            {
                height = this.GetPosition().height;
            }

            return height > 0f ? height : VerticalSpacing;
        }

        private void AddResizeHandle()
        {
            IMGUIContainer handle = new(() =>
            {
                EditorGUIUtility.AddCursorRect(new Rect(0f, 0f, ResizeHandleSize, ResizeHandleSize), MouseCursor.ResizeHorizontal);
            });
            handle.name = "resize-handle";
            handle.AddToClassList("bt-node-resize-handle");
            handle.style.width = ResizeHandleSize;
            handle.style.height = ResizeHandleSize;
            handle.style.position = UnityEngine.UIElements.Position.Absolute;
            handle.style.right = 2;
            handle.style.bottom = 2;
            handle.pickingMode = PickingMode.Position;
            this.hierarchy.Add(handle);
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
            float horizontalStep = node.GetHorizontalStep();
            float nodeHeight = node.GetNodeHeight();

            node.ApplyNodeWidth();

            // 获取当前节点的子节点
            List<NodeView> children = node.GetNotCollapsedChildren();

            // 如果没有子节点，直接设置当前节点的位置并返回
            if (children.Count == 0)
            {
                node.Position = new Vector2(currentX, startY); // 设置节点位置
                totalHeight = nodeHeight; // 节点的高度
                nextX = currentX + horizontalStep; // 节点的宽度
                return new Vector2(currentX, startY);
            }

            if (children.Count == 1)
            {
                NodeView child = children[0];
                LayoutNode(child, currentX + horizontalStep, startY, out float childHeight, out nextX);

                maxChildWidth = Mathf.Max(maxChildWidth, nextX - currentX);
                totalHeight = Mathf.Max(childHeight, nodeHeight);

                float nodeYSingle = startY + (totalHeight - nodeHeight) / 2f;
                node.Position = new Vector2(currentX, nodeYSingle);

                float deltaY = nodeYSingle - child.Position.y;
                if (!Mathf.Approximately(deltaY, 0f))
                {
                    child.AjustPosition(new Vector2(0f, -deltaY));
                }

                nextX = currentX + maxChildWidth;
                return new Vector2(currentX, nodeYSingle);
            }

            // 遍历子节点，递归布局
            foreach (NodeView child in children)
            {
                float childHeight;
                LayoutNode(child, currentX + horizontalStep, childY, out childHeight, out nextX);

                // 更新垂直方向位置
                childY += childHeight + VerticalSpacing;

                // 计算子节点中的最大宽度
                maxChildWidth = Mathf.Max(maxChildWidth, nextX - currentX);
            }

            // 计算子节点总高度，并保证不小于当前节点高度，避免兄弟节点重叠
            totalHeight = childY - startY - VerticalSpacing;
            totalHeight = Mathf.Max(totalHeight, nodeHeight);

            // 将当前节点居中放置在子节点左侧
            float nodeY = startY + (totalHeight - nodeHeight) / 2f;
            node.Position = new Vector2(currentX, nodeY);

            nextX = currentX + maxChildWidth;

            return new Vector2(currentX, nodeY);
        }

        #endregion
        
    }
}
