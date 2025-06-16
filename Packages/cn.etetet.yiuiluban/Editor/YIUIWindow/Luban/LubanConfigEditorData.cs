using System;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace YIUILuban.Editor
{
    public class LubanConfigEditorData
    {
        [VerticalGroup("操作")]
        [TableColumnWidth(50, Resizable = false)]
        [Button("", 50, Icon = SdfIconType.FileEarmarkSpreadsheet, IconAlignment = IconAlignment.LeftOfText)]
        [PropertyOrder(-999)]
        private void OpenFolder()
        {
            if (string.IsNullOrEmpty(Path))
            {
                return;
            }

            Application.OpenURL(Path);
        }

        //打开文件夹
        private void OpenFile()
        {
            if (string.IsNullOrEmpty(Path))
            {
                return;
            }

            var directoryPath = Directory.GetParent(Path);

            Application.OpenURL(directoryPath.FullName);
        }

        [TableColumnWidth(200, Resizable = false)]
        [VerticalGroup("名称")]
        [ReadOnly]
        [HideLabel]
        public string Name; //文件名

        [VerticalGroup("名称")]
        [HideLabel]
        [InlineButton("OpenFile", SdfIconType.FolderFill, "")]
        public string Alias; //别名

        [HideInInspector]
        public string Path; //路径

        [TextArea(3, 10)]
        [VerticalGroup("描述")]
        [HideLabel]
        public string Desc; //描述

        [VerticalGroup("信息")]
        [TableColumnWidth(50, Resizable = false)]
        [Button("", 25, Icon = SdfIconType.InfoCircle, IconAlignment = IconAlignment.LeftOfText)]
        [ShowIf("ShowType", ELubanEditorShowType.All)]
        private void Operation()
        {
            Module?.Operation(this);
        }

        [VerticalGroup("信息")]
        [TableColumnWidth(50, Resizable = false)]
        [Button("", 25, Icon = SdfIconType.Gear, IconAlignment = IconAlignment.LeftOfText)]
        private void SettingInfo()
        {
        }

        [GUIColor(0.4f, 0.8f, 1)]
        [TableColumnWidth(200, Resizable = false)]
        [VerticalGroup("名称")]
        [ReadOnly]
        [HideLabel]
        [NonSerialized]
        [ShowInInspector]
        [DisplayAsString]
        public string PackageName; //包名

        [NonSerialized]
        [HideInInspector]
        public ELubanEditorShowType ShowType; //显示类型

        [NonSerialized]
        [HideInInspector]
        public LubanConfigEditorModule Module;
    }

    public class LubanPackageEditorData
    {
        public string PackageName; //包名

        public string Alias; //别名

        public string Desc; //描述
    }

    public enum ELubanEditorShowType
    {
        None = 0,
        All = 1,
        Base = 2,
        Defines = 3,
        Datas = 4,
    }
}