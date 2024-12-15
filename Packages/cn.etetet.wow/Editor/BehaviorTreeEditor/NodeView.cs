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

        private Port @in;

        private Port @out;

        public Edge edge;

        private readonly List<NodeView> children = new();
        
        public NodeView(TreeView treeView, BTNode node)
        {
            this.treeView = treeView;
            this.Node = node;
            base.title = node.GetType().Name;
            
            style.backgroundColor = new Color(0f, 0f, 0f, 1f);

            NodeIMGUI nodeIMGUI = ScriptableObject.CreateInstance<NodeIMGUI>();
            nodeIMGUI.Node = this.Node;
            
            Editor editor = Editor.CreateEditor(nodeIMGUI);
            
            IMGUIContainer imgui = new(() => { editor.OnInspectorGUI(); });
            
            Add(imgui);
            
            this.AddManipulator(new ResizableManipulator());
            
            this.@in = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            inputContainer.Add(this.@in);
            this.@out = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            outputContainer.Add(this.@out);
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
            nodeView.edge = nodeView.Parent.@out.ConnectTo(nodeView.@in);
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
    }
}