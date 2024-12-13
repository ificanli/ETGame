using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ET;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine;
using YIUIFramework;
using YIUIFramework.Editor;
using Logger = UnityEngine.Logger;

namespace WOW.Editor
{
    public class WOWBuffConfigData
    {
        public string Path;
        public string Name;
    }

    public class WOWBuffConfig : BaseYIUIToolModule
    {
        [FolderPath]
        [LabelText("Buff 资源路径")]
        [ReadOnly]
        [ShowInInspector]
        public string ResPath;

        [Title("Buff 配置", TitleAlignment = TitleAlignments.Centered)]
        [NonSerialized, OdinSerialize]
        [HideLabel]
        [HideReferenceObjectPicker]
        public BuffConfig BuffConfig;

        public override void Initialize()
        {
            base.Initialize();
            if (this.UserData is WOWBuffConfigData data)
            {
                ResPath = data.Path;
                var buffConfig = OdinSerializationUtility.Load<BuffConfig>(data.Path);
                if (buffConfig == null)
                {
                    Debug.LogError($"数据为空 {data.Path}");
                }
                else
                {
                    BuffConfig = buffConfig;
                }
            }
            else
            {
                Debug.LogError($"数据错误");
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (BuffConfig == null || string.IsNullOrEmpty(ResPath))
            {
                return;
            }

            OdinSerializationUtility.Save(BuffConfig, ResPath);
        }
    }
}