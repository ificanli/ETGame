using System;
using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    [System.Serializable]
    [Module(ModuleName.Spell)]
    public class TargetSelectorPosition : TargetSelector
    {
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Pos = "Pos";

        public int Radius;
        
        [InlineProperty] // 去掉折叠和标题
        [HideReferenceObjectPicker]
        [LabelText("技能指示器")]
        public OdinUnityObject SpellIndicator = new();
    }
}