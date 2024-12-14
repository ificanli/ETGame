using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ET
{
    public class BehaviorTreeWindow : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        private TreeView treeView;

        [MenuItem("ET/BehaviorTreeWindow _F8")]
        public static void ShowWindow()
        {
            BehaviorTreeWindow wnd = GetWindow<BehaviorTreeWindow>();
            wnd.titleContent = new GUIContent("BehaviorTreeWindow");
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;
            this.treeView = root.Q<TreeView>();
        }
    }
}