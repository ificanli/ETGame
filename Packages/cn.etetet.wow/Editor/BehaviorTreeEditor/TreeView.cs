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

        public NodeView CopyNode;
        public bool IsCut;

        public long idGenerater;
        
        public long GeneraterId()
        {
            return ++idGenerater;
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

        private bool OnSelectEntryHandler(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            Type type = searchTreeEntry.userData as Type;
            NodeView nodeView = new(this, Activator.CreateInstance(type) as BTNode);
            this.AddElement(nodeView);
            
            this.root = nodeView;
            
            return true;
        }

        //点击右键菜单时触发
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (this.root == null)
            {
                evt.menu.AppendAction("Create", this.CreateNode);
            }
        }

        private void CreateNode(DropdownMenuAction obj)
        {
            VisualElement windowRoot = BehaviorTreeEditor.Instance.rootVisualElement;
            Vector2 pos = windowRoot.ChangeCoordinatesTo(windowRoot.parent, obj.eventInfo.mousePosition + BehaviorTreeEditor.Instance.position.position);
            this.RightClickMenu.OnSelectEntryHandler = OnSelectEntryHandler;
            SearchWindow.Open(new SearchWindowContext(pos), this.RightClickMenu);
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


