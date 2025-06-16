using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using YIUIFramework.Editor;

namespace YIUILuban.Editor
{
    public abstract class LubanConfigEditorModule : BaseYIUIToolModule
    {
        [ButtonGroup("导出")]
        [Button("Luban 导出", 30, Icon = SdfIconType.Upload, IconAlignment = IconAlignment.LeftOfText)]
        [PropertyOrder(int.MinValue)]
        [GUIColor(0, 1, 0)]
        public void ExcelExporter()
        {
            if (!EditorApplication.ExecuteMenuItem("ET/Excel/ExcelExporter"))
            {
                Debug.LogError("执行失败,请检查菜单是否存在 ET/Excel/ExcelExporter");
            }
            else
            {
                YIUILubanTool.CloseWindowRefresh();
            }
        }

        [Flags]
        private enum ELubanConfigEditorSearchType
        {
            [LabelText("所有")]
            All = -1,

            [LabelText("无")]
            None = 0,

            [LabelText("名称")]
            Name = 1 << 0,

            [LabelText("描述")]
            Description = 1 << 1,
        }

        protected abstract ELubanEditorShowType EditorShowType { get; }

        [BoxGroup("搜索", centerLabel: true)]
        [HideLabel]
        [ShowInInspector]
        [PropertyOrder(-99)]
        [EnumToggleButtons]
        [OnValueChanged(nameof(OnSearchChanged))]
        private ELubanConfigEditorSearchType m_SearchType = ELubanConfigEditorSearchType.Name;

        [BoxGroup("搜索", centerLabel: true)]
        [HideLabel]
        [OnValueChanged(nameof(OnSearchChanged))]
        [Delayed]
        [ShowInInspector]
        [PropertyOrder(-99)]
        private string Search = "";

        protected readonly List<LubanConfigEditorData> m_AllData = new();

        protected void AddData(string fileName, string file)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            if (!YIUILubanTool.LubanEditorData.ValidFileSuffixPattern.IsMatch(fileName))
            {
                return;
            }

            if (YIUILubanTool.LubanEditorData.InvalidFileNamePattern.IsMatch(fileName))
            {
                return;
            }

            var data = YIUILubanTool.LubanEditorData.GetLubanConfigEditorData(fileName, file);

            if (data == null)
            {
                return;
            }

            m_AllData.Add(data);
        }

        [TableList(DrawScrollView = true, IsReadOnly = true, AlwaysExpanded = true)]
        [BoxGroup("所有配置", centerLabel: true)]
        [HideLabel]
        [ShowInInspector]
        private List<LubanConfigEditorData> ShowPackages = new();

        protected void OnSearchChanged()
        {
            if (string.IsNullOrEmpty(Search) || m_SearchType == ELubanConfigEditorSearchType.None)
            {
                ShowPackages.Clear();
                foreach (var data in m_AllData)
                {
                    data.ShowType = EditorShowType;
                    data.Module = this;
                    ShowPackages.Add(data);
                }
            }
            else
            {
                var search = Search?.ToLower() ?? "";
                ShowPackages.Clear();
                foreach (var data in m_AllData)
                {
                    var name = data.Name?.ToLower() ?? "";
                    var alias = data.Alias?.ToLower() ?? "";
                    var desc = data.Desc?.ToLower() ?? "";

                    if (m_SearchType.HasFlag(ELubanConfigEditorSearchType.Name) && (name.Contains(search) || alias.Contains(search)) ||
                        m_SearchType.HasFlag(ELubanConfigEditorSearchType.Description) && desc.Contains(search))
                    {
                        data.ShowType = EditorShowType;
                        data.Module = this;
                        ShowPackages.Add(data);
                    }
                }
            }

            ShowPackages.Sort(DataSort);
        }

        private int DataSort(LubanConfigEditorData x, LubanConfigEditorData y)
        {
            return String.CompareOrdinal(x.Name, y.Name);
        }

        public override void SelectionMenu()
        {
            OnSearchChanged();
        }

        public virtual void Operation(object obj) { }
    }
}