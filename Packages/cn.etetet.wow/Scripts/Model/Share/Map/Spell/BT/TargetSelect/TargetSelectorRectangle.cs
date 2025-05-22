using Unity.Mathematics;
using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    [System.Serializable]
    public class TargetSelectorRectangle : TargetSelector
    {
#if UNITY
        [InlineProperty] // 去掉折叠和标题
        [HideReferenceObjectPicker]
        [LabelText("技能指示器")]
        public OdinUnityObject SpellIndicator = new();
#endif

        public float2 Offset; 
        public float Width;
        public float Length;
    }
}