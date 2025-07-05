using Unity.Mathematics;
using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    [System.Serializable]
    [Module(ModuleName.Spell)]
    public class TargetSelectorRectangle : TargetSelector
    {
        [InlineProperty] // 去掉折叠和标题
        [HideReferenceObjectPicker]
        [LabelText("技能指示器")]
        public OdinUnityObject SpellIndicator = new();

        //public float2 Offset; 
        public float Width;
        public float Length;
        
        public UnitType UnitType;
    }
}