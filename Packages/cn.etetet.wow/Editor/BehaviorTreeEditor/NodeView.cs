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

        public NodeView Parent { get; set; }

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
        }
        
        public List<NodeView> GetChildren()
        {
            return this.children;
        }
        
        public void AddNode(NodeView nodeView)
        {
            nodeView.Parent = this;
            this.treeView.AddElement(nodeView);
            this.children.Add(nodeView);
        }
        
        public void RemoveNode(NodeView nodeView)
        {
            this.children.Remove(nodeView);
            this.treeView.RemoveNode(nodeView);
        }
        
        //点击右键菜单时触发
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Create", this.CreateNode);
            evt.menu.AppendAction("Delete", this.DeleteNode);
        }

        private void CreateNode(DropdownMenuAction obj)
        {
            VisualElement windowRoot = BehaviorTreeEditor.Instance.rootVisualElement;
            Vector2 pos = windowRoot.ChangeCoordinatesTo(windowRoot.parent, obj.eventInfo.mousePosition + BehaviorTreeEditor.Instance.position.position);
            this.treeView.RightClickMenu.OnSelectEntryHandler = OnSelectEntryHandler;
            SearchWindow.Open(new SearchWindowContext(pos), this.treeView.RightClickMenu);
        }

        private bool OnSelectEntryHandler(SearchTreeEntry searchTreeEntry, SearchWindowContext arg2)
        {
            Type type = searchTreeEntry.userData as Type;
            NodeView nodeView = new(this.treeView, Activator.CreateInstance(type) as BTNode);
            this.AddNode(nodeView);
            return true;
        }

        private void DeleteNode(DropdownMenuAction obj)
        {
            if (this.Parent == null)
            {
                this.treeView.RemoveNode(this);
                return;
            }
            this.Parent.RemoveNode(this);
        }
    }
}