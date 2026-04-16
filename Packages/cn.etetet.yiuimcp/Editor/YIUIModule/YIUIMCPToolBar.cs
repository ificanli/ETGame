#if YIUI
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace YIUIFramework.Editor.MCP
{
    [InitializeOnLoad]
    public static class YIUIMCPToolBar
    {
        const string ModuleMenuPath = "YIUIMCP";
        const string LastSelectMenuKey = "YIUIAutoTool_LastSelectMenu";
        const string LastSelectModulePathKey = "YIUIAutoTool_LastSelectToolModulePath";

        private static Texture _cachedIcon1;
        private static Texture _cachedIcon2;

        static YIUIMCPToolBar()
        {
            TryRegisterToolbar();
        }

        [MenuItem("ET/YIUIMCP")]
        private static void OpenFromMenu()
        {
            OpenYIUIMCPModule();
        }

        private static void TryRegisterToolbar()
        {
            var extenderType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetType("YIUIFramework.Editor.YIUIToolbarExtender", false))
                    .FirstOrDefault(type => type != null);

            if (extenderType == null)
            {
                return;
            }

            var addMethod = extenderType.GetMethod("AddRightToolbarGUI",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Action), typeof(int) },
                null);

            addMethod?.Invoke(null, new object[] { (Action)OnYIUIMCPToolbarGUI, 1100 });
        }

        private static void OnYIUIMCPToolbarGUI()
        {
            Texture icon = null;
            if (YIUIMCPServerHelper.IsRunning)
            {
                _cachedIcon1 ??= AssetDatabase.LoadAssetAtPath<Texture>("Packages/cn.etetet.yiuimcp/Editor/YIUIModule/Icon/YIUIMCPIcon1.png");
                icon = _cachedIcon1;
            }
            else
            {
                _cachedIcon2 ??= AssetDatabase.LoadAssetAtPath<Texture>("Packages/cn.etetet.yiuimcp/Editor/YIUIModule/Icon/YIUIMCPIcon2.png");
                icon = _cachedIcon2;
            }

            GUILayout.Space(5);
            GUIContent iconContent = new(string.Empty, icon)
            {
                tooltip = "YIUIMCP"
            };

            if (GUILayout.Button(iconContent))
            {
                OpenYIUIMCPModule();
            }

            GUILayout.Space(5);
        }

        private static void OpenYIUIMCPModule()
        {
            EditorPrefs.SetString(LastSelectMenuKey, ModuleMenuPath);
            EditorPrefs.SetString(LastSelectModulePathKey, ModuleMenuPath);
            YIUIAutoTool.OpenWindow();
            EditorApplication.delayCall += SelectMcpModule;
        }

        private static void SelectMcpModule()
        {
            var window = EditorWindow.GetWindow<YIUIAutoTool>();
            if (window == null)
            {
                return;
            }

            var selectMethod = typeof(YIUIAutoTool).GetMethod("SelectChainByPath", BindingFlags.Instance | BindingFlags.NonPublic);
            selectMethod?.Invoke(window, new object[] { ModuleMenuPath });
            window.Repaint();
        }
    }
}
#endif
