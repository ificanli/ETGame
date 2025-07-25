using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using YIUIFramework;
using YIUIFramework.Editor;

namespace YIUILuban.Editor
{
    public class LubanEditorSerializationData
    {
        [ShowInInspector]
        [OdinSerialize]
        [LabelText("根节点显示所有配置")]
        [OnValueChanged("OnRootShowAllConfigChanged")]
        public bool RootShowAllConfig = true; //根节点就显示所有配置 否则降低到子节点 优点: 少跳一次选择 打开即用 缺点: 以后非常多文件后可能会很慢 优化: 可以后根据需求在调整 前期可以先默认显示所有

        [ShowInInspector]
        [OdinSerialize]
        [LabelText("最小尺寸限制")]
        private Vector2 m_MinWindowSize = new(1000, 600); //限制窗口最小 (太小了根本看不到)

        public Vector2 MinWindowSize
        {
            get
            {
                if (m_MinWindowSize.x <= 0 || m_MinWindowSize.y <= 0)
                {
                    m_MinWindowSize = new(1000, 600);
                }

                return m_MinWindowSize;
            }
        }

        [ShowInInspector]
        [OdinSerialize]
        [LabelText("窗口菜单宽度")]
        private float m_MenuWidth = 400;

        public float MenuWidth
        {
            get
            {
                if (m_MenuWidth <= 0)
                {
                    m_MenuWidth = 400;
                }

                return m_MenuWidth;
            }
        }

        [ShowInInspector]
        [OdinSerialize]
        [LabelText("文件排除(正则表达式)")]
        public string InvalidFileNameRegex = @"^[\.\\\\/~#\$%]"; //正则表达式 建议问AI 你提需求让AI写

        [NonSerialized]
        private Regex m_InvalidFileNamePattern;

        public Regex InvalidFileNamePattern
        {
            get
            {
                if (m_InvalidFileNamePattern != null)
                {
                    return m_InvalidFileNamePattern;
                }

                if (string.IsNullOrEmpty(InvalidFileNameRegex))
                {
                    InvalidFileNameRegex = @"^[\.\\\\/~#\$%]";
                }

                m_InvalidFileNamePattern = new Regex(InvalidFileNameRegex);

                return m_InvalidFileNamePattern;
            }
        }

        [ShowInInspector]
        [OdinSerialize]
        [LabelText("有效文件后缀(正则表达式)")]
        public string ValidFileSuffixRegex = @"\.(xlsx|xlsm|xml|json|txt)$";

        [NonSerialized]
        private Regex m_ValidFileSuffixPattern;

        public Regex ValidFileSuffixPattern
        {
            get
            {
                if (m_ValidFileSuffixPattern != null)
                {
                    return m_ValidFileSuffixPattern;
                }

                if (string.IsNullOrEmpty(ValidFileSuffixRegex))
                {
                    ValidFileSuffixRegex = @"\.(xlsx|xlsm|xml|json|txt)$";
                }

                m_ValidFileSuffixPattern = new Regex(ValidFileSuffixRegex, RegexOptions.IgnoreCase);

                return m_ValidFileSuffixPattern;
            }
        }

        private void OnRootShowAllConfigChanged()
        {
            UnityTipsHelper.Show($"修改了根节点显示所有配置, 下一次重新打开窗口生效。");
        }

        [HideInInspector]
        [OdinSerialize]
        private Dictionary<string, LubanPackageEditorData> LubanEditorPackageSettings = new();

        [HideInInspector]
        [OdinSerialize]
        private Dictionary<string, Dictionary<string, LubanConfigEditorData>> LubanEditorConfigSettings = new();

        public const string LubanEditorPackageSettingsPath = "Assets/../ProjectSettings/LubanEditorSerializationDataSettings.txt";

        public LubanPackageEditorData GetLubanEditorPackageSettings(string packageName)
        {
            if (!LubanEditorPackageSettings.TryGetValue(packageName, out var packageData))
            {
                packageData = new LubanPackageEditorData();
                LubanEditorPackageSettings.Add(packageName, packageData);
            }

            packageData.PackageName = packageName;
            return packageData;
        }

        public LubanConfigEditorData GetLubanConfigEditorData(string configName, string path)
        {
            var packageName = UIOperationHelper.GetETPackagesName(path);
            if (string.IsNullOrEmpty(packageName))
            {
                return null;
            }

            if (!LubanEditorConfigSettings.TryGetValue(packageName, out var configData))
            {
                configData = new Dictionary<string, LubanConfigEditorData>();
                LubanEditorConfigSettings.Add(packageName, configData);
            }

            int keywordIndex = path.IndexOf("cn.etetet.", StringComparison.OrdinalIgnoreCase);
            if (keywordIndex < 0)
            {
                Debug.LogError($"路径有问题 {path}");
                return null;
            }

            var keyName = path.Substring(keywordIndex);

            if (!configData.TryGetValue(keyName, out var configEditorData))
            {
                configEditorData = new LubanConfigEditorData();
                configData.Add(keyName, configEditorData);
            }

            configEditorData.Name = configName;
            configEditorData.Path = path;
            configEditorData.PackageName = packageName;
            return configEditorData;
        }

        public static LubanEditorSerializationData LoadData()
        {
            return OdinSerializationUtility.Load<LubanEditorSerializationData>(LubanEditorPackageSettingsPath) ?? new();
        }

        public void SaveData()
        {
            OdinSerializationUtility.Save(this, LubanEditorPackageSettingsPath);
        }
    }
}