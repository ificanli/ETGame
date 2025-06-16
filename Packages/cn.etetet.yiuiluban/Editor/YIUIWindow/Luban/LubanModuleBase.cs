using System;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace YIUILuban.Editor
{
    public class LubanModuleBase : LubanConfigEditorModule
    {
        protected override ELubanEditorShowType EditorShowType => ELubanEditorShowType.None;

        [LabelText("模块名")]
        [ReadOnly]
        [PropertyOrder(int.MinValue + 100)]
        public string PackagesName;

        [FolderPath]
        [LabelText("配置路径")]
        [ReadOnly]
        [PropertyOrder(int.MinValue + 100)]
        public string ConfigPath;

        [LabelText("分类")]
        [ReadOnly]
        [PropertyOrder(int.MinValue + 100)]
        [ShowIf(nameof(ShowIfFolderName))]
        public string FolderName;

        private bool ShowIfFolderName()
        {
            return !string.IsNullOrEmpty(FolderName);
        }

        [LabelText("别名")]
        [ReadOnly]
        [PropertyOrder(int.MinValue + 100)]
        public string PackageAlias;

        [LabelText("描述")]
        [ReadOnly]
        [PropertyOrder(int.MinValue + 100)]
        public string PackageDesc;

        protected LubanConfigModuleData Data;

        public override void Initialize()
        {
            base.Initialize();
            if (this.UserData is LubanConfigModuleData data)
            {
                Data = data;
                PackagesName = Data.LubanConfigModule.PackagesName;
                ConfigPath = Data.LubanConfigModule.ConfigPath;
                FolderName = Data.LubanConfigModule.FolderName;
                PackageAlias = Data.LubanConfigModule.PackageAlias;
                PackageDesc = Data.LubanConfigModule.PackageDesc;
            }
            else
            {
                Debug.LogError($"数据错误");
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected virtual void GetAllData(string path)
        {
            m_AllData.Clear();

            if (!Directory.Exists(path))
            {
                return;
            }

            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                string extension = Path.GetExtension(file);

                if (extension.Equals(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fileName = Path.GetFileName(file);

                AddData(fileName, file);
            }

            OnSearchChanged();
        }
    }
}