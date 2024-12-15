using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ET
{
    public class BehaviorTreeEditor : EditorWindow
    {
        public static BehaviorTreeEditor Instance;
        
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        private TreeView treeView;

        [MenuItem("ET/BehaviorTreeEditor _F8")]
        public static void ShowWindow()
        {
            BehaviorTreeEditor wnd = GetWindow<BehaviorTreeEditor>();
            wnd.titleContent = new GUIContent("BehaviorTreeEditor");
        }

        public void CreateGUI()
        {
            Instance = this;
            
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/cn.etetet.wow/Editor/BehaviorTreeEditor/BehaviorTreeEditor.uxml");
            visualTree.CloneTree(root);
            
            this.treeView = root.Q<TreeView>();
        }
    }
}