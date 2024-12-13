using System;
using ET;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using YIUIFramework;
using YIUIFramework.Editor;

namespace WOW.Editor
{
    [HideReferenceObjectPicker]
    [HideLabel]
    public class WOWBuffCreateModule : BaseCreateModule
    {
        [HideInInspector]
        public WOWBuffModule BuffModule;

        [LabelText("ID")]
        public int BuffId;

        [Title("Buff 配置", TitleAlignment = TitleAlignments.Centered)]
        [NonSerialized, OdinSerialize]
        [HideLabel]
        [HideReferenceObjectPicker]
        public BuffConfig BuffConfig = new();

        [GUIColor(0, 1, 0)]
        [Button("添加", 30)]
        private void Create()
        {
            if (BuffConfig.Id == 0)
            {
                UnityTipsHelper.Show("BuffId 不能为0");
                return;
            }

            if (!UIOperationHelper.CheckUIOperation()) return;
            BuffModule.AddBuffConfig(BuffConfig);
            BuffConfig = new();
        }

        public override void Initialize()
        {
        }

        public override void OnDestroy()
        {
        }
    }
}