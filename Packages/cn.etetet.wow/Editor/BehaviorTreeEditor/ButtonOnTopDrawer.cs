using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace ET
{
    // 自定义 Drawer：为每个成员的顶部添加按钮
    public class BTNodeDrawer : OdinValueDrawer<BTRoot>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            // 获取当前对象的值
            BTRoot node = (BTRoot)this.ValueEntry.WeakSmartValue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 绘制顶部按钮
            if (GUILayout.Button($"Open BehaviorTreeEditor", GUILayout.Height(25)))
            {
                // 打开行为树编辑器
                BTRoot.OpenNode = node;
                EditorApplication.ExecuteMenuItem("ET/BehaviorTreeEditor");
            }

            // 分割线
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // 保留 Odin 默认的内容显示
            this.CallNextDrawer(label);

            EditorGUILayout.EndVertical();
        }
    }
}