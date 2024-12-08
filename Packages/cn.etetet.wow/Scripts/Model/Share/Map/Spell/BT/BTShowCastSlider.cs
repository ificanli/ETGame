using System;
using Sirenix.OdinInspector;
#if UNITY
using UnityEditor;
using UnityEngine;
#endif

namespace ET
{
    [LabelText("显示施法进度条")]
    [HideReferenceObjectPicker]
    public class BTShowCastSlider : BTNode
    {
        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        [ReadOnly]
        [ShowIf("@ShowInInspector.Value")]
        public string Buff = BTEvnKey.Buff;

        [LabelText("是否增加施法进度条")]
        [InfoBox("如: 寒冰箭读条就是增长，暴风雪引导就是减少")]
        public bool IsIncrease = true;

#if UNITY
        [BoxGroup("显示信息", CenterLabel = true)]
        [LabelText("进度条显示名称")]
        public string ShowDisplayName;

        [BoxGroup("显示信息")]
        [LabelText("显示图标名称")]
        [ReadOnly]
        [ShowIf("ShowIcon")]
        public string IconName;

        [BoxGroup("显示信息")]
        [HideLabel]
        [ShowInInspector, PreviewField(45, ObjectFieldAlignment.Left)]
        [OnValueChanged("OnIconValueChanged")]
        [NonSerialized]
        private Sprite Icon;

        [NonSerialized]
        private string LastIconName;

        private void OnIconValueChanged()
        {
            if (Icon == null)
            {
                IconName = "";
                return;
            }

            IconName = Icon.name;
        }

        private bool ShowIcon()
        {
            if (string.IsNullOrEmpty(IconName))
            {
                Icon         = null;
                LastIconName = "";
                return true;
            }

            if (IconName == LastIconName)
            {
                return true;
            }

            LastIconName = IconName;

#if UNITY_EDITOR
            foreach (string guid in AssetDatabase.FindAssets($"{IconName} t:Sprite", null))
            {
                var path   = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    Debug.LogError($"找不到图标资源: {IconName}");
                }
                else
                {
                    Icon = sprite;
                    break;
                }
            }
#endif
            return true;
        }

#endif
    }
}