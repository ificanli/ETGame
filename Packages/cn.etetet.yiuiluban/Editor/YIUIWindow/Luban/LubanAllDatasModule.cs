using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace YIUILuban.Editor
{
    public class LubanAllConfigModuleData
    {
        public Dictionary<string, List<string>> AllExcelData;
    }

    public class LubanAllDatasModule : LubanConfigEditorModule
    {
        [ShowInInspector]
        [HideLabel]
        [HideReferenceObjectPicker]
        [ShowIf(nameof(ShowIfRoot))]
        public LubanToolRoot ToolRoot;

        private bool ShowIfRoot()
        {
            return YIUILubanTool.LubanEditorData.RootShowAllConfig;
        }

        protected override ELubanEditorShowType EditorShowType => ELubanEditorShowType.All;

        [HideLabel]
        [ReadOnly]
        [HorizontalGroup("Name")]
        [PropertyOrder(int.MaxValue)]
        [ShowIf(nameof(ShowIfInfo))]
        public string PackageName;

        [HorizontalGroup("Name", Width = 20)]
        [Button("", 20, Icon = SdfIconType.FolderFill, IconAlignment = IconAlignment.LeftOfText)]
        [PropertyOrder(int.MaxValue)]
        [ShowIf(nameof(ShowIfInfo))]
        private void OpenFile()
        {
            if (string.IsNullOrEmpty(PackagePath))
            {
                return;
            }

            Application.OpenURL(PackagePath);
        }

        [HideInInspector]
        public string PackagePath;

        //[LabelText("所在模块别名")]
        [HideLabel]
        [ReadOnly]
        [PropertyOrder(int.MaxValue)]
        [ShowIf(nameof(ShowIfInfo))]
        public string PackageAlias;

        //[LabelText("所在模块描述")]
        [HideLabel]
        [ReadOnly]
        [PropertyOrder(int.MaxValue)]
        [ShowIf(nameof(ShowIfInfo))]
        public string PackageDesc;

        private bool ShowIfInfo()
        {
            return !string.IsNullOrEmpty(PackageName);
        }

        protected LubanAllConfigModuleData Data;

        public override void Initialize()
        {
            base.Initialize();

            if (this.ShowIfRoot())
            {
                ToolRoot = new();
                ToolRoot.RefreshAllLuban(this.AutoTool, this.Tree);
                Data = new();
                this.Data.AllExcelData = this.ToolRoot.m_AllExcelData;
                GetAllData();
            }
            else
            {
                if (this.UserData is LubanAllConfigModuleData data)
                {
                    Data = data;
                    GetAllData();
                }
                else
                {
                    Debug.LogError($"数据错误");
                }
            }
        }

        protected void GetAllData()
        {
            m_AllData.Clear();

            foreach (var item in Data.AllExcelData)
            {
                var pathList = item.Value;
                for (int index = 0; index < pathList.Count; index++)
                {
                    var path = pathList[index];
                    if (!Directory.Exists(path))
                    {
                        continue;
                    }

                    ProcessDirectory(path, index);
                }
            }

            OnSearchChanged();
        }

        private void ProcessDirectory(string path, int index)
        {
            var files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);

            foreach (string file in files)
            {
                var extension = Path.GetExtension(file);

                if (extension.Equals(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fileName = Path.GetFileName(file);

                AddData(fileName, file);
            }

            var subDirectories = Directory.GetDirectories(path, "*.*", SearchOption.TopDirectoryOnly);

            foreach (string subDir in subDirectories)
            {
                string dirName = Path.GetFileName(subDir);

                if (dirName.Equals("Base", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ProcessDirectory(subDir, index);
            }
        }

        private string m_LastPackageInfo;

        public override void Operation(object obj)
        {
            if (obj is LubanConfigEditorData data)
            {
                var dataPackageName = data.PackageName;
                if (dataPackageName == m_LastPackageInfo)
                {
                    ClearShowInfo();
                    return;
                }

                var packageSettings = YIUILubanTool.LubanEditorData.GetLubanEditorPackageSettings(dataPackageName);
                if (packageSettings != null)
                {
                    PackageName = packageSettings.PackageName;
                    PackagePath = $"{Application.dataPath}/../Packages/cn.etetet.{PackageName}";
                    PackageAlias = packageSettings.Alias;
                    PackageDesc = packageSettings.Desc;
                    m_LastPackageInfo = PackageName;
                }
                else
                {
                    Debug.LogError($"没有找到这个包 {dataPackageName}");
                    ClearShowInfo();
                }
            }
            else
            {
                Debug.LogError($"数据错误");
                ClearShowInfo();
            }
        }

        private void ClearShowInfo()
        {
            PackageName = null;
            PackagePath = null;
            PackageAlias = null;
            PackageDesc = null;
            m_LastPackageInfo = null;
        }
    }
}