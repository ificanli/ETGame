#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ET;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using YIUIFramework;
using YIUIFramework.Editor;
using Object = UnityEngine.Object;

namespace WOW.Editor
{
    public class WOWBuffModule : BaseYIUIToolModule
    {
        [FolderPath]
        [LabelText("资源路径")]
        [ReadOnly]
        [ShowInInspector]
        private string m_AllResPath = "Packages/cn.etetet.wow/Bundles/Skill/Buff";

        [HideInInspector]
        public const string MenuName = "Buff";

        public const string BuffNamePrefix = "Buff_";

        [BoxGroup("创建模块", centerLabel: true)]
        [ShowInInspector]
        internal WOWBuffCreateModule CreateResModule = new();

        private Dictionary<string, List<string>> m_AllInfo = new();

        private int m_AllCount;

        [HideInInspector]
        private Dictionary<int, BuffConfig> BuffConfigDice = new();

        private void RefreshAllPkg()
        {
            EditorHelper.CreateExistsDirectory(m_AllResPath);

            string[] allFiles = Directory.GetFiles(m_AllResPath, "*.txt", SearchOption.AllDirectories);
            m_AllCount = allFiles.Length;
            for (int i = 0; i < allFiles.Length; i++)
            {
                var    path     = allFiles[i].Replace("\\", "/");
                string fileName = path.Split('/').Last().Split('.').First();
                string pattern  = @"\d+";
                var    matches  = Regex.Matches(fileName, pattern);
                var    result   = string.Join("", matches.Cast<Match>().Select(m => m.Value));
                AddMenu($"{MenuName}/{result}", path);
            }
        }

        [GUIColor(0.4f, 0.8f, 1)]
        [Button("全部发布", 50)]
        [PropertyOrder(-99)]
        public void PublishAll()
        {
            if (!UIOperationHelper.CheckUIOperation()) return;

            UnityTipsHelper.CallBack("确定发布全部YIUI?", () =>
            {
                //EditorHelper.DisplayProgressBar("发布", $"正在发布 {pkgName} ...", index, m_AllCount);
                //EditorHelper.ClearProgressBar();
                UnityTipsHelper.CallBackOk("YIUI全部 发布完毕", YIUIAutoTool.CloseWindowRefresh);
            });
        }

        public override void Initialize()
        {
            this.RefreshAllPkg();
            CreateResModule.BuffModule = this;
            CreateResModule?.Initialize();
        }

        public override void OnDestroy()
        {
            CreateResModule?.OnDestroy();
        }

        public bool AddBuffConfig(BuffConfig buffConfig)
        {
            if (this.BuffConfigDice.TryAdd(buffConfig.Id, buffConfig))
            {
                var resPath = $"{m_AllResPath}/{BuffNamePrefix}{buffConfig.Id}.txt";
                AddMenu($"{MenuName}/{buffConfig.Id}", resPath);
                OdinSerializationUtility.Save(buffConfig, resPath);
                AssetDatabase.SaveAssets();
                EditorApplication.ExecuteMenuItem("Assets/Refresh");
                return true;
            }

            return false;
        }

        public void AddMenu(string menuName, string resPath)
        {
            var menu = new TreeMenuItem<WOWBuffConfig>(this.AutoTool, this.Tree, menuName, EditorIcons.Bell);
            menu.UserData = new WOWBuffConfigData
            {
                Path = resPath,
            };
        }
    }
}
#endif