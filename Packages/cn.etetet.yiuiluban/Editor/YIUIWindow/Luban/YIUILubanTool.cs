using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using YIUI.Luban.Editor;
using YIUIFramework;
using YIUIFramework.Editor;
using AssemblyHelper = YIUIFramework.AssemblyHelper;

namespace YIUILuban.Editor
{
    public class YIUILubanTool : OdinMenuEditorWindow
    {
        [MenuItem("ET/YIUI Luban 配置工具")]
        public static void OpenWindow()
        {
            m_LubanEditorData = null;
            var window = GetWindow<YIUILubanTool>("YIUI Luban 配置工具");
            if (window != null)
            {
                window.minSize = LubanEditorData.MinWindowSize;
                window.Show();
            }
        }

        //[MenuItem("ET/关闭 YIUI Luban 配置工具")]
        //错误时使用的 面板出现了错误 会导致如何都打不开 就需要先关闭
        public static void CloseWindow()
        {
            GetWindow<YIUILubanTool>()?.Close();
        }

        //关闭后刷新资源
        public static void CloseWindowRefresh()
        {
            CloseWindow();
            AssetDatabase.SaveAssets();
            EditorApplication.ExecuteMenuItem("Assets/Refresh");
        }

        private OdinMenuTree m_OdinMenuTree;

        private readonly List<BaseTreeMenuItem> m_AllMenuItem = new();

        protected override OdinMenuTree BuildMenuTree()
        {
            m_OdinMenuTree                            =  new OdinMenuTree();
            m_OdinMenuTree.Selection.SelectionChanged += OnSelectionChanged;

            m_AllMenuItem.Clear();

            if (LubanEditorData.RootShowAllConfig)
            {
                m_AllMenuItem.Add(new TreeMenuItem<LubanAllDatasModule>(this, m_OdinMenuTree, LubanToolRoot.m_LubanName, EditorIcons.UnityFolderIcon));
            }
            else
            {
                m_AllMenuItem.Add(new TreeMenuItem<LubanToolModule>(this, m_OdinMenuTree, LubanToolRoot.m_LubanName, EditorIcons.UnityFolderIcon));
            }

            var    assembly = AssemblyHelper.GetAssembly("ET.YIUI.LubanTools.Editor");
            Type[] types    = assembly.GetTypes();

            var allAutoMenus = new List<YIUIAutoMenuData>();

            foreach (Type type in types)
            {
                if (type.IsDefined(typeof(YIUIAutoMenuAttribute), false))
                {
                    YIUIAutoMenuAttribute attribute = (YIUIAutoMenuAttribute)Attribute.GetCustomAttribute(type, typeof(YIUIAutoMenuAttribute));
                    allAutoMenus.Add(new YIUIAutoMenuData
                    {
                        Type     = type,
                        MenuName = attribute.MenuName,
                        Order    = attribute.Order
                    });
                }
            }

            allAutoMenus.Sort((a, b) => a.Order.CompareTo(b.Order));

            foreach (var attribute in allAutoMenus)
            {
                m_AllMenuItem.Add(NewTreeMenuItem(attribute.Type, attribute.MenuName));
            }

            m_OdinMenuTree.Add("设置", this, EditorIcons.SettingsCog);

            return m_OdinMenuTree;
        }

        private BaseTreeMenuItem NewTreeMenuItem(Type moduleType, string moduleName)
        {
            var treeMenuItemType = typeof(TreeMenuItem<>);

            var specificTreeMenuItemType = treeMenuItemType.MakeGenericType(moduleType);

            var constructor = specificTreeMenuItemType.GetConstructor(new Type[]
            {
                typeof(OdinMenuEditorWindow),
                typeof(OdinMenuTree),
                typeof(string)
            });

            object treeMenuItem = constructor.Invoke(new object[]
            {
                this,
                m_OdinMenuTree,
                moduleName
            });

            return (BaseTreeMenuItem)treeMenuItem;
        }

        private bool        m_FirstInit           = true;
        private StringPrefs m_LastSelectMenuPrefs = new("YIUILubanTool_LastSelectMenu", null, "全局设置");

        private void OnSelectionChanged(SelectionChangedType obj)
        {
            if (obj != SelectionChangedType.ItemAdded)
            {
                return;
            }

            if (m_FirstInit)
            {
                m_FirstInit = false;

                foreach (var menu in m_OdinMenuTree.MenuItems)
                {
                    if (menu.Name != m_LastSelectMenuPrefs.Value) continue;
                    menu.Select();
                    return;
                }

                return;
            }

            if (m_OdinMenuTree.Selection.SelectedValue is BaseTreeMenuItem menuItem)
            {
                menuItem.SelectionMenu();
            }

            foreach (var menu in m_OdinMenuTree.MenuItems)
            {
                if (!menu.IsSelected) continue;
                m_LastSelectMenuPrefs.Value = menu.Name;
                break;
            }
        }

        public static StringPrefs UserNamePrefs = new("YIUILubanTool_UserName", null, "YIUI");

        [LabelText("用户名")]
        [Required("请填写用户名")]
        [ShowInInspector]
        private static string m_Author;

        public static string Author
        {
            get
            {
                if (string.IsNullOrEmpty(m_Author))
                {
                    m_Author = UserNamePrefs.Value;
                }

                return m_Author;
            }
        }

        [ShowInInspector]
        [HideLabel]
        [HideReferenceObjectPicker]
        private static LubanEditorSerializationData m_LubanEditorData;

        public static LubanEditorSerializationData LubanEditorData
        {
            get
            {
                if (m_LubanEditorData == null)
                {
                    m_LubanEditorData = LubanEditorSerializationData.LoadData();
                }

                return m_LubanEditorData;
            }
        }

        [ButtonGroup("文档")]
        [Button("Luban 官方文档", 50, Icon = SdfIconType.Link45deg, IconAlignment = IconAlignment.LeftOfText)]
        [PropertyOrder(int.MinValue)]
        public void OpenDocumentLuban()
        {
            Application.OpenURL("https://luban.doc.code-philosophy.com/docs/intro");
        }

        [ButtonGroup("文档")]
        [Button("YIUI Luban 文档", 50, Icon = SdfIconType.Link45deg, IconAlignment = IconAlignment.LeftOfText)]
        [PropertyOrder(int.MinValue)]
        public void OpenDocumentYIUILuban()
        {
            Application.OpenURL("https://lib9kmxvq7k.feishu.cn/wiki/W1ylwC9xDip1YQk4eijcxgO9nh0");
        }

        [ButtonGroup("文档")]
        [Button("YIUI Luban 可视化工具文档", 50, Icon = SdfIconType.Link45deg, IconAlignment = IconAlignment.LeftOfText)]
        [PropertyOrder(int.MinValue)]
        public void OpenDocumentYIUILubanTools()
        {
            Application.OpenURL("https://lib9kmxvq7k.feishu.cn/wiki/BS5kw4sPyihRY5ku96Accro9ncI");
        }

        protected override void Initialize()
        {
            LubanTools.CloseWindow        = CloseWindow;
            LubanTools.CloseWindowRefresh = CloseWindowRefresh;
            base.Initialize();
            m_Author = UserNamePrefs.Value;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UserNamePrefs.Value = Author;

            foreach (var menuItem in m_AllMenuItem)
            {
                menuItem.OnDestroy();
            }

            m_LubanEditorData?.SaveData();
        }

        #region LubanTools 基础功能

        [HideInInspector]
        [HideLabel]
        [HideReferenceObjectPicker]
        private LubanTools Tools = new();

        private bool GenPackageExists()
        {
            return Tools.GenPackageExists();
        }

        [Button("创建 LubanGen包", 50)]
        [GUIColor("#00cc00")]
        [HideIf(nameof(GenPackageExists))]
        public void CreateLubanGenTemplate()
        {
            Tools.InitGen();
        }

        [Button("创建模版", 50, Icon = SdfIconType.TerminalSplit, IconAlignment = IconAlignment.LeftOfText)]
        [GUIColor("#0099ff")]
        [ShowIf(nameof(GenPackageExists))]
        public void CreateLubanTemplate()
        {
            var folderPath = EditorUtility.OpenFolderPanel("选择 ET包", "", "");
            if (!string.IsNullOrEmpty(folderPath))
            {
                Tools.CreateToPackage(folderPath);
            }
        }

        protected bool DemoPackageExists()
        {
            return Tools.DemoPackageExists();
        }

        [Button("删除 LubanDemo包", 40)]
        [GUIColor("#ff0000")]
        [ShowIf(nameof(DemoPackageExists))]
        public void DeleteLubanDemoPackage()
        {
            Tools.DeleteLubanDemoPackage();
        }

        [Button("创建 LubanDemo包", 40)]
        [GUIColor("#00cc00")]
        [HideIf(nameof(DemoPackageExists))]
        public void CreateLubanDemoPackage()
        {
            Tools.CreateLubanDemoPackage();
        }

        [Button("替换 ET.Excel To Luban", 40)]
        [ShowIf(nameof(GenPackageExists))]
        public void ReplaceAllExcelToLuban()
        {
            if (LubanTools.ReplaceAll(true))
            {
                CloseWindowRefresh();
            }
        }

        [Button("同步覆盖 LoaderInvoker", 40)]
        [ShowIf(nameof(GenPackageExists))]
        public void SyncLoaderInvoker()
        {
            LubanTools.SyncInvoke();
        }

        #endregion
    }
}