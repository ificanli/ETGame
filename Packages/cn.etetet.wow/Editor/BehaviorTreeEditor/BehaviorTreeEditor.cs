using System;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ET
{
    public class BehaviorTreeEditor : EditorWindow
    {
        public TreeView TreeView;

        [MenuItem("ET/BehaviorTreeEditor _F8")]
        public static void ShowWindow()
        {
            BehaviorTreeEditor wnd = GetWindow<BehaviorTreeEditor>();
            wnd.titleContent = new GUIContent("BehaviorTreeEditor");
            
            wnd.TreeView.InitTree(wnd, BTRoot.OpenNode);
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/cn.etetet.wow/Editor/BehaviorTreeEditor/BehaviorTreeEditor.uxml");
            visualTree.CloneTree(root);
            
            this.TreeView = root.Q<TreeView>();
        }

        public void OnDestroy()
        {
            BTRoot.OpenNode = null;
        }
    }
}