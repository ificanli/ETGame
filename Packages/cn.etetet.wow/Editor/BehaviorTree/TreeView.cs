using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ET
{
    [UxmlElement]
    public partial class TreeView: GraphView
    {
        private RightClickMenu menuWindowProvider;
        
        public TreeView()
        {
            Insert(0, new GridBackground());
            
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/cn.etetet.wow/Editor/BehaviorTree/BehaviorTreeWindow.uss");
            styleSheets.Add(styleSheet);

            RightClickMenu();
        }
        
        
        //点击右键菜单时触发
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Create Node", this.CreateNode);
            
            base.BuildContextualMenu(evt);
        }

        private void CreateNode(DropdownMenuAction obj)
        {
            Debug.Log("1111111111111111111111111111111111");
        }
        

        private void RightClickMenu()
        {
            menuWindowProvider = ScriptableObject.CreateInstance<RightClickMenu>();
            //menuWindowProvider.OnSelectEntryHandler = OnMenuSelectEntry;
        
            nodeCreationRequest += context =>
            {
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), menuWindowProvider);
            };
        }
        
        //点击菜单时菜单创建Node
        //private bool OnMenuSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        //{
        //    var windowRoot = BehaviourTreeView.TreeWindow.rootVisualElement;
        //    
        //    var windowMousePosition = windowRoot.ChangeCoordinatesTo(windowRoot.parent, 
        //        context.screenMousePosition - BehaviourTreeView.TreeWindow.position.position);
        //    
        //    var graphMousePosition = contentViewContainer.WorldToLocal(windowMousePosition);
        //    var nodeBase = System.Activator.CreateInstance((System.Type)searchTreeEntry.userData) as BtNodeBase;
        //    var nodeLabel = nodeBase.GetType().GetCustomAttribute(typeof(NodeLabelAttribute)) as NodeLabelAttribute;
        //    nodeBase.NodeName = nodeBase.GetType().Name;
        //    if (nodeLabel!=null)
        //    {
        //        if (nodeLabel.Label != "")
        //        {
        //            nodeBase.NodeName = nodeLabel.Label;
        //        }
        //    }
        //    nodeBase.NodeType = nodeBase.GetType().GetNodeType();
        //    nodeBase.Position = graphMousePosition;
        //    nodeBase.Guid = System.Guid.NewGuid().ToString();
        //    NodeView group =  new NodeView(nodeBase);
        //    group.SetPosition(new Rect(graphMousePosition, Vector2.one));
        //    this.AddElement(group);
        //    BehaviourTreeView.TreeData.NodeData.Add(nodeBase);
        //    AddToSelection(group);
        //    return true;
        //}

    }
}


