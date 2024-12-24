using System;
using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace ET
{
    public class BTShowCastSlider : BTAction
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
        [InlineProperty] // 去掉折叠和标题
        [HideReferenceObjectPicker]
        [LabelText("进度条显示图标")]
        
        public OdinUnityObject Icon = new();
#endif
    }
}