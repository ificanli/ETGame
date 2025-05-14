using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    [System.Serializable]
    public class TargetSelectorCircle : TargetSelector
    {
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Unit = "Unit";
        
        
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(List<long>))]
        public string Units = "Units";
        
        public int Radius;
        
        public UnitType UnitType;
        
#if UNITY
        [InlineProperty] // 去掉折叠和标题
        [HideReferenceObjectPicker]
        [LabelText("技能指示器")]
        public OdinUnityObject SpellIndicator = new();
#endif
    }
}