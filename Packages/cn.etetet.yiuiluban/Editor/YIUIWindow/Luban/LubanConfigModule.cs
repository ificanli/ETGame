using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEngine;
using YIUIFramework.Editor;

namespace YIUILuban.Editor
{
    public class LubanConfigModuleData
    {
        public LubanConfigModule LubanConfigModule;
        public string ModulePath;
        public string PackagesName;
        public string ConfigPath;
        public string FolderName;
    }

    public class LubanConfigModule : BaseYIUIToolModule
    {
        [HideInInspector]
        public LubanConfigModuleData Data;

        [HideInInspector]
        public LubanPackageEditorData PackageData;

        [LabelText("模块名")]
        [ReadOnly]
        public string PackagesName;

        [FolderPath]
        [LabelText("配置路径")]
        [ReadOnly]
        public string ConfigPath;

        [LabelText("分类")]
        [ReadOnly]
        [ShowIf(nameof(ShowIfFolderName))]
        public string FolderName;

        private bool ShowIfFolderName()
        {
            return !string.IsNullOrEmpty(FolderName);
        }

        [LabelText("别名")]
        [ShowInInspector]
        public string PackageAlias
        {
            get => PackageData.Alias;
            set
            {
                PackageData.Alias = value;
            }
        }

        [LabelText("描述")]
        [ShowInInspector]
        public string PackageDesc
        {
            get => PackageData.Desc;
            set
            {
                PackageData.Desc = value;
            }
        }

        #region 初始化

        public LubanConfigModule()
        {
        }

        public LubanConfigModule(string configPath, string pkgName, string folderName)
        {
            SetData(configPath, pkgName, folderName);
        }

        private void SetData(string configPath, string pkgName, string folderName)
        {
            PackagesName = pkgName;
            ConfigPath = configPath;
            FolderName = folderName;
            PackageData = YIUILubanTool.LubanEditorData.GetLubanEditorPackageSettings(pkgName);
        }

        public override void Initialize()
        {
            if (this.UserData is LubanConfigModuleData data)
            {
                Data = data;
                Data.LubanConfigModule = this;
                SetData(data.ConfigPath, data.PackagesName, data.FolderName);
                AddAllAssetsAtPath();
            }
            else
            {
                Debug.LogError($"数据错误");
            }
        }

        internal const string m_LubanBase = "Base";
        internal const string m_LubanDefines = "Defines";
        internal const string m_LubanDatas = "Datas";

        private void AddAllAssetsAtPath()
        {
            var modulePath = Data.ModulePath;

            var baseModule = new TreeMenuItem<LubanBaseModule>(this.AutoTool, this.Tree, $"{modulePath}/{m_LubanBase}", EditorIcons.Folder);
            baseModule.UserData = Data;

            var definesModule = new TreeMenuItem<LubanDefinesModule>(this.AutoTool, this.Tree, $"{modulePath}/{m_LubanDefines}", EditorIcons.Folder);
            definesModule.UserData = Data;

            var datasModule = new TreeMenuItem<LubanDatasModule>(this.AutoTool, this.Tree, $"{modulePath}/{m_LubanDatas}", EditorIcons.Folder);
            datasModule.UserData = Data;
        }

        #endregion
    }
}