using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ET
{
    [UxmlElement]
    public partial class TreeView: GraphView
    {
        public readonly RightClickMenu RightClickMenu = ScriptableObject.CreateInstance<RightClickMenu>();

        private NodeView root;

        private Dictionary<long, NodeView> nodes = new();

        public NodeView CopyNode;
        public bool IsCut;
        private int maxId;
        public int GenerateId()
        {
            return ++this.maxId;
        }
        
        public TreeView()
        {
            Insert(0, new GridBackground());
            
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/cn.etetet.wow/Editor/BehaviorTreeEditor/BehaviorTreeEditor.uss");
            styleSheets.Add(styleSheet);
        }

        public void InitTree(BTNode node)
        {
            GetMaxId(node);
            
            this.root = new NodeView(this, node);
            foreach (BTNode child in node.Children)
            {
                this.root.AddChild(child);
            }
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
            this.nodes.Add(node.Id, node);
        }

        public NodeView GetNode(long id)
        {
            this.nodes.TryGetValue(id, out NodeView node);
            return node;
        }
        
        public NodeView RemoveNode(long id)
        {
            this.nodes.Remove(id, out NodeView node);
            return node;
        }

        //点击右键菜单时触发
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (this.root == null)
            {
                evt.menu.AppendAction("Create", (o)=>
                {
                    this.CreateNode(o).NoContext();
                });
            }
        }

        private async ETTask CreateNode(DropdownMenuAction obj)
        {
            VisualElement windowRoot = BehaviorTreeEditor.Instance.rootVisualElement;
            Vector2 pos = windowRoot.ChangeCoordinatesTo(windowRoot.parent, obj.eventInfo.mousePosition + BehaviorTreeEditor.Instance.position.position);
            (SearchTreeEntry searchTreeEntry, SearchWindowContext context) = await this.RightClickMenu.WaitSelect(pos);
            
            Type type = searchTreeEntry.userData as Type;
            BTNode btNode = Activator.CreateInstance(type) as BTNode;
            this.InitTree(btNode);
        }

        public void Layout()
        {
            if (this.root == null)
            {
                return;
            }
            
            this.root.Layout();
        }
    }
}


