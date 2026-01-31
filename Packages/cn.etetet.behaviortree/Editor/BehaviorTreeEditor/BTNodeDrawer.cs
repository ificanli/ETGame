using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace ET
{
    // 自定义 Drawer：为每个成员的顶部添加按钮
    public class BTNodeDrawer : OdinValueDrawer<BTRoot>
    {
        public static BTRoot OpenNode;

        public static UnityEngine.Object ScriptableObject;

        internal static bool IsDrawingInBehaviorTreeEditor;
        

        protected override void DrawPropertyLayout(GUIContent label)
        {
            // 获取当前对象的值
            BTRoot node = (BTRoot)this.ValueEntry.WeakSmartValue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 仅在Inspector中显示，行为树编辑器内隐藏
            if (!IsDrawingInBehaviorTreeEditor && GUILayout.Button($"Open BehaviorTreeEditor", GUILayout.Height(25)))
            {
                // 打开行为树编辑器
                OpenNode = node;
                ScriptableObject = this.Property.Tree.WeakTargets[0] as UnityEngine.Object;

                EditorApplication.ExecuteMenuItem("ET/BehaviorTree/BehaviorTreeEditor");
            }

            // 分割线
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // 保留 Odin 默认的内容显示
            this.CallNextDrawer(label);

            EditorGUILayout.EndVertical();
        }
    }
}
